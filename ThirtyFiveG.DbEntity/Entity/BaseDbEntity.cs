using Newtonsoft.Json;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ThirtyFiveG.Commons.Event;
using ThirtyFiveG.DbEntity.Event;
using ThirtyFiveG.DbEntity.Common;
using ThirtyFiveG.DbEntity.Query;
using ThirtyFiveG.DbEntity.Tracking;
using ThirtyFiveG.DbEntity.Validation;
using ThirtyFiveG.DbEntity.Projection.Entity;
using ThirtyFiveG.Validation;

namespace ThirtyFiveG.DbEntity.Entity
{
    public abstract class BaseDbEntity : IDbEntity
    {
        #region Private variables
        private Tuple<string, object>[] _primaryKeys;
        private PropertyChangeTracker _tracker;
        private long _beginEditTicks;
        private bool _isDisposing;
        private bool _isTracked;
        #endregion

        #region Public events
        public event EventHandler DbEntityDeleted;
        public event EventHandler<DbEntityPropertyChangedEventArgs> DbEntityPropertyChanged;
        public event EventHandler DbEntityUndeleted;
        public event EventHandler DbEntityValidated;
        public event EventHandler Editing;
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler Pushing;
        public event EventHandler<DataEventArgs<IEnumerable<string>>> Pushed;
        public event EventHandler Undo;
        public event EventHandler<DataEventArgs<IEnumerable<IValidationResult>>> Validated;
        public event EventHandler Focus;
        public event EventHandler Blur;
        #endregion

        #region Constructor
        public BaseDbEntity() : this(System.Guid.NewGuid().ToString()) { }

        public BaseDbEntity(string guid)
        {
            BeforePushActions = new HashSet<Action<IDbEntity>>();
            Guid = guid;
            IsEditing = false;
            IsEditable = true;
            State = EntityState.New;
            ValidationResults = new HashSet<IValidationResult>();
            //ValidationRules = new HashSet<IValidationRule>();
            _isDisposing = false;
        }
        #endregion

        #region Public properties
        [JsonIgnore]
        public ISet<Action<IDbEntity>> BeforePushActions { get; private set; }
        [JsonIgnore]
        public virtual bool CanPush { get { return _tracker != null; } }
        [JsonIgnore]
        public double EditDuration { get { return Math.Round(IsEditing ? TimeSpan.FromTicks(DateTime.UtcNow.Ticks - _beginEditTicks).TotalMilliseconds : 0); } }
        public IEnumerable<DbEntityActionMessage> EntityActions { get; set; }
        public string Guid { get; set; }
        [JsonIgnore]
        public virtual bool HasChanges { get { return IsTracked && _tracker != null && !IsPushing && PropertyChanges(_tracker).Count() > 0; } }
        [JsonIgnore]
        public bool IsEditable { get; set; }
        [JsonIgnore]
        public bool IsEditing { get; set; }
        [JsonIgnore]
        public bool IsNew { get { return State == EntityState.New; } }
        [JsonIgnore]
        public bool IsPersisted { get { return State == EntityState.Persisted; } }
        [JsonIgnore]
        public bool IsPushing { get; private set; }
        [DoNotNotify]
        [JsonIgnore]
        public bool IsTrackingChanges { get; set; }
        [DoNotNotify]
        [JsonIgnore]
        public bool IsTracked
        {
            get { return _isTracked; }
            set
            {
                _isTracked = value;
                //if (_isTracked)
                //    foreach (IValidationRule rule in GetValidationRules())
                //        ValidationRules.Add(rule);
                //else
                //    ValidationRules.Clear();
            }
        }
        [JsonIgnore]
        public Tuple<string, object>[] PrimaryKeys
        {
            get
            {
                List<Tuple<string, object>> keys = new List<Tuple<string, object>>();
                PropertyInfo key;
                foreach (string name in PrimaryKeysNames)
                {
                    key = this.GetType().GetProperty(name);
                    if (key == default(PropertyInfo))
                        throw new ArgumentNullException("No primary key property with name '" + name + "' was found for type '" + this.GetType().Name + "'.");
                    object value = key.GetGetMethod().Invoke(this, null);
                    // TODO: We should consider converting all the ints to longs as the JSON deserialisers we are using always return ints as longs!
                    //if (key.GetGetMethod().ReturnType.Equals(typeof (int)))
                    //    value = Convert.ChangeType(value, typeof (long), null);
                    keys.Add(new Tuple<string, object>(name, value));
                }

                // Account for any other key that might have been attached to this object temporarily
                if (_primaryKeys != default(Tuple<string, object>[]))
                {
                    List<string> keysNames = keys.Select(k => k.Item1).ToList();
                    foreach (Tuple<string, object> primaryKey in _primaryKeys)
                    {
                        if (!keysNames.Contains(primaryKey.Item1))
                        {
                            keys.Add(primaryKey);
                        }
                    }
                }

                _primaryKeys = keys.ToArray();
                return _primaryKeys;
            }

            set
            {
                PropertyInfo keyProperty;
                foreach (Tuple<string, object> key in value)
                {
                    keyProperty = this.GetType().GetProperty(key.Item1);
                    if (keyProperty != default(PropertyInfo))
                    {
                        // Ignore any key that does not have a matching property
                        keyProperty.SetValue(this, Convert.ChangeType(key.Item2, keyProperty.PropertyType, null), null);
                    }
                }

                _primaryKeys = value;
            }
        }
        [JsonIgnore]
        protected virtual string[] PrimaryKeysNames { get { return new string[] { "id" }; } }
        public EntityState State { get; private set; }
        [JsonIgnore]
        public IPropertyChangeTracker Tracker { get { return _tracker; } }
        [JsonIgnore]
        public ISet<IValidationResult> ValidationResults { get; private set; }
        //[JsonIgnore]
        //public ISet<IValidationRule> ValidationRules { get; protected set; }
        #endregion

