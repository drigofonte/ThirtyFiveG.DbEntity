using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using ThirtyFiveG.Commons.Extensions;
using ThirtyFiveG.DbEntity.Common;
using ThirtyFiveG.DbEntity.Tracking;

namespace ThirtyFiveG.DbEntity.Portable.Test.Common
{
    [TestClass]
    public class DbEntityJsonConvertTest
    {
        [TestMethod]
        public void SerializeEntity_new_entity_flat_property_changes()
        {
            MockEntity entity = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.IntProperty = 1;

            string json = DbEntityJsonConvert.SerializeEntity(entity, tracker.DbEntityChanges());

            MockEntity deserialised = TypeExtensions.DeserializeObject<MockEntity>(json);

            Assert.AreEqual("{\"$id\":\"" + entity.Guid + "\",\"Guid\":\"" + entity.Guid + "\",\"IntProperty\":1}", json);
            Assert.AreEqual(entity.Guid, deserialised.Guid);
            Assert.AreEqual(entity.IntProperty, deserialised.IntProperty);
        }

        [TestMethod]
        public void SerializeEntity_persisted_entity_flat_property_changes()
        {
            MockEntity entity = new MockEntity() { id = 1 };
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.IntProperty = 1;

            string json = DbEntityJsonConvert.SerializeEntity(entity, tracker.DbEntityChanges());

            MockEntity deserialised = TypeExtensions.DeserializeObject<MockEntity>(json);

            Assert.AreEqual("{\"$id\":\"" + entity.Guid + "\",\"id\":" + entity.id + ",\"IntProperty\":1}", json);
            Assert.AreEqual(entity.id, deserialised.id);
            Assert.AreEqual(entity.IntProperty, deserialised.IntProperty);
        }

        [TestMethod]
        public void SerializeEntity_new_entity_relational_property_changes()
        {
            MockEntity entity = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.RelationalEntity1 = new MockEntity();
            entity.RelationalEntity1.StringProperty = "Changed";

            string json = DbEntityJsonConvert.SerializeEntity(entity, tracker.DbEntityChanges());

            MockEntity deserialised = TypeExtensions.DeserializeObject<MockEntity>(json);

            Assert.AreEqual("{\"$id\":\"" + entity.Guid + "\",\"Guid\":\"" + entity.Guid + "\",\"RelationalEntity1\":{\"$id\":\"" + entity.RelationalEntity1.Guid + "\",\"Guid\":\"" + entity.RelationalEntity1.Guid + "\",\"StringProperty\":\"" + entity.RelationalEntity1.StringProperty + "\"}}", json);
            Assert.AreEqual(entity.Guid, deserialised.Guid);
            Assert.AreEqual(entity.RelationalEntity1.Guid, deserialised.RelationalEntity1.Guid);
            Assert.AreEqual(entity.RelationalEntity1.StringProperty, deserialised.RelationalEntity1.StringProperty);
        }

        [TestMethod]
        public void SerializeEntity_new_entity_relational_collection_property_changes()
        {
            MockEntity entity = new MockEntity();
            MockAssociativeEntity associativeEntity1 = new MockAssociativeEntity();
            MockAssociativeEntity associativeEntity2 = new MockAssociativeEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            associativeEntity1.IntProperty = 1;
            entity.AssociativeEntities.Add(associativeEntity1);
            associativeEntity1.BoolProperty = true;
            entity.AssociativeEntities.Add(associativeEntity2);
            associativeEntity2.IntProperty = 1;

            string json = DbEntityJsonConvert.SerializeEntity(entity, tracker.DbEntityChanges());

            MockEntity deserialised = TypeExtensions.DeserializeObject<MockEntity>(json);

            Assert.AreEqual("{\"$id\":\"" + entity.Guid + "\",\"Guid\":\"" + entity.Guid + "\",\"AssociativeEntities\":[{\"$id\":\"" + associativeEntity1.Guid + "\",\"Guid\":\"" + associativeEntity1.Guid + "\",\"BoolProperty\":" + associativeEntity1.BoolProperty.ToString().ToLower() + "},{\"$id\":\"" + associativeEntity2.Guid + "\",\"Guid\":\"" + associativeEntity2.Guid + "\",\"IntProperty\":" + associativeEntity2.IntProperty + "}]}", json);
            Assert.AreEqual(entity.Guid, deserialised.Guid);
            Assert.AreEqual(associativeEntity1.Guid, deserialised.AssociativeEntities.First().Guid);
            Assert.AreEqual(associativeEntity1.BoolProperty, deserialised.AssociativeEntities.First().BoolProperty);
            Assert.AreEqual(associativeEntity2.Guid, deserialised.AssociativeEntities.Last().Guid);
            Assert.AreEqual(associativeEntity2.IntProperty, deserialised.AssociativeEntities.Last().IntProperty);
        }

