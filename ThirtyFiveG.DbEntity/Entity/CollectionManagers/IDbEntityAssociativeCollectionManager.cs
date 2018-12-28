using System.Collections.Generic;

namespace ThirtyFiveG.DbEntity.Entity.CollectionManagers
{
    public interface IDbEntityAssociativeCollectionManager<TEntity, TAssociative>
        where TEntity : class, IDbEntity, IDbEntityBasic
        where TAssociative : class, IDbEntity, IDbEntityBasic, new()
    {
        IDbEntity AssociativeCollectionOwner { get; }
        ICollection<TAssociative> AssociativeCollection { get; }
        bool Contains(TEntity entity);
        IEnumerable<TAssociative> Add(IEnumerable<TEntity> entities);
        TAssociative Add(TEntity entity);
        TAssociative Remove(TEntity entity);
        void RemoveAll();
        TAssociative Get(TEntity entity);
    }
}