        #region Private methods
        private void InvokeBeforePushActions()
        {
            foreach (Action<IDbEntity> a in BeforePushActions)
                a(this);
        }

        private void BeginEdit(Type projection)
        {
            if (!IsEditable)
                throw new ArgumentException("This entity is not editable.");

            IsEditing = true;
            if (!IsTracked)
            {
                _tracker = new PropertyChangeTracker(this, projection);
                _tracker.Validated += RaiseValidated;
                _tracker.Start();
                Subscribe(_tracker);
                _beginEditTicks = DateTime.UtcNow.Ticks;
                OnEditing();
            }
        }

        private void ClearTracker()
        {
            if (_tracker != null)
            {
                Unsubscribe(_tracker);
                _tracker.Validated -= RaiseValidated;
                _tracker.Stop();
                _tracker.Dispose();
                _tracker = null;
            }
        }

        private void RaiseValidated(object sender, DataEventArgs<IEnumerable<IValidationResult>> e)
        {
            OnValidated(e.Data);
        }

        private void RaiseDbEntityPropertyChanged(string propertyName, object before, object after)
        {
            DbEntityPropertyChanged?.Invoke(this, new DbEntityPropertyChangedEventArgs(propertyName, before, after));
        }

        private void RaiseDbEntityDeleted()
        {
            DbEntityDeleted?.Invoke(this, EventArgs.Empty);
        }

        private void RaiseDbEntityUndeleted()
        {
            DbEntityUndeleted?.Invoke(this, EventArgs.Empty);
        }

        private void RaiseDbEntityValidated()
        {
            DbEntityValidated?.Invoke(this, EventArgs.Empty);
        }

        private void RaisePushing()
        {
            Pushing?.Invoke(this, EventArgs.Empty);
        }

        private void RaisePushed(IEnumerable<string> changes)
        {
            Pushed?.Invoke(this, new DataEventArgs<IEnumerable<string>>(changes));
        }

        private void Subscribe(PropertyChangeTracker tracker)
        {
            _tracker.ChangesAdded += OnChangesAdded;
            _tracker.ChangeRemoved += OnChangesRemoved;
            _tracker.ChangeRedone += OnChangeRedone;
            _tracker.ChangeUndone += OnChangeUndone;
        }

        private void Unsubscribe(PropertyChangeTracker tracker)
        {
            _tracker.ChangesAdded -= OnChangesAdded;
            _tracker.ChangeRemoved -= OnChangesRemoved;
            _tracker.ChangeRedone -= OnChangeRedone;
            _tracker.ChangeUndone -= OnChangeUndone;
        }

        private void OnChangesRemoved(object sender, EventArgs e)
        {
            RaisePropertyChanged("HasChanges");
        }

