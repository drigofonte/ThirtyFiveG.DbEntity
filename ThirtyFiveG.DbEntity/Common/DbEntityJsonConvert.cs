using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System;
using System.Reflection;
using ThirtyFiveG.Commons.Extensions;
using ThirtyFiveG.DbEntity.Entity;

namespace ThirtyFiveG.DbEntity.Common
{
    public static class DbEntityJsonConvert
    {
        private static readonly Func<object, bool> _nullOrZero = k => (k ?? 0).Equals(0);
        private static readonly Func<string, string> _firstEntityPath = (p) => p.Substring(0, p.IndexOf(".", 1));

        public static string SerializePrimaryKeys(IDbEntity entity)
        {
            string json = string.Empty;

            StringBuilder sb = new StringBuilder();
            using (StringWriter sw = new StringWriter(sb))
            {
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    SerializePrimaryKeys(entity, writer);
                }

                sw.Flush();
                json = sw.ToString();
                sb.Clear();
            }

            return json;
        }

        private static void SerializePrimaryKeys(IDbEntity entity, JsonWriter writer)
        {
            // Start object
            writer.WriteStartObject();
            writer.WritePropertyName("$id");
            writer.WriteValue(entity.Guid);

            if (entity.PrimaryKeys.Select(k => k.Item2).Any(_nullOrZero))
            {
                // Only encode the Guid if this is a new entity
                writer.WritePropertyName("Guid");
                writer.WriteValue(entity.Guid);
            }

            // Encode any non-zero, non-null primary keys
            foreach (Tuple<string, object> key in entity.PrimaryKeys.Where(k => !_nullOrZero(k.Item2)).OrderBy(k => k.Item1))
            {
                writer.WritePropertyName(key.Item1);
                writer.WriteValue(key.Item2);
            }
        }

        public static string SerializeEntity(IDbEntity entity, IEnumerable<string> propertyPaths)
        {
            return SerializeEntity(entity, propertyPaths, new List<string>());
        }

        private static string SerializeEntity(IDbEntity entity, IEnumerable<string> propertyPaths, ICollection<string> entityReferences)
        {
            string json = string.Empty;

            if (entityReferences.Contains(entity.Guid))
                json = SerializeAsReference(entity);
            else
            {
                json = SerializeAsObject(entity, propertyPaths, entityReferences);
                entityReferences.Add(entity.Guid);
            }

            return json;
        }

        private static string SerializeAsReference(IDbEntity entity)
        {
            string json = string.Empty;

            StringBuilder sb = new StringBuilder();
            using (StringWriter sw = new StringWriter(sb))
            {
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    // Start object
                    writer.WriteStartObject();
                    writer.WritePropertyName("$ref");
                    writer.WriteValue(entity.Guid);
                }

                sw.Flush();
                json = sw.ToString();
                sb.Clear();
            }

