using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ThirtyFiveG.Commons.Extensions;
using ThirtyFiveG.DbEntity.Extensions;
using ThirtyFiveG.DbEntity.Projection.Entity;

namespace ThirtyFiveG.DbEntity.Query.Parallel
{
    public static class ParallelQueryUtilities
    {
        private static readonly IDictionary<Type, IDictionary<Type, ParallelQuery>> QueriesCache = new Dictionary<Type, IDictionary<Type, ParallelQuery>>();

        public static ParallelQuery BuildFlatQuery<TSource, TProjection>(Tuple<string, object>[] primaryKeys, DbContext context) 
            where TSource : class
            where TProjection : class
        {
            Type sourceType = typeof(TSource);
            Type projectionType = typeof(TProjection);
            var sourceProperties = sourceType.GetProperties();
            var flatProperties = projectionType.GetProperties()
                .Where(p => p.CanWrite)
                .Where(p => !p.PropertyType.GetInterfaces().Contains(typeof(IDbEntityProjection)) && !p.PropertyType.IsIEnumerable());
            ParameterExpression rootParameterExpression = Expression.Parameter(sourceType, "t");
            Expression rootKeyExpression = GetPrimaryKeysEqual(sourceType, primaryKeys);
            //LambdaExpression rootKeyExpression = GetPrimaryKeyEquals(id, sourceType, context, sourceProperties, rootParameterExpression);

            ParallelQuery query = GetCachedQuery(sourceType, projectionType);
            if (query == default(ParallelQuery))
            {
                // Build select clause
                var bindings = flatProperties
                                    .Select(destinationProperty => BuildBinding(rootParameterExpression, destinationProperty, sourceProperties))
                                    .Where(binding => binding != null);
                Type function = typeof(Func<,>).MakeGenericType(sourceType, projectionType);
                LambdaExpression select = Expression.Lambda(function, Expression.MemberInit(Expression.New(projectionType), bindings), rootParameterExpression);

                query = new ParallelQuery(sourceType, projectionType, rootKeyExpression, select, null, false);
                Cache(query);
            }
            else
            {
                // The 'where' clause always changes to reflect the fact we are wanting an entity with a different id this time
                query = new ParallelQuery(query.SourceType, query.TargetType, rootKeyExpression, query.Select, null, query.IsCollection);
            }

            return query;
        }

        public static IEnumerable<ParallelQuery> BuildCollectionNavigationQueries<TSource, TProjection>(Tuple<string, object>[] primaryKeys, DbContext context) 
            where TSource : class
            where TProjection : class
        {
            List<ParallelQuery> queries = new List<ParallelQuery>();

            Type sourceType = typeof(TSource);
            Type projectionType = typeof(TProjection);
            var sourceProperties = sourceType.GetProperties();
            var flatProperties = projectionType.GetProperties()
                .Where(p => p.CanWrite)
                .Where(p => !p.PropertyType.GetInterfaces().Contains(typeof(IDbEntityProjection)) && !p.PropertyType.IsIEnumerable());

            // Multiple navigation properties
            var collectionNavigationProperties = projectionType.GetProperties()
                .Where(p => p.CanWrite)
                .Where(p => p.PropertyType.IsIEnumerable());

            Type targetType = sourceType;
            foreach (PropertyInfo destProperty in collectionNavigationProperties)
            {
                PropertyInfo sourceProperty = sourceProperties.FirstOrDefault(p => p.Name == destProperty.Name);
                // These properties can be parallelised while retrieving the root
                if (sourceProperty != default(PropertyInfo))
                {
                    Type destinationPropertyType = destProperty.PropertyType.GenericTypeArguments[0];
                    Type relationSourceType = sourceProperty.PropertyType.GenericTypeArguments[0];

                    // Get foreign key
                    PropertyInfo foreignKeyProperty = context.ForeignKeyFor(relationSourceType, targetType).GetForeignKeyProperty(relationSourceType);

                    // Create where foreign_key = id expression
                    var foreignKeyExpression = Expression.Parameter(foreignKeyProperty.DeclaringType, "t");
                    LambdaExpression relationForeignKeyEquals = GetWherePropertyEquals(foreignKeyExpression, foreignKeyProperty, primaryKeys.First().Item2.ChangeType(foreignKeyProperty.PropertyType));

                    ParallelQuery query = GetCachedQuery(relationSourceType, destinationPropertyType);
                    if (query == default(ParallelQuery))
                    {
                        // Create select expression
                        LambdaExpression select = GetSelect(destinationPropertyType, foreignKeyProperty.DeclaringType, context);
                        query = new ParallelQuery(relationSourceType, destinationPropertyType, relationForeignKeyEquals, select, destProperty, true);
                        Cache(query);
                    }
                    else
                    {
                        // The 'where' clause always changes to reflect the fact we are wanting an entity with a different id this time
                        query = new ParallelQuery(query.SourceType, query.TargetType, relationForeignKeyEquals, query.Select, destProperty, query.IsCollection);
                    }

                    queries.Add(query);
                }
            }

            return queries;
        }

