using System;
using System.Collections.Generic;
using ThirtyFiveG.DbEntity.Entity;

namespace ThirtyFiveG.DbEntity.Query.Data
{
    public interface IDbEntityDataBundle
    {
        int UserID { get; }
        Type Type { get; }
        int Action { get; }
        string Guid { get; }
        IEnumerable<Tuple<string, object>[]> GetPrimaryKeys();
        List<TEntity> GetEntities<TEntity>() where TEntity : class, IDbEntity;
        TEntity GetEntity<TEntity>() where TEntity : class, IDbEntity;
    }
}
