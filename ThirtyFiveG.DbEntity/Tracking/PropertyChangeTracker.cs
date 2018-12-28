using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ThirtyFiveG.Commons.Collections;
using ThirtyFiveG.Commons.Event;
using ThirtyFiveG.Commons.Extensions;
using ThirtyFiveG.DbEntity.Common;
using ThirtyFiveG.DbEntity.Entity;
using ThirtyFiveG.DbEntity.Event;
using ThirtyFiveG.DbEntity.Validation;
using ThirtyFiveG.Validation;

namespace ThirtyFiveG.DbEntity.Tracking
{
    public class PropertyChangeTracker : IPropertyChangeTracker, IPropertyValidator, IDisposable
    {
        #region Private variables
        private IDbEntity _entity;
        private Type _projection;
        private List<PropertyChange> _changes;
        private PropertyChange _lastChange;
        private IDictionary<Type, ISet<string>> _validEntitiesProperties;
        private IDictionary<string, EventHandler<DbEntityPropertyChangedEventArgs>> _entityUpdateHandlers;
        private IDictionary<string, NotifyCollectionChangedEventHandler> _entityCollectionChangedHandlers;
        private IDictionary<string, EventHandler<DataEventArgs<IEnumerable>>> _entityCollectionAddedHandlers;
        private IDictionary<string, EventHandler<DataEventArgs<IEnumerable>>> _entityCollectionRemovedHandlers;
        private IDictionary<string, ISet<string>> _entitiesPaths;
        private Type _observableCollectionType;
        private object[] _emptyObjectArray;
        private bool _isCreatingHandlers;
        private ISet<Regex> _validationRulesFilters;
        private bool _isDisposing;

        private PropertyInfo _isTracked;
        private Action<IDbEntity, string> _removeHandlersBeforeRecursionAction;
        private Func<IDbEntity, IEnumerable<IDbEntity>, string, string, PropertyInfo, IEnumerable<IDbEntity>> _removeHandlersCollectionPropertyAction;
        private Action<IDbEntity, string> _createHandlersAfterRecursionAction;
        private Func<IDbEntity, IEnumerable<IDbEntity>, string, string, PropertyInfo, IEnumerable<IDbEntity>> _createHandlersBeforeCollectionRecursionAction;
        private Action<IDbEntity, IEnumerable<IDbEntity>, string, string, PropertyInfo> _createHandlersAfterCollectionRecursionAction;
        #endregion

        #region Public events
        public event EventHandler ChangesAdded;
        public event EventHandler ChangeRemoved;
        public event EventHandler ChangeUndone;
        public event EventHandler ChangeRedone;
        public event EventHandler<DataEventArgs<IEnumerable<IValidationResult>>> Validated;
        #endregion

        #region Constructor
        public PropertyChangeTracker(IDbEntity entity) : this(entity, entity.GetType()) { }

        public PropertyChangeTracker(IDbEntity entity, Type projection)
        {
            _entity = entity;
            _projection = projection;
            _changes = new List<PropertyChange>();
            _validEntitiesProperties = new Dictionary<Type, ISet<string>>();
            _entityUpdateHandlers = new Dictionary<string, EventHandler<DbEntityPropertyChangedEventArgs>>();
            _entityCollectionChangedHandlers = new Dictionary<string, NotifyCollectionChangedEventHandler>();
            _entityCollectionAddedHandlers = new Dictionary<string, EventHandler<DataEventArgs<IEnumerable>>>();
            _entityCollectionRemovedHandlers = new Dictionary<string, EventHandler<DataEventArgs<IEnumerable>>>();
            _entitiesPaths = new Dictionary<string, ISet<string>>();
            _emptyObjectArray = new object[0];
            _observableCollectionType = typeof(ObservableRangeCollection<>);
            _isTracked = typeof(IDbEntity).GetProperty("IsTracked");
            _validationRulesFilters = new HashSet<Regex>();
            ValidationRules = new Dictionary<string, ICollection<IValidationRule>>();

            InitialiseHandlerActions();
        }
        #endregion

        #region Public properties
        public IDictionary<string, ICollection<IValidationRule>> ValidationRules { get; private set; }
        #endregion

        #region Public methods
        public void Undo()
        {
            if (_lastChange != null)
                Undo(_lastChange);
        }

        public void Undo(int index)
        {
            Undo(_changes[index]);
        }

