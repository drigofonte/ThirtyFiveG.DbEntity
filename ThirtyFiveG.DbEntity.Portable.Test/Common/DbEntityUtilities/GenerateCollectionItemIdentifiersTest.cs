using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

using U = ThirtyFiveG.DbEntity.Common.DbEntityUtilities;

namespace ThirtyFiveG.DbEntity.Portable.Test.Common.DbEntityUtilities
{
    [TestClass]
    public class GenerateCollectionItemIdentifiersTest
    {
        [TestMethod]
        public void GenerateCollectionItemIdentifiers_empty_primary_keys()
        {
            Tuple<string, object>[] primaryKeys = new Tuple<string, object>[] { };
            string guid = Guid.NewGuid().ToString();
            string idendifiers = U.GenerateCollectionItemIdentifiers(primaryKeys, guid);

            Assert.AreEqual("Guid=" + guid, idendifiers);
        }

        [TestMethod]
        public void GenerateCollectionItemIdentifiers_null_primary_key()
        {
            Tuple<string, object>[] primaryKeys = new Tuple<string, object>[] { new Tuple<string, object>("NullKey", null) };
            string guid = Guid.NewGuid().ToString();
            string idendifiers = U.GenerateCollectionItemIdentifiers(primaryKeys, guid);

            Assert.AreEqual("Guid=" + guid, idendifiers);
        }

        [TestMethod]
        public void GenerateCollectionItemIdentifiers_zero_primary_key()
        {
            Tuple<string, object>[] primaryKeys = new Tuple<string, object>[] { new Tuple<string, object>("ZeroKey", 0) };
            string guid = Guid.NewGuid().ToString();
            string identifiers = U.GenerateCollectionItemIdentifiers(primaryKeys, guid);

            Assert.AreEqual("Guid=" + guid, identifiers);
        }

        [TestMethod]
        public void GenerateCollectionItemIdentifiers_null_and_non_null_primary_key()
        {
            Tuple<string, object> nonNullKey = new Tuple<string, object>("NonNullKey", 1);
            Tuple<string, object>[] primaryKeys = new Tuple<string, object>[] { new Tuple<string, object>("NullKey", null), nonNullKey };
            string guid = Guid.NewGuid().ToString();
            string identifiers = U.GenerateCollectionItemIdentifiers(primaryKeys, guid);

            Assert.AreEqual("Guid=" + guid + "," + nonNullKey.Item1 + "=" + nonNullKey.Item2, identifiers);
        }

        [TestMethod]
        public void GenerateCollectionItemIdentifiers_zero_and_non_zero_primary_key()
        {
            Tuple<string, object> nonZeroKey = new Tuple<string, object>("NonZeroKey", 1);
            Tuple<string, object>[] primaryKeys = new Tuple<string, object>[] { new Tuple<string, object>("ZeroKey", 0), nonZeroKey };
            string guid = Guid.NewGuid().ToString();
            string identifiers = U.GenerateCollectionItemIdentifiers(primaryKeys, guid);

            Assert.AreEqual("Guid=" + guid + "," + nonZeroKey.Item1 + "=" + nonZeroKey.Item2, identifiers);
        }

        [TestMethod]
        public void GenerateCollectionItemIdentifiers_non_null_non_zero_primary_keys()
        {
            Tuple<string, object> nonZeroKey = new Tuple<string, object>("NonZeroKey", 1);
            Tuple<string, object>[] primaryKeys = new Tuple<string, object>[] { nonZeroKey };
            string guid = Guid.NewGuid().ToString();
            string identifiers = U.GenerateCollectionItemIdentifiers(primaryKeys, guid);

            Assert.AreEqual(nonZeroKey.Item1 + "=" + nonZeroKey.Item2, identifiers);
        }
    }
}
