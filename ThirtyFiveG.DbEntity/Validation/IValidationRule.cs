using System;
using ThirtyFiveG.DbEntity.Entity;
using ThirtyFiveG.Validation;

namespace ThirtyFiveG.DbEntity.Validation
{ 
    public interface IValidationRule
    {
        string PropertyName { get; }
        string Message { get; }
        Func<IDbEntity, bool> Matches { get; }
        ValidationResultType Type { get; }
        string GetPropertyPath(string entityPath);
        IValidationResult AsResult(string entityPath);
    }
}
