using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

using U = ThirtyFiveG.DbEntity.Common.DbEntityUtilities;

namespace ThirtyFiveG.DbEntity.Portable.Test.Common.DbEntityUtilities
{
    [TestClass]
    public class PrimaryKeysEqualTest
    {
        [TestMethod]
        public void One_equal_numeric_element_returns_true()
        {
            Tuple<string, object>[] s1 = new Tuple<string, object>[] { new Tuple<string, object>("a", 1) };
            Tuple<string, object>[] s2 = new Tuple<string, object>[] { new Tuple<string, object>("a", 1) };
            Assert.IsTrue(U.PrimaryKeysEqual(s1, s2));
        }

        [TestMethod]
        public void One_different_numeric_element_returns_false()
        {
            Tuple<string, object>[] s1 = new Tuple<string, object>[] { new Tuple<string, object>("a", 1) };
            Tuple<string, object>[] s2 = new Tuple<string, object>[] { new Tuple<string, object>("a", 2) };
            Assert.IsFalse(U.PrimaryKeysEqual(s1, s2));
        }

        [TestMethod]
        public void Two_equal_numeric_elements_returns_true()
        {
            Tuple<string, object>[] s1 = new Tuple<string, object>[] { new Tuple<string, object>("a", 1), new Tuple<string, object>("b", 2) };
            Tuple<string, object>[] s2 = new Tuple<string, object>[] { new Tuple<string, object>("a", 1), new Tuple<string, object>("b", 2) };
            Assert.IsTrue(U.PrimaryKeysEqual(s1, s2));
        }

        [TestMethod]
        public void Two_different_numeric_elements_returns_false()
        {
            Tuple<string, object>[] s1 = new Tuple<string, object>[] { new Tuple<string, object>("a", 1), new Tuple<string, object>("b", 2) };
            Tuple<string, object>[] s2 = new Tuple<string, object>[] { new Tuple<string, object>("a", 3), new Tuple<string, object>("b", 4) };
            Assert.IsFalse(U.PrimaryKeysEqual(s1, s2));
        }

        [TestMethod]
        public void One_equal_string_element_returns_true()
        {
            Tuple<string, object>[] s1 = new Tuple<string, object>[] { new Tuple<string, object>("a", "b") };
            Tuple<string, object>[] s2 = new Tuple<string, object>[] { new Tuple<string, object>("a", "b") };
            Assert.IsTrue(U.PrimaryKeysEqual(s1, s2));
        }

        [TestMethod]
        public void One_different_string_element_returns_false()
        {
            Tuple<string, object>[] s1 = new Tuple<string, object>[] { new Tuple<string, object>("a", "b") };
            Tuple<string, object>[] s2 = new Tuple<string, object>[] { new Tuple<string, object>("a", "c") };
            Assert.IsFalse(U.PrimaryKeysEqual(s1, s2));
        }

        [TestMethod]
        public void Two_equal_string_elements_returns_true()
        {
            Tuple<string, object>[] s1 = new Tuple<string, object>[] { new Tuple<string, object>("a", "b"), new Tuple<string, object>("c", "d") };
            Tuple<string, object>[] s2 = new Tuple<string, object>[] { new Tuple<string, object>("a", "b"), new Tuple<string, object>("c", "d") };
            Assert.IsTrue(U.PrimaryKeysEqual(s1, s2));
        }

        [TestMethod]
        public void Two_different_string_elements_returns_false()
        {
            Tuple<string, object>[] s1 = new Tuple<string, object>[] { new Tuple<string, object>("a", "b"), new Tuple<string, object>("c", "d") };
            Tuple<string, object>[] s2 = new Tuple<string, object>[] { new Tuple<string, object>("a", "e"), new Tuple<string, object>("c", "f") };
            Assert.IsFalse(U.PrimaryKeysEqual(s1, s2));
        }

        [TestMethod]
        public void Two_equal_mixed_type_elements_returns_true()
        {
            Tuple<string, object>[] s1 = new Tuple<string, object>[] { new Tuple<string, object>("a", 1), new Tuple<string, object>("b", "c") };
            Tuple<string, object>[] s2 = new Tuple<string, object>[] { new Tuple<string, object>("a", 1), new Tuple<string, object>("b", "c") };
            Assert.IsTrue(U.PrimaryKeysEqual(s1, s2));
        }

        [TestMethod]
        public void Two_different_mixed_type_elements_returns_false()
        {
            Tuple<string, object>[] s1 = new Tuple<string, object>[] { new Tuple<string, object>("a", 1), new Tuple<string, object>("b", "c") };
            Tuple<string, object>[] s2 = new Tuple<string, object>[] { new Tuple<string, object>("a", 2), new Tuple<string, object>("b", "d") };
            Assert.IsFalse(U.PrimaryKeysEqual(s1, s2));
        }

        [TestMethod]
        public void One_element_equal_integer_and_long_returns_true()
        {
            Tuple<string, object>[] s1 = new Tuple<string, object>[] { new Tuple<string, object>("a", 1L) };
            Tuple<string, object>[] s2 = new Tuple<string, object>[] { new Tuple<string, object>("a", 1) };
            Assert.IsTrue(U.PrimaryKeysEqual(s1, s2));
        }

        [TestMethod]
        public void One_element_different_integer_and_long_returns_false()
        {
            Tuple<string, object>[] s1 = new Tuple<string, object>[] { new Tuple<string, object>("a", 1L) };
            Tuple<string, object>[] s2 = new Tuple<string, object>[] { new Tuple<string, object>("a", 2) };
            Assert.IsFalse(U.PrimaryKeysEqual(s1, s2));
        }

        [TestMethod]
        public void Two_mixed_type_elements_equal_integer_and_long_and_equal_strings_returns_true()
        {
            Tuple<string, object>[] s1 = new Tuple<string, object>[] { new Tuple<string, object>("a", 1L), new Tuple<string, object>("b", "c") };
            Tuple<string, object>[] s2 = new Tuple<string, object>[] { new Tuple<string, object>("a", 1), new Tuple<string, object>("b", "c") };
            Assert.IsTrue(U.PrimaryKeysEqual(s1, s2));
        }

        [TestMethod]
        public void Two_mixed_type_elements_different_integer_and_long_and_different_strings_returns_false()
        {
            Tuple<string, object>[] s1 = new Tuple<string, object>[] { new Tuple<string, object>("a", 1L), new Tuple<string, object>("b", "c") };
            Tuple<string, object>[] s2 = new Tuple<string, object>[] { new Tuple<string, object>("a", 2), new Tuple<string, object>("b", "d") };
            Assert.IsFalse(U.PrimaryKeysEqual(s1, s2));
        }

        [TestMethod]
        public void Different_number_of_elements_returns_false()
        {
            Tuple<string, object>[] s1 = new Tuple<string, object>[] { new Tuple<string, object>("a", 1L) };
            Tuple<string, object>[] s2 = new Tuple<string, object>[] { new Tuple<string, object>("a", 1), new Tuple<string, object>("b", "c") };
            Assert.IsFalse(U.PrimaryKeysEqual(s1, s2));
        }

        [TestMethod]
        public void Same_number_of_elements_different_keys_returns_false()
        {
            Tuple<string, object>[] s1 = new Tuple<string, object>[] { new Tuple<string, object>("a", 1L), new Tuple<string, object>("b", "c") };
            Tuple<string, object>[] s2 = new Tuple<string, object>[] { new Tuple<string, object>("c", 1), new Tuple<string, object>("d", "c") };
            Assert.IsFalse(U.PrimaryKeysEqual(s1, s2));
        }
    }
}