        public static IEnumerable<ParallelQuery> BuildSingleNavigationQueries<TSource>(IDbEntityProjection rootProjection, DbContext context)
        {
            List<ParallelQuery> queries = new List<ParallelQuery>();
            Type sourceType = typeof(TSource);
            var sourceProperties = sourceType.GetProperties();

            var flatProperties = rootProjection.GetType().GetProperties()
                .Where(p => p.CanWrite)
                .Where(p => !p.PropertyType.GetInterfaces().Contains(typeof(IDbEntityProjection)) && !p.PropertyType.IsIEnumerable());

            // Build and index the relational properties
            var singleNavigationProperties = rootProjection.GetType().GetProperties()
                .Where(p => p.CanWrite)
                .Where(p => p.PropertyType.GetInterfaces().Contains(typeof(IDbEntityProjection)));

            foreach (PropertyInfo destProperty in singleNavigationProperties)
            {
                PropertyInfo sourceProperty = sourceProperties.FirstOrDefault(p => p.Name == destProperty.Name);
                // These properties need to be parallelised after the root object is retrieved
                if (sourceProperty != default(PropertyInfo))
                {
                    // Get the foreign key
                    PropertyInfo foreignKeyProperty = context.GetForeignKeyProperty(sourceProperty);
                    PropertyInfo rootProjectionProperty = flatProperties.SingleOrDefault(p => p.Name == foreignKeyProperty.Name);

                    // Create where id = foreign_key expression
                    ParameterExpression parameterExpression = Expression.Parameter(sourceProperty.PropertyType, "t");
                    object foreignKeyValue = rootProjectionProperty.GetMethod.Invoke(rootProjection, null);
                    if (foreignKeyValue != null)
                    {
                        //LambdaExpression idEquals = GetPrimaryKeyEquals((int)foreignKeyValue, sourceProperty.PropertyType, context, sourceProperty.PropertyType.GetProperties(), parameterExpression);
                        Expression idEquals = GetPrimaryKeyEqual(sourceProperty.PropertyType, context, foreignKeyValue);

                        ParallelQuery query = GetCachedQuery(sourceProperty.PropertyType, destProperty.PropertyType);
                        if (query == default(ParallelQuery))
                        {
                            if (rootProjectionProperty != default(PropertyInfo))
                            {
                                // Create select expression
                                LambdaExpression select = GetSelect(destProperty.PropertyType, sourceProperty.PropertyType, context);

                                // Store in the parallel queries collection
                                query = new ParallelQuery(sourceProperty.PropertyType, destProperty.PropertyType, idEquals, select, destProperty, false);
                                Cache(query);
                            }
                        }
                        else
                        {
                            // Use a new object in case the same cached query is referenced twice
                            query = new ParallelQuery(query.SourceType, query.TargetType, idEquals, query.Select, destProperty, query.IsCollection);
                        }

                        if (query != default(ParallelQuery))
                            queries.Add(query);
                    }
                }
            }

            return queries;
        }

        private static Expression GetPrimaryKeysEqual(Type entityType, Tuple<string, object>[] primaryKeys)
        {
            return entityType.PropertiesEqualLambda(primaryKeys.ToDictionary(k => k.Item1, k => k.Item2));
        }

