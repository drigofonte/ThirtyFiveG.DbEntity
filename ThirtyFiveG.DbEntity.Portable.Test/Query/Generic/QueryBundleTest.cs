using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThirtyFiveG.DbEntity.Query.Generic;

namespace ThirtyFiveG.DbEntity.Portable.Test.Query.Generic
{
    [TestClass]
    public class QueryBundleTest
    {
        [TestMethod]
        public void AddParameter_empty_parameters()
        {
            QueryBundle<MockEntity> bundle = QueryBundle<MockEntity>.Create(1, null, (e) => true);

            string parameter = "Parameter";
            string value = "Value";
            QueryBundle<MockEntity> returned = bundle.AddParameter(parameter, value);

            Assert.AreEqual(bundle, returned);
            Assert.AreEqual(1, bundle.Parameters.Count);
            Assert.IsTrue(bundle.Parameters.Keys.Contains(typeof(string)));
            Assert.AreEqual(1, bundle.Parameters[typeof(string)].Count);
            Assert.IsTrue(bundle.Parameters[typeof(string)].Keys.Contains(parameter));
            Assert.AreEqual(value, bundle.Parameters[typeof(string)][parameter]);
        }

        [TestMethod]
        public void AddParameter_overwrite_existing()
        {
            QueryBundle<MockEntity> bundle = QueryBundle<MockEntity>.Create(1, null, (e) => true);

            string parameter = "Parameter";
            string original = "Original";
            string changed = "Changed";
            bundle.AddParameter(parameter, original);
            bundle.AddParameter(parameter, changed);

            Assert.AreEqual(1, bundle.Parameters.Count);
            Assert.IsTrue(bundle.Parameters.Keys.Contains(typeof(string)));
            Assert.AreEqual(1, bundle.Parameters[typeof(string)].Count);
            Assert.IsTrue(bundle.Parameters[typeof(string)].Keys.Contains(parameter));
            Assert.AreEqual(changed, bundle.Parameters[typeof(string)][parameter]);
        }
    }
}