        public void Undo(PropertyChange lastChange, bool inclusive = true)
        {
            if (_changes.Count == 0)
                throw new ArgumentException("Cannot undo before any changes are tracked.");

            Untracked((e) =>
            {
                PropertyChange originalLastChange = lastChange;
                string groupGuid = lastChange.GroupGuid;
                lastChange = Undo(e, lastChange);
                while (lastChange != null && lastChange.IsGrouped && lastChange.GroupGuid.Equals(groupGuid))
                {
                    lastChange = Undo(e, lastChange);
                }

                if (!inclusive)
                    AddChange(originalLastChange);
            });

            RaiseChangeUndone();
        }

        public void UndoAll()
        {
            for (int i = 0; i < _changes.Count; i++)
                Undo();
        }

        public void Redo()
        {
            if (_changes.Count == 0)
                throw new ArgumentException("Cannot redo before any changes are tracked.");
            if (_changes.Count > 0 && _lastChange != null && _lastChange.Equals(_changes.Last()))
                throw new ArgumentException("Cannot redo before any changes are undone.");

            Redo(_changes[_changes.IndexOf(_lastChange) + 1]);
        }

        public void RedoAll()
        {
            int nextChangeIndex = _changes.IndexOf(_lastChange) + 1;
            for (; nextChangeIndex < _changes.Count; nextChangeIndex++)
            {
                Redo();
            }
        }

        public string DbEntityAsJson(IDbEntity root = null, long utcTimestamp = long.MaxValue)
        {
            IEnumerable<string> changes = DbEntityChanges(false, root, utcTimestamp);
            return DbEntityJsonConvert.SerializeEntity(root == null ? _entity : root, changes);
        }

        public void Start()
        {
            Stop();
            CreateHandlers();
        }

        public void Stop()
        {
            if (_entityCollectionChangedHandlers.Count > 0 || _entityUpdateHandlers.Count > 0)
            {
                RemoveHandlers();
                _entityUpdateHandlers.Clear();
                _entityCollectionChangedHandlers.Clear();
            }
            _entitiesPaths.Clear<string, string>();
        }

        public bool Untracked(Action<IDbEntity> a)
        {
            bool invoked = false;
            try
            {
                _entity.IsTrackingChanges = false;
                a.Invoke(_entity);
                invoked = true;
            }
            finally
            {
                _entity.IsTrackingChanges = true;
            }
            return invoked;
        }

        public string Grouped(Action<IDbEntity> a)
        {
            string guid = Guid.NewGuid().ToString();
            int lastChangeIndex = 0;
            if (_lastChange != null)
                lastChangeIndex = _changes.IndexOf(_lastChange) + 1;

            a.Invoke(_entity);
            IEnumerable<PropertyChange> groupedChanges = _changes.Where(c => _changes.IndexOf(c) >= lastChangeIndex).ToList();

            foreach (PropertyChange change in groupedChanges)
                change.GroupGuid = guid;

            return guid;
        }

        public IEnumerable<PropertyChange> AllChanges()
        {
            return _changes;
        }

        public IEnumerable<PropertyChange> Changes(IDbEntity root = null)
        {
            ISet<string> changePathsPrefixes = GetChangePathsPrefixes(root);
            return _changes.ToList().Where(c => _changes.IndexOf(c) <= _changes.IndexOf(_lastChange) && !c.IsOrphan(_entity) && changePathsPrefixes.Any(p => c.EntityPath.StartsWith(p)));
        }

        public IEnumerable<string> DbEntityChanges(bool flattenNewEntityChanges = false, IDbEntity root = null, long utcTimestamp = long.MaxValue)
        {
            ISet<string> changePathsPrefixes = GetChangePathsPrefixes(root);
            IEnumerable<PropertyChange> dbEntityChanges = ChangesForNonDeletedEntities(changePathsPrefixes, utcTimestamp)
                .Where(c => _changes.IndexOf(c) <= _changes.IndexOf(_lastChange));

            IEnumerable<PropertyChange> persistedDbEntityChanges = dbEntityChanges
                .Where(c => c.State == EntityState.Persisted || (c.IsDbEntityEnumerable && c.State == EntityState.None));

            IEnumerable<PropertyChange> newDbEntityChanges = dbEntityChanges
                .Where(c => c.State == EntityState.New
                            && changePathsPrefixes.Any(p => c.EntityPath.StartsWith(p)));
            ISet<string> changes = GetPropertyPaths(newDbEntityChanges, root);

            if (flattenNewEntityChanges)
            {
                foreach (PropertyChange change in newDbEntityChanges.OrderBy(c => c.PropertyPath.Split('.').Length))
                {
                    string path = change.EntityPath;
                    if (path.Contains("["))
                        path = path.Substring(0, path.LastIndexOf("["));

                    ICollection<string> subChanges = changes.Where(s => s.StartsWith(path) && !s.Equals(path)).ToList();
                    foreach (string subChange in subChanges)
                        changes.Remove(subChange);
                    subChanges.Clear();

                    if (changePathsPrefixes.Any(p => path.StartsWith(p)) && !changes.Any(s => !s.Equals(".") && path.StartsWith(s)))
                        changes.Add(path);
                }
            }

            changes.UnionWith(GetPropertyPaths(persistedDbEntityChanges, root));
            changes.Remove(".");

            return changes;
        }