        private void OnChangesAdded(object sender, EventArgs e)
        {
            RaisePropertyChanged("HasChanges");
        }

        private void OnChangeUndone(object sender, EventArgs e)
        {
            RaisePropertyChanged("HasChanges");
        }

        private void OnChangeRedone(object sender, EventArgs e)
        {
            RaisePropertyChanged("HasChanges");
        }
        #endregion

        #region Public methods
        public virtual void BeginEdit()
        {
            BeginEdit(GetType());
        }

        public virtual void BeginEdit<TProjection>()
            where TProjection : class, IDbEntityProjection
        {
            BeginEdit(typeof(TProjection));
        }

        public virtual string ChangesAsJson(IPropertyChangeTracker tracker, long utcTimestamp = long.MaxValue)
        {
            return tracker.DbEntityAsJson(this, utcTimestamp);
        }

        public virtual string ChangesAsJson()
        {
            if (_tracker == null)
                throw new ArgumentNullException("You cannot get the changes of an entity without first starting to edit it.");
            return ChangesAsJson(_tracker);
        }

        public virtual IEnumerable<string> DbEntityChanges(IPropertyChangeTracker tracker, long utcTimestamp = long.MaxValue)
        {
            return tracker.DbEntityChanges(true, this, utcTimestamp);
        }

        public virtual IEnumerable<string> DbEntityChanges()
        {
            if (_tracker == null)
                throw new ArgumentNullException("You cannot get the changes of an entity without first starting to edit it.");
            return DbEntityChanges(_tracker);
        }

        public void DiscardEdit()
        {
            if (!IsEditing)
                throw new ArgumentNullException("You cannot cancel the editing of an entity without first starting it.");

            UndoAllEdit();
            IsEditing = false;
            ClearTracker();
            _beginEditTicks = 0;
        }

        public virtual void EndEdit()
        {
            if (!IsEditing)
                throw new ArgumentNullException("You cannot end the editing of an entity without first starting it.");

            IsEditing = false;
            ClearTracker();
            _beginEditTicks = 0;
        }

        public virtual string Grouped(Action<IDbEntity> a)
        {
            if (!IsEditing)
                throw new ArgumentException("This entity is not being edited.");

            string changesGuid = string.Empty;
            if (_tracker != null)
                changesGuid = _tracker.Grouped(a);
            else
                a(this);

            return changesGuid;
        }

        public void MarkPersisted()
        {
            State = EntityState.Persisted;
        }

        public void MarkNew()
        {
            State = EntityState.New;
        }

        public virtual IEnumerable<PropertyChange> PropertyChanges(IPropertyChangeTracker tracker)
        {
            return tracker.Changes(this);
        }

        public virtual IEnumerable<PropertyChange> PropertyChanges()
        {
            if (_tracker == null)
                throw new ArgumentNullException("You cannot get the changes of an entity without first starting to edit it.");
            return PropertyChanges(_tracker);
        }

        public virtual void PurgeChanges(IPropertyChangeTracker tracker, long utcTimestamp = long.MaxValue)
        {
            tracker.PurgeChanges(this, utcTimestamp);
        }

        public virtual void PurgeChanges()
        {
            if (_tracker == null)
                throw new ArgumentNullException("You cannot purge the changes of an entity without first starting to edit it.");
            PurgeChanges(_tracker);
        }

        public virtual async Task<IEnumerable<string>> PushAsync(IDataAccessLayer dal, int action, int commentId)
        {
            if (_tracker == null)
                throw new ArgumentNullException("You cannot push the changes of an entity without first starting to edit it.");
            return await PushAsync(_tracker, dal, action, commentId);
        }

        public virtual async Task<IEnumerable<string>> PushAsync(IPropertyChangeTracker tracker, IDataAccessLayer dal, int action, int commentId)
        {
            InvokeBeforePushActions();
            long utcTimestamp = DateTime.UtcNow.Ticks;
            IsPushing = true;
            RaisePushing();
            IEnumerable<string> changes = DbEntityChanges(tracker, utcTimestamp);
            try
            {
                int editDuration = (int)Math.Round(EditDuration);
                IDictionary<string, Tuple<string, object>[]> newEntitiesPrimaryKeys = await dal.UpdateEntity(GetType(), ChangesAsJson(tracker, utcTimestamp), changes, action, editDuration, commentId);
                tracker.Untracked((e) =>
                {
                    DbEntityUtilities.UpdatePrimaryKeys(newEntitiesPrimaryKeys, this);
                });
                // TODO: Only clear the changes if the push has been successful
                PurgeChanges(tracker, utcTimestamp);
                RaisePushed(changes);
            }
            catch(Exception) { changes = Enumerable.Empty<string>(); }
            finally { IsPushing = false; }

            return changes;
        }

