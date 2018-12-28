using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using ThirtyFiveG.Commons.Extensions;

namespace ThirtyFiveG.DbEntity.Extensions
{
    public static class QueryableExtensions
    {
        public static ProjectionExpression<TSource> Project<TSource>(this IQueryable<TSource> source)
        {
            return new ProjectionExpression<TSource>(source);
        }
    }

    public class ProjectionExpression<TSource>
    {
        private static readonly Dictionary<string, Expression> ExpressionCache = new Dictionary<string, Expression>();

        private readonly IQueryable<TSource> _source;

        public ProjectionExpression(IQueryable<TSource> source)
        {
            _source = source;
        }

        public IQueryable To<TDest>(TDest projection, DbContext context) where TDest : class
        {
            return To<TDest>(context);
        }

        public IQueryable To<TDest>(DbContext context) where TDest : class
        {
            var queryExpression = BuildExpression<TDest>(context);
            return _source.Select(queryExpression);
        }

        public static Expression<Func<TSource, TDest>> GetCachedExpression<TDest>()
        {
            var key = GetCacheKey<TDest>();

            return ExpressionCache.ContainsKey(key) ? ExpressionCache[key] as Expression<Func<TSource, TDest>> : null;
        }

        public static Expression<Func<TSource, TDest>> BuildExpression<TDest>(DbContext context)
        {
            var expression = GetCachedExpression<TDest>();

            if (expression == default(Expression<Func<TSource, TDest>>))
            {
                var sourceProperties = typeof(TSource).GetProperties();
                var destinationProperties = typeof(TDest).GetProperties().Where(dest => dest.CanWrite);
                var parameterExpression = Expression.Parameter(typeof(TSource), "t");

                var bindings = destinationProperties
                                    .Select(destinationProperty => BuildBinding(parameterExpression, destinationProperty, sourceProperties, context))
                                    .Where(binding => binding != null);

                expression = Expression.Lambda<Func<TSource, TDest>>(Expression.MemberInit(Expression.New(typeof(TDest)), bindings), parameterExpression);

                var key = GetCacheKey<TDest>();

                if (!ExpressionCache.ContainsKey(key))
                    ExpressionCache.Add(key, expression);
            }

            return expression;
        }

        private static MemberAssignment BuildBinding(Expression parameterExpression, MemberInfo destinationProperty, IEnumerable<PropertyInfo> sourceProperties, DbContext context)
        {
            PropertyInfo sourceProperty = sourceProperties.FirstOrDefault(src => src.Name == destinationProperty.Name);

            if (sourceProperty != null)
            {
                MemberExpression memberExpression = Expression.Property(parameterExpression, sourceProperty);

                try
                {
                    return Expression.Bind(destinationProperty, memberExpression);
                }
                catch (ArgumentException)
                {
                    Type sourcePropertyType = (sourceProperty as PropertyInfo).PropertyType;
                    Type destinationPropertyType = (destinationProperty as PropertyInfo).PropertyType;
                    bool isEnumerable = sourcePropertyType.IsIEnumerable();

                    if (!isEnumerable)
                    {
                        // The property is a one-to-one or many-to-one navigation property
                        return GetSingleNavigationExpression(sourcePropertyType, destinationPropertyType, parameterExpression, memberExpression, sourceProperty, destinationProperty, context);
                    }
                    else
                    {
                        // The property is a one-to-many navigation property
                        return GetCollectionNavigationExpression(sourcePropertyType, destinationPropertyType, parameterExpression, sourceProperty, destinationProperty, context);
                    }
                }
            }

            var propertyNames = SplitCamelCase(destinationProperty.Name);

            if (propertyNames.Length == 2)
            {
                sourceProperty = sourceProperties.FirstOrDefault(src => src.Name == propertyNames[0]);

                if (sourceProperty != null)
                {
                    var sourceChildProperty = sourceProperty.PropertyType.GetProperties().FirstOrDefault(src => src.Name == propertyNames[1]);

                    if (sourceChildProperty != null)
                    {
                        MemberExpression pE = Expression.Property(parameterExpression, sourceProperty);
                        return Expression.Bind(destinationProperty, Expression.Property(pE, sourceChildProperty));
                    }
                }
            }

            return null;
        }

