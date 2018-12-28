using System;
using System.Collections.Generic;
using ThirtyFiveG.DbEntity.Entity;
using ThirtyFiveG.DbEntity.Validation;

namespace ThirtyFiveG.DbEntity.Portable.Test
{
    public partial class MockEntity : BaseDbEntity, IDbEntityBasic
    {
        private ISet<IValidationRule> _validationRules;

        public MockEntity() : this(new HashSet<IValidationRule>(), System.Guid.NewGuid().ToString()) { }
        public MockEntity(ISet<IValidationRule> validationRules) : this(validationRules, System.Guid.NewGuid().ToString()) { }
        public MockEntity(ISet<IValidationRule> validationRules, string guid) : base(guid)
        {
            AssociativeEntities = new HashSet<MockAssociativeEntity>();
            RelationalEntities = new HashSet<MockEntity>();
            _validationRules = validationRules;
        }

        public int id { get; set; }
        public string StringProperty { get; set; }
        public string StringProperty2 { get; set; }
        public int IntProperty { get; set; }
        public double DoubleProperty { get; set; }
        public float FloatProperty { get; set; }
        public long LongProperty { get; set; }
        public bool BoolProperty { get; set; }
        public int CreatedByID { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public DateTime RecordCreated { get; set; }

        public virtual MockEntity RelationalEntity1 { get; set; }
        public virtual MockEntity RelationalEntity2 { get; set; }
        public virtual ICollection<MockEntity> RelationalEntities { get; set; }
        public virtual ICollection<MockAssociativeEntity> AssociativeEntities { get; set; }

        public override IEnumerable<IValidationRule> GetValidationRules()
        {
            return _validationRules;
        }
    }
}
