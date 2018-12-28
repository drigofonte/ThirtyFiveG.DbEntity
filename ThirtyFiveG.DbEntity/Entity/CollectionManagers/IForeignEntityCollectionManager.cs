using System;
using System.Collections.Generic;

namespace ThirtyFiveG.DbEntity.Entity.CollectionManagers
{
    public interface IForeignEntityCollectionManager<TEntity>
        where TEntity : IDbEntity, IDbEntityBasic, new()
    {
        IDbEntity CollectionOwner { get; }
        ICollection<TEntity> Collection { get; }
        bool Contains(TEntity entity);
        TEntity Add();
        TEntity Add(TEntity entity);
        TEntity Remove(TEntity entity);
        TEntity Get(Tuple<string, object>[] primaryKeys);
        TEntity Get(string guid);
    }
}
