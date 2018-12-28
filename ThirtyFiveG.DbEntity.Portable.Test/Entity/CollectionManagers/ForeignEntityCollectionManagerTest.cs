using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using ThirtyFiveG.DbEntity.Entity;
using ThirtyFiveG.DbEntity.Entity.CollectionManagers;

namespace ThirtyFiveG.DbEntity.Portable.Test.Entity.CollectionManagers
{
    [TestClass]
    public class ForeignEntityCollectionManagerTest
    {
        [TestMethod]
        public void Add_new_entity()
        {
            int userId = 1;
            bool isSetForeignKeysActionCalled = false;
            MockEntity entity = new MockEntity();

            Action<IDbEntity, MockEntity> setForeignKeys = (o, e) => { isSetForeignKeysActionCalled = true; };
            ForeignEntityCollectionManager<MockEntity> manager = new ForeignEntityCollectionManager<MockEntity>(userId, entity, () => entity.RelationalEntities, setForeignKeys);

            MockEntity added = manager.Add();

            Assert.AreEqual(1, entity.RelationalEntities.Count);
            Assert.AreEqual(added, entity.RelationalEntities.First());
            Assert.IsTrue(isSetForeignKeysActionCalled);
            Assert.IsFalse(added.IsDeleted);
            Assert.AreEqual(userId, added.CreatedByID);
        }

        [TestMethod]
        public void Add_new_entity_2()
        {
            int userId = 1;
            bool isSetForeignKeysActionCalled = false;
            MockEntity entity = new MockEntity();

            Action<IDbEntity, MockEntity> setForeignKeys = (o, e) => { isSetForeignKeysActionCalled = true; };
            ForeignEntityCollectionManager<MockEntity> manager = new ForeignEntityCollectionManager<MockEntity>(userId, entity, () => entity.RelationalEntities, setForeignKeys);

            MockEntity newEntity = new MockEntity();
            MockEntity added = manager.Add(newEntity);

            Assert.AreEqual(1, entity.RelationalEntities.Count);
            Assert.AreEqual(newEntity, entity.RelationalEntities.First());
            Assert.AreEqual(added, newEntity);
            Assert.IsTrue(isSetForeignKeysActionCalled);
            Assert.IsFalse(added.IsDeleted);
            Assert.AreEqual(userId, added.CreatedByID);
        }

        [TestMethod]
        public void ReAdd_previously_deleted_entity()
        {
            int userId = 1;
            bool isSetForeignKeysActionCalled = false;
            MockEntity entity = new MockEntity();

            Action<IDbEntity, MockEntity> setForeignKeys = (o, e) => { isSetForeignKeysActionCalled = true; };
            ForeignEntityCollectionManager<MockEntity> manager = new ForeignEntityCollectionManager<MockEntity>(userId, entity, () => entity.RelationalEntities, setForeignKeys);

            MockEntity existing = new MockEntity() { id = 2, IsDeleted = true, CreatedByID = 3 };
            existing.MarkPersisted();
            entity.RelationalEntities.Add(existing);
            MockEntity added = manager.Add(existing);

            Assert.AreEqual(1, entity.RelationalEntities.Count);
            Assert.AreEqual(added, existing);
            Assert.IsFalse(isSetForeignKeysActionCalled);
            Assert.IsFalse(added.IsDeleted);
            Assert.AreNotEqual(userId, added.CreatedByID);
        }

        [TestMethod]
        public void Remove_new_entity()
        {
            int userId = 1;
            MockEntity newEntity = new MockEntity();
            MockEntity entity = new MockEntity();
            entity.RelationalEntities.Add(newEntity);

            ForeignEntityCollectionManager<MockEntity> manager = new ForeignEntityCollectionManager<MockEntity>(userId, entity, () => entity.RelationalEntities, null);

            MockEntity removed = manager.Remove(newEntity);

            Assert.AreEqual(0, entity.RelationalEntities.Count);
            Assert.AreEqual(newEntity, removed);
        }

        [TestMethod]
        public void Remove_persisted_entity()
        {
            int userId = 1;
            MockEntity persistedEntity = new MockEntity() { id = int.MaxValue };
            persistedEntity.MarkPersisted();
            MockEntity entity = new MockEntity();
            entity.RelationalEntities.Add(persistedEntity);

            ForeignEntityCollectionManager<MockEntity> manager = new ForeignEntityCollectionManager<MockEntity>(userId, entity, () => entity.RelationalEntities, null);

            MockEntity removed = manager.Remove(persistedEntity);

            Assert.AreEqual(1, entity.RelationalEntities.Count);
            Assert.AreEqual(persistedEntity, removed);
            Assert.IsTrue(removed.IsDeleted);
        }

        [TestMethod]
        public void Contains_new_entity_true()
        {
            int userId = 1;
            MockEntity persistedEntity = new MockEntity();
            MockEntity entity = new MockEntity();
            entity.RelationalEntities.Add(persistedEntity);

            ForeignEntityCollectionManager<MockEntity> manager = new ForeignEntityCollectionManager<MockEntity>(userId, entity, () => entity.RelationalEntities, null);

            Assert.IsTrue(manager.Contains(persistedEntity));
        }

