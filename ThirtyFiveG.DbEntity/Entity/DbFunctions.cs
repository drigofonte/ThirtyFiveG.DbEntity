using System;

namespace ThirtyFiveG.DbEntity.Entity
{
    public static class DbFunctions
    {
        [System.Data.Entity.DbFunction("Edm", "AddHours")]
        public static DateTime? AddHours(DateTime? dateTime, int? hours)
        {
            if (!dateTime.HasValue || !hours.HasValue)
                return null;

            return dateTime.Value.AddHours(hours.Value);
        }
    }
}
