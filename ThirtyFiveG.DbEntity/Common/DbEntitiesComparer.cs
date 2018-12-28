using System;
using System.Collections.Generic;
using System.Linq;
using ThirtyFiveG.DbEntity.Entity;

namespace ThirtyFiveG.DbEntity.Common
{
    public class DbEntitiesComparer<T> : IEqualityComparer<T>
        where T : class, IDbEntity
    {
        public bool Equals(T x, T y)
        {
            bool equals = x != null && y != null && x.State == y.State;
            if (equals)
            {
                foreach (Tuple<string, object> key in x.PrimaryKeys)
                    equals &= y.PrimaryKeys.Any(k => k.Item1.Equals(key.Item1) && k.Item2.Equals(key.Item2));
                if (equals && x.State == EntityState.New && y.State == EntityState.New)
                    equals &= x.Guid.Equals(y.Guid);
            }
            return equals;
        }

        public int GetHashCode(T obj)
        {
            int hashCode = 17;
            foreach (Tuple<string, object> tuple in obj.PrimaryKeys)
            {
                hashCode = hashCode * 23 + tuple.Item1.GetHashCode();
                hashCode = hashCode * 23 + tuple.Item2.GetHashCode();
            }
            if (obj.State == EntityState.New)
                hashCode = hashCode * 23 + obj.Guid.GetHashCode();
            return hashCode;
        }
    }
}
