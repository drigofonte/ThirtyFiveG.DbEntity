using System;

namespace ThirtyFiveG.DbEntity.Query.Data
{
    public interface IDataBundle
    {
        IDataBundle AddParameter<TValue>(string key, TValue value);
        IDataBundle RemoveParameter<TValue>(string key);
        bool Contains<T>(string key);
        T GetParameter<T>(string key);
        T GetParameter<T>();
        void Clear();
    }
}