        [TestMethod]
        public void SerializeEntity_existing_entity_relational_collection_property_changes()
        {
            MockEntity entity = new MockEntity();
            MockAssociativeEntity associativeEntity1 = new MockAssociativeEntity();
            MockAssociativeEntity associativeEntity2 = new MockAssociativeEntity();
            entity.AssociativeEntities.Add(associativeEntity1);
            entity.AssociativeEntities.Add(associativeEntity2);
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            associativeEntity1.BoolProperty = true;
            associativeEntity2.IntProperty = 1;

            string json = DbEntityJsonConvert.SerializeEntity(entity, tracker.DbEntityChanges());

            MockEntity deserialised = TypeExtensions.DeserializeObject<MockEntity>(json);

            Assert.AreEqual("{\"$id\":\"" + entity.Guid + "\",\"Guid\":\"" + entity.Guid + "\",\"AssociativeEntities\":[{\"$id\":\"" + associativeEntity1.Guid + "\",\"Guid\":\"" + associativeEntity1.Guid + "\",\"BoolProperty\":" + associativeEntity1.BoolProperty.ToString().ToLower() + "},{\"$id\":\"" + associativeEntity2.Guid + "\",\"Guid\":\"" + associativeEntity2.Guid + "\",\"IntProperty\":" + associativeEntity2.IntProperty + "}]}", json);
            Assert.AreEqual(entity.Guid, deserialised.Guid);
            Assert.AreEqual(associativeEntity1.Guid, deserialised.AssociativeEntities.First().Guid);
            Assert.AreEqual(associativeEntity1.BoolProperty, deserialised.AssociativeEntities.First().BoolProperty);
            Assert.AreEqual(associativeEntity2.Guid, deserialised.AssociativeEntities.Last().Guid);
            Assert.AreEqual(associativeEntity2.IntProperty, deserialised.AssociativeEntities.Last().IntProperty);
        }

        [TestMethod]
        public void SerializeEntity_persisted_entity_multi_hop_relational_collection_property_changes()
        {
            MockEntity entity = new MockEntity();
            MockAssociativeEntity associativeEntity = new MockAssociativeEntity();
            MockEntity relationalEntity1 = new MockEntity();
            associativeEntity.RelationalEntity2 = relationalEntity1;
            relationalEntity1.RelationalEntity1 = new MockEntity();
            MockEntity relationalEntity2 = new MockEntity();
            relationalEntity1.RelationalEntity1.RelationalEntities.Add(relationalEntity2);
            entity.AssociativeEntities.Add(associativeEntity);
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            relationalEntity2.IntProperty = 1;

            string json = DbEntityJsonConvert.SerializeEntity(entity, tracker.DbEntityChanges());

            Assert.AreEqual("{\"$id\":\"" + entity.Guid + "\",\"Guid\":\"" + entity.Guid + "\",\"AssociativeEntities\":[{\"$id\":\"" + associativeEntity.Guid + "\",\"Guid\":\"" + associativeEntity.Guid + "\",\"RelationalEntity2\":{\"$id\":\"" + relationalEntity1.Guid + "\",\"Guid\":\"" + relationalEntity1.Guid + "\",\"RelationalEntity1\":{\"$id\":\"" + relationalEntity1.RelationalEntity1.Guid + "\",\"Guid\":\"" + relationalEntity1.RelationalEntity1.Guid + "\",\"RelationalEntities\":[{\"$id\":\"" + relationalEntity2.Guid + "\",\"Guid\":\"" + relationalEntity2.Guid + "\",\"IntProperty\":" + relationalEntity2.IntProperty + "}]}}}]}", json);
        }

