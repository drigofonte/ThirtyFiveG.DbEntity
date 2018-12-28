using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using ThirtyFiveG.Commons.Extensions;

namespace ThirtyFiveG.DbEntity.Query
{
    public class QueryBundle
    {
        public int UserID { get; set; }

        public IDictionary<Type, IDictionary<string, object>> Parameters { get; set; }

        public IEnumerable<string> Includes { get; set; }

        public object Projection { get; set; }

        public int Offset { get; set; }

        public int Count { get; set; }

        public string QueryExecutor { get; set; }

        public QueryBundle() { }

        public QueryBundle(int queriedById, IEnumerable<string> includes, object projection, string queryExecutor, int offset, int count)
        {
            Initialise(queriedById, includes, projection, queryExecutor, offset, count);
        }

        protected void Initialise(int queriedById, IEnumerable<string> includes, object projection, string queryExecutor, int offset, int count)
        {
            UserID = queriedById;
            Includes = includes;
            Projection = projection;
            Offset = offset;
            Count = count;
            QueryExecutor = queryExecutor;
            Parameters = new Dictionary<Type, IDictionary<string, object>>();
        }

        public static QueryBundle Create(int queriedById, object projection, string queryExecutor, int offset = -1, int count = -1)
        {
            return new QueryBundle(queriedById, null, projection, queryExecutor, offset, count);
        }

        public static QueryBundle Create(int queriedById, IEnumerable<string> includes, string queryExecutor, int offset = -1, int count = -1)
        {
            return new QueryBundle(queriedById, includes, null, queryExecutor, offset, count);
        }

        public QueryBundle AddParameter<TValue>(string key, TValue value)
        {
            Parameters.Add(typeof(TValue), key, value as object, true);
            return this;
        }

        public bool Contains<T>(string key)
        {
            bool contains = false;
            IDictionary<string, object> values;
            if (Parameters.TryGetValue(typeof(T), out values))
            {
                contains = values.ContainsKey(key);
            }
            return contains;
        }

        public T GetParameter<T>(string key)
        {
            T t = default(T);
            IDictionary<string, object> values;
            if (Parameters.TryGetValue(typeof(T), out values))
            {
                object value;
                if (values.TryGetValue(key, out value))
                {
                    t = GetValue<T>(value);
                }
            }
            return t;
        }

        public T GetParameter<T>()
        {
            T t = default(T);
            IDictionary<string, object> values;
            if (Parameters.TryGetValue(typeof(T), out values))
            {
                object value = values.Values.FirstOrDefault();
                if (value != default(object))
                {
                    t = GetValue<T>(value);
                }
            }
            return t;
        }

        private T GetValue<T>(object value)
        {
            T t = default(T);
            if (value.GetType().Equals(typeof(JArray)))
            {
                t = (value as JArray).ToObject<T>();
            }
            else
            {
                t = (T)Convert.ChangeType(value, typeof(T), null);
            }
            return t;
        }
    }
}
