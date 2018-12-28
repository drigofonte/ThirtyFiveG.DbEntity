using System.Collections.Generic;

namespace ThirtyFiveG.DbEntity.Entity.CollectionManagers
{
    public interface IDualDbEntityAssociativeCollectionManager<TEntity1, TEntity2, TAssociative>
        where TEntity1 : class, IDbEntity, IDbEntityBasic
        where TEntity2 : class, IDbEntity, IDbEntityBasic
        where TAssociative : class, IDbEntity, IDbEntityBasic, new()
    {
        IDbEntity AssociativeCollectionOwner { get; }
        bool Contains(TEntity1 entity1);
        bool Contains(TEntity2 entity2);
        bool Contains(TEntity1 entity1, TEntity2 entity2);
        TAssociative Add(TEntity1 entity1, TEntity2 entity2);
        IEnumerable<TAssociative> Remove(TEntity1 entity1);
        IEnumerable<TAssociative> Remove(TEntity2 entity2);
        TAssociative Remove(TEntity1 entity1, TEntity2 entity2);
    }
}