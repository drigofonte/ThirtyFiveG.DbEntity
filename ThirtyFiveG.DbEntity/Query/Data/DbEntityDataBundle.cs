using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using ThirtyFiveG.Commons.Extensions;
using ThirtyFiveG.DbEntity.Entity;
using ThirtyFiveG.DbEntity.Common;

namespace ThirtyFiveG.DbEntity.Query.Data
{
    public class DbEntityDataBundle : DataBundle, IDbEntityDataBundle
    {
        #region Private variables
        public const string EntitiesJsonParameterKey = "entities.json";
        public const string EntityJsonParameterKey = "entity.json";
        public const string EntityParameterKey = "entity";
        public const string EntitiesParameterKey = "entities";
        #endregion

        #region Constructor
        public DbEntityDataBundle() { }
        #endregion

        #region Public properties
        public int UserID { get; set; }
        public Type Type { get; set; }
        public int Action { get; set; }
        #endregion

        #region Private methods
        private void SerializeEntities()
        {
            Type t = GetGenericListType(Type);
            IEnumerable entities = GetParameter(t, EntitiesParameterKey) as IEnumerable;
            if (entities != null)
            {
                string json = "[";
                foreach (IDbEntity entity in entities)
                    json += SerializeEntity(entity) + ",";
                json = (json.EndsWith(",") ? json.Substring(0, json.Length - 1) : json) + "]";
                AddParameter(EntitiesJsonParameterKey, json);
                Parameters.Remove(t);
            }
        }

        private void SerializeEntity()
        {
            IDbEntity entity = GetParameter(Type, EntityParameterKey) as IDbEntity;
            if (entity != null)
            {
                AddParameter(EntityJsonParameterKey, SerializeEntity(entity));
                Parameters.Remove(Type);
            }
        }

        private void DeserializeEntities()
        {
            if (Contains<string>(EntitiesJsonParameterKey))
            {
                string json = GetParameter<string>(EntitiesJsonParameterKey);
                Type t = GetGenericListType(Type);
                var entities = t.DeserializeObject(json);
                AddParameter(entities.GetType(), EntitiesParameterKey, entities);
                Parameters[typeof(string)].Remove(EntitiesJsonParameterKey);
            }
        }

        private void DeserializeEntity()
        {
            if (Contains<string>(EntityJsonParameterKey))
            {
                string json = GetParameter<string>(EntityJsonParameterKey);
                var entity = Type.DeserializeObject(json);
                AddParameter(Type, EntityParameterKey, entity);
                Parameters[typeof(string)].Remove(EntityJsonParameterKey);
            }
        }
        #endregion

        #region Private static methods
        private static string SerializeEntity(IDbEntity entity)
        {
            string json = DbEntityJsonConvert.SerializePrimaryKeys(entity);
            if (entity.HasChanges)
                json = DbEntityJsonConvert.SerializeEntity(entity, entity.DbEntityChanges());
            return json;
        }

        private static Type GetGenericListType(Type type)
        {
            return typeof(List<>).MakeGenericType(type);
        }
        #endregion

        #region Public methods
        public virtual IEnumerable<Tuple<string, object>[]> GetPrimaryKeys()
        {
            MethodInfo method = typeof(DbEntityDataBundle).GetMethod("GetEntities");
            MethodInfo generic = method.MakeGenericMethod(Type);
            IEnumerable entities = generic.Invoke(this, new object[] { }) as IEnumerable;
            return entities.Cast<IDbEntity>().Select(e => e.PrimaryKeys);
        }

        public virtual List<TEntity> GetEntities<TEntity>()
            where TEntity : class, IDbEntity
        {
            if (!typeof(TEntity).Equals(Type))
                throw new ArgumentException("The generic type must match the type of the data bundle.");

            List<TEntity> entities = new List<TEntity>();
            if (Contains<List<TEntity>>(EntitiesParameterKey))
            {
                entities = GetParameter<List<TEntity>>(EntitiesParameterKey);
            }

            if (entities.Count == 0)
            {
                TEntity entity = GetEntity<TEntity>();
                if (entity != null)
                    entities.Add(entity);
            }
            return entities;
        }

        public virtual TEntity GetEntity<TEntity>()
            where TEntity : class, IDbEntity
        {
            TEntity entity = null;
            if (Contains<TEntity>(EntityParameterKey))
                entity = GetParameter(Type, EntityParameterKey) as TEntity;
            return entity;
        }

        [OnSerializing]
        public void OnSerializing(StreamingContext context)
        {
            SerializeEntities();
            SerializeEntity();
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context)
        {
            DeserializeEntities();
            DeserializeEntity();
            Parameters.Remove(typeof(string));
        }
        #endregion

        #region Public static methods
        public static DbEntityDataBundle GetInstance<TEntity>(int userId, int action, TEntity entity)
            where TEntity : class, IDbEntity
        {
            DbEntityDataBundle dataBundle = new DbEntityDataBundle();
            dataBundle.Type = entity.GetType().GetBaseType();
            dataBundle.Action = action;
            dataBundle.UserID = userId;
            dataBundle.AddParameter(dataBundle.Type, EntityParameterKey, entity);
            return dataBundle;
        }

        public static DbEntityDataBundle GetInstance<TEntity>(int userId, int action, List<TEntity> entities)
            where TEntity : class, IDbEntity
        {
            DbEntityDataBundle dataBundle = new DbEntityDataBundle();
            dataBundle.Type = typeof(TEntity).GetBaseType();
            if (entities.Count > 0)
                dataBundle.Type = entities.First().GetType();
            dataBundle.Action = action;
            dataBundle.UserID = userId;
            dataBundle.AddParameter(EntitiesParameterKey, entities);
            return dataBundle;
        }
        #endregion
    }
}