        public void PurgeChanges(IDbEntity root = null, long utcTimestamp = long.MaxValue)
        {
            ICollection<PropertyChange> changesToDelete = ChangesForNonDeletedEntities(GetChangePathsPrefixes(root), utcTimestamp);
            foreach (PropertyChange change in changesToDelete)
                RemoveChange(change);
            _lastChange = _changes.LastOrDefault();
            changesToDelete.Clear();
        }

        public void Validate(PropertyChange change, bool isIDbEntity)
        {
            DateTime start = DateTime.UtcNow;
            //string propertyPath = change.PropertyPath;
            //if (!change.IsDbEntityEnumerable || change.After != null)
            //    propertyPath = change.DbEntityPropertyPath(_entity);

            //string propertyPathPrefix = propertyPath;
            //if (!isIDbEntity)
            //    propertyPathPrefix = propertyPath.Substring(0, propertyPath.LastIndexOf("."));

            //if (propertyPathPrefix.Contains("["))
            //    propertyPathPrefix = propertyPathPrefix.Substring(0, propertyPathPrefix.LastIndexOf("["));

            //if (string.IsNullOrEmpty(propertyPathPrefix))
            //    propertyPathPrefix = ".";

            IDictionary<string, ICollection<IValidationRule>> allRules = ValidationRules.Where(r => _validationRulesFilters.Any(p => p.Match(r.Key).Success)).ToDictionary(p => p.Key, p => p.Value);
            //if (allRules.Count == 0)
            //    allRules = ValidationRules.Where(p => p.Key.StartsWith(propertyPathPrefix)).ToDictionary(p => p.Key, p => p.Value);

            ICollection<IValidationResult> results = GetValidationResults(allRules);

            RaiseValidated(results);
        }

        public void Validate(string propertyPath)
        {
            ICollection<KeyValuePair<string, ICollection<IValidationRule>>> rules = ValidationRules
                .Where(p => ValidationUtilities.Matches(propertyPath, p.Key))
                .ToList();
            ICollection<IValidationResult> results = GetValidationResults(rules);
            rules.Clear();
            RaiseValidated(results);
        }

        public void Validate(IEnumerable<string> propertyPaths)
        {
            ICollection<KeyValuePair<string, ICollection<IValidationRule>>> rules = ValidationRules
                .Where(r => propertyPaths.Any(p => ValidationUtilities.Matches(p, r.Key)))
                .ToList();
            ICollection<IValidationResult> results = GetValidationResults(rules);
            rules.Clear();
            RaiseValidated(results);
        }

        public IEnumerable<IValidationResult> Validate(IEnumerable<IValidationResult> validationResults)
        {
            ICollection<KeyValuePair<string, ICollection<IValidationRule>>> rules = ValidationRules
                .Where(r => validationResults.Any(vr => vr.PropertyPath.Equals(r.Key)))
                .ToList();
            IEnumerable<IValidationResult> results = GetValidationResults(rules);
            rules.Clear();
            return results;
        }

        public void AddValidationRulesFilters(ISet<string> filters)
        {
            foreach(string filter in filters)
                _validationRulesFilters.Add(new Regex(filter));
        }

        public void ClearValidationRulesFilters()
        {
            _validationRulesFilters.Clear();
        }

        public bool IsDeleted(string entityPath, out IDbEntity e)
        {
            string[] split = entityPath.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            e = _entity;
            bool isDeleted = (_entity as IDbEntityBasic).IsDeleted;
            string path = string.Empty;
            for (int i = 0; i < split.Length && !isDeleted; i++)
            {
                path += "." + split[i];
                IDbEntity entity = DbEntityDataBinder.Eval(path, _entity).Entity;
                if (entity != null)
                {
                    e = entity;
                    isDeleted = (entity as IDbEntityBasic).IsDeleted;
                }
            }

            return isDeleted;
        }