        [TestMethod]
        public void SerializeEntity_existing_entity_relational_property_changes()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity = new MockEntity();
            MockAssociativeEntity associativeEntity = new MockAssociativeEntity() { RelationalEntity2 = relationalEntity };
            entity.AssociativeEntities.Add(associativeEntity);
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            associativeEntity.RelationalEntity2.StringProperty = "Changed";

            string json = DbEntityJsonConvert.SerializeEntity(entity, tracker.DbEntityChanges());

            MockEntity deserialised = TypeExtensions.DeserializeObject<MockEntity>(json);

            Assert.AreEqual("{\"$id\":\"" + entity.Guid + "\",\"Guid\":\"" + entity.Guid + "\",\"AssociativeEntities\":[{\"$id\":\"" + associativeEntity.Guid + "\",\"Guid\":\"" + associativeEntity.Guid + "\",\"RelationalEntity2\":{\"$id\":\"" + relationalEntity.Guid + "\",\"Guid\":\"" + relationalEntity.Guid + "\",\"StringProperty\":\"" + relationalEntity.StringProperty + "\"}}]}", json);
            Assert.AreEqual(entity.Guid, deserialised.Guid);
            Assert.AreEqual(associativeEntity.Guid, deserialised.AssociativeEntities.First().Guid);
            Assert.AreEqual(associativeEntity.RelationalEntity2.StringProperty, deserialised.AssociativeEntities.First().RelationalEntity2.StringProperty);
        }

        [TestMethod]
        public void SerializeEntity_new_entity_relational_property_empty_changes_not_serialized_to_json()
        {
            MockEntity entity = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            MockEntity relationalEntity = new MockEntity();
            entity.RelationalEntity1 = relationalEntity;

            string json = DbEntityJsonConvert.SerializeEntity(entity, tracker.DbEntityChanges());

            Assert.AreEqual("{\"$id\":\"" + entity.Guid + "\",\"Guid\":\"" + entity.Guid + "\",\"RelationalEntity1\":{\"$id\":\"" + relationalEntity.Guid + "\",\"Guid\":\"" + relationalEntity.Guid + "\"}}", json);
        }

        [TestMethod]
        public void SerializeEntity_new_entity_empty_relational_collection_changes()
        {
            MockEntity entity = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            MockAssociativeEntity associativeEntity = new MockAssociativeEntity();
            entity.AssociativeEntities.Add(associativeEntity);

            string json = DbEntityJsonConvert.SerializeEntity(entity, tracker.DbEntityChanges());

            Assert.AreEqual("{\"$id\":\"" + entity.Guid + "\",\"Guid\":\"" + entity.Guid + "\",\"AssociativeEntities\":[{\"$id\":\"" + associativeEntity.Guid + "\",\"Guid\":\"" + associativeEntity.Guid + "\"}]}", json);
        }

        [TestMethod]
        public void SerializeEntity_linker_entity_serialise_keys_only_once()
        {
            MockEntity entity = new MockEntity() { id = 1 };
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            MockAssociativeEntity associativeEntity = new MockAssociativeEntity();
            entity.AssociativeEntities.Add(associativeEntity);
            associativeEntity.RelationalEntity1Id = entity.id;
            associativeEntity.RelationalEntity2Id = 2;

            string json = DbEntityJsonConvert.SerializeEntity(entity, tracker.DbEntityChanges());

            Assert.AreEqual("{\"$id\":\"" + entity.Guid + "\",\"id\":" + entity.id + ",\"AssociativeEntities\":[{\"$id\":\"" + associativeEntity.Guid + "\",\"RelationalEntity1Id\":" + associativeEntity.RelationalEntity1Id + ",\"RelationalEntity2Id\":" + associativeEntity.RelationalEntity2Id + "}]}", json);
        }

        [TestMethod]
        public void SerializeEntity_null_value()
        {
            MockEntity entity = new MockEntity() { id = 1, StringProperty = "Original" };
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.StringProperty = null;

            string json = DbEntityJsonConvert.SerializeEntity(entity, tracker.DbEntityChanges());

            Assert.AreEqual("{\"$id\":\"" + entity.Guid + "\",\"id\":" + entity.id + ",\"StringProperty\":null}", json);
        }

