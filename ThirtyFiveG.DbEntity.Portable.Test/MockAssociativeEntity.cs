using System;
using ThirtyFiveG.DbEntity.Entity;

namespace ThirtyFiveG.DbEntity.Portable.Test
{
    public class MockAssociativeEntity : BaseDbEntity, IDbEntityBasic
    {
        #region Constructor
        public MockAssociativeEntity() : this(System.Guid.NewGuid().ToString()) { }
        public MockAssociativeEntity(string guid) : base(guid) { }
        #endregion

        #region Public properties
        public int RelationalEntity1Id { get; set; }
        public int RelationalEntity2Id { get; set; }
        public int IntProperty { get; set; }
        public bool BoolProperty { get; set; }
        public int CreatedByID { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public DateTime RecordCreated { get; set; }

        public virtual MockEntity RelationalEntity1 { get; set; }
        public virtual MockEntity RelationalEntity2 { get; set; }
        #endregion

        #region Overrides
        protected override string[] PrimaryKeysNames { get { return new string[] { "RelationalEntity1Id", "RelationalEntity2Id" }; } }
        #endregion
    }
}