        public void Dispose()
        {
            if (!_isDisposing)
            {
                _isDisposing = true;
                _entity = null;
                _projection = null;
                _changes.Clear();
                _lastChange = null;
                _validEntitiesProperties.Clear<Type, string>();
                _entityUpdateHandlers.Clear();
                _entityCollectionChangedHandlers.Clear();
                _entityCollectionAddedHandlers.Clear();
                _entityCollectionRemovedHandlers.Clear();
                _entitiesPaths.Clear<string, string>();
                _observableCollectionType = null;
                _emptyObjectArray = null;
                _validationRulesFilters.Clear();
                _isTracked = null;
                _removeHandlersBeforeRecursionAction = null;
                _removeHandlersCollectionPropertyAction = null;
                _createHandlersAfterRecursionAction = null;
                _createHandlersBeforeCollectionRecursionAction = null;
                _createHandlersAfterCollectionRecursionAction = null;
                _isDisposing = false;
            }
        }
        #endregion

        #region Private methods
        private ICollection<IValidationResult> GetValidationResults(IEnumerable<KeyValuePair<string, ICollection<IValidationRule>>> indexedRules)
        {
            IDbEntity entity;
            ICollection<IValidationResult> results = new List<IValidationResult>();
            foreach (KeyValuePair<string, ICollection<IValidationRule>> propertyRules in indexedRules)
            {
                if (!IsDeleted(propertyRules.Key, out entity))
                {
                    foreach (IValidationRule rule in propertyRules.Value)
                        if (rule.Matches(entity))
                            results.Add(rule.AsResult(propertyRules.Key));
                }
            }
            return results;
        }

        private ISet<string> GetPropertyPaths(IEnumerable<PropertyChange> changes, IDbEntity root = null)
        {
            ISet<string> propertyPaths = new HashSet<string>();
            ISet<string> changePathsPrefixes = GetChangePathsPrefixes(root);

            foreach (string propertyPath in changes.Select(c => c.DbEntityPropertyPath(_entity)))
            {
                string prefix = changePathsPrefixes.Single(p => propertyPath.StartsWith(p));
                string path = propertyPath.Substring(prefix.Length > 1 ? prefix.Length : 0).Trim();
                if (!string.IsNullOrEmpty(path))
                    propertyPaths.Add(path);
            }

            return propertyPaths;
        }

        private void InitialiseHandlerActions()
        {
            _removeHandlersBeforeRecursionAction = (entity, path) =>
            {
                EventHandler<DbEntityPropertyChangedEventArgs> entityPropertyChanged;
                if (_entityUpdateHandlers.TryGetValue(path, out entityPropertyChanged))
                {
                    entity.DbEntityPropertyChanged -= entityPropertyChanged;
                    entity.IsTrackingChanges = false;
                    _entityUpdateHandlers.Remove(path);
                    ISet<string> entityPaths;
                    if (_entitiesPaths.TryGetValue(entity.Guid, out entityPaths))
                        entityPaths.Remove(path);
                    RemoveValidationRules(path, entity.GetValidationRules());
                }
                SetIsTracked(entity, false);
            };

            _removeHandlersCollectionPropertyAction = (entity, entities, path, propertyPath, property) =>
            {
                return RemoveCollectionHandlers(entity, entities, path, propertyPath, property);
            };

            _createHandlersAfterRecursionAction = (entity, path) =>
            {
                EventHandler<DbEntityPropertyChangedEventArgs> entityPropertyChanged = delegate (object s, DbEntityPropertyChangedEventArgs e) { EntityPropertyChanged(s, e, path); };
                entity.DbEntityPropertyChanged += entityPropertyChanged;
                entity.IsTrackingChanges = true;
                _entityUpdateHandlers.Add(path, entityPropertyChanged);
                string noLeadingDotPath = path.EndsWith(".") ? path.Substring(0, path.Length - 1) : path;
                if (!string.IsNullOrEmpty(noLeadingDotPath))
                    _entitiesPaths.Add(entity.Guid, noLeadingDotPath);
                SetIsTracked(entity, true);
                IndexValidationRules(path, entity.GetValidationRules());
            };

            _createHandlersBeforeCollectionRecursionAction = (entity, entities, path, propertyPath, property) =>
            {
                // Changing the original set for an observable collection enables the tracker to monitor the addition or removal of items
                var originalCollection = entities;
                if (!originalCollection.GetType().GetGenericTypeDefinition().Equals(_observableCollectionType))
                {
                    var observableCollectionType = _observableCollectionType.MakeGenericType(property.GetGetMethod().ReturnType.GetGenericArguments()[0]);
                    var observableCollection = (IList)Activator.CreateInstance(observableCollectionType);
                    foreach (IDbEntity e in entities)
                        observableCollection.Add(e);
                    originalCollection = observableCollection as IEnumerable<IDbEntity>;
                }
                return originalCollection;
            };
            
            _createHandlersAfterCollectionRecursionAction = (entity, entities, path, propertyPath, property) =>
            {
                // Changing the original set for an observable collection enables the tracker to monitor the addition or removal of items
                var originalCollection = property.GetValue(entity, null);
                if (originalCollection == null || !originalCollection.Equals(entities))
                    property.SetValue(entity, entities, null);

                NotifyCollectionChangedEventHandler entityCollectionChanged = delegate (object s, NotifyCollectionChangedEventArgs e) { EntityCollectionChanged(s, e, path, property.Name, entity); };
                (entities as INotifyCollectionChanged).CollectionChanged -= entityCollectionChanged;
                (entities as INotifyCollectionChanged).CollectionChanged += entityCollectionChanged;
                _entityCollectionChangedHandlers.Add(propertyPath, entityCollectionChanged);

                EventHandler<DataEventArgs<IEnumerable>> entityCollectionAdded = delegate (object s, DataEventArgs<IEnumerable> added) { EntityCollectionAdded(s, added, path, property.Name, entity); };
                (entities as IObservableRangeCollection).Added -= entityCollectionAdded;
                (entities as IObservableRangeCollection).Added += entityCollectionAdded;
                _entityCollectionAddedHandlers.Add(propertyPath, entityCollectionAdded);

                EventHandler<DataEventArgs<IEnumerable>> entityCollectionRemoved = delegate (object s, DataEventArgs<IEnumerable> removed) { EntityCollectionRemoved(s, removed, path, property.Name, entity); };
                (entities as IObservableRangeCollection).Removed -= entityCollectionRemoved;
                (entities as IObservableRangeCollection).Removed += entityCollectionRemoved;
                _entityCollectionRemovedHandlers.Add(propertyPath, entityCollectionRemoved);
            };
        }

