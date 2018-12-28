using System;

namespace ThirtyFiveG.DbEntity.Logging
{
    public class DbEntityPropertyChangedLogEntry : DbEntityLogEntry
    {
        public DbEntityPropertyChangedLogEntry(int userId, Tuple<string, object>[] entityPrimaryKeys, Type entityType, string propertyPath) : base(userId, entityPrimaryKeys, entityType)
        {
            PropertyPath = propertyPath;
        }

        public string PropertyPath { get; private set; }
    }
}
