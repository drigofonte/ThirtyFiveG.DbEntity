using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ThirtyFiveG.Commons.Extensions;
using ThirtyFiveG.DbEntity.Common;
using ThirtyFiveG.DbEntity.Entity;

namespace ThirtyFiveG.DbEntity.Common
{
    public static class DbEntityRecursion
    {
        private static object[] _emptyObjectArray = new object[] { };

        public static void DepthFirst(IDbEntity entity, Type projectionType, Action<IDbEntity, string> beforeRecursionAction)
        {
            DepthFirst(entity, projectionType, beforeRecursionAction, (e, p1, p2, p) => { }, (e, es, p1, p2, p) => { return es; }, (e, es, p1, p2, p) => { }, (e, p) => { });
        }

        public static void DepthFirst(IDbEntity entity,
            Type projectionType,
            Action<IDbEntity, string> beforeRecursionAction,
            Action<IDbEntity, string, string, PropertyInfo> beforePropertyRecursionAction,
            Func<IDbEntity, IEnumerable<IDbEntity>, string, string, PropertyInfo, IEnumerable<IDbEntity>> beforeCollectionRecursionAction,
            Action<IDbEntity, IEnumerable<IDbEntity>, string, string, PropertyInfo> afterCollectionRecursionAction,
            Action<IDbEntity, string> afterRecursionAction)
        {
            DepthFirst(entity, projectionType, ".", beforeRecursionAction, beforePropertyRecursionAction, beforeCollectionRecursionAction, afterCollectionRecursionAction, afterRecursionAction);
        }

        public static void DepthFirst(IDbEntity entity,
            Type projectionType,
            string rootPath,
            Action<IDbEntity, string> beforeRecursionAction,
            Action<IDbEntity, string, string, PropertyInfo> beforePropertyRecursionAction,
            Func<IDbEntity, IEnumerable<IDbEntity>, string, string, PropertyInfo, IEnumerable<IDbEntity>> beforeCollectionRecursionAction,
            Action<IDbEntity, IEnumerable<IDbEntity>, string, string, PropertyInfo> afterCollectionRecursionAction,
            Action<IDbEntity, string> afterRecursionAction)
        {
            IDictionary<Type, ISet<string>> processedEntities = new Dictionary<Type, ISet<string>>();

            DepthFirst(entity, projectionType, rootPath, processedEntities, beforeRecursionAction, beforePropertyRecursionAction, beforeCollectionRecursionAction, afterCollectionRecursionAction, afterRecursionAction);

            // Cleanup
            foreach (var processedEntity in processedEntities)
            {
                processedEntity.Value.Clear();
            }
            processedEntities.Clear();
        }

        public static void DepthFirst(IDbEntity entity,
            Type projectionType,
            string path,
            IDictionary<Type, ISet<string>> processedEntities,
            Action<IDbEntity, string> beforeRecursionAction,
            Action<IDbEntity, string, string, PropertyInfo> beforePropertyRecursionAction,
            Func<IDbEntity, IEnumerable<IDbEntity>, string, string, PropertyInfo, IEnumerable<IDbEntity>> beforeCollectionRecursionAction,
            Action<IDbEntity, IEnumerable<IDbEntity>, string, string, PropertyInfo> afterCollectionRecursionAction,
            Action<IDbEntity, string> afterRecursionAction)
        {
            bool processed = !processedEntities.Add(entity.GetType(), entity.Guid);
            if (processed)
                // Prevents self-referencing entities from causing infinite loops
                return;
            beforeRecursionAction(entity, path);

            IEnumerable<PropertyInfo> virtualProperties = entity.GetType().GetVirtualProperties(true, true);
            virtualProperties = virtualProperties.Where(ep => projectionType.GetProperties().Any(pp => ep.Name.Equals(pp.Name)));
            foreach (PropertyInfo property in virtualProperties)
            {
                PropertyInfo projectionProperty = projectionType.GetProperty(property.Name);
                //PropertyInfo projectionProperty = property;
                string propertyPath = DbEntityUtilities.GeneratePropertyPath(path, property.Name);
                beforePropertyRecursionAction(entity, path, propertyPath, property);

                object value = property.GetGetMethod().Invoke(entity, _emptyObjectArray);
                if (value != null)
                {
                    if (property.GetGetMethod().ReturnType.IsIEnumerable() 
                        && DbEntityUtilities.IsIDbEntity(property.GetGetMethod().ReturnType.GetGenericArguments()[0]))
                    {
                        IEnumerable<IDbEntity> entities = value as IEnumerable<IDbEntity>;
                        entities = beforeCollectionRecursionAction(entity, entities, path, propertyPath, property);

                        for (int i = 0; i < entities.Count(); i++)
                        {
                            DepthFirst(entities.ElementAt(i), projectionProperty.GetGetMethod().ReturnType.GetGenericArguments()[0], DbEntityUtilities.GenerateCollectionItemPath(propertyPath, entities.ElementAt(i).Guid), processedEntities, beforeRecursionAction, beforePropertyRecursionAction, beforeCollectionRecursionAction, afterCollectionRecursionAction, afterRecursionAction);
                        }

                        afterCollectionRecursionAction(entity, entities, path, propertyPath, property);
                    }
                    else if (DbEntityUtilities.IsIDbEntity(property.GetGetMethod().ReturnType))
                    {
                        DepthFirst(value as IDbEntity, projectionProperty.GetGetMethod().ReturnType, propertyPath, processedEntities, beforeRecursionAction, beforePropertyRecursionAction, beforeCollectionRecursionAction, afterCollectionRecursionAction, afterRecursionAction);
                    }
                }
            }

            afterRecursionAction(entity, path);
        }
    }
}