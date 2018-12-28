using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThirtyFiveG.DbEntity.Validation;
using ThirtyFiveG.Validation;

namespace Grundfos.Portable.Test.Validation
{
    [TestClass]
    public class ValidationUtilitiesTest
    {
        [TestMethod]
        public void Matches_exact_property_path()
        {
            Assert.IsTrue(ValidationUtilities.Matches(".RelationalEntity.FlatProperty", ".RelationalEntity.FlatProperty"));
        }

        [TestMethod]
        public void Matches_wildcard_property_path_1()
        {
            Assert.IsTrue(ValidationUtilities.Matches(".RelationalEntity.*", ".RelationalEntity.FlatProperty"));
        }

        [TestMethod]
        public void Matches_wildcard_property_path_2()
        {
            Assert.IsTrue(ValidationUtilities.Matches(".*", ".RelationalEntity.FlatProperty"));
        }

        [TestMethod]
        public void Matches_wildcard_property_path_3()
        {
            Assert.IsTrue(ValidationUtilities.Matches(@"^.RelationalEntities\[.*\].RelationalEntity.(\w*)$", ".RelationalEntities[Guid=abc-1234-def].RelationalEntity.FlatProperty"));
        }

        [TestMethod]
        public void Matches_non_matching_property_paths()
        {
            Assert.IsFalse(ValidationUtilities.Matches(".RelationalEntity.FlatProperty1", ".RelationalEntity.FlatProperty2"));
        }

        [TestMethod]
        public void Matches_non_matching_wildcard_path_1()
        {
            Assert.IsFalse(ValidationUtilities.Matches(".RelationalEntity1.*", ".RelationalEntity2.FlatProperty"));
        }

        [TestMethod]
        public void Matches_non_matching_wildcard_path_2()
        {
            Assert.IsFalse(ValidationUtilities.Matches(@"^.RelationalEntities\[.*\].RelationalEntity.(\w*)$", ".RelationalEntities[Guid=abc-1234-def].RelationalEntity.RelationalEntity.FlatProperty"));
        }
    }
}
