using System;
using ThirtyFiveG.DbEntity.Entity;
using ThirtyFiveG.Validation;

namespace ThirtyFiveG.DbEntity.Validation
{
    public class ValidationRule : IValidationRule
    {
        #region Constructor
        public ValidationRule(string propertyName, string message, Func<IDbEntity, bool> matches, ValidationResultType type)
        {
            PropertyName = propertyName;
            Message = message;
            Matches = matches;
            Type = type;
        }
        #endregion

        #region Public properties
        public string PropertyName { get; private set; }
        public string Message { get; private set; }
        public Func<IDbEntity, bool> Matches { get; protected set; }
        public ValidationResultType Type { get; private set; }
        #endregion

        #region Public methods
        public string GetPropertyPath(string entityPath)
        {
            return entityPath.EndsWith(PropertyName) ? entityPath : entityPath + "." + PropertyName;
        }

        public IValidationResult AsResult(string entityPath)
        {
            return new ValidationResult(PropertyName, GetPropertyPath(entityPath), Message, Type);
        }
        #endregion
    }
}