            return json;
        }

        private static string SerializeAsObject(IDbEntity entity, IEnumerable<string> propertyPaths, ICollection<string> entityReferences)
        {
            string json = string.Empty;

            StringBuilder sb = new StringBuilder();
            using (StringWriter sw = new StringWriter(sb))
            {
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    // Reduce the string size as much as possible
                    writer.Formatting = Formatting.None;
                    ISet<string> oneHopPaths = new HashSet<string>(propertyPaths.Where(p => HopsCount(p) == 1 && !p.Contains("[")).ToList());
                    oneHopPaths.UnionWith(CreateMissingOneHopCollectionPaths(propertyPaths));
                    oneHopPaths.UnionWith(CreateMissingOneHopEntityPaths(propertyPaths));

                    SerializePrimaryKeys(entity, writer);

                    // Encode any non-zero, non-null primary keys
                    foreach (Tuple<string, object> key in entity.PrimaryKeys.Where(k => !_nullOrZero(k.Item2)).OrderBy(k => k.Item1))
                        oneHopPaths.Remove("." + key.Item1);

                    // Navigate flat property paths
                    ISet<string> processedPaths = new HashSet<string>();
                    string propertyName = string.Empty;
                    foreach (string path in oneHopPaths)
                    {
                        if (processedPaths.Add(path))
                        {
                            propertyName = path.Substring(1);
                            PropertyInfo property = entity.GetType().GetProperty(propertyName);
                            var value = property.GetValue(entity, null);
                            // Property : Value
                            if (!IsIDbEntity(property) && (value == null || !value.GetType().IsIEnumerable()))
                            {
                                // Not a relation and not a 
                                writer.WritePropertyName(propertyName);
                                writer.WriteValue(value);
                            }
                            else
                            {
                                ICollection<string> relationalPropertyPaths = propertyPaths
                                    .Where(p => p.StartsWith(path))
                                    .ToList();

                                // Essentially, every time a new relation is created, it should be attached to its related entity before any changes are made to it (e.g. set virtual property or add to virtual collection).
                                if (IsIDbEntity(property))
                                {
                                    // It is a many-to-one relation
                                    relationalPropertyPaths = relationalPropertyPaths
                                        .Where(p => HopsCount(p) > 1)
                                        .ToList();

                                    ICollection<string> flattenedRelationalPropertyPaths = relationalPropertyPaths
                                        .Select(p => p.Substring(path.Length))
                                        .Where(p => !string.IsNullOrEmpty(p))
                                        .ToList();

                                    IDbEntity relationalEntity = DbEntityDataBinder.Eval(path, entity).Entity;
                                    string relationalJson = "null";
                                    if (relationalEntity != null)
                                        relationalJson = SerializeEntity(relationalEntity, flattenedRelationalPropertyPaths, entityReferences);
                                    flattenedRelationalPropertyPaths.Clear();

                                    writer.WritePropertyName(propertyName);
                                    writer.WriteRawValue(relationalJson);
                                }
                                else
                                {
                                    // It is a collection relation (i.e. one-to-many or many-to-many)
                                    writer.WritePropertyName(propertyName);
                                    writer.WriteStartArray();
                                    List<string> collectionEntityPaths = relationalPropertyPaths
                                        .Where(p => HopsCount(p) > 1)
                                        .Select(p => p.Substring(0, p.IndexOf('.', 1)))
                                        .Distinct()
                                        .ToList();
                                    collectionEntityPaths.AddRange(relationalPropertyPaths.Where(p => HopsCount(p) == 1));
                                    foreach (string collectionEntityPath in collectionEntityPaths.Distinct())
                                    {
                                        IDbEntity collectionEntity = DbEntityDataBinder.Eval(collectionEntityPath, entity).Entity;
                                        ICollection<string> flattenedCollectionEntityPaths = relationalPropertyPaths
                                            .Where(p => p.StartsWith(collectionEntityPath))
                                            .Select(p => p.Substring(collectionEntityPath.Length))
                                            .ToList();

                                        string collectionEntityJson = SerializeEntity(collectionEntity, flattenedCollectionEntityPaths, entityReferences);
                                        writer.WriteRawValue(collectionEntityJson);
                                        flattenedCollectionEntityPaths.Clear();
                                    }

                                    writer.WriteEndArray();
                                    collectionEntityPaths.Clear();
                                }

                                processedPaths.UnionWith(relationalPropertyPaths);
                                relationalPropertyPaths.Clear();
                            }
                        }
                    }

                    oneHopPaths.Clear();

                    // End object
                    writer.WriteEndObject();
                }

                sw.Flush();
                json = sw.ToString();
                sb.Clear();
            }

            return json;
        }

        private static ISet<string> CreateMissingOneHopCollectionPaths(IEnumerable<string> propertyPaths)
        {
            ISet<string> oneHopCollectionPaths = new HashSet<string>();
            ICollection<string> collectionEntityPaths = propertyPaths.Where(p => p.Contains("[")).ToList();
            foreach (string path in collectionEntityPaths)
            {
                string oneHopCollectionPath = path.Substring(0, path.IndexOf("["));
                if (HopsCount(oneHopCollectionPath) == 1)
                    oneHopCollectionPaths.Add(oneHopCollectionPath);
            }
                
            collectionEntityPaths.Clear();
            return oneHopCollectionPaths;
        }

        private static ISet<string> CreateMissingOneHopEntityPaths(IEnumerable<string> propertyPaths)
        {             
            ISet<string> oneHopEntityPaths = new HashSet<string>();
            oneHopEntityPaths.UnionWith(propertyPaths
                .Where(p => HopsCount(p) > 1 && !_firstEntityPath(p).Contains("["))
                .Select(p => _firstEntityPath(p)));
            return oneHopEntityPaths;
        }

        private static bool IsIDbEntity(Type type)
        {
            return type.GetInterfaces().Contains(typeof(IDbEntity));
        }

        private static bool IsIDbEntity(PropertyInfo p)
        {
            return IsIDbEntity(p.GetGetMethod().ReturnType);
        }

        private static int HopsCount(string s)
        {
            return s.Length - s.Replace(".", "").Length;
        }
    }
}