        private IEnumerable<IDbEntity> RemoveCollectionHandlers(IDbEntity entity, IEnumerable<IDbEntity> entities, string path, string propertyPath, PropertyInfo property)
        {

            // Unsubscribe collection changed listeners
            if (entities.GetType().IsINotifyCollectionChanged())
            {
                NotifyCollectionChangedEventHandler entityCollectionChanged;
                if (_entityCollectionChangedHandlers.TryGetValue(propertyPath, out entityCollectionChanged))
                {
                    (entities as INotifyCollectionChanged).CollectionChanged -= entityCollectionChanged;
                    _entityCollectionChangedHandlers.Remove(propertyPath);
                }
            }

            if (entities.GetType().IsIObservableRangeCollection())
            {
                IObservableRangeCollection c = entities as IObservableRangeCollection;

                EventHandler<DataEventArgs<IEnumerable>> entityCollectionAdded;
                if (_entityCollectionAddedHandlers.TryGetValue(propertyPath, out entityCollectionAdded))
                {
                    c.Added -= entityCollectionAdded;
                    _entityCollectionAddedHandlers.Remove(propertyPath);
                }

                EventHandler<DataEventArgs<IEnumerable>> entityCollectionRemoved;
                if (_entityCollectionRemovedHandlers.TryGetValue(propertyPath, out entityCollectionRemoved))
                {
                    c.Removed -= entityCollectionRemoved;
                    _entityCollectionRemovedHandlers.Remove(propertyPath);
                }
            }
            return entities;
        }

        private void RefreshHandlers()
        {
            // Stop listening to property changes
            RemoveHandlers();
            // Start listening to property changes
            CreateHandlers();
        }

        private void CreateHandlers()
        {
            CreateHandlers(_entity, ".");
        }

        private void CreateHandlers(IDbEntity entity, string path)
        {
            _isCreatingHandlers = true;
            try
            {
                Type projection = _projection.Eval(path);
                DbEntityRecursion.DepthFirst(entity, projection, path, (e, p) => { }, (e, entityPath, propertyPath, prop) => { }, _createHandlersBeforeCollectionRecursionAction, _createHandlersAfterCollectionRecursionAction, _createHandlersAfterRecursionAction);
            }
            catch (ArgumentNullException) { /** This is thrown in cases where the projection does not cover the path specified **/ }
            finally
            {
                _isCreatingHandlers = false;
            }
        }

        private void RemoveHandlers()
        {
            RemoveHandlers(_entity, ".");
        }

        private void RemoveHandlers(IDbEntity entity, string path)
        {
            Type projection = _projection.Eval(path);
            DbEntityRecursion.DepthFirst(entity, projection, path, _removeHandlersBeforeRecursionAction, (e, p, propertyPath, prop) => { }, _removeHandlersCollectionPropertyAction, (e, es, p, propertyPath, prop) => { }, (e, p) => { });
        }

