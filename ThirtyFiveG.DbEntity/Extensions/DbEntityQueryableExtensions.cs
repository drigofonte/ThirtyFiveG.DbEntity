using System;
using System.Data.Entity;
using System.Linq;
using ThirtyFiveG.DbEntity.Entity;
using ThirtyFiveG.DbEntity.Projection.Entity;

namespace ThirtyFiveG.DbEntity.Extensions
{
    public static class DbEntityQueryableExtensions
    {
        public static IQueryable<TProjection> Project<TProjection, TEntity>(this IQueryable<TEntity> queryable, DbContext context)
            where TProjection : class, IDbEntityProjection
            where TEntity : class, IDbEntity
        {
            return queryable.Project().To<TProjection>(context) as IQueryable<TProjection>;
        }
    }
}