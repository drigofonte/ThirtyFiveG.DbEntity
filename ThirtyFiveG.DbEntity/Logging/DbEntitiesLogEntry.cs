using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using ThirtyFiveG.Commons.Logging;

namespace ThirtyFiveG.DbEntity.Logging
{
    public class DbEntitiesLogEntry : LogEntry
    {
        public DbEntitiesLogEntry(int userId, IEnumerable<Tuple<string, object>[]> entitiesPrimaryKeys, Type entitiesType) : base(userId)
        {
            EntitiesPrimaryKeys = JsonConvert.SerializeObject(entitiesPrimaryKeys);
            EntitiesType = entitiesType.FullName;
        }

        public string EntitiesPrimaryKeys { get; private set; }
        public string EntitiesType { get; private set; }
    }
}
