using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ThirtyFiveG.Commons.Interfaces;
using ThirtyFiveG.DbEntity.Entity;

namespace ThirtyFiveG.DbEntity.Common
{
    public static class DbEntityUtilities
    {
        public static readonly Func<object, bool> NullOrZero = k => (k ?? 0).Equals(0);

        public static string GenerateCollectionItemIdentifiers(Tuple<string, object>[] primaryKeys, string guid)
        {
            ICollection<Tuple<string, object>> primaryKeysOrdered = new List<Tuple<string, object>>(primaryKeys);
            // If we are dealing with a new entity, use the guid. If not, then it is already uniquely identifiable through its primary keys alone
            if (primaryKeys.Select(k => k.Item2).Any(NullOrZero) || primaryKeys.Length == 0)
                primaryKeysOrdered.Add(new Tuple<string, object>("Guid", guid));
            string identifiers = string.Empty;
            foreach (Tuple<string, object> primaryKey in primaryKeysOrdered.OrderBy(k => k.Item1))
            {
                // Only use the keys that are not null or zero to uniquely refer to this collection item
                if (!NullOrZero(primaryKey.Item2))
                    identifiers += primaryKey.Item1 + "=" + primaryKey.Item2 + ",";
            }
            return identifiers.Substring(0, identifiers.Length - 1);
        }

        public static string GenerateCollectionItemPath(string path, Tuple<string, object>[] primaryKeys, string guid)
        {
            path = path.Substring(path.Length - 1).Equals(".") ? path.Substring(0, path.Length - 1) : path;
            
            return path + "[" + GenerateCollectionItemIdentifiers(primaryKeys, guid) + "].";
        }

        public static string GenerateCollectionItemPath(string path, string guid)
        {
            return GenerateCollectionItemPath(path, new Tuple<string, object>[] { new Tuple<string, object>("Null", null) }, guid);
        }

        public static string GeneratePropertyPath(string path, string propertyName)
        {
            if (!path.EndsWith("."))
                path += ".";

            return path + propertyName + ".";
        }

        public static bool IsIDbEntity(Type type)
        {
            return type.GetInterfaces().Contains(typeof(Entity.IDbEntity));
        }

        public static bool IsIDbEntity(PropertyInfo p)
        {
            return IsIDbEntity(p.GetGetMethod().ReturnType);
        }

        public static int GetNextWeight(IEnumerable<IWeighted> weightedEntities)
        {
            return weightedEntities.OrderBy(e => e.Weight).Last().Weight + 1;
        }

        public static void UpdatePrimaryKeys(IDictionary<string, Tuple<string, object>[]> pathsAndKeys, IDbEntity entity)
        {
            foreach(KeyValuePair<string, Tuple<string, object>[]> pathAndKeys in pathsAndKeys)
            {
                IDbEntity entityToUpdate = DbEntityDataBinder.Eval(pathAndKeys.Key, entity).Entity;
                UpdatePrimaryKeys(pathAndKeys.Value, entityToUpdate);
            }
        }

        public static void UpdatePrimaryKeys(Tuple<string, object>[] primaryKeys, IDbEntity entity)
        {
            foreach (Tuple<string, object> primaryKey in primaryKeys)
            {
                PropertyInfo p = entity.GetType().GetProperty(primaryKey.Item1);
                object value = primaryKey.Item2;
                if (!p.GetGetMethod().ReturnType.Equals(value.GetType()))
                    value = Convert.ChangeType(value, p.GetGetMethod().ReturnType, null);
                p.SetValue(entity, value, null);
            }
                
            entity.MarkPersisted();
        }

        public static bool MatchesType(Type expected, Type actual)
        {
            if (expected.BaseType != null && expected.Namespace == "System.Data.Entity.DynamicProxies")
                expected = expected.BaseType;
            if (actual.BaseType != null && actual.Namespace == "System.Data.Entity.DynamicProxies")
                actual = actual.BaseType;
            return expected.Equals(actual);
        }

        public static string GetDbEntityPropertyPath(string path, IDbEntity entity)
        {
            if (path.Contains("["))
            {
                // It goes through collection items, so we must convert it
                IEnumerable<string> collectionItems = path.Split(new string[] { "]." }, StringSplitOptions.RemoveEmptyEntries).Select(i => i.Contains("[") && !i.EndsWith("]") ? i += "]" : i);
                path = ConvertPath(entity, string.Empty, collectionItems.ElementAt(0));
                for (int i = 1; i <= collectionItems.Count() - 2; i++)
                {
                    path += ConvertPath(entity, path, collectionItems.ElementAt(i));
                }

                if (collectionItems.Count() > 1)
                {
                    string lastPathElement = collectionItems.ElementAt(collectionItems.Count() - 1);
                    if (lastPathElement.Contains("["))
                    {
                        lastPathElement = ConvertPath(entity, path, lastPathElement);
                        if (lastPathElement.EndsWith("."))
                            path += lastPathElement.Substring(0, lastPathElement.Length - 1);
                        else
                            path += lastPathElement;
                    }
                    else
                        path += lastPathElement;
                }
                else
                    path = path.Substring(0, path.Length - 1);
            }
            return path;
        }

        public static bool PrimaryKeysEqual(IEnumerable<Tuple<string, object>> s1, IEnumerable<Tuple<string, object>> s2)
        {
            bool sequenceEqual = true;
            // Same number of elements
            sequenceEqual &= s1.Count() == s2.Count();

            // All keys in s1 occur in s2
            sequenceEqual &= s1.Select(t => t.Item1).All(k => s2.Select(t => t.Item1).Contains(k));

            if (sequenceEqual)
            {
                // All the key-value pairs in s1 match those of s2
                object v1;
                object v2;
                foreach (Tuple<string, object> t1 in s1)
                {
                    Tuple<string, object> t2 = s2.Single(t => t.Item1.Equals(t1.Item1));
                    v1 = t1.Item2;
                    v2 = t2.Item2;

                    if (v1.GetType().Equals(typeof(int)))
                        v1 = Convert.ChangeType(v1, typeof(long), null);

                    if (v2.GetType().Equals(typeof(int)))
                        v2 = Convert.ChangeType(v2, typeof(long), null);

                    sequenceEqual &= v1.Equals(v2);
                }
            }

            return sequenceEqual;
        }

        private static string ConvertPath(IDbEntity entity, string pathPrefix, string collectionItemPath)
        {
            IDbEntity collectionItem = DbEntityDataBinder.Eval(pathPrefix + collectionItemPath, entity).Entity;
            return GenerateCollectionItemPath(collectionItemPath.Substring(0, collectionItemPath.IndexOf("[")), collectionItem.PrimaryKeys, collectionItem.Guid);
        }
    }
}
