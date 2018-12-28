using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using ThirtyFiveG.DbEntity.Entity;
using ThirtyFiveG.DbEntity.Tracking;

namespace ThirtyFiveG.DbEntity.Portable.Test.Tracking
{
    [TestClass]
    public class PropertyChangeTest
    {
        [TestMethod]
        public void Revert_flat_property()
        {
            string before = "1";
            string after = "2";
            MockEntity entity = new MockEntity();
            entity.StringProperty = after;
            PropertyChange change = new PropertyChange(".", "StringProperty", string.Empty, before, after, entity.State);
            change.Revert(entity);
            Assert.AreEqual(before, entity.StringProperty);
        }

        [TestMethod]
        public void Revert_flat_property_relational_entity()
        {
            string before = null;
            string after = "Changed";
            MockEntity entity = new MockEntity();
            entity.RelationalEntity1 = new MockEntity() { StringProperty = after };
            PropertyChange change = new PropertyChange(".RelationalEntity1", "StringProperty", string.Empty, before, after, entity.State);
            change.Revert(entity);
            Assert.AreEqual(before, entity.RelationalEntity1.StringProperty);
        }

        [TestMethod]
        public void Apply_flat_property()
        {
            string before = "1";
            string after = "2";
            MockEntity entity = new MockEntity();
            entity.StringProperty = before;
            PropertyChange change = new PropertyChange(".", "StringProperty", string.Empty, before, after, entity.State);
            change.Apply(entity);
            Assert.AreEqual(after, entity.StringProperty);
        }

        [TestMethod]
        public void Apply_flat_property_relational_property()
        {
            string before = null;
            string after = "Changed";
            MockEntity entity = new MockEntity();
            entity.RelationalEntity1 = new MockEntity() { StringProperty = before };
            PropertyChange change = new PropertyChange(".RelationalEntity1", "StringProperty", string.Empty, before, after, entity.State);
            change.Apply(entity);
            Assert.AreEqual(after, entity.RelationalEntity1.StringProperty);
        }

        [TestMethod]
        public void Revert_collection_property_added()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity = new MockEntity();
            entity.RelationalEntities.Add(relationalEntity);
            string entityPath = ".RelationalEntities[Guid=" + relationalEntity.Guid + "]";
            PropertyChange change = new PropertyChange(entityPath, string.Empty, string.Empty, null, relationalEntity);
            change.Revert(entity);

            Assert.AreEqual(0, entity.RelationalEntities.Count);
        }

        [TestMethod]
        public void Revert_collection_property_removed()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity = new MockEntity();
            string entityPath = ".RelationalEntities[Guid=" + relationalEntity.Guid + "]";
            PropertyChange change = new PropertyChange(entityPath, string.Empty, string.Empty, relationalEntity, null);
            change.Revert(entity);

