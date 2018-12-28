using System;
using System.Collections.Generic;
using ThirtyFiveG.DbEntity.Entity;

namespace ThirtyFiveG.DbEntity.Tracking
{
    public interface IPropertyChangeTracker
    {
        void Undo();
        void Undo(int index);
        void Undo(PropertyChange lastChange, bool inclusive = true);
        void UndoAll();
        void Redo();
        void RedoAll();
        bool Untracked(Action<IDbEntity> a);
        string Grouped(Action<IDbEntity> a);
        string DbEntityAsJson(IDbEntity root = null, long utcTimestamp = long.MaxValue);
        void Start();
        void Stop();
        IEnumerable<PropertyChange> AllChanges();
        IEnumerable<PropertyChange> Changes(IDbEntity root = null);
        IEnumerable<string> DbEntityChanges(bool flattenNewEntityChanges = false, IDbEntity root = null, long utcTimestamp = long.MaxValue);
        void PurgeChanges(IDbEntity root = null, long utcTimestamp = long.MaxValue);
        bool IsDeleted(string propertyPath, out IDbEntity entity);
    }
}
