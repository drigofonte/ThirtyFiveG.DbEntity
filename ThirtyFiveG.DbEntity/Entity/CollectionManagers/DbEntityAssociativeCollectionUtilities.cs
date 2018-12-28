using ThirtyFiveG.DbEntity.Entity;

namespace ThirtyFiveG.DbEntity.Entity.CollectionManagers
{
    public static class DbEntityAssociativeCollectionUtilities
    {
        public static bool MatchAssociativeEntities<TEntity>(TEntity existingEntity, object existingEntityForeignKey, TEntity entityToMatch, object entityToMatchForeignKey)
            where TEntity : class, IDbEntity
        {
            return (existingEntity != null && existingEntity.State == EntityState.New && existingEntity.Equals(entityToMatch))
                || ((existingEntity == null || existingEntity.State == EntityState.Persisted) && existingEntityForeignKey.Equals(entityToMatchForeignKey));
        }
    }
}
