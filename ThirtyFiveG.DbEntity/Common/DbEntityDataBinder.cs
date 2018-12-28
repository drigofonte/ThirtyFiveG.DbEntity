using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ThirtyFiveG.DbEntity.Entity;
using ThirtyFiveG.DbEntity.Extensions;
using ThirtyFiveG.DbEntity.Extensions.Portable;

namespace ThirtyFiveG.DbEntity.Common
{
    public class DbEntityDataBinder
    {
        private static readonly Regex CollectionPathMatch = new Regex(@"(.*?)\[(.*?)\](.*)");

        public static BinderResult Eval(string path, IDbEntity entity)
        {
            BinderResult result = null;

            string processedPath = path;
            if (processedPath.StartsWith(".") && processedPath.Length > 1)
                processedPath = processedPath.Substring(1);

            if (processedPath.Contains("["))
            {
                // The path goes through a collection
                result = GetEntity(entity, processedPath);
            }
            else
            {
                // The path does not go through a collection
                try
                {
                    result = new BinderResult(processedPath.Equals(".") ? entity : entity.Eval(processedPath) as IDbEntity, path, path);
                }
                catch (Exception)
                {
                    result = new BinderResult(null, path, path);
                }
            }

            result.Path = path;
            if (!result.ActualPath.StartsWith("."))
                result.ActualPath = "." + result.ActualPath;

            if (result.ActualPath.EndsWith(".") && result.ActualPath.Length > 1)
                result.ActualPath = result.ActualPath.Substring(0, result.ActualPath.Length - 1);

            return result;
        }

        private static BinderResult GetEntity(IDbEntity entity, string path)
        {
            Match match = CollectionPathMatch.Match(path);
            string collectionPath = match.Groups[1].Value;
            string collectionItemIndexers = match.Groups[2].Value;
            string collectionPathSuffix = match.Groups[3].Value;

            IEnumerable entities = entity.Eval(collectionPath) as IEnumerable;
            Type entityType = entities.GetType().GetGenericArguments()[0];
            MethodInfo method = typeof(DbEntityDataBinder).GetMethod("GetEntityGeneric", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo generic = method.MakeGenericMethod(entityType);
            BinderResult collectionItemResult = generic.Invoke(null, new object[] { entity, entities, path, collectionPath, collectionItemIndexers }) as BinderResult;

            if (collectionItemResult.Entity != null && !string.IsNullOrEmpty(collectionPathSuffix) && collectionPathSuffix.Contains("["))
            {
                BinderResult childResult = GetEntity(collectionItemResult.Entity, collectionPathSuffix.Substring(1));
                collectionItemResult.Entity = childResult.Entity;
                collectionItemResult.ActualPath += childResult.ActualPath;
            }
            else if (collectionItemResult.Entity != null && !string.IsNullOrEmpty(collectionPathSuffix))
            {
                collectionItemResult = new BinderResult(collectionItemResult.Entity.Eval(collectionPathSuffix.Substring(1)) as IDbEntity, path, collectionItemResult.ActualPath + collectionPathSuffix.Substring(1));
            }
            return collectionItemResult;
        }

        private static BinderResult GetEntityGeneric<T>(IDbEntity entity, IEnumerable entities, string path, string collectionPath, string indexers)
            where T : class, IDbEntity
        {
            BinderResult result = null;

            IEnumerable<T> typedEntities = entities as IEnumerable<T>;
            string[] collectionEntityIndexers = indexers.Split(',');
            List<Tuple<string, object>> collectionEntityKeys = new List<Tuple<string, object>>();
            Type type = typeof(T);
            foreach (string indexer in collectionEntityIndexers)
            {
                string[] keyAndValue = indexer.Split('=');
                collectionEntityKeys.Add(new Tuple<string, object>(keyAndValue[0], Convert.ChangeType(keyAndValue[1], type.GetProperty(keyAndValue[0]).GetGetMethod().ReturnType, null)));
            }

            IDbEntity collectionEntity = typedEntities.AsQueryable().WherePrimaryKeysEqual(collectionEntityKeys.ToArray()).SingleOrDefault() as IDbEntity;
            string actualPath = path;
            if (collectionEntity != null)
                actualPath = DbEntityUtilities.GenerateCollectionItemPath(collectionPath, collectionEntity.PrimaryKeys, collectionEntity.Guid);
            result = new BinderResult(collectionEntity, path, actualPath);

            return result;
        }

        public class BinderResult
        {
            public BinderResult(IDbEntity entity, string path, string actualPath)
            {
                Entity = entity;
                Path = path;
                ActualPath = actualPath;
            }

            public IDbEntity Entity { get; set; }
            public string Path { get; set; }
            public string ActualPath { get; set; }
        }
    }
}
