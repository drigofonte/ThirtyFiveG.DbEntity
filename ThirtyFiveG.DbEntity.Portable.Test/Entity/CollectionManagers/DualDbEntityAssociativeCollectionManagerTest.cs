using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using ThirtyFiveG.DbEntity.Entity;
using ThirtyFiveG.DbEntity.Entity.CollectionManagers;

namespace ThirtyFiveG.DbEntity.Portable.Test.Entity.CollectionManagers
{
    [TestClass]
    public class DualDbEntityAssociativeCollectionManagerTest
    {
        [TestMethod]
        public void Add_new_entities()
        {
            int userId = 1;
            bool isSetForeignKeysActionCalled = false;
            bool isSetEntity1ForeignEntityActionCalled = false;
            bool isSetEntity2ForeignEntityActionCalled = false;
            Action<IDbEntity, MockEntity, MockEntity, MockEntity> setForeignKeys = (o, e1, e2, ae) => { isSetForeignKeysActionCalled = true; };
            Action<IDbEntity, MockEntity, MockEntity> setEntity1ForeignEntity = (o, e1, ae) => { isSetEntity1ForeignEntityActionCalled = true; };
            Action<IDbEntity, MockEntity, MockEntity> setEntity2ForeignEntity = (o, e2, ae) => { isSetEntity2ForeignEntityActionCalled = true; };
            MockEntity entity = new MockEntity();
            DualDbEntityAssociativeCollectionManager<MockEntity, MockEntity, MockEntity> manager = new DualDbEntityAssociativeCollectionManager<MockEntity, MockEntity, MockEntity>(userId, entity, () => entity.RelationalEntities, setEntity1ForeignEntity, setEntity2ForeignEntity, setForeignKeys, null, null, null);

            MockEntity newLocation = new MockEntity() { id = int.MaxValue };
            MockEntity newContract = new MockEntity() { id = int.MinValue };
            MockEntity added = manager.Add(newContract, newLocation);

            Assert.AreEqual(1, entity.RelationalEntities.Count);
            Assert.AreEqual(added, entity.RelationalEntities.First());
            Assert.IsTrue(isSetEntity1ForeignEntityActionCalled);
            Assert.IsTrue(isSetEntity2ForeignEntityActionCalled);
            Assert.IsTrue(isSetForeignKeysActionCalled);
            Assert.IsFalse(added.IsDeleted);
            Assert.AreEqual(userId, added.CreatedByID);
        }

        [TestMethod]
        public void Add_new_entity1()
        {
            int userId = 1;
            bool isSetEntity1ForeignEntityActionCalled = false;
            bool isSetEntity2ForeignEntityActionCalled = false;
            Action<IDbEntity, MockEntity, MockEntity> setEntity1ForeignEntity = (o, e1, ae) => { isSetEntity1ForeignEntityActionCalled = true; };
            Action<IDbEntity, MockEntity, MockEntity> setEntity2ForeignEntity = (o, e2, ae) => { isSetEntity2ForeignEntityActionCalled = true; };
            MockEntity entity = new MockEntity();
            DualDbEntityAssociativeCollectionManager<MockEntity, MockEntity, MockEntity> manager = new DualDbEntityAssociativeCollectionManager<MockEntity, MockEntity, MockEntity>(userId, entity, () => entity.RelationalEntities, setEntity1ForeignEntity, setEntity2ForeignEntity, (o, e1, e2, ae) => { }, null, null, null);

            MockEntity persistedLocation = new MockEntity() { id = int.MaxValue };
            persistedLocation.MarkPersisted();
            MockEntity newContract = new MockEntity() { id = int.MinValue };
            MockEntity added = manager.Add(newContract, persistedLocation);

            Assert.IsTrue(isSetEntity1ForeignEntityActionCalled);
            Assert.IsFalse(isSetEntity2ForeignEntityActionCalled);
        }

