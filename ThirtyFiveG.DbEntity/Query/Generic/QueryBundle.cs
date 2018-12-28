using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using ThirtyFiveG.Commons.Extensions;

namespace ThirtyFiveG.DbEntity.Query.Generic
{
    public class QueryBundle<T> : QueryBundle where T : class
    {
        [JsonIgnore]
        public Expression<Func<T, bool>> Where { get; set; }

        public QueryBundle(int queriedById, IEnumerable<string> includes, object projection, Expression<Func<T, bool>> where, int offset, int count)
        {
            Where = where;
            Initialise(queriedById, includes, projection, string.Empty, offset, count);
        }

        public static QueryBundle<T> Create(int queriedById, object projection, Expression<Func<T, bool>> where, int offset = -1, int count = -1)
        {
            return new QueryBundle<T>(queriedById, null, projection, where, offset, count);
        }

        public static QueryBundle<T> Create(int queriedById, IEnumerable<string> includes, Expression<Func<T, bool>> where, int offset = -1, int count = -1)
        {
            return new QueryBundle<T>(queriedById, includes, null, where, offset, count);
        }

        public new QueryBundle<T> AddParameter<TValue>(string key, TValue value)
        {
            Parameters.Add(typeof(TValue), key, value as object, true);
            return this;
        }
    }
}
