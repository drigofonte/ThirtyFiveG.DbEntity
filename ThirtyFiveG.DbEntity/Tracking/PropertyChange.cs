using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ThirtyFiveG.DbEntity.Common;
using ThirtyFiveG.DbEntity.Entity;
using ThirtyFiveG.DbEntity.Extensions;

namespace ThirtyFiveG.DbEntity.Tracking
{
    public class PropertyChange
    {
        #region Private variables
        private string _entityPath;
        #endregion

        #region Constructor
        public PropertyChange(string entityPath, string propertyName, string entityGuid, object before, object after, bool isDbEntityEnumerable, EntityState state)
        {
            EntityPath = entityPath;
            PropertyName = propertyName;
            EntityGuid = entityGuid;
            Before = before;
            After = after;
            IsDbEntityEnumerable = isDbEntityEnumerable;
            UtcTimestamp = DateTime.UtcNow.Ticks;
            State = state;
        }

        public PropertyChange(string entityPath, string propertyName, string entityGuid, object before, object after, EntityState state) : this(entityPath, propertyName, entityGuid, before, after, false, state) { }

        public PropertyChange(string entityPath, string propertyName, string entityGuid, object before, object after) : this(entityPath, propertyName, entityGuid, before, after, true, EntityState.None) { }
        #endregion

        #region Public properties
        public string EntityPath
        {
            get { return _entityPath; }
            set
            {
                _entityPath = value;
                if (_entityPath != null && _entityPath.EndsWith(".") && _entityPath.Length > 1)
                    _entityPath = _entityPath.Substring(0, _entityPath.Length - 1);
            }
        }
        public string EntityGuid { get; private set; }
        public string PropertyName { get; private set; }
        public object Before { get; set; }
        public object After { get; set; }
        public long UtcTimestamp { get; private set; }
        public bool IsDbEntityEnumerable { get; private set; }
        [JsonIgnore]
        public string PropertyPath { get { return EntityPath.Length == 1 ? "." + PropertyName : EntityPath + "." + PropertyName; } }
        public EntityState State { get; private set; }
        public bool IsGrouped { get { return !string.IsNullOrEmpty(GroupGuid); } }
        public string GroupGuid { get; set; }
        #endregion

        #region Public methods
        public void Revert(IDbEntity entity)
        {
            if (!IsDbEntityEnumerable)
                Set(entity, Before);
            else
            {
                RevertCollectionChange(entity);
            }
        }

        public void Apply(IDbEntity entity)
        {
            if (!IsDbEntityEnumerable)
                Set(entity, After);
            else
            {
                ApplyCollectionChange(entity);
            }
        }

        public string DbEntityPropertyPath(IDbEntity entity)
        {
            return DbEntityUtilities.GetDbEntityPropertyPath(PropertyPath, entity);
        }

        public bool IsOrphan(IDbEntity entity)
        {
            IDbEntity matchedEntity = DbEntityDataBinder.Eval(EntityPath, entity).Entity;
            return matchedEntity == null || !matchedEntity.Guid.Equals(EntityGuid);
        }

        public void Destroy()
        {
            EntityPath = null;
            PropertyName = null;
            Before = null;
            After = null;
        }
        #endregion

        #region Private methods
        private void Set(IDbEntity entity, object value)
        {
            IDbEntity matchedEntity = DbEntityDataBinder.Eval(EntityPath, entity).Entity;
            PropertyInfo property = matchedEntity.GetType().GetProperty(PropertyName);
            property.SetValue(matchedEntity, value, null);
        }

        private IEnumerable<IDbEntity> GetCollection(IDbEntity entity)
        {
            return (entity.Eval(EntityPath.Substring(1, EntityPath.LastIndexOf("[") - 1)) as IEnumerable).Cast<IDbEntity>();
        }

        private void ApplyCollectionChange(IDbEntity entity)
        {
            IEnumerable<IDbEntity> entities = GetCollection(entity);
            MethodInfo generic = typeof(PropertyChange)
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Single(x => x.Name == "ApplyCollectionChange" && x.GetParameters().Length == 1 && x.GetGenericArguments().Count() == 1)
                .MakeGenericMethod(entities.GetType().GetGenericArguments()[0]);
            generic.Invoke(this, new object[] { entities });
        }

        private void ApplyCollectionChange<T>(IEnumerable entities)
            where T : class, IDbEntity
        {
            // If remove (i.e. before = typeof(IDbEntity), after = null), remove before value
            ICollection<T> collection = entities as ICollection<T>;
            if (Before == null && After != null && DbEntityUtilities.IsIDbEntity(After.GetType()) && !collection.Contains(After as T))
                // This property change refers to a newly added entity to this collection. To re-apply it, re-add the added entity to the collection
                collection.Add(After as T);
            else if (Before != null && DbEntityUtilities.IsIDbEntity(Before.GetType()) && After == null)
                // This property change refers to an entity removed from this collection. To re-apply it, remove the removed entity from the collection
                collection.Remove(Before as T);
        }

        private void RevertCollectionChange(IDbEntity entity)
        {
            IEnumerable<IDbEntity> entities = GetCollection(entity);
            MethodInfo generic = typeof(PropertyChange)
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Single(x => x.Name == "RevertCollectionChange" && x.GetParameters().Length == 1 && x.GetGenericArguments().Count() == 1)
                .MakeGenericMethod(entities.GetType().GetGenericArguments()[0]);
            generic.Invoke(this, new object[] { entities });
        }

        private void RevertCollectionChange<T>(IEnumerable entities)
            where T : class, IDbEntity
        {
            ICollection<T> collection = entities as ICollection<T>;
            if (Before == null && After != null && DbEntityUtilities.IsIDbEntity(After.GetType()))
                // This property change refers to a newly added entity to this collection. To revert it, remove the added entity from the collection
                collection.Remove(After as T);
            else if (Before != null && DbEntityUtilities.IsIDbEntity(Before.GetType()) && After == null && !collection.Contains(Before as T))
                // This property change refers to an entity removed from this collection. To revert it, re-add the removed entity to the collection
                collection.Add(Before as T);
        }
        #endregion
    }
}
