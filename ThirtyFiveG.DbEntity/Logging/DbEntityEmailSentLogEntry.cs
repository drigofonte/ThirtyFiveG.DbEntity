using System;

namespace ThirtyFiveG.DbEntity.Logging
{
    public class DbEntityEmailSentLogEntry : DbEntityLogEntry
    {
        #region Constructor
        public DbEntityEmailSentLogEntry(int actionId, Tuple<string, object>[] entityPrimaryKeys, Type entityType) : base(1, entityPrimaryKeys, entityType)
        {
            Action = actionId;
        }
        #endregion

        #region Public properties
        public int Action { get; set; }
        #endregion
    }
}