        [TestMethod]
        public void Add_new_entity2()
        {
            int userId = 1;
            bool isSetEntity1ForeignEntityActionCalled = false;
            bool isSetEntity2ForeignEntityActionCalled = false;
            Action<IDbEntity, MockEntity, MockEntity> setEntity1ForeignEntity = (o, e1, ae) => { isSetEntity1ForeignEntityActionCalled = true; };
            Action<IDbEntity, MockEntity, MockEntity> setEntity2ForeignEntity = (o, e2, ae) => { isSetEntity2ForeignEntityActionCalled = true; };
            MockEntity entity = new MockEntity();
            DualDbEntityAssociativeCollectionManager<MockEntity, MockEntity, MockEntity> manager = new DualDbEntityAssociativeCollectionManager<MockEntity, MockEntity, MockEntity>(userId, entity, () => entity.RelationalEntities, setEntity1ForeignEntity, setEntity2ForeignEntity, (o, e1, e2, ae) => { }, null, null, null);

            MockEntity newLocation = new MockEntity() { id = int.MaxValue };
            MockEntity persistedContract = new MockEntity() { id = int.MinValue };
            persistedContract.MarkPersisted();
            MockEntity added = manager.Add(persistedContract, newLocation);

            Assert.IsFalse(isSetEntity1ForeignEntityActionCalled);
            Assert.IsTrue(isSetEntity2ForeignEntityActionCalled);
        }

        [TestMethod]
        public void ReAdd_previously_deleted_entity()
        {
            bool isSetForeignKeysActionCalled = false;
            bool isSetEntity1ForeignEntityActionCalled = false;
            bool isSetEntity2ForeignEntityActionCalled = false;
            Action<IDbEntity, MockEntity, MockEntity, MockEntity> setForeignKeys = (o, e1, e2, ae) => { isSetForeignKeysActionCalled = true; };
            Action<IDbEntity, MockEntity, MockEntity> setEntity1ForeignEntity = (o, e1, ae) => { isSetEntity1ForeignEntityActionCalled = true; };
            Action<IDbEntity, MockEntity, MockEntity> setEntity2ForeignEntity = (o, e2, ae) => { isSetEntity2ForeignEntityActionCalled = true; };
            MockEntity entity = new MockEntity();
            DualDbEntityAssociativeCollectionManager<MockEntity, MockEntity, MockEntity> manager = new DualDbEntityAssociativeCollectionManager<MockEntity, MockEntity, MockEntity>(1, entity, () => entity.RelationalEntities, setEntity1ForeignEntity, setEntity2ForeignEntity, setForeignKeys, (o, ae) => true, (e1, ae) => true, (e2, ae) => true);

            MockEntity persistedLocation = new MockEntity() { id = int.MaxValue };
            persistedLocation.MarkPersisted();
            MockEntity persistedContract = new MockEntity() { id = int.MinValue };
            persistedContract.MarkPersisted();
            MockEntity persistedServiceContractSiteContact = new MockEntity() { IsDeleted = true };
            persistedServiceContractSiteContact.MarkPersisted();
            entity.RelationalEntities.Add(persistedServiceContractSiteContact);
            MockEntity added = manager.Add(persistedContract, persistedLocation);

            Assert.AreEqual(1, entity.RelationalEntities.Count);
            Assert.AreEqual(added, persistedServiceContractSiteContact);
            Assert.IsFalse(isSetEntity1ForeignEntityActionCalled);
            Assert.IsFalse(isSetEntity2ForeignEntityActionCalled);
            Assert.IsFalse(isSetForeignKeysActionCalled);
            Assert.IsFalse(added.IsDeleted);
        }

