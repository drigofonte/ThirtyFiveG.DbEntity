using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using ThirtyFiveG.DbEntity.Common;

namespace ThirtyFiveG.DbEntity.Portable.Test.Common
{
    [TestClass]
    public class DbEntitiesComparerTest
    {
        [TestMethod]
        public void Equals_persisted_entities_true()
        {
            MockEntity e1 = new MockEntity() { id = 1 };
            e1.MarkPersisted();
            MockEntity e2 = new MockEntity() { id = 1 };
            e2.MarkPersisted();

            IEqualityComparer<MockEntity> comparer = new DbEntitiesComparer<MockEntity>();

            Assert.IsTrue(comparer.Equals(e1, e2));
        }

        [TestMethod]
        public void Equals_persisted_entities_false()
        {
            MockEntity e1 = new MockEntity() { id = 1 };
            e1.MarkPersisted();
            MockEntity e2 = new MockEntity() { id = 2 };
            e2.MarkPersisted();

            IEqualityComparer<MockEntity> comparer = new DbEntitiesComparer<MockEntity>();

            Assert.IsFalse(comparer.Equals(e1, e2));
        }

        [TestMethod]
        public void Equals_new_entities_true()
        {
            string guid = Guid.NewGuid().ToString();
            MockEntity e1 = new MockEntity() { id = 0, Guid = guid };
            MockEntity e2 = new MockEntity() { id = 0, Guid = guid };

            IEqualityComparer<MockEntity> comparer = new DbEntitiesComparer<MockEntity>();

            Assert.IsTrue(comparer.Equals(e1, e2));
        }

        [TestMethod]
        public void Equals_new_entities_false()
        {
            MockEntity e1 = new MockEntity() { id = 0, Guid = Guid.NewGuid().ToString() };
            MockEntity e2 = new MockEntity() { id = 0, Guid = Guid.NewGuid().ToString() };

            IEqualityComparer<MockEntity> comparer = new DbEntitiesComparer<MockEntity>();

            Assert.IsFalse(comparer.Equals(e1, e2));
        }

        [TestMethod]
        public void Equals_persisted_new_false()
        {
            MockEntity e1 = new MockEntity() { id = 1 };
            MockEntity e2 = new MockEntity() { id = 1 };
            e2.MarkPersisted();

            IEqualityComparer<MockEntity> comparer = new DbEntitiesComparer<MockEntity>();

            Assert.IsFalse(comparer.Equals(e2, e1));
        }

        [TestMethod]
        public void Equals_new_persisted_true()
        {
            MockEntity e1 = new MockEntity() { id = 1 };
            MockEntity e2 = new MockEntity() { id = 1 };
            e2.MarkPersisted();

            IEqualityComparer<MockEntity> comparer = new DbEntitiesComparer<MockEntity>();

            Assert.IsFalse(comparer.Equals(e1, e2));
        }

        [TestMethod]
        public void GetHashCode_persisted_entities_differ()
        {
            MockEntity e1 = new MockEntity() { id = 1 };
            e1.MarkPersisted();
            MockEntity e2 = new MockEntity() { id = 2 };
            e2.MarkPersisted();

            IEqualityComparer<MockEntity> comparer = new DbEntitiesComparer<MockEntity>();

            Assert.AreNotEqual(comparer.GetHashCode(e1), comparer.GetHashCode(e2));
        }

        [TestMethod]
        public void GetHashCode_persisted_entities_equal()
        {
            MockEntity e1 = new MockEntity() { id = 1 };
            e1.MarkPersisted();
            MockEntity e2 = new MockEntity() { id = 1 };
            e2.MarkPersisted();

            IEqualityComparer<MockEntity> comparer = new DbEntitiesComparer<MockEntity>();

            Assert.AreEqual(comparer.GetHashCode(e1), comparer.GetHashCode(e2));
        }

        [TestMethod]
        public void GetHashCode_new_entities_differ()
        {
            MockEntity e1 = new MockEntity() { id = 0, Guid = Guid.NewGuid().ToString() };
            MockEntity e2 = new MockEntity() { id = 0, Guid = Guid.NewGuid().ToString() };

            IEqualityComparer<MockEntity> comparer = new DbEntitiesComparer<MockEntity>();

            Assert.AreNotEqual(comparer.GetHashCode(e1), comparer.GetHashCode(e2));
        }

        [TestMethod]
        public void GetHashCode_new_entities_equal()
        {
            string guid = Guid.NewGuid().ToString();
            MockEntity e1 = new MockEntity() { id = 0, Guid = guid };
            MockEntity e2 = new MockEntity() { id = 0, Guid = guid };

            IEqualityComparer<MockEntity> comparer = new DbEntitiesComparer<MockEntity>();

            Assert.AreEqual(comparer.GetHashCode(e1), comparer.GetHashCode(e2));
        }
    }
}
