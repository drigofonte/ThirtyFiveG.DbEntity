using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using ThirtyFiveG.DbEntity.Entity;
using ThirtyFiveG.DbEntity.Projection.Entity;
using ThirtyFiveG.DbEntity.Query;
using ThirtyFiveG.DbEntity.Query.Generic;

namespace ThirtyFiveG.DbEntity.Common
{
    public static class DbEntityRepository
    {
        private static readonly Action<IDbEntity, string> _markPersisted = (e, p) => { e.MarkPersisted(); };

        public static async Task<T> PullAsync<T, P>(IDataAccessLayer dal, Tuple<string, object>[] primaryKeys)
            where T : class, IDbEntity
            where P : class, IDbEntityProjection
        {
            long start = DateTime.UtcNow.Ticks;
            T entity = await dal.GetEntity<T, P>(primaryKeys);
            long end = DateTime.UtcNow.Ticks;
            Debug.WriteLine("Retrieved " + typeof(T).Name + " in " + ((end - start) / 10000) + "ms.");
            start = DateTime.UtcNow.Ticks;
            MarkPersisted(entity, typeof(P));
            end = DateTime.UtcNow.Ticks;
            Debug.WriteLine("Traversed " + typeof(T).Name + " in " + ((end - start) / 10000) + "ms.");
            return entity;
        }

        public static async Task<IEnumerable<T>> PullAsync<T, P>(IDataAccessLayer dal, IEnumerable<Tuple<string, object>[]> primaryKeys)
            where T : class, IDbEntity
            where P : class, IDbEntityProjection
        {
            IEnumerable<T> entities = await dal.GetMultipleEntities<T>(primaryKeys, typeof(P));
            MarkPersisted(entities, typeof(P));
            return entities;
        }

        public static async Task<IEnumerable<T>> PullAsync<T>(IDataAccessLayer dal, QueryBundle<T> query)
            where T : class, IDbEntity
        {
            IEnumerable<T> entities = await dal.GetEntities(query);
            MarkPersisted(entities, query.Projection.GetType());
            return entities;
        }

        public static async Task<IEnumerable<T>> PullAsync<T>(IDataAccessLayer dal, QueryBundle query)
            where T : class, IDbEntity
        {
            IEnumerable<T> entities = await dal.GetEntities<T>(query);
            MarkPersisted(entities, query.Projection.GetType());
            return entities;
        }

        public static void MarkPersisted(IDbEntity entity, Type projection)
        {
            DbEntityRecursion.DepthFirst(entity, projection, _markPersisted);
        }

        public static void MarkPersisted(IEnumerable<IDbEntity> entities, Type projection)
        {
            foreach (IDbEntity entity in entities)
                MarkPersisted(entity, projection);
        }

        public static T CreateInstance<T>(int userId)
            where T : class, IDbEntity, IDbEntityBasic, new()
        {
            T t = new T();
            t.BeginEdit();
            t.RecordCreated = DateTime.UtcNow;
            t.LastModifiedDate = DateTime.UtcNow;
            t.IsDeleted = false;
            t.CreatedByID = userId;
            return t;
        }
    }
}