        [TestMethod]
        public void Remove_new_entity()
        {
            MockEntity entity = new MockEntity();
            MockEntity newLocation = new MockEntity() { id = int.MaxValue };
            MockEntity newContract = new MockEntity() { id = int.MinValue };
            MockEntity newServiceContractSiteContact = new MockEntity() { IsDeleted = true };
            entity.RelationalEntities.Add(newServiceContractSiteContact);
            DualDbEntityAssociativeCollectionManager<MockEntity, MockEntity, MockEntity> manager = new DualDbEntityAssociativeCollectionManager<MockEntity, MockEntity, MockEntity>(1, entity, () => entity.RelationalEntities, null, null, null, (o, ae) => true, (e1, ae) => true, (e2, ae) => true);

            MockEntity removed = manager.Remove(newContract, newLocation);

            Assert.AreEqual(0, entity.RelationalEntities.Count);
            Assert.AreEqual(newServiceContractSiteContact, removed);
        }

        [TestMethod]
        public void Remove_new_entity1()
        {
            MockEntity entity = new MockEntity();
            MockEntityTwo newLocation = new MockEntityTwo() { id = int.MaxValue };
            MockEntityOne newContract = new MockEntityOne() { id = int.MinValue };
            MockEntity newServiceContractSiteContact = new MockEntity() { IsDeleted = true };
            entity.RelationalEntities.Add(newServiceContractSiteContact);
            DualDbEntityAssociativeCollectionManager<MockEntityOne, MockEntityTwo, MockEntity> manager = new DualDbEntityAssociativeCollectionManager<MockEntityOne, MockEntityTwo, MockEntity>(1, entity, () => entity.RelationalEntities, null, null, null, (o, ae) => true, (e1, ae) => true, (e2, ae) => true);

            MockEntity removed = manager.Remove(newLocation).Single();

            Assert.AreEqual(0, entity.RelationalEntities.Count);
            Assert.AreEqual(newServiceContractSiteContact, removed);
        }

        [TestMethod]
        public void Remove_new_entity2()
        {
            MockEntity entity = new MockEntity();
            MockEntityTwo newLocation = new MockEntityTwo() { id = int.MaxValue };
            MockEntityOne newContract = new MockEntityOne() { id = int.MinValue };
            MockEntity newServiceContractSiteContact = new MockEntity() { IsDeleted = true };
            entity.RelationalEntities.Add(newServiceContractSiteContact);
            DualDbEntityAssociativeCollectionManager<MockEntityOne, MockEntityTwo, MockEntity> manager = new DualDbEntityAssociativeCollectionManager<MockEntityOne, MockEntityTwo, MockEntity>(1, entity, () => entity.RelationalEntities, null, null, null, (o, ae) => true, (e1, ae) => true, (e2, ae) => true);

            MockEntity removed = manager.Remove(newContract).Single();

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

            DualDbEntityAssociativeCollectionManager<MockEntity, MockEntity, MockEntity> manager = new DualDbEntityAssociativeCollectionManager<MockEntity, MockEntity, MockEntity>(1, entity, () => entity.RelationalEntities, null, null, null, (o, ae) => true, (e1, ae) => true, (e2, ae) => true);

            MockEntity removed = manager.Remove(persistedContract, persistedLocation);

            Assert.AreEqual(1, entity.RelationalEntities.Count);
            Assert.AreEqual(persistedServiceContractSiteContact, removed);
            Assert.IsTrue(removed.IsDeleted);
        }

        [TestMethod]
        public void Remove_persisted_entity1()
        {
            MockEntity entity = new MockEntity();

            MockEntityTwo persistedLocation = new MockEntityTwo() { id = int.MaxValue };
            persistedLocation.MarkPersisted();
            MockEntityOne persistedContract = new MockEntityOne() { id = int.MinValue };
            persistedContract.MarkPersisted();
            MockEntity persistedServiceContractSiteContact = new MockEntity() { IsDeleted = true };
            persistedServiceContractSiteContact.MarkPersisted();
            entity.RelationalEntities.Add(persistedServiceContractSiteContact);

            DualDbEntityAssociativeCollectionManager<MockEntityOne, MockEntityTwo, MockEntity> manager = new DualDbEntityAssociativeCollectionManager<MockEntityOne, MockEntityTwo, MockEntity>(1, entity, () => entity.RelationalEntities, null, null, null, (o, ae) => true, (e1, ae) => true, (e2, ae) => true);

            MockEntity removed = manager.Remove(persistedContract).Single();

            Assert.AreEqual(1, entity.RelationalEntities.Count);
            Assert.AreEqual(persistedServiceContractSiteContact, removed);
            Assert.IsTrue(removed.IsDeleted);
        }

