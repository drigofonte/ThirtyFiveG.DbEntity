using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

using U = ThirtyFiveG.DbEntity.Common.DbEntityUtilities;

namespace ThirtyFiveG.DbEntity.Portable.Test.Common.DbEntityUtilities
{
    [TestClass]
    public class GenerateCollectionItemPathTest
    {
        [TestMethod]
        public void GenerateCollectionItemPath()
        {
            string path = ".Collection";
            Tuple<string, object>[] primaryKeys = new Tuple<string, object>[] { };
            string guid = Guid.NewGuid().ToString();
            string collectionItemPath = U.GenerateCollectionItemPath(path, primaryKeys, guid);

            Assert.IsTrue(collectionItemPath.StartsWith(".Collection["));
            Assert.IsTrue(collectionItemPath.EndsWith("]."));
        }
    }
}