        private void ProcessCollectionChange(NotifyCollectionChangedAction action, IEnumerable items, string path, string propertyName, IDbEntity parentEntity)
        {
            if (!_isCreatingHandlers && IsValid(parentEntity.GetType(), path, propertyName))
            {
                if (action == NotifyCollectionChangedAction.Add)
                {
                    ICollection<PropertyChange> changes = new List<PropertyChange>();
                    foreach (IDbEntity navigationEntity in items.Cast<IDbEntity>())
                    {
                        string entityPath = DbEntityUtilities.GenerateCollectionItemPath(path + propertyName, navigationEntity.Guid);
                        PropertyChange change = new PropertyChange(entityPath, string.Empty, navigationEntity.Guid, null, navigationEntity, true, navigationEntity.State);
                        if (_entity.IsTrackingChanges)
                        {
                            changes.Add(change);
                            MergeChanges(navigationEntity, entityPath, !_entity.IsTrackingChanges);
                        }
                        CreateHandlers(navigationEntity, entityPath);
                    }

                    if (_entity.IsTrackingChanges)
                        AddChanges(changes);
                    Validate(null, true);
                }
                else if (action == NotifyCollectionChangedAction.Remove)
                {
                    ICollection<PropertyChange> changes = new List<PropertyChange>();
                    string propertyPath = DbEntityUtilities.GeneratePropertyPath(path, propertyName);
                    foreach (IDbEntity removedEntity in items.Cast<IDbEntity>())
                    {
                        string entityPath = DbEntityUtilities.GenerateCollectionItemPath(propertyPath, removedEntity.Guid);
                        PropertyChange change = new PropertyChange(entityPath, string.Empty, removedEntity.Guid, removedEntity, null, true, removedEntity.State);
                        if (_entity.IsTrackingChanges)
                            changes.Add(change);
                        RemoveHandlers(removedEntity, entityPath);
                        Validate(change, true);
                    }

                    if (_entity.IsTrackingChanges)
                        AddChanges(changes);
                    changes.Clear();
                }
            }
        }

        private void EntityCollectionAdded(object sender, DataEventArgs<IEnumerable> added, string path, string propertyName, IDbEntity parentEntity)
        {
            ProcessCollectionChange(NotifyCollectionChangedAction.Add, added.Data, path, propertyName, parentEntity);
        }

        private void EntityCollectionRemoved(object sender, DataEventArgs<IEnumerable> removed, string path, string propertyName, IDbEntity parentEntity)
        {
            ProcessCollectionChange(NotifyCollectionChangedAction.Remove, removed.Data, path, propertyName, parentEntity);
        }

        private void EntityCollectionChanged(object sender, NotifyCollectionChangedEventArgs e, string path, string propertyName, IDbEntity parentEntity)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
                ProcessCollectionChange(e.Action, e.NewItems, path, propertyName, parentEntity);
            else if (e.Action == NotifyCollectionChangedAction.Remove)
                ProcessCollectionChange(e.Action, e.OldItems, path, propertyName, parentEntity);
        }

        private void EntityPropertyChanged(object sender, DbEntityPropertyChangedEventArgs e, string path)
        {
            IDbEntity entity = sender as IDbEntity;
            PropertyChange change = null;

            // Save a reference to the property changed
            if (!_isCreatingHandlers && IsValid(sender.GetType(), path, e.PropertyName) && (!e.PropertyName.Equals("IsDeleted") || entity.State == EntityState.Persisted))
            {
                string propertyPath = path + e.PropertyName;
                PropertyInfo p = entity.GetType().GetProperty(e.PropertyName);
                bool isEnumerable = p.GetGetMethod().ReturnType.IsIEnumerable();
                bool isDbEntityEnumerable = false;
                bool isIDbEntity = DbEntityUtilities.IsIDbEntity(p);
                if (isIDbEntity)
                {
                    propertyPath = propertyPath + ".";
                    if (e.Before != null)
                    {
                        RemoveHandlers(e.Before as IDbEntity, propertyPath);
                    }                    

                    if (e.After != null)
                    {
                        // Defer any changes tracking that might have been recorded on the navigation entity to the root entity
                        IDbEntity navigationEntity = e.After as IDbEntity;
                        if (_entity.IsTrackingChanges)
                            MergeChanges(navigationEntity, propertyPath, !_entity.IsTrackingChanges);
                        CreateHandlers(navigationEntity, propertyPath);
                    }
                }
                else if ((isEnumerable && p.GetGetMethod().ReturnType.GetGenericArguments().Count() > 0 && DbEntityUtilities.IsIDbEntity(p.GetGetMethod().ReturnType.GetGenericArguments()[0])))
                {
                    isDbEntityEnumerable = true;
                    if (e.Before != null)
                    {
                        RemoveCollectionHandlers(entity, e.Before as IEnumerable<IDbEntity>, path, propertyPath + ".", p);
                    }

                    if (e.After != null)
                    {
                        foreach (IDbEntity navigationEntity in e.After as IEnumerable<IDbEntity>)
                        {
                            if (navigationEntity != null)
                            {
                                string entityPath = DbEntityUtilities.GenerateCollectionItemPath(propertyPath, navigationEntity.Guid);
                                if (_entity.IsTrackingChanges)
                                {
                                    MergeChanges(navigationEntity, entityPath, !_entity.IsTrackingChanges);
                                }
                                CreateHandlers(navigationEntity, entityPath);
                            }
                        }
                    }
                }

                if (_entity.IsTrackingChanges)
                {
                    change = new PropertyChange(path, e.PropertyName, entity.Guid, e.Before, e.After, entity.State);
                    if (isDbEntityEnumerable)
                        change = new PropertyChange(path, e.PropertyName, entity.Guid, e.Before, e.After);

                    PurgeLast(p.GetGetMethod().ReturnType, change);
                    AddChange(change);
                    Validate(change, isIDbEntity);
                }

                //_logTo.DebugQueue(new EntityPropertyChangedLogEntry(SettingsModel.Current.User.id, entity.PrimaryKeys, entity.GetType(), propertyPath));
            }
        }

