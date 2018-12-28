using System;
using System.Linq;
using ThirtyFiveG.Commons.Extensions;

namespace ThirtyFiveG.DbEntity.Extensions.Portable
{
    public static class DbEntityQueryableExtensions
    {
        public static IQueryable<T> WherePrimaryKeysEqual<T>(this IQueryable<T> queryable, Tuple<string, object>[] primaryKeys)
        {
            Type entityType = typeof(T);
            foreach(Tuple<string, object> primaryKey in primaryKeys)
                queryable = queryable.Where(entityType.PropertyEqualsLambda<T>(primaryKey.Item1, primaryKey.Item2));
            return queryable;
        }
    }
}