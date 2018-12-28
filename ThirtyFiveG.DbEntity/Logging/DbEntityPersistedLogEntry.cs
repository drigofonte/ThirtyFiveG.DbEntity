using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ThirtyFiveG.DbEntity.Logging
{
    public class DbEntityPersistedLogEntry : DbEntityLogEntry
    {
        #region Constructor
        public DbEntityPersistedLogEntry(string entity, int actionId, string actionLabel, IEnumerable<string> propertiesToUpdate, int userId, Tuple<string, object>[] entityPrimaryKeys, Type entityType) : base(userId, entityPrimaryKeys, entityType)
        {
            DbEntityActionID = actionId;
            DbEntityAction = actionLabel;
            PropertiesToUpdate = JsonConvert.SerializeObject(propertiesToUpdate);
            Entity = entity;
        }
        #endregion

        #region Public properties
        public int DbEntityActionID { get; set; }
        public string DbEntityAction { get; set; }
        public string PropertiesToUpdate { get; set; }
        public string Entity { get; set; }
        #endregion
    }
}