        [TestMethod]
        public void Remove_persisted_entity2()
        {
            MockEntity entity = new MockEntity();

            MockEntityTwo persistedLocation = new MockEntityTwo() { id = int.MaxValue };
            persistedLocation.MarkPersisted();
            MockEntityOne persistedContract = new MockEntityOne() { id = int.MinValue };
            persistedContract.MarkPersisted();
            MockEntity persistedServiceContractSiteContact = new MockEntity() { IsDeleted = true };
            persistedServiceContractSiteContact.MarkPersisted();
            entity.RelationalEntities.Add(persistedServiceContractSiteContact);

            DualDbEntityAssociativeCollectionManager<MockEntityOne, MockEntityTwo, MockEntity> manager = new DualDbEntityAssociativeCollectionManager<MockEntityOne, MockEntityTwo, MockEntity>(1, entity, () => entity.RelationalEntities, null, null, null, (o, ae) => true, (e1, ae) => true, (e2, ae) => true);

            MockEntity removed = manager.Remove(persistedLocation).Single();

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
            bool isEntity2Matched = false;
            Func<IDbEntity, MockEntity, bool> matchesAssociativeEntityOwner = (o, ae) => { return isAssociativeEntityOwnerMatched = true; };
            Func<MockEntity, MockEntity, bool> matchesEntity1 = (e1, ae) => { return isEntity1Matched = true; };
            Func<MockEntity, MockEntity, bool> matchesEntity2 = (e2, ae) => { return isEntity2Matched = true; };
            DualDbEntityAssociativeCollectionManager<MockEntity, MockEntity, MockEntity> manager = new DualDbEntityAssociativeCollectionManager<MockEntity, MockEntity, MockEntity>(1, entity, () => entity.RelationalEntities, null, null, null, matchesAssociativeEntityOwner, matchesEntity1, matchesEntity2);

            entity.RelationalEntities.Add(new MockEntity());

            Assert.IsTrue(manager.Contains(new MockEntity(), new MockEntity()));
            Assert.IsTrue(isAssociativeEntityOwnerMatched);
            Assert.IsTrue(isEntity1Matched);
            Assert.IsTrue(isEntity2Matched);
        }

        [TestMethod]
        public void Contains_entity1_true()
        {
            MockEntity entity = new MockEntity() { id = int.MinValue };
            bool isAssociativeEntityOwnerMatched = false;
            bool isEntity1Matched = false;
            bool isEntity2Matched = false;
            Func<IDbEntity, MockEntity, bool> matchesAssociativeEntityOwner = (o, ae) => { return isAssociativeEntityOwnerMatched = true; };
            Func<MockEntityOne, MockEntity, bool> matchesEntity1 = (e1, ae) => { return isEntity1Matched = true; };
            Func<MockEntityTwo, MockEntity, bool> matchesEntity2 = (e2, ae) => { return isEntity2Matched = true; };
            DualDbEntityAssociativeCollectionManager<MockEntityOne, MockEntityTwo, MockEntity> manager = new DualDbEntityAssociativeCollectionManager<MockEntityOne, MockEntityTwo, MockEntity>(1, entity, () => entity.RelationalEntities, null, null, null, matchesAssociativeEntityOwner, matchesEntity1, matchesEntity2);

            entity.RelationalEntities.Add(new MockEntity());

            Assert.IsTrue(manager.Contains(new MockEntityOne()));
            Assert.IsTrue(isAssociativeEntityOwnerMatched);
            Assert.IsTrue(isEntity1Matched);
            Assert.IsFalse(isEntity2Matched);
        }

