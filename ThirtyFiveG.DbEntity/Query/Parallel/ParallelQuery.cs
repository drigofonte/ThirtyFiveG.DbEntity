using System;
using System.Linq.Expressions;
using System.Reflection;

namespace ThirtyFiveG.DbEntity.Query.Parallel
{
    public class ParallelQuery
    {
        public ParallelQuery(Type sourceType, Type targetType, Expression where, LambdaExpression select, PropertyInfo property, bool isCollection)
        {
            SourceType = sourceType;
            TargetType = targetType;
            Where = where;
            Select = select;
            Property = property;
            IsCollection = isCollection;
        }

        public Expression Where { get; set; }
        public LambdaExpression Select { get; private set; }
        public Type SourceType { get; private set; }
        public Type TargetType { get; private set; }
        public PropertyInfo Property { get; set; }
        public bool IsCollection { get; private set; }
    }
}
