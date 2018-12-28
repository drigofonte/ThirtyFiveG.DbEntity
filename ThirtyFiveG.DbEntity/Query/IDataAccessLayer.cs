using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using ThirtyFiveG.Commons.Interfaces;
using ThirtyFiveG.DbEntity.Entity;
using ThirtyFiveG.DbEntity.Projection.Entity;
using ThirtyFiveG.DbEntity.Query.Data;
using ThirtyFiveG.DbEntity.Query.Generic;

namespace ThirtyFiveG.DbEntity.Query
{
    public interface IDataAccessLayer
    {
        Task Connect(CookieContainer c);
        Task<T> GetEntity<T>(QueryBundle query) where T : class, IDbEntity;
        Task<T> GetEntity<T, E>(Tuple<string, object>[] keys) where E : class, IDbEntityProjection;
        Task<IEnumerable<T>> GetEntities<T>(QueryBundle query) where T : class, IDbEntity;
        Task<IEnumerable<T>> GetEntities<T>(QueryBundle<T> query) where T : class, IDbEntity;
        Task<IEnumerable<T>> GetMultipleEntities<T>(IEnumerable<Tuple<string, object>[]> keys, Type projectionType);
        Task<IDictionary<string, Tuple<string, object>[]>> UpdateEntity(Type t, string json, IEnumerable<string> propertiesToUpdate, int action, int duration, int commentId);
        Task<Tuple<string, object>[]> UpdateEntity<T>(T entity, IEnumerable<string> propertiesToUpdate, int action, int duration, int commentId) where T : class, IDbEntity;
        Task<IEnumerable<Tuple<string, object>[]>> UpdateEntities<T>(IEnumerable<T> entities, IEnumerable<IEnumerable<string>> allPropertiesToUpdate, int action, IEnumerable<int> allDurations, int commentId) where T : class, IDbEntity;
        Task<IEnumerable<DbEntityActionMessage>> GetDbEntityActionMessages<T>(Tuple<string, object>[] keys);
        Task<IEnumerable<EntityActionsResult>> GetDbEntityActionMessages<T>(
            IEnumerable<Tuple<string, object>[]> keys);
        Task<string> GetActionData(string blobFilename);
        Task<T> ExecuteQuery<T>(QueryBundle query) where T : class;
        Task PersistDbEntityAction<T>(T entity, int action, IActionData actionData) where T : class, IDbEntity;
        Task Do(IDbEntityDataBundle data);
    }
}
