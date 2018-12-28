using System;
using System.Collections.Generic;

namespace ThirtyFiveG.DbEntity.Query
{
    public class EntityActionsResult
    {
        public Tuple<string, object>[] PrimaryKeys { get; set; }
        public IEnumerable<DbEntityActionMessage> EntityActions { get; set; } 
    }
}
