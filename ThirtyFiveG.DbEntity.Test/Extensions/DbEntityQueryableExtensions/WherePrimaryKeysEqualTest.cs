using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThirtyFiveG.DbEntity.Entity;
using ThirtyFiveG.DbEntity.Extensions.Portable;
using System.Linq;
using System;

namespace ThirtyFiveG.DbEntity.Test.Extensions.DbEntityQueryableExtensions
{
    [TestClass]
    public class WherePrimaryKeysEqualTest
    {
        [TestMethod]
        public void WherePrimaryKeysEqual_single_key_equals()
        {
            Tuple<string, object>[] primaryKeys = new Tuple<string, object>[] { new Tuple<string, object>("KeyOne", 1) };
            SingleKeyEntity mockEntity1 = new SingleKeyEntity(1);
            SingleKeyEntity mockEntity2 = new SingleKeyEntity(2);

            IQueryable<SingleKeyEntity> entities = new SingleKeyEntity[] { mockEntity1, mockEntity2 }.AsQueryable();

            IQueryable queryable = entities.WherePrimaryKeysEqual(primaryKeys);

            Assert.AreEqual(1, queryable.Cast<IDbEntity>().Count());
            Assert.AreEqual(mockEntity1, queryable.Cast<IDbEntity>().Single());
        }

        [TestMethod]
        public void WherePrimaryKeysEqual_single_key_not_equals()
        {
            Tuple<string, object>[] primaryKeys = new Tuple<string, object>[] { new Tuple<string, object>("KeyOne", 3) };
            SingleKeyEntity mockEntity1 = new SingleKeyEntity(1);
            SingleKeyEntity mockEntity2 = new SingleKeyEntity(2);

            IQueryable<SingleKeyEntity> entities = new SingleKeyEntity[] { mockEntity1, mockEntity2 }.AsQueryable();

            IQueryable queryable = entities.WherePrimaryKeysEqual(primaryKeys);

            Assert.AreEqual(0, queryable.Cast<IDbEntity>().Count());
        }

        [TestMethod]
        public void WherePrimaryKeysEqual_multiple_keys_equals()
        {
            Tuple<string, object>[] primaryKeys = new Tuple<string, object>[] { new Tuple<string, object>("KeyOne", 1), new Tuple<string, object>("KeyTwo", 2) };
            DualKeyEntity mockEntity1 = new DualKeyEntity(1, 2);
            DualKeyEntity mockEntity2 = new DualKeyEntity(3, 4);

            IQueryable<DualKeyEntity> entities = new DualKeyEntity[] { mockEntity1, mockEntity2 }.AsQueryable();

            IQueryable queryable = entities.WherePrimaryKeysEqual(primaryKeys);

            Assert.AreEqual(1, queryable.Cast<IDbEntity>().Count());
            Assert.AreEqual(mockEntity1, queryable.Cast<IDbEntity>().Single());
        }

        [TestMethod]
        public void WherePrimaryKeysEqual_multiple_keys_not_equals()
        {
            Tuple<string, object>[] primaryKeys = new Tuple<string, object>[] { new Tuple<string, object>("KeyOne", 5), new Tuple<string, object>("KeyTwo", 6) };
            DualKeyEntity mockEntity1 = new DualKeyEntity(1, 2);
            DualKeyEntity mockEntity2 = new DualKeyEntity(3, 4);

            IQueryable<DualKeyEntity> entities = new DualKeyEntity[] { mockEntity1, mockEntity2 }.AsQueryable();

            IQueryable queryable = entities.WherePrimaryKeysEqual(primaryKeys);

            Assert.AreEqual(0, queryable.Cast<IDbEntity>().Count());
        }

        private class SingleKeyEntity : BaseDbEntity
        {
            public SingleKeyEntity(int keyOne)
            {
                KeyOne = keyOne;
                PrimaryKeys = new Tuple<string, object>[] { new Tuple<string, object>("KeyOne", keyOne) };
            }

            public int KeyOne { get; set; }
            protected override string[] PrimaryKeysNames { get { return new string[] { "KeyOne" }; } }
        }

        private class DualKeyEntity : BaseDbEntity
        {
            public DualKeyEntity(int keyOne, int keyTwo)
            {
                KeyOne = keyOne;
                KeyTwo = keyTwo;
                PrimaryKeys = new Tuple<string, object>[] { new Tuple<string, object>("KeyOne", keyOne), new Tuple<string, object>("KeyTwo", keyTwo) };
            }

            public int KeyOne { get; set; }
            public int KeyTwo { get; set; }
            protected override string[] PrimaryKeysNames { get { return new string[] { "KeyOne", "KeyTwo" }; } }
        }
    }
}