        private bool PurgeLast(Type propertyReturnType, PropertyChange change)
        {
            bool purged = false;
            PropertyChange lastChange = _changes.LastOrDefault();
            /*
             * Tries to reduce the number of changes recorded when string properties are being updated on every user key stroke. In these cases, if the last change was on the same property and the length difference between both is only one character, the last change is purged.
             */
            if (_changes.Count > 0
                && propertyReturnType.Equals(typeof(string))
                && lastChange.PropertyPath.Equals(change.PropertyPath)
                && change.After != null
                && lastChange.After != null
                && Math.Abs((lastChange.After as string).Length - (change.After as string).Length) == 1)
            {
                RemoveChange(lastChange);
                change.Before = lastChange.Before;
                purged = true;
            }
            return purged;
        }

        private bool IsValid(Type t, string path, string propertyName)
        {
            ISet<string> propertyNames;
            if (!_validEntitiesProperties.TryGetValue(t, out propertyNames))
            {
                propertyNames = InitialiseValidEntityProperties(_projection.Eval(path));
            }
            return propertyNames.Contains(propertyName);
        }

        private ISet<string> InitialiseValidEntityProperties(Type t)
        {
            _validEntitiesProperties.UnionWith(t, t.GetBaseProperties(true, true).Select(p => p.Name));
            _validEntitiesProperties.UnionWith(t, t.GetVirtualProperties(true, true).Select(p => p.Name));
            return _validEntitiesProperties[t];
        }

        private ICollection<PropertyChange> ChangesForNonDeletedEntities(ISet<string> changePathsPrefixes, long utcTimestamp = long.MaxValue)
        { 
            // Get all changes for entities that are not deleted
            List<PropertyChange> dbEntityChanges = new List<PropertyChange>(
                _changes
                    .Where(c => c.UtcTimestamp <= utcTimestamp)
                    .Where(c => !c.IsOrphan(_entity))
                    .Where(c =>
                    {
                        bool returnChange = false;
                        if (!string.IsNullOrEmpty(c.EntityPath))
                        {
                            bool startsWithPathPrefix = changePathsPrefixes.Any(p => c.EntityPath.StartsWith(p));
                            IDbEntity entity = DbEntityDataBinder.Eval(c.EntityPath, _entity).Entity;
                            returnChange = startsWithPathPrefix && (!(entity as IDbEntityBasic).IsDeleted || entity.State == EntityState.Persisted);
                        }
                        return returnChange;
                    }));
            // Get all flat property changes
            dbEntityChanges.AddRange(
                _changes
                    .Where(c => string.IsNullOrEmpty(c.EntityPath)));

            return dbEntityChanges;
        }

        private void SetIsTracked(IDbEntity entity, bool isTracked)
        {
            _isTracked.SetValue(entity, isTracked, null);
        }

        private void MergeChanges(IDbEntity entity, string entityPath, bool raiseEvent)
        {
            if (entity.HasChanges)
            {
                ICollection<PropertyChange> changes = entity.PropertyChanges().ToList();
                foreach (PropertyChange change in changes)
                    change.EntityPath = entityPath + change.EntityPath.Substring(1);
                AddChanges(changes, raiseEvent);
                changes.Clear();
            }

            if (entity.IsEditing && !entity.Equals(_entity))
                entity.EndEdit();
        }