        [TestMethod]
        public void Contains_entity2_true()
        {
            MockEntity entity = new MockEntity() { id = int.MinValue };
            bool isAssociativeEntityOwnerMatched = false;
            bool isEntity1Matched = false;
            bool isEntity2Matched = false;
            Func<IDbEntity, MockEntity, bool> matchesAssociativeEntityOwner = (o, ae) => { return isAssociativeEntityOwnerMatched = true; };
            Func<MockEntityOne, MockEntity, bool> matchesEntity1 = (e1, ae) => { return isEntity1Matched = true; };
            Func<MockEntityTwo, MockEntity, bool> matchesEntity2 = (e2, ae) => { return isEntity2Matched = true; };
            DualDbEntityAssociativeCollectionManager<MockEntityOne, MockEntityTwo, MockEntity> manager = new DualDbEntityAssociativeCollectionManager<MockEntityOne, MockEntityTwo, MockEntity>(1, entity, () => entity.RelationalEntities, null, null, null, matchesAssociativeEntityOwner, matchesEntity1, matchesEntity2);

            entity.RelationalEntities.Add(new MockEntity());

            Assert.IsTrue(manager.Contains(new MockEntityTwo()));
            Assert.IsTrue(isAssociativeEntityOwnerMatched);
            Assert.IsFalse(isEntity1Matched);
            Assert.IsTrue(isEntity2Matched);
        }

        [TestMethod]
        public void Contains_false()
        {
            MockEntity entity = new MockEntity() { id = int.MinValue };
            Func<IDbEntity, MockEntity, bool> matchesAssociativeEntityOwner = (o, ae) => false;
            Func<MockEntity, MockEntity, bool> matchesEntity1 = (e1, ae) => false;
            Func<MockEntity, MockEntity, bool> matchesEntity2 = (e2, ae) => false;
            DualDbEntityAssociativeCollectionManager<MockEntity, MockEntity, MockEntity> manager = new DualDbEntityAssociativeCollectionManager<MockEntity, MockEntity, MockEntity>(1, entity, () => entity.RelationalEntities, null, null, null, matchesAssociativeEntityOwner, matchesEntity1, matchesEntity2);

            entity.RelationalEntities.Add(new MockEntity());

            Assert.IsFalse(manager.Contains(new MockEntity(), new MockEntity()));
        }

        [TestMethod]
        public void Contains_entity1_false()
        {
            MockEntity entity = new MockEntity() { id = int.MinValue };
            Func<IDbEntity, MockEntity, bool> matchesAssociativeEntityOwner = (o, ae) => false;
            Func<MockEntityOne, MockEntity, bool> matchesEntity1 = (e1, ae) => false;
            Func<MockEntityTwo, MockEntity, bool> matchesEntity2 = (e2, ae) => false;
            DualDbEntityAssociativeCollectionManager<MockEntityOne, MockEntityTwo, MockEntity> manager = new DualDbEntityAssociativeCollectionManager<MockEntityOne, MockEntityTwo, MockEntity>(1, entity, () => entity.RelationalEntities, null, null, null, matchesAssociativeEntityOwner, matchesEntity1, matchesEntity2);

            entity.RelationalEntities.Add(new MockEntity());

            Assert.IsFalse(manager.Contains(new MockEntityOne()));
        }

        [TestMethod]
        public void Contains_entity2_false()
        {
            MockEntity entity = new MockEntity() { id = int.MinValue };
            Func<IDbEntity, MockEntity, bool> matchesAssociativeEntityOwner = (o, ae) => false;
            Func<MockEntityOne, MockEntity, bool> matchesEntity1 = (e1, ae) => false;
            Func<MockEntityTwo, MockEntity, bool> matchesEntity2 = (e2, ae) => false;
            DualDbEntityAssociativeCollectionManager<MockEntityOne, MockEntityTwo, MockEntity> manager = new DualDbEntityAssociativeCollectionManager<MockEntityOne, MockEntityTwo, MockEntity>(1, entity, () => entity.RelationalEntities, null, null, null, matchesAssociativeEntityOwner, matchesEntity1, matchesEntity2);

            entity.RelationalEntities.Add(new MockEntity());

            Assert.IsFalse(manager.Contains(new MockEntityTwo()));
        }

