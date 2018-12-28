using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using ThirtyFiveG.Commons.Extensions;

namespace ThirtyFiveG.DbEntity.Query.Data
{
    public class DataBundle : IDataBundle
    {
        #region Constructor
        public DataBundle()
        {
            Parameters = new Dictionary<Type, IDictionary<string, object>>();
            Guid = System.Guid.NewGuid().ToString();
        }
        #endregion

        #region Public properties
        public string Guid { get; private set; }
        public IDictionary<Type, IDictionary<string, object>> Parameters { get; private set; }

        #endregion

        #region Private methods
        private T GetValue<T>(object value)
        {
            T t = default(T);
            if (value.GetType().Equals(typeof(JArray)))
            {
                t = (value as JArray).ToObject<T>();
            } else
            {
                try
                {
                    t = (T)value;
                }
                catch (Exception)
                {
                    t = (T)Convert.ChangeType(value, typeof(T), null);
                }

                if (t == null)
                {
                    t = (T)Convert.ChangeType(value, typeof(T), null);
                }
            }
            return t;
        }
        #endregion

        #region Public methods
        public IDataBundle AddParameter<TValue>(string key, TValue value)
        {
            AddParameter(typeof(TValue), key, value as object);
            return this;
        }

        public IDataBundle AddParameter(Type t, string key, object value)
        {
            Parameters.Add(t, key, value, true);
            return this;
        }

        public IDataBundle RemoveParameter<TValue>(string key)
        {
            Parameters.Remove(typeof(TValue), key);
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

        public object GetParameter(Type t, string key)
        {
            object o = null;
            IDictionary<string, object> values;
            if (Parameters.TryGetValue(t, out values))
            {
                o = values[key];
            }
            return o;
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

        public void Clear()
        {
            Parameters.ClearAll();
        }
        #endregion

        #region Public static methods
        public static IDataBundle GetInstance()
        {
            return new DataBundle();
        }
        #endregion
    }
}
