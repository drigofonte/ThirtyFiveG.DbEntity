using System;

namespace ThirtyFiveG.DbEntity.Entity
{
    public class WrapperEntity<TEntity> : BaseDbEntity, IDbEntityBasic
        where TEntity : class, IDbEntity, IDbEntityBasic
    {
        #region Public properties
        public TEntity Entity { get; set; }
        public int id { get; set; }
        public int CreatedByID
        {
            get { return Entity != null ? Entity.CreatedByID : 0; }
            set { Entity.CreatedByID = value; }
        }
        public bool IsDeleted
        {
            get { return Entity != null ? Entity.IsDeleted : false; }
            set { Entity.IsDeleted = value; }
        }
        public DateTime LastModifiedDate
        {
            get { return Entity != null ? Entity.LastModifiedDate : default(DateTime); }
            set { Entity.LastModifiedDate = value; }
        }
        public DateTime RecordCreated
        {
            get { return Entity != null ? Entity.RecordCreated : default(DateTime); }
            set { Entity.RecordCreated = value; }
        }
        #endregion
    }
}