        [TestMethod]
        public void Contains_empty_collection()
        {
            MockEntity entity = new MockEntity() { id = int.MinValue };

            DualDbEntityAssociativeCollectionManager<MockEntity, MockEntity, MockEntity> manager = new DualDbEntityAssociativeCollectionManager<MockEntity, MockEntity, MockEntity>(1, entity, () => entity.RelationalEntities, null, null, null, (o, ae) => true, (e1, ae) => true, (e2, ae) => true);

            Assert.IsFalse(manager.Contains(new MockEntity(), new MockEntity()));
        }

        [TestMethod]
        public void Contains_entity1_empty_collection()
        {
            MockEntity entity = new MockEntity() { id = int.MinValue };

            DualDbEntityAssociativeCollectionManager<MockEntityOne, MockEntityTwo, MockEntity> manager = new DualDbEntityAssociativeCollectionManager<MockEntityOne, MockEntityTwo, MockEntity>(1, entity, () => entity.RelationalEntities, null, null, null, (o, ae) => true, (e1, ae) => true, (e2, ae) => true);

            Assert.IsFalse(manager.Contains(new MockEntityOne()));
        }

        [TestMethod]
        public void Contains_entity2_empty_collection()
        {
            MockEntity entity = new MockEntity() { id = int.MinValue };

            DualDbEntityAssociativeCollectionManager<MockEntityOne, MockEntityTwo, MockEntity> manager = new DualDbEntityAssociativeCollectionManager<MockEntityOne, MockEntityTwo, MockEntity>(1, entity, () => entity.RelationalEntities, null, null, null, (o, ae) => true, (e1, ae) => true, (e2, ae) => true);

            Assert.IsFalse(manager.Contains(new MockEntityTwo()));
        }

        [TestMethod]
        public void Contains_deleted_entity()
        {
            MockEntity entity = new MockEntity() { id = int.MinValue };

            DualDbEntityAssociativeCollectionManager<MockEntity, MockEntity, MockEntity> manager = new DualDbEntityAssociativeCollectionManager<MockEntity, MockEntity, MockEntity>(1, entity, () => entity.RelationalEntities, null, null, null, (o, ae) => true, (e1, ae) => true, (e2, ae) => true);

            entity.RelationalEntities.Add(new MockEntity() { IsDeleted = true });

            Assert.IsFalse(manager.Contains(new MockEntity(), new MockEntity()));
        }

        [TestMethod]
        public void Contains_entity1_deleted_entity()
        {
            MockEntity entity = new MockEntity() { id = int.MinValue };

            DualDbEntityAssociativeCollectionManager<MockEntityOne, MockEntityTwo, MockEntity> manager = new DualDbEntityAssociativeCollectionManager<MockEntityOne, MockEntityTwo, MockEntity>(1, entity, () => entity.RelationalEntities, null, null, null, (o, ae) => true, (e1, ae) => true, (e2, ae) => true);

            entity.RelationalEntities.Add(new MockEntity() { IsDeleted = true });

            Assert.IsFalse(manager.Contains(new MockEntityOne()));
        }

        [TestMethod]
        public void Contains_entity2_deleted_entity()
        {
            MockEntity entity = new MockEntity() { id = int.MinValue };

            DualDbEntityAssociativeCollectionManager<MockEntityOne, MockEntityTwo, MockEntity> manager = new DualDbEntityAssociativeCollectionManager<MockEntityOne, MockEntityTwo, MockEntity>(1, entity, () => entity.RelationalEntities, null, null, null, (o, ae) => true, (e1, ae) => true, (e2, ae) => true);

            entity.RelationalEntities.Add(new MockEntity() { IsDeleted = true });

            Assert.IsFalse(manager.Contains(new MockEntityOne(), new MockEntityTwo()));
        }

        public class MockEntityOne : MockEntity { }
        public class MockEntityTwo : MockEntity { }
    }
}
