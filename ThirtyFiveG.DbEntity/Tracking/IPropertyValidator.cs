using System;
using System.Collections.Generic;
using ThirtyFiveG.Commons.Event;
using ThirtyFiveG.DbEntity.Validation;
using ThirtyFiveG.Validation;

namespace ThirtyFiveG.DbEntity.Tracking
{
    public interface IPropertyValidator
    {
        event EventHandler<DataEventArgs<IEnumerable<IValidationResult>>> Validated;

        IDictionary<string, ICollection<IValidationRule>> ValidationRules { get; }
        void AddValidationRulesFilters(ISet<string> filters);
        void ClearValidationRulesFilters();
        void Validate(PropertyChange change, bool isIDbEntity);
        void Validate(string propertyPath);
        void Validate(IEnumerable<string> propertyPaths);
        IEnumerable<IValidationResult> Validate(IEnumerable<IValidationResult> results);
    }
}
