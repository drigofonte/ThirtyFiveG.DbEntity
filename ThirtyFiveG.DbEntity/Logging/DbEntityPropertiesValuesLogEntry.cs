using System;
using System.Collections.Generic;

namespace ThirtyFiveG.DbEntity.Logging
{
    public class DbEntityPropertiesValuesLogEntry : DbEntityLogEntry
    {
        #region Constructor
        public DbEntityPropertiesValuesLogEntry(int userId, Tuple<string, object>[] entityPrimaryKeys, Type entityType) : base(userId, entityPrimaryKeys, entityType)
        {
            PropertiesValues = new Dictionary<string, object>();
        }
        #endregion

        #region Public properties
        public IDictionary<string, object> PropertiesValues { get; private set; }
        #endregion
    }
}