        private static string GetCacheKey<TDest>()
        {
            return string.Concat(typeof(TSource).FullName, typeof(TDest).FullName);
        }

        private static string[] SplitCamelCase(string input)
        {
            return Regex.Replace(input, "([A-Z])", " $1", RegexOptions.Compiled).Trim().Split(' ');
        }

        private static MemberAssignment GetSingleNavigationExpression(Type sourcePropertyType, Type destinationPropertyType, Expression parentParameterExpression, Expression childParameterExpression, PropertyInfo sourceProperty, MemberInfo destinationProperty, DbContext context)
        {
            var childSourceProperties = sourcePropertyType.GetProperties();
            var destinationProperties = destinationPropertyType.GetProperties().Where(dest => dest.CanWrite);

            // Account for situations where the foreign key is null
            PropertyInfo foreignKeyProperty = context.GetForeignKeyProperty(sourceProperty);
            bool isNullable = Nullable.GetUnderlyingType(foreignKeyProperty.PropertyType) != null;

            var bindings = GetBindings(destinationProperties, childParameterExpression, destinationProperty, childSourceProperties, context);

            // (p) => new T() {Property = p.Property, ...}
            Expression propertyInit = Expression.MemberInit(Expression.New(destinationPropertyType), bindings);

            if (isNullable)
            {
                // (p) => if (pID != null) new T() {Property = p.Property, ...} else null as T
                Expression nullConstant = Expression.Constant(null);
                Expression typedNull = Expression.Constant(null, destinationPropertyType);
                propertyInit = Expression.Condition(
                    Expression.NotEqual(Expression.Property(parentParameterExpression, foreignKeyProperty), nullConstant),
                    propertyInit,
                    typedNull
                );
            }

            return Expression.Bind(destinationProperty, propertyInit);
        }

        private static MemberAssignment GetCollectionNavigationExpression(Type sourcePropertyType, Type destinationPropertyType, Expression parameterExpression, PropertyInfo sourceProperty, MemberInfo destinationProperty, DbContext context)
        {
            // Prepare the inner lambda expression for the select statement
            sourcePropertyType = sourcePropertyType.GenericTypeArguments[0];
            destinationPropertyType = destinationPropertyType.GenericTypeArguments[0];
            var childSourceProperties = sourcePropertyType.GetProperties();
            var destinationProperties = destinationPropertyType.GetProperties().Where(dest => dest.CanWrite);
            var selectParameterExpression = Expression.Parameter(sourcePropertyType);

            var bindings = GetBindings(destinationProperties, selectParameterExpression, destinationProperty, childSourceProperties, context);
            MemberInitExpression propertyInit = Expression.MemberInit(Expression.New(destinationPropertyType), bindings);
            Type function = typeof(Func<,>).MakeGenericType(sourcePropertyType, destinationPropertyType);

            // (t) => new T() { Property = t.Property, ... }
            LambdaExpression lambda = Expression.Lambda(function, propertyInit, selectParameterExpression);
            MemberExpression collection = Expression.Property(parameterExpression, sourceProperty);

            IEnumerable<string> a = new List<string>();

            // p.Select(t => new T() { Property = t.Property, ... })
            Expression selectExpression = collection.CallSelect(lambda, destinationPropertyType);
            // Create select predicate
            return Expression.Bind(destinationProperty, selectExpression);
        }

        private static IEnumerable<MemberAssignment> GetBindings(IEnumerable<PropertyInfo> destinationProperties, Expression memberExpression, MemberInfo destinationProperty, IEnumerable<PropertyInfo> sourceProperties, DbContext context)
        {
            return destinationProperties
                    .Select(p => BuildBinding(memberExpression, p, sourceProperties, context))
                    .Where(binding => binding != null);
        }
    }
}
