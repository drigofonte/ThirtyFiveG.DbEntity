using System;

namespace ThirtyFiveG.DbEntity.Entity
{
    public interface IDbEntityBasic
    {
        int CreatedByID { get; set; }
        bool IsDeleted { get; set; }
        DateTime LastModifiedDate { get; set; }
        DateTime RecordCreated { get; set; }
    }
}
