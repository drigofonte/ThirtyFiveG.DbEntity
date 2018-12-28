using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ThirtyFiveG.DbEntity.Logging
{
    public class DbEntitiesPersistedLogEntry : DbEntitiesLogEntry
    {
        #region Constructor
        public DbEntitiesPersistedLogEntry(string entities, int actionId, string actionLabel, IEnumerable<IEnumerable<string>> propertiesToUpdate, int userId, IEnumerable<Tuple<string, object>[]> entitiesPrimaryKeys, Type entitiesType) : base(userId, entitiesPrimaryKeys, entitiesType)
        {
            DbEntityActionID = actionId; //(int)action;
            DbEntityAction = actionLabel; //DbEntityActionLabels.ResourceManager.GetString(action.ToString());
            PropertiesToUpdate = JsonConvert.SerializeObject(propertiesToUpdate);
            Entities = entities;
        }
        #endregion

        #region Public properties
        public int DbEntityActionID { get; set; }
        public string DbEntityAction { get; set; }
        public string PropertiesToUpdate { get; set; }
        public string Entities { get; set; }
        #endregion
    }
}