        private PropertyChange Undo(IDbEntity e, PropertyChange lastChange)
        {
            if (!lastChange.IsOrphan(e)
                || (lastChange.IsDbEntityEnumerable && lastChange.After == null && lastChange.Before != null))
                lastChange.Revert(e);
            return MoveLastChange(lastChange);
        }

        private PropertyChange MoveLastChange(PropertyChange lastChange)
        {
            if (lastChange.Equals(_changes.First()))
                _lastChange = null;
            else
                _lastChange = _changes[_changes.IndexOf(lastChange) - 1];
            return _lastChange;
        }

        private void Redo(PropertyChange lastChange)
        {
            Untracked((e) =>
            {
                string groupGuid = lastChange.GroupGuid;
                lastChange = Redo(e, lastChange);
                while (lastChange != null && lastChange.IsGrouped && lastChange.GroupGuid.Equals(groupGuid))
                {
                    lastChange = Redo(e, lastChange);
                }
            });

            RaiseChangeRedone();
        }

        private PropertyChange Redo(IDbEntity e, PropertyChange lastChange)
        {
            if (!lastChange.IsOrphan(e)
                || (lastChange.IsDbEntityEnumerable && lastChange.After != null && lastChange.Before == null))
                lastChange.Apply(e);
            _lastChange = lastChange;
            PropertyChange nextChange = null;
            if (!_lastChange.Equals(_changes.Last()))
                nextChange = _changes[_changes.IndexOf(_lastChange) + 1];
            return nextChange;
        }

        private void AddChanges(IEnumerable<PropertyChange> changes, bool raiseEvent = true)
        {
            if (_lastChange != null && !_lastChange.Equals(_changes.Last()))
            {
                int lastChangeIndex = _changes.IndexOf(_lastChange) + 1;
                _changes.RemoveRange(lastChangeIndex, _changes.Count - lastChangeIndex);
            }
            else if (_lastChange == null && _changes.Count > 0)
                _changes.Clear();

            foreach(PropertyChange change in changes)
            {
                _lastChange = change;
                _changes.Add(_lastChange);
            }
            
            if (raiseEvent)
                RaiseChangesAdded();
        }

        private void AddChange(PropertyChange change)
        {
            AddChanges(new PropertyChange[] { change });
        }

        private void RemoveChange(PropertyChange change)
        {
            if (_changes.Count > 1 && _lastChange.Equals(_changes.Last()))
                _lastChange = _changes.ElementAt(_changes.Count - 2);
            else if (_changes.Count == 1)
                _lastChange = null;

            _changes.Remove(change);
            RaiseChangeRemoved();
        }

        private ISet<string> GetChangePathsPrefixes(IDbEntity root)
        {
            ISet<string> changePathsPrefixes = new HashSet<string>() { "." };
            if (root != null)
            {
                ISet<string> entitiesPaths;
                if (_entitiesPaths.TryGetValue(root.Guid, out entitiesPaths))
                {
                    changePathsPrefixes.Clear();
                    foreach (string prefix in entitiesPaths)
                    {
                        changePathsPrefixes.Add(DbEntityUtilities.GetDbEntityPropertyPath(prefix, _entity));
                        changePathsPrefixes.Add(prefix);
                    }
                }
            }
            return changePathsPrefixes;
        }

        private void IndexValidationRules(string entityPath, IEnumerable<IValidationRule> validationRules)
        {
            foreach (IValidationRule rule in validationRules)
            {
                string propertyPath = entityPath + rule.PropertyName;
                ValidationRules.Add(propertyPath, rule);
            }
        }

        private void RemoveValidationRules(string path, IEnumerable<IValidationRule> validationRules)
        {
            string propertyPath;
            foreach(IValidationRule rule in validationRules)
            {
                propertyPath = rule.GetPropertyPath(path);
                ICollection<IValidationRule> rules;
                if (ValidationRules.TryGetValue(propertyPath, out rules))
                {
                    rules.Remove(rule);
                    if (rules.Count == 0)
                        ValidationRules.Remove(propertyPath);
                }
            }
        }

        private void RaiseChangesAdded()
        {
            ChangesAdded?.Invoke(this, EventArgs.Empty);
        }

        private void RaiseChangeRemoved()
        {
            ChangeRemoved?.Invoke(this, EventArgs.Empty);
        }

        private void RaiseChangeUndone()
        {
            ChangeUndone?.Invoke(this, EventArgs.Empty);
        }

        private void RaiseChangeRedone()
        {
            ChangeRedone?.Invoke(this, EventArgs.Empty);
        }

        private void RaiseValidated(IEnumerable<IValidationResult> validationResults)
        {
            Validated?.Invoke(this, new DataEventArgs<IEnumerable<IValidationResult>>(validationResults));
        }
        #endregion
    }
}