        private static Expression GetPrimaryKeyEqual(Type entityType, DbContext context, object foreignKeyValue)
        {
            return GetPrimaryKeysEqual(entityType, new Tuple<string, object>[] { new Tuple<string, object>(context.KeysFor(entityType).Single(), foreignKeyValue) });
        }

        //private static LambdaExpression GetPrimaryKeyEquals(int id, Type type, DbContext context, PropertyInfo[] typeProperties, ParameterExpression parameterExpression)
        //{
        //    // Build where clause
        //    IEnumerable<string> keys = context.KeysFor(type);
        //    if (keys.Count() > 1)
        //        throw new ArgumentException("Found " + keys.Count() + " primary keys for entity type '" + type + "'. Multiple primary keys are not currently supported by parallel queries.");
        //    else if (keys.Count() == 0)
        //        throw new ArgumentException("Found 0 primary keys for entity type '" + type + "'. Need at least one primary key to build parallel queries.");

        //    PropertyInfo rootKeyProperty = typeProperties.FirstOrDefault(src => src.Name == keys.First());
        //    LambdaExpression keyEquals = GetWherePropertyEquals(parameterExpression, rootKeyProperty, id);
        //    return keyEquals;
        //}

        private static LambdaExpression GetSelect(Type destinationPropertyType, Type sourceType, DbContext context)
        {
            Type projectionExpressionType = typeof(ProjectionExpression<>).MakeGenericType(sourceType);
            MethodInfo buildExpression = projectionExpressionType.GetMethod("BuildExpression");

            MethodInfo buildExpressionGeneric = buildExpression.MakeGenericMethod(destinationPropertyType);
            return buildExpressionGeneric.Invoke(null, new object[] { context }) as LambdaExpression;
        }

        private static LambdaExpression GetWherePropertyEquals(ParameterExpression parameterExpression, PropertyInfo property, object value)
        {
            Type function = typeof(Func<,>).MakeGenericType(property.DeclaringType, typeof(bool));
            LambdaExpression propertyEquals = default(LambdaExpression);
            if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                MemberExpression propertyExpression = Expression.Property(parameterExpression, property);
                MemberExpression propertyHasValue = Expression.Property(propertyExpression, "HasValue");
                MemberExpression propertyActualValue = Expression.Property(propertyExpression, "Value");
                BinaryExpression hasValue = Expression.Equal(propertyHasValue, Expression.Constant(true));
                BinaryExpression equalsValue = Expression.Equal(propertyActualValue, Expression.Constant(value));
                propertyEquals = Expression.Lambda(function, Expression.And(hasValue, equalsValue), parameterExpression);
            }
            else
            {
                MemberExpression propertyExpression = Expression.Property(parameterExpression, property);
                BinaryExpression propertyEqualsValue = Expression.Equal(propertyExpression, Expression.Constant(value));
                propertyEquals = Expression.Lambda(function, propertyEqualsValue, parameterExpression);
            }
            return propertyEquals;
        }

        private static MemberAssignment BuildBinding(Expression parameterExpression, MemberInfo destinationProperty, IEnumerable<PropertyInfo> sourceProperties)
        {
            PropertyInfo sourceProperty = sourceProperties.FirstOrDefault(src => src.Name == destinationProperty.Name);
            Type destinationPropertyType = (destinationProperty as PropertyInfo).PropertyType;

            if (sourceProperty != null)
                return Expression.Bind(destinationProperty, Expression.Property(parameterExpression, sourceProperty));

            return null;
        }

        private static void Cache(ParallelQuery query)
        {
            QueriesCache.Add(query.SourceType, query.TargetType, query);
        }

        private static ParallelQuery GetCachedQuery(Type sourceType, Type targetType)
        {
            ParallelQuery query = default(ParallelQuery);
            IDictionary<Type, ParallelQuery> cachedQueries = default(IDictionary<Type, ParallelQuery>);
            bool foundCachedQueries = QueriesCache.TryGetValue(sourceType, out cachedQueries);
            if (foundCachedQueries)
                cachedQueries.TryGetValue(targetType, out query);
            return query;
        }
    }
}
