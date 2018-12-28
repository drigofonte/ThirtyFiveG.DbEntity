using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using ThirtyFiveG.DbEntity.Entity;
using ThirtyFiveG.DbEntity.Entity.CollectionManagers;

namespace ThirtyFiveG.DbEntity.Portable.Test.Entity.CollectionManagers
{
    [TestClass]
    public class DbEntityAssociativeCollectionManagerTest
    {
        [TestMethod]
        public void Add_new_entity()
        {
            int userId = 1;
            bool isSetForeignKeysActionCalled = false;
            bool isSetForeignEntitiesActionCalled = false;
            Action<IDbEntity, MockEntity, MockEntity> setForeignKeys = (o, e, ae) => { isSetForeignKeysActionCalled = true; };
            Action<IDbEntity, MockEntity, MockEntity> setForeignEntities = (o, e, ae) => { isSetForeignEntitiesActionCalled = true; };
            MockEntity entity = new MockEntity();
            DbEntityAssociativeCollectionManager<MockEntity, MockEntity> manager = new DbEntityAssociativeCollectionManager<MockEntity, MockEntity>(userId, entity, () => entity.RelationalEntities, setForeignEntities, setForeignKeys, null, null);

            MockEntity newLocation = new MockEntity();
            MockEntity added = manager.Add(newLocation);

            Assert.AreEqual(1, entity.RelationalEntities.Count);
            Assert.AreEqual(added, entity.RelationalEntities.First());
            Assert.IsTrue(isSetForeignEntitiesActionCalled);
            Assert.IsTrue(isSetForeignKeysActionCalled);
            Assert.IsFalse(added.IsDeleted);
            Assert.AreEqual(userId, added.CreatedByID);
            Assert.AreEqual(userId, newLocation.CreatedByID);
        }

        [TestMethod]
        public void ReAdd_previously_deleted_entity()
        {
            int userId = 1;
            bool isSetForeignKeysActionCalled = false;
            bool isSetForeignEntitiesActionCalled = false;
            Action<IDbEntity, MockEntity, MockEntity> setForeignKeys = (o, e, ae) => { isSetForeignKeysActionCalled = true; };
            Action<IDbEntity, MockEntity, MockEntity> setForeignEntities = (o, e, ae) => { isSetForeignEntitiesActionCalled = true; };
            MockEntity entity = new MockEntity();
            DbEntityAssociativeCollectionManager<MockEntity, MockEntity> manager = new DbEntityAssociativeCollectionManager<MockEntity, MockEntity>(userId, entity, () => entity.RelationalEntities, setForeignEntities, setForeignKeys, (o, ae) => true, (e, ae) => true);

            MockEntity persistedLocation = new MockEntity() { id = int.MaxValue };
            persistedLocation.MarkPersisted();
            MockEntity persistedContract = new MockEntity() { id = int.MinValue };
            persistedContract.MarkPersisted();
            MockEntity persistedServiceContractSiteContact = new MockEntity() { IsDeleted = true };
            persistedServiceContractSiteContact.MarkPersisted();
            entity.RelationalEntities.Add(persistedServiceContractSiteContact);
            MockEntity added = manager.Add(persistedLocation);

            Assert.AreEqual(1, entity.RelationalEntities.Count);
            Assert.AreEqual(added, persistedServiceContractSiteContact);
            Assert.IsFalse(isSetForeignEntitiesActionCalled);
            Assert.IsFalse(isSetForeignKeysActionCalled);
            Assert.IsFalse(added.IsDeleted);
            Assert.AreNotEqual(userId, added.CreatedByID);
        }

        [TestMethod]
        public void Remove_new_entity()
        {
            MockEntity entity = new MockEntity();
            MockEntity newLocation = new MockEntity() { id = int.MaxValue };
            MockEntity newContract = new MockEntity() { id = int.MinValue };
            MockEntity newServiceContractSiteContact = new MockEntity() { IsDeleted = true };
            entity.RelationalEntities.Add(newServiceContractSiteContact);
            DbEntityAssociativeCollectionManager<MockEntity, MockEntity> manager = new DbEntityAssociativeCollectionManager<MockEntity, MockEntity>(1, entity, () => entity.RelationalEntities, null, null, (o, ae) => true, (e, ae) => true);

            MockEntity removed = manager.Remove(newLocation);

            Assert.AreEqual(0, entity.RelationalEntities.Count);
            Assert.AreEqual(newServiceContractSiteContact, removed);
        }