        [TestMethod]
        public void SerializeEntity_null_relational_entity()
        {
            MockEntity entity = new MockEntity() { RelationalEntity1 = new MockEntity(), StringProperty = "Original" };
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.StringProperty = null;
            entity.RelationalEntity1 = null;

            string json = DbEntityJsonConvert.SerializeEntity(entity, tracker.DbEntityChanges());

            Assert.AreEqual("{\"$id\":\"" + entity.Guid + "\",\"Guid\":\"" + entity.Guid + "\",\"StringProperty\":null,\"RelationalEntity1\":null}", json);
        }

        [TestMethod]
        public void SerializeEntity_relational_entity_and_foreign_key_change_same_prefix()
        {
            MockEntity entity = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            MockEntity relationalEntity = new MockEntity() { id = 1 };
            relationalEntity.MarkPersisted();
            entity.RelationalEntity1 = relationalEntity;
            entity.IntProperty = relationalEntity.id;

            string json = DbEntityJsonConvert.SerializeEntity(entity, tracker.DbEntityChanges());

            Assert.AreEqual("{\"$id\":\"" + entity.Guid + "\",\"Guid\":\"" + entity.Guid + "\",\"RelationalEntity1\":{\"$id\":\"" + relationalEntity.Guid + "\",\"id\":" + relationalEntity.id + "},\"IntProperty\":" + relationalEntity.id + "}", json);
        }

        [TestMethod]
        public void SerializeEntity_entity_references()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity1 = new MockEntity();
            MockEntity relationalEntity2 = new MockEntity();
            entity.RelationalEntities.Add(relationalEntity1);
            entity.RelationalEntities.Add(relationalEntity2);
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            MockEntity relationalEntity3 = new MockEntity();
            relationalEntity1.RelationalEntities.Add(relationalEntity3);
            relationalEntity2.RelationalEntities.Add(relationalEntity3);
            relationalEntity3.IntProperty = 1;

            string json = DbEntityJsonConvert.SerializeEntity(entity, tracker.DbEntityChanges());

            Assert.AreEqual("{\"$id\":\"" + entity.Guid + "\",\"Guid\":\"" + entity.Guid + "\",\"RelationalEntities\":[{\"$id\":\"" + relationalEntity1.Guid + "\",\"Guid\":\"" + relationalEntity1.Guid + "\",\"RelationalEntities\":[{\"$id\":\"" + relationalEntity3.Guid + "\",\"Guid\":\"" + relationalEntity3.Guid + "\",\"IntProperty\":" + relationalEntity3.IntProperty + "}]},{\"$id\":\"" + relationalEntity2.Guid + "\",\"Guid\":\"" + relationalEntity2.Guid + "\",\"RelationalEntities\":[{\"$ref\":\"" + relationalEntity3.Guid + "\"}]}]}", json);
        }

        [TestMethod]
        public void SerializePrimaryKeys_non_zero()
        {
            MockAssociativeEntity entity = new MockAssociativeEntity() { RelationalEntity1Id = 1, RelationalEntity2Id = 2 };

            string json = DbEntityJsonConvert.SerializePrimaryKeys(entity);

            Assert.AreEqual("{\"$id\":\"" + entity.Guid + "\",\"RelationalEntity1Id\":" + entity.RelationalEntity1Id + ",\"RelationalEntity2Id\":" + entity.RelationalEntity2Id + "}", json);
        }

        [TestMethod]
        public void SerializePrimaryKeys_zero()
        {
            MockAssociativeEntity entity = new MockAssociativeEntity() { RelationalEntity1Id = 0, RelationalEntity2Id = 0 };

            string json = DbEntityJsonConvert.SerializePrimaryKeys(entity);

            Assert.AreEqual("{\"$id\":\"" + entity.Guid + "\",\"Guid\":\"" + entity.Guid + "\"}", json);
        }

        [TestMethod]
        public void SerializePrimaryKeys_zero_and_non_zero()
        {
            MockAssociativeEntity entity = new MockAssociativeEntity() { RelationalEntity1Id = 1, RelationalEntity2Id = 0 };

            string json = DbEntityJsonConvert.SerializePrimaryKeys(entity);

            Assert.AreEqual("{\"$id\":\"" + entity.Guid + "\",\"Guid\":\"" + entity.Guid + "\",\"RelationalEntity1Id\":" + entity.RelationalEntity1Id + "}", json);
        }
    }
}
