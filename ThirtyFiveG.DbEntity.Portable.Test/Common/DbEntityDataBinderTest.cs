using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using ThirtyFiveG.DbEntity.Common;

namespace ThirtyFiveG.DbEntity.Portable.Test.Common
{
    [TestClass]
    public class DbEntityDataBinderTest
    {
        [TestMethod]
        public void Eval_null_entity_return_null()
        {
            MockEntity entity = new MockEntity();
            string path = ".RelationalEntity1";

            DbEntityDataBinder.BinderResult result = DbEntityDataBinder.Eval(path, entity);

            Assert.IsNull(result.Entity);
            Assert.AreEqual(path, result.Path);
            Assert.AreEqual(path, result.ActualPath);
        }

        [TestMethod]
        public void Eval_root_path_return_root_entity()
        {
            MockEntity entity = new MockEntity();
            string path = ".";

            DbEntityDataBinder.BinderResult result = DbEntityDataBinder.Eval(path, entity);

            Assert.AreEqual(entity, result.Entity);
            Assert.AreEqual(path, result.Path);
            Assert.AreEqual(path, result.ActualPath);
        }

        [TestMethod]
        public void Eval_one_degree_separation_relation_return_entity()
        {
            MockEntity relationalEntity = new MockEntity();
            MockEntity entity = new MockEntity() { RelationalEntity1 = relationalEntity };
            string path = ".RelationalEntity1";

            DbEntityDataBinder.BinderResult result = DbEntityDataBinder.Eval(path, entity);

            Assert.AreEqual(relationalEntity, result.Entity);
            Assert.AreEqual(path, result.Path);
            Assert.AreEqual(path, result.ActualPath);
        }

        [TestMethod]
        public void Eval_two_degrees_separation_relation_return_entity()
        {
            MockEntity relationalEntity = new MockEntity() { RelationalEntity1 = new MockEntity() };
            MockEntity entity = new MockEntity() { RelationalEntity1 = relationalEntity };
            string path = ".RelationalEntity1.RelationalEntity1";

            DbEntityDataBinder.BinderResult result = DbEntityDataBinder.Eval(path, entity);

            Assert.AreEqual(relationalEntity.RelationalEntity1, result.Entity);
            Assert.AreEqual(path, result.Path);
            Assert.AreEqual(path, result.ActualPath);
        }

        [TestMethod]
        public void Eval_one_degree_separation_collection_property_indexers_return_entity()
        {
            int entity1Id = 1;
            int entity2Id = 2;
            MockAssociativeEntity associativeEntity = new MockAssociativeEntity() { RelationalEntity1Id = entity1Id, RelationalEntity2Id = entity2Id };
            MockEntity entity = new MockEntity() { id = entity1Id, AssociativeEntities = new HashSet<MockAssociativeEntity>() { associativeEntity } };
            string path = ".AssociativeEntities[Guid=" + associativeEntity.Guid + ",RelationalEntity1Id=" + entity1Id + ",RelationalEntity2Id=" + entity2Id + "]";

            DbEntityDataBinder.BinderResult result = DbEntityDataBinder.Eval(path, entity);

            Assert.AreEqual(associativeEntity, result.Entity);
            Assert.AreEqual(path, result.Path);
            Assert.AreEqual(".AssociativeEntities[RelationalEntity1Id=" + entity1Id + ",RelationalEntity2Id=" + entity2Id + "]", result.ActualPath);
        }

        [TestMethod]
        public void Eval_one_degree_separation_collection_one_degree_separation_entity_property_indexers_return_entity()
        {
            int entity1Id = 1;
            int entity2Id = 2;
            MockAssociativeEntity associativeEntity = new MockAssociativeEntity() { RelationalEntity1Id = entity1Id, RelationalEntity2Id = entity2Id, RelationalEntity2 = new MockEntity() { id = entity2Id } };
            MockEntity entity = new MockEntity() { id = entity1Id, AssociativeEntities = new HashSet<MockAssociativeEntity>() { associativeEntity } };
            string path = ".AssociativeEntities[Guid=" + associativeEntity.Guid + ",RelationalEntity1Id=" + entity1Id + ",RelationalEntity2Id=" + entity2Id + "].RelationalEntity2";

            DbEntityDataBinder.BinderResult result = DbEntityDataBinder.Eval(path, entity);

            Assert.AreEqual(associativeEntity.RelationalEntity2, result.Entity);
            Assert.AreEqual(path, result.Path);
            Assert.AreEqual(".AssociativeEntities[RelationalEntity1Id=" + entity1Id + ",RelationalEntity2Id=" + entity2Id + "].RelationalEntity2", result.ActualPath);
        }
    }
}
