using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ThirtyFiveG.Commons.Interfaces;

using U = ThirtyFiveG.DbEntity.Common.DbEntityUtilities;

namespace ThirtyFiveG.DbEntity.Portable.Test.Common.DbEntityUtilities
{
    [TestClass]
    public class GetNextWeightTest
    {
        [TestMethod]
        public void GetLastWeight()
        {
            Mock<IWeighted> mockWeighted1 = new Mock<IWeighted>();
            mockWeighted1.SetupGet(m => m.Weight).Returns(1);
            Mock<IWeighted> mockWeighted2 = new Mock<IWeighted>();
            mockWeighted2.SetupGet(m => m.Weight).Returns(2);

            int nextWeight = U.GetNextWeight(new IWeighted[] { mockWeighted1.Object, mockWeighted2.Object });

            Assert.AreEqual(3, nextWeight);
        }
    }
}