        [TestMethod]
        public void Remove_persisted_entity()
        {
            MockEntity entity = new MockEntity();

            MockEntity persistedLocation = new MockEntity() { id = int.MaxValue };
            persistedLocation.MarkPersisted();
            MockEntity persistedContract = new MockEntity() { id = int.MinValue };
            persistedContract.MarkPersisted();
            MockEntity persistedServiceContractSiteContact = new MockEntity() { IsDeleted = true };
            persistedServiceContractSiteContact.MarkPersisted();
            entity.RelationalEntities.Add(persistedServiceContractSiteContact);

            DbEntityAssociativeCollectionManager<MockEntity, MockEntity> manager = new DbEntityAssociativeCollectionManager<MockEntity, MockEntity>(1, entity, () => entity.RelationalEntities, null, null, (o, ae) => true, (e, ae) => true);

            MockEntity removed = manager.Remove(persistedLocation);

            Assert.AreEqual(1, entity.RelationalEntities.Count);
            Assert.AreEqual(persistedServiceContractSiteContact, removed);
            Assert.IsTrue(removed.IsDeleted);
        }

        [TestMethod]
        public void Contains_true()
        {
            MockEntity entity = new MockEntity() { id = int.MinValue };
            bool isAssociativeEntityOwnerMatched = false;
            bool isEntity1Matched = false;
            Func<IDbEntity, MockEntity, bool> matchesAssociativeEntityOwner = (o, ae) => { return isAssociativeEntityOwnerMatched = true; };
            Func<MockEntity, MockEntity, bool> matchesEntity = (e1, ae) => { return isEntity1Matched = true; };
            DbEntityAssociativeCollectionManager<MockEntity, MockEntity> manager = new DbEntityAssociativeCollectionManager<MockEntity, MockEntity>(1, entity, () => entity.RelationalEntities, null, null, matchesAssociativeEntityOwner, matchesEntity);

            entity.RelationalEntities.Add(new MockEntity());

            Assert.IsTrue(manager.Contains(new MockEntity()));
            Assert.IsTrue(isAssociativeEntityOwnerMatched);
            Assert.IsTrue(isEntity1Matched);
        }

        [TestMethod]
        public void Contains_false()
        {
            MockEntity entity = new MockEntity() { id = int.MinValue };
            Func<IDbEntity, MockEntity, bool> matchesAssociativeEntityOwner = (o, ae) => false;
            Func<MockEntity, MockEntity, bool> matchesEntity = (e1, ae) => false;
            DbEntityAssociativeCollectionManager<MockEntity, MockEntity> manager = new DbEntityAssociativeCollectionManager<MockEntity, MockEntity>(1, entity, () => entity.RelationalEntities, null, null, matchesAssociativeEntityOwner, matchesEntity);

            entity.RelationalEntities.Add(new MockEntity());

            Assert.IsFalse(manager.Contains(new MockEntity()));
        }

        [TestMethod]
        public void Contains_empty_collection()
        {
            MockEntity entity = new MockEntity() { id = int.MinValue };

            DbEntityAssociativeCollectionManager<MockEntity, MockEntity> manager = new DbEntityAssociativeCollectionManager<MockEntity, MockEntity>(1, entity, () => entity.RelationalEntities, null, null, (o, ae) => true, (e, ae) => true);

            Assert.IsFalse(manager.Contains(new MockEntity()));
        }

        [TestMethod]
        public void Contains_deleted_entity()
        {
            MockEntity entity = new MockEntity() { id = int.MinValue };

            DbEntityAssociativeCollectionManager<MockEntity, MockEntity> manager = new DbEntityAssociativeCollectionManager<MockEntity, MockEntity>(1, entity, () => entity.RelationalEntities, null, null, (o, ae) => true, (e, ae) => true);

            entity.RelationalEntities.Add(new MockEntity() { IsDeleted = true });

            Assert.IsFalse(manager.Contains(new MockEntity()));
        }

        [TestMethod]
        public void Get()
        {
            MockEntity entity = new MockEntity() { id = int.MinValue };

            DbEntityAssociativeCollectionManager<MockEntity, MockEntity> manager = new DbEntityAssociativeCollectionManager<MockEntity, MockEntity>(1, entity, () => entity.RelationalEntities, null, null, (o, ae) => true, (e, ae) => true);

            MockEntity associativeEntity = new MockEntity();
            entity.RelationalEntities.Add(associativeEntity);

            Assert.AreEqual(associativeEntity, manager.Get(new MockEntity()));
        }

        [TestMethod]
        public void Get_deleted_entity()
        {
            MockEntity entity = new MockEntity() { id = int.MinValue };

            DbEntityAssociativeCollectionManager<MockEntity, MockEntity> manager = new DbEntityAssociativeCollectionManager<MockEntity, MockEntity>(1, entity, () => entity.RelationalEntities, null, null, (o, ae) => true, (e, ae) => true);

            entity.RelationalEntities.Add(new MockEntity() { IsDeleted = true });

            Assert.IsNull(manager.Get(new MockEntity()));
        }
    }
}
