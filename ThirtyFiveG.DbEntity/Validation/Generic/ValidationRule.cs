using System;
using ThirtyFiveG.DbEntity.Entity;
using ThirtyFiveG.Validation;

namespace ThirtyFiveG.DbEntity.Validation.Generic
{
    public class ValidationRule<TEntity> : ValidationRule
        where TEntity : class, IDbEntity
    {
        #region Private variables
        private readonly Func<TEntity, bool> _matches;
        #endregion

        #region Constructor
        public ValidationRule(string propertyName, string message, Func<TEntity, bool> matches, ValidationResultType type) : base(propertyName, message, null, type)
        {
            _matches = matches;
            Matches = (e) => e != null && e.GetType().Equals(typeof(TEntity)) && _matches(e as TEntity);
        }
        #endregion

        #region Public static methods
        public static IValidationRule NonNullValueRule(string propertyName, string message, Func<TEntity, object> value, ValidationResultType type)
        {
            return new ValidationRule<TEntity>(propertyName, message, (e) => value(e) == null, type);
        }

        public static IValidationRule NonNullValueRule(string propertyName, string message, Func<TEntity, DateTime?> value, ValidationResultType type)
        {
            return new ValidationRule<TEntity>(propertyName, message, (e) => !value(e).HasValue || value(e).Value.Equals(default(DateTime)), type);
        }

        public static IValidationRule NonNullValueRule(string propertyName, string message, Func<TEntity, int?> value, ValidationResultType type)
        {
            return new ValidationRule<TEntity>(propertyName, message, (e) => !value(e).HasValue, type);
        }

        public static IValidationRule NonZeroValueRule(string propertyName, string message, Func<TEntity, int?> value, ValidationResultType type)
        {
            return new ValidationRule<TEntity>(propertyName, message, (e) => value(e).GetValueOrDefault(0) == 0, type);
        }

        public static IValidationRule NonZeroValueRule(string propertyName, string message, Func<TEntity, double?> value, ValidationResultType type)
        {
            return new ValidationRule<TEntity>(propertyName, message, (e) => value(e).GetValueOrDefault(0) == 0, type);
        }

        public static IValidationRule NonZeroValueRule(string propertyName, string message, Func<TEntity, int> value, ValidationResultType type)
        {
            return new ValidationRule<TEntity>(propertyName, message, (e) => value(e) == 0, type);
        }

        public static IValidationRule NonZeroValueRule(string propertyName, string message, Func<TEntity, decimal> value, ValidationResultType type)
        {
            return new ValidationRule<TEntity>(propertyName, message, (e) => value(e) == 0, type);
        }

        public static IValidationRule NonNullOrEmptyString(string propertyName, string message, Func<TEntity, string> value, ValidationResultType type)
        {
            return new ValidationRule<TEntity>(propertyName, message, (e) => string.IsNullOrEmpty(value(e)), type);
        }

        public static IValidationRule NonDefaultValue<TValue>(string propertyName, string message, Func<TEntity, TValue> value, ValidationResultType type)
        {
            return new ValidationRule<TEntity>(propertyName, message, (e) => default(TValue).Equals(value(e)), type);
        }
        #endregion
    }
}