        [TestMethod]
        public void Contains_new_entity_false()
        {
            int userId = 1;
            MockEntity persistedEntity = new MockEntity();
            MockEntity entity = new MockEntity();
            entity.RelationalEntities.Add(persistedEntity);

            ForeignEntityCollectionManager<MockEntity> manager = new ForeignEntityCollectionManager<MockEntity>(userId, entity, () => entity.RelationalEntities, null);

            Assert.IsFalse(manager.Contains(new MockEntity()));
        }

        [TestMethod]
        public void Contains_persisted_entity_true()
        {
            int userId = 1;
            MockEntity persistedEntity = new MockEntity() { id = int.MaxValue };
            persistedEntity.MarkPersisted();
            MockEntity entity = new MockEntity();
            entity.RelationalEntities.Add(persistedEntity);

            ForeignEntityCollectionManager<MockEntity > manager = new ForeignEntityCollectionManager<MockEntity>(userId, entity, () => entity.RelationalEntities, null);

            Assert.IsTrue(manager.Contains(persistedEntity));
        }

        [TestMethod]
        public void Contains_persisted_entity_false()
        {
            int userId = 1;
            MockEntity persistedEntity = new MockEntity() { id = int.MaxValue };
            persistedEntity.MarkPersisted();
            MockEntity entity = new MockEntity();
            entity.RelationalEntities.Add(persistedEntity);

            ForeignEntityCollectionManager<MockEntity> manager = new ForeignEntityCollectionManager<MockEntity>(userId, entity, () => entity.RelationalEntities, null);

            Assert.IsFalse(manager.Contains(new MockEntity()));
        }

        [TestMethod]
        public void Contains_empty_collection()
        {
            int userId = 1;
            MockEntity entity = new MockEntity();

            ForeignEntityCollectionManager<MockEntity> manager = new ForeignEntityCollectionManager<MockEntity>(userId, entity, () => entity.RelationalEntities, null);

            Assert.IsFalse(manager.Contains(new MockEntity()));
        }

        [TestMethod]
        public void Contains_deleted_entity()
        {
            int userId = 1;
            MockEntity deletedEntity = new MockEntity() { IsDeleted = true };
            MockEntity entity = new MockEntity();
            entity.RelationalEntities.Add(deletedEntity);

            ForeignEntityCollectionManager<MockEntity> manager = new ForeignEntityCollectionManager<MockEntity>(userId, entity, () => entity.RelationalEntities, null);

            Assert.IsFalse(manager.Contains(deletedEntity));
        }

        [TestMethod]
        public void Get_persisted_entity()
        {
            int userId = 1;
            MockEntity persistedEntity = new MockEntity() { id = int.MaxValue };
            persistedEntity.MarkPersisted();
            MockEntity entity = new MockEntity();
            entity.RelationalEntities.Add(persistedEntity);

            ForeignEntityCollectionManager<MockEntity> manager = new ForeignEntityCollectionManager<MockEntity>(userId, entity, () => entity.RelationalEntities, null);

            Assert.AreEqual(persistedEntity, manager.Get(persistedEntity.PrimaryKeys));
        }

        [TestMethod]
        public void Get_new_entity()
        {
            int userId = 1;
            MockEntity newEntity = new MockEntity();
            MockEntity entity = new MockEntity();
            entity.RelationalEntities.Add(newEntity);

            ForeignEntityCollectionManager<MockEntity> manager = new ForeignEntityCollectionManager<MockEntity>(userId, entity, () => entity.RelationalEntities, null);

            Assert.AreEqual(newEntity, manager.Get(newEntity.Guid));
        }

        [TestMethod]
        public void Get_deleted_new_entity()
        {
            int userId = 1;
            MockEntity deletedEntity = new MockEntity() { IsDeleted = true };
            MockEntity entity = new MockEntity();
            entity.RelationalEntities.Add(deletedEntity);

            ForeignEntityCollectionManager<MockEntity> manager = new ForeignEntityCollectionManager<MockEntity>(userId, entity, () => entity.RelationalEntities, null);

            Assert.IsNull(manager.Get(deletedEntity.Guid));
        }

        [TestMethod]
        public void Get_deleted_persisted_entity()
        {
            int userId = 1;
            MockEntity deletedEntity = new MockEntity() { id = int.MaxValue, IsDeleted = true };
            deletedEntity.MarkPersisted();
            MockEntity entity = new MockEntity();
            entity.RelationalEntities.Add(deletedEntity);

            ForeignEntityCollectionManager<MockEntity> manager = new ForeignEntityCollectionManager<MockEntity>(userId, entity, () => entity.RelationalEntities, null);

            Assert.IsNull(manager.Get(deletedEntity.PrimaryKeys));
        }
    }
}
