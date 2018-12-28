using Newtonsoft.Json;
using System;
using ThirtyFiveG.Commons.Logging;

namespace ThirtyFiveG.DbEntity.Logging
{
    public class DbEntityLogEntry : LogEntry
    {
        public DbEntityLogEntry(int userId, Tuple<string, object>[] entityPrimaryKeys, Type entityType) : base(userId)
        {
            EntityPrimaryKeys = JsonConvert.SerializeObject(entityPrimaryKeys);
            EntityType = entityType.FullName;
        }

        public string EntityPrimaryKeys { get; private set; }
        public string EntityType { get; private set; }
    }
}