            Assert.AreEqual(1, entity.RelationalEntities.Count);
            Assert.AreEqual(relationalEntity, entity.RelationalEntities.Single());
        }

        [TestMethod]
        public void Apply_collection_property_added()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity = new MockEntity();
            string entityPath = ".RelationalEntities[Guid=" + relationalEntity.Guid + "]";
            PropertyChange change = new PropertyChange(entityPath, string.Empty, string.Empty, null, relationalEntity);
            change.Apply(entity);

            Assert.AreEqual(1, entity.RelationalEntities.Count);
            Assert.AreEqual(relationalEntity, entity.RelationalEntities.Single());
        }

        [TestMethod]
        public void Apply_collection_property_removed()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity = new MockEntity();
            entity.RelationalEntities.Add(relationalEntity);
            string entityPath = ".RelationalEntities[Guid=" + relationalEntity.Guid + "]";
            PropertyChange change = new PropertyChange(entityPath, string.Empty, string.Empty, relationalEntity, null);
            change.Apply(entity);

            Assert.AreEqual(0, entity.RelationalEntities.Count);
        }

        [TestMethod]
        public void IsOrphan_many_to_one_relation_property_false()
        {
            string after = "Site Name";
            MockEntity entity = new MockEntity();
            entity.RelationalEntity1 = new MockEntity() { StringProperty = after };
            PropertyChange change = new PropertyChange(".RelationalEntity1", "StringProperty", entity.RelationalEntity1.Guid, null, after, entity.State);
            Assert.IsFalse(change.IsOrphan(entity));
        }

        [TestMethod]
        public void IsOrphan_many_to_one_relation_property_true()
        {
            string after = "Site Name";
            MockEntity entity = new MockEntity();
            entity.RelationalEntity1 = null;
            PropertyChange change = new PropertyChange(".RelationalEntity1", "StringProperty", null, after, false, entity.State);
            Assert.IsTrue(change.IsOrphan(entity));
        }

        [TestMethod]
        public void IsOrphan_many_to_one_relation_property_different_guid_true()
        {
            string after = "Site Name";
            MockEntity entity = new MockEntity();
            entity.RelationalEntity1 = new MockEntity();
            PropertyChange change = new PropertyChange(".RelationalEntity1", "StringProperty", Guid.NewGuid().ToString(), null, after, entity.State);
            Assert.IsTrue(change.IsOrphan(entity));
        }

        [TestMethod]
        public void IsOrphan_one_to_many_relation_property_false()
        {
            string after = "true";
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity = new MockEntity() { StringProperty = after };
            entity.RelationalEntities.Add(relationalEntity);
            string entityPath = ".RelationalEntities[Guid=" + relationalEntity.Guid + "]";
            string propertyName = "StringProperty";
            PropertyChange change = new PropertyChange(entityPath, propertyName, relationalEntity.Guid, null, after);
            Assert.IsFalse(change.IsOrphan(entity));
        }

        [TestMethod]
        public void IsOrphan_one_to_many_relation_property_true()
        {
            string after = "true";
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity = new MockEntity() { StringProperty = after };
            string entityPath = ".RelationalEntities[Guid=" + relationalEntity.Guid + "]";
            string propertyName = "StringProperty";
            PropertyChange change = new PropertyChange(entityPath, propertyName, string.Empty, null, after);
            Assert.IsTrue(change.IsOrphan(entity));
        }

        [TestMethod]
        public void IsOrphan_many_to_one_many_to_one_relation_entity_false()
        {
            string after = "Address Line 1";
            MockEntity entity = new MockEntity();
            entity.RelationalEntity1 = new MockEntity() { RelationalEntity1 = new MockEntity() { StringProperty = after } };
            PropertyChange change = new PropertyChange(".RelationalEntity1.RelationalEntity1", "StringProperty", entity.RelationalEntity1.RelationalEntity1.Guid, null, after, entity.State);
            Assert.IsFalse(change.IsOrphan(entity));
        }

        [TestMethod]
        public void IsOrphan_many_to_one_many_to_one_relation_entity_true()
        {
            string after = "Address Line 1";
            MockEntity entity = new MockEntity();
            entity.RelationalEntity1 = null;
            PropertyChange change = new PropertyChange(".RelationalEntity1.RelationalEntity1", "StringProperty", null, null, after, entity.State);
            Assert.IsTrue(change.IsOrphan(entity));
        }

        [TestMethod]
        public void IsOrphan_one_to_many_many_to_one_relation_entity_false()
        {
            MockEntity entity = new MockEntity();
            MockEntity manyToOneRelation = new MockEntity();
            MockEntity oneToManyRelation = new MockEntity() { RelationalEntity1 = manyToOneRelation };
            entity.RelationalEntities.Add(oneToManyRelation);
            string entityPath = ".RelationalEntities[Guid=" + oneToManyRelation.Guid + "].RelationalEntity1";
            PropertyChange change = new PropertyChange(entityPath, string.Empty, manyToOneRelation.Guid, null, manyToOneRelation);
            Assert.IsFalse(change.IsOrphan(entity));
        }

        [TestMethod]
        public void IsOrphan_one_to_many_many_to_one_relation_entity_true()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity = new MockEntity();
            string entityPath = ".RelationalEntities[Guid=" + relationalEntity.Guid + "].Contact";
            PropertyChange change = new PropertyChange(entityPath, string.Empty, string.Empty, null, new MockEntity());
            Assert.IsTrue(change.IsOrphan(entity));
        }

        [TestMethod]
        public void IsOrphan_one_to_many_relation_entity_false()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity = new MockEntity();
            entity.RelationalEntities.Add(relationalEntity);
            string entityPath = ".RelationalEntities[Guid=" + relationalEntity.Guid + "]";
            PropertyChange change = new PropertyChange(entityPath, string.Empty, relationalEntity.Guid, null, relationalEntity);
            Assert.IsFalse(change.IsOrphan(entity));
        }

        [TestMethod]
        public void IsOrphan_one_to_many_relation_entity_true()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity = new MockEntity();
            string entityPath = ".RelationalEntities[Guid=" + relationalEntity.Guid + "]";
            PropertyChange change = new PropertyChange(entityPath, string.Empty, string.Empty, relationalEntity, null);
            Assert.IsTrue(change.IsOrphan(entity));
        }

        [TestMethod]
        public void EntityPath()
        {
            PropertyChange change = new PropertyChange(".", "StringProperty", string.Empty, 0, 1, EntityState.New);
            Assert.AreEqual(".", change.EntityPath);
        }

        [TestMethod]
        public void EntityPath_many_to_one_navigation_entity()
        {
            PropertyChange change = new PropertyChange(".RelationalEntity1", "StringProperty", string.Empty, 0, 1, EntityState.New);
            Assert.AreEqual(".RelationalEntity1", change.EntityPath);
        }

        [TestMethod]
        public void PropertyPath()
        {
            PropertyChange change = new PropertyChange(".RelationalEntity1", "StringProperty", string.Empty, string.Empty, "Changed", EntityState.New);
            Assert.AreEqual(".RelationalEntity1.StringProperty", change.PropertyPath);
        }

        [TestMethod]
        public void PropertyPath_root_entity()
        {
            PropertyChange change = new PropertyChange(".", "StringProperty", string.Empty, string.Empty, "Changed", EntityState.New);
            Assert.AreEqual(".StringProperty", change.PropertyPath);
        }

        [TestMethod]
        public void PropertyPath_entity_path_dot_suffix()
        {
            PropertyChange change = new PropertyChange(".RelationalEntity1.", "StringProperty", string.Empty, string.Empty, "Changed", EntityState.New);
            Assert.AreEqual(".RelationalEntity1.StringProperty", change.PropertyPath);
        }

        [TestMethod]
        public void DbEntityPropertyPath_non_collection_entity()
        {
            PropertyChange change = new PropertyChange(".RelationalEntity1", "StringProperty", string.Empty, string.Empty, "Changed", EntityState.New);
            string path = change.DbEntityPropertyPath(new MockEntity() { RelationalEntity1 = new MockEntity() { StringProperty = "Changed" } });
            Assert.AreEqual(".RelationalEntity1.StringProperty", path);
        }

        [TestMethod]
        public void DbEntityPropertyPath_collection_entity_zero_or_null_primary_keys()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity = new MockEntity();
            entity.RelationalEntities.Add(relationalEntity);
            string entityPath = ".RelationalEntities[Guid=" + relationalEntity.Guid + "]";
            string propertyName = "StringProperty";
            PropertyChange change = new PropertyChange(entityPath, propertyName, string.Empty, string.Empty, false);
            string path = change.DbEntityPropertyPath(entity);
            Assert.AreEqual(entityPath + "." + propertyName, path);
        }

        [TestMethod]
        public void DbEntityPropertyPath_multiple_collection_entitities_zero_or_null_primary_keys()
        {
            MockEntity manyToOneRelation = new MockEntity();
            MockEntity oneToManyRelation = new MockEntity();
            manyToOneRelation.RelationalEntities.Add(oneToManyRelation);
            MockEntity manyToManyRelation = new MockEntity() { RelationalEntity1 = manyToOneRelation };
            MockEntity entity = new MockEntity();
            entity.RelationalEntities.Add(manyToManyRelation);
            string entityPath = ".RelationalEntities[Guid=" + manyToManyRelation.Guid + "].RelationalEntity1.RelationalEntities[Guid=" + oneToManyRelation.Guid + "]";
            string propertyName = "StringProperty";
            PropertyChange change = new PropertyChange(entityPath, propertyName, string.Empty, 2, 3);
            string path = change.DbEntityPropertyPath(entity);
            Assert.AreEqual(entityPath + "." + propertyName, path);
        }

        [TestMethod]
        public void DbEntityPropertyPath_collection_entity_non_zero_non_null_primary_keys()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity = new MockEntity() { id = 1 };
            entity.RelationalEntities.Add(relationalEntity);
            string entityPath = ".RelationalEntities[Guid=" + relationalEntity.Guid + "]";
            string propertyName = "StringProperty";
            PropertyChange change = new PropertyChange(entityPath, propertyName, string.Empty, false, true);
            string path = change.DbEntityPropertyPath(entity);
            Assert.AreEqual(".RelationalEntities[id=1]." + propertyName, path);
        }

        [TestMethod]
        public void DbEntityPropertyPath_multiple_collection_entitities_non_zero_non_null_primary_keys()
        {
            MockEntity manyToOneRelation = new MockEntity();
            MockEntity oneToManyRelation = new MockEntity() { id = 1 };
            manyToOneRelation.RelationalEntities.Add(oneToManyRelation);
            MockEntity manyToManyRelation = new MockEntity() { id = 2, RelationalEntity1 = manyToOneRelation };
            MockEntity entity = new MockEntity();
            entity.RelationalEntities.Add(manyToManyRelation);
            string entityPath = ".RelationalEntities[Guid=" + manyToManyRelation.Guid + "].RelationalEntity1.RelationalEntities[Guid=" + oneToManyRelation.Guid + "]";
            string propertyName = "StringProperty";
            PropertyChange change = new PropertyChange(entityPath, propertyName, string.Empty, 2, 3);
            string path = change.DbEntityPropertyPath(entity);
            Assert.AreEqual(".RelationalEntities[id=2].RelationalEntity1.RelationalEntities[id=1]." + propertyName, path);
        }

        [TestMethod]
        public void DbEntityPropertyPath_multiple_collection_entitities_mixed_zero_non_zero_primary_keys()
        {
            MockEntity manyToOneRelation = new MockEntity();
            MockEntity oneToManyRelation = new MockEntity();
            manyToOneRelation.RelationalEntities.Add(oneToManyRelation);
            MockEntity manyToManyRelation = new MockEntity() { id = 2, RelationalEntity1 = manyToOneRelation };
            MockEntity entity = new MockEntity();
            entity.RelationalEntities.Add(manyToManyRelation);
            string entityPath = ".RelationalEntities[Guid=" + manyToManyRelation.Guid + "].RelationalEntity1.RelationalEntities[Guid=" + oneToManyRelation.Guid + "]";
            string propertyName = "StringProperty";
            PropertyChange change = new PropertyChange(entityPath, propertyName, string.Empty, 2, 3);
            string path = change.DbEntityPropertyPath(entity);
            Assert.AreEqual(".RelationalEntities[id=2].RelationalEntity1.RelationalEntities[Guid="+oneToManyRelation.Guid+"]." + propertyName, path);
        }

        [TestMethod]
        public void DbEntityPropertyPath_entity_path_no_property_path()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity = new MockEntity();
            entity.RelationalEntities.Add(relationalEntity);
            string entityPath = ".RelationalEntities[Guid=" + relationalEntity.Guid + "]";
            PropertyChange change = new PropertyChange(entityPath, string.Empty, string.Empty, null, null);
            Assert.AreEqual(entityPath, change.DbEntityPropertyPath(entity));
        }

        [TestMethod]
        public void DbEntityPropertyPath_entity_path_no_property_path_2()
        {
            MockEntity entity = new MockEntity();
            MockEntity manyToManyEntity = new MockEntity();
            MockEntity oneToOneEntity = new MockEntity();
            manyToManyEntity.RelationalEntities.Add(oneToOneEntity);
            MockEntity associativeEntity = new MockEntity() { RelationalEntity1 = manyToManyEntity };
            entity.RelationalEntities.Add(associativeEntity);
            string entityPath = ".RelationalEntities[Guid=" + associativeEntity.Guid + "].RelationalEntity1.RelationalEntities[Guid=" + oneToOneEntity.Guid + "]";
            PropertyChange change = new PropertyChange(entityPath, string.Empty, string.Empty, null, null);
            Assert.AreEqual(entityPath, change.DbEntityPropertyPath(entity));
        }

        [TestMethod]
        public void DbEntityPropertyPath_entity_path_no_property_path_3()
        {
            MockEntity entity1 = new MockEntity() { id = 1 };
            entity1.MarkPersisted();
            MockEntity entity2 = new MockEntity() { id = 2 };
            entity2.MarkPersisted();
            MockAssociativeEntity associativeEntity = new MockAssociativeEntity() { RelationalEntity1Id = entity1.id, RelationalEntity2Id = entity2.id, RelationalEntity2 = entity2 };
            associativeEntity.MarkPersisted();
            entity1.AssociativeEntities.Add(associativeEntity);
            MockEntity entity3 = new MockEntity();
            entity2.RelationalEntity1 = entity3;
            MockEntity entity4 = new MockEntity() { id = 3, StringProperty = "3" };
            entity3.RelationalEntities.Add(entity4);
            string entityPath = ".AssociativeEntities[Guid=" + associativeEntity.Guid + "].RelationalEntity2.RelationalEntity1.RelationalEntities[Guid=" + entity4.Guid + "]";
            string expectedEntityPath = ".AssociativeEntities[RelationalEntity1Id=" + entity1.id + ",RelationalEntity2Id=" + entity2.id + "].RelationalEntity2.RelationalEntity1.RelationalEntities[id=" + entity4.id + "]";
            PropertyChange change = new PropertyChange(entityPath, string.Empty, string.Empty, null, null);
            Assert.AreEqual(expectedEntityPath, change.DbEntityPropertyPath(entity1));
        }

        [TestMethod]
        public void Destroy()
        {
            PropertyChange change = new PropertyChange(".", "StringProperty", string.Empty, 1, 2, EntityState.New);
            change.Destroy();
            Assert.IsNull(change.Before);
            Assert.IsNull(change.After);
            Assert.IsNull(change.EntityPath);
            Assert.IsNull(change.PropertyName);
        }
    }
}
