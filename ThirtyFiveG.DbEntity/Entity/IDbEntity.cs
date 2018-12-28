using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using ThirtyFiveG.Commons.Event;
using ThirtyFiveG.DbEntity.Event;
using ThirtyFiveG.DbEntity.Projection.Entity;
using ThirtyFiveG.DbEntity.Query;
using ThirtyFiveG.DbEntity.Tracking;
using ThirtyFiveG.DbEntity.Validation;
using ThirtyFiveG.Validation;

namespace ThirtyFiveG.DbEntity.Entity
{
    public interface IDbEntity : INotifyPropertyChanged, IDisposable
    {
        event EventHandler DbEntityDeleted;
        event EventHandler DbEntityUndeleted;
        event EventHandler DbEntityValidated;
        event EventHandler Pushing;
        event EventHandler<DataEventArgs<IEnumerable<string>>> Pushed;
        event EventHandler Undo;
        event EventHandler Editing;
        event EventHandler<DataEventArgs<IEnumerable<IValidationResult>>> Validated;
        event EventHandler<DbEntityPropertyChangedEventArgs> DbEntityPropertyChanged;
        event EventHandler Focus;
        event EventHandler Blur;

        ISet<Action<IDbEntity>> BeforePushActions { get; }
        bool CanPush { get; }
        double EditDuration { get; }
        IEnumerable<DbEntityActionMessage> EntityActions { get; set; }
        string Guid { get; set; }
        bool HasChanges { get; }
        bool IsEditing { get; }
        bool IsEditable { get; set; }
        bool IsNew { get; }
        bool IsPersisted { get; }
        bool IsPushing { get; }
        bool IsTracked { get; set; }
        bool IsTrackingChanges { get; set; }
        Tuple<string, object>[] PrimaryKeys { get; set; }
        EntityState State { get; }
        IPropertyChangeTracker Tracker { get; }
        ISet<IValidationResult> ValidationResults { get; }
        //ISet<IValidationRule> ValidationRules { get; }

        bool Untracked(Action<IDbEntity> a);
        string Grouped(Action<IDbEntity> a);
        void DiscardEdit();
        string ChangesAsJson(IPropertyChangeTracker tracker, long utcTimestamp = long.MaxValue);
        string ChangesAsJson();
        IEnumerable<string> DbEntityChanges(IPropertyChangeTracker tracker, long utcTimestamp = long.MaxValue);
        IEnumerable<string> DbEntityChanges();
        IEnumerable<PropertyChange> PropertyChanges(IPropertyChangeTracker tracker);
        IEnumerable<PropertyChange> PropertyChanges();
        void PurgeChanges(IPropertyChangeTracker tracker, long utcTimestamp = long.MaxValue);
        void PurgeChanges();
        void UndoEdit(PropertyChange lastChange, bool inclusive = true);
        void UndoEdit();
        void UndoAllEdit();
        void BeginEdit();
        void BeginEdit<TProjection>() where TProjection : class, IDbEntityProjection;
        void EndEdit();
        Task<IEnumerable<string>> PushAsync(IDataAccessLayer dal, int action, int commentId);
        Task<IEnumerable<string>> PushAsync(IPropertyChangeTracker tracker, IDataAccessLayer dal, int action, int commentId);
        void MarkPersisted();
        bool Matches(string filter);
        void Validate(string propertyPath);
        void Validate(IEnumerable<string> propertyPaths);
        void AddValidationRulesFilters(ISet<string> filters);
        void ClearValidationRulesFilters();
        void OnFocus();
        void OnBlur();
        IEnumerable<IValidationRule> GetValidationRules();
    }
}