        public virtual void UndoEdit(PropertyChange lastChange, bool inclusive = true)
        {
            if (!IsEditing)
                throw new ArgumentNullException("You cannot cancel the editing of an entity without first starting it.");

            if (_tracker != null)
            {
                _tracker.Undo(lastChange, inclusive);
                OnUndo();
            }
        }

        public virtual void UndoEdit()
        {
            if (!IsEditing)
                throw new ArgumentNullException("You cannot cancel the editing of an entity without first starting it.");

            if (_tracker != null)
            {
                _tracker.Undo();
                OnUndo();
            }
        }

        public virtual void UndoAllEdit()
        {
            if (!IsEditing)
                throw new ArgumentNullException("You cannot cancel the editing of an entity without first starting it.");

            if (_tracker != null)
            {
                _tracker.UndoAll();
                OnUndo();
            }
        }

        public virtual bool Untracked(Action<IDbEntity> a)
        {
            bool untracked = true;
            if (_tracker != null)
                untracked = _tracker.Untracked(a);
            else
                a(this);

            return untracked;
        }

        public void Validate(string propertyPath)
        {
            if (_tracker == null)
                throw new ArgumentNullException("You cannot validate an entity without first starting to edit it.");
            _tracker.Validate(propertyPath);
        }

        public void Validate(IEnumerable<string> propertyPaths)
        {
            if (_tracker == null)
                throw new ArgumentNullException("You cannot validate an entity without first starting to edit it.");
            _tracker.Validate(propertyPaths);
        }

        public void AddValidationRulesFilters(ISet<string> filters)
        {
            if (_tracker == null)
                throw new ArgumentNullException("You cannot add validation rules filters to an entity without first starting to edit it.");
            _tracker.AddValidationRulesFilters(filters);
        }

        public void ClearValidationRulesFilters()
        {
            if (_tracker == null)
                throw new ArgumentNullException("You cannot clear the validation rules filters of an entity without first starting to edit it.");
            _tracker.ClearValidationRulesFilters();
        }

        public void OnPropertyChanged(string propertyName, object before, object after)
        {
            RaisePropertyChanged(propertyName);
            RaiseDbEntityPropertyChanged(propertyName, before, after);
            if (propertyName.Equals("IsDeleted") && after != null && after.GetType().Equals(typeof(bool)))
            {
                bool isDeleted = (bool)after;
                bool wasDeleted = !isDeleted && before != null && (bool)before;
                if (isDeleted)
                    RaiseDbEntityDeleted();
                else if (wasDeleted)
                    RaiseDbEntityUndeleted();
            }
        }

        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void OnEditing()
        {
            Editing?.Invoke(this, EventArgs.Empty);
        }

        public void OnUndo()
        {
            Undo?.Invoke(this, EventArgs.Empty);
        }

        public void OnValidated(IEnumerable<IValidationResult> results)
        {
            IEnumerable<IValidationResult> reValidatedResults = _tracker.Validate(ValidationResults);
            ValidationResults.Clear();
            ValidationResults.UnionWith(reValidatedResults);
            ValidationResults.UnionWith(results);
            Validated?.Invoke(this, new DataEventArgs<IEnumerable<IValidationResult>>(ValidationResults));
        }

        public virtual void OnFocus()
        {
            Focus?.Invoke(this, EventArgs.Empty);
        }

        public virtual void OnBlur()
        {
            Blur?.Invoke(this, EventArgs.Empty);
        }

        public virtual bool Matches(string query)
        {
            return true;
        }

        public void Dispose()
        {
            if (!_isDisposing)
            {
                _isDisposing = true;
                EntityActions = null;
                ClearTracker();
                BeforePushActions.Clear();
                ValidationResults.Clear();
                //ValidationRules.Clear();
                _isDisposing = false;
            }
        }
        #endregion

        #region Virtual methods
        public virtual IEnumerable<IValidationRule> GetValidationRules() { return Enumerable.Empty<IValidationRule>(); }
        #endregion
    }
}
