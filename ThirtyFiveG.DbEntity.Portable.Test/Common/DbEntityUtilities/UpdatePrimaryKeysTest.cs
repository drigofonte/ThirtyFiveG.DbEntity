using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using ThirtyFiveG.DbEntity.Entity;

using U = ThirtyFiveG.DbEntity.Common.DbEntityUtilities;

namespace ThirtyFiveG.DbEntity.Portable.Test.Common.DbEntityUtilities
{
    [TestClass]
    public class UpdatePrimaryKeysTest
    {
        [TestMethod]
        public void UpdatePrimaryKeys_object_tree()
        {
            MockEntity entity = new MockEntity() { id = 1 };
            entity.RelationalEntity1 = new MockEntity() { id = 2 };
            entity.AssociativeEntities.Add(new MockAssociativeEntity() { RelationalEntity1Id = entity.id, RelationalEntity2Id = 3, RelationalEntity2 = new MockEntity() { id = 3 } });
            string jobPath = ".";
            string equipmentLocationPath = "RelationalEntity1";
            string jobContactsPath = "AssociativeEntities[Guid=" + entity.AssociativeEntities.First().Guid + "]";
            string contactPath = jobContactsPath + ".RelationalEntity2";

            IDictionary<string, Tuple<string, object>[]> keysToUpdate = new Dictionary<string, Tuple<string, object>[]>()
            {
                { jobPath, entity.PrimaryKeys },
                { equipmentLocationPath, entity.RelationalEntity1.PrimaryKeys },
                { jobContactsPath, entity.AssociativeEntities.First().PrimaryKeys },
                { contactPath, entity.AssociativeEntities.First().RelationalEntity2.PrimaryKeys }
            };

            MockEntity entityCopy = new MockEntity() { Guid = entity.Guid };
            entityCopy.RelationalEntity1 = new MockEntity() { Guid = entity.RelationalEntity1.Guid };
            entityCopy.AssociativeEntities.Add(new MockAssociativeEntity() { Guid = entity.AssociativeEntities.First().Guid, RelationalEntity2 = new MockEntity() { Guid = entity.AssociativeEntities.First().RelationalEntity2.Guid } });

            Assert.AreEqual(EntityState.New, entityCopy.State);
            Assert.AreEqual(EntityState.New, entityCopy.RelationalEntity1.State);
            Assert.AreEqual(EntityState.New, entityCopy.AssociativeEntities.First().State);
            Assert.AreEqual(EntityState.New, entityCopy.AssociativeEntities.First().RelationalEntity2.State);

            U.UpdatePrimaryKeys(keysToUpdate, entityCopy);

            Assert.IsTrue(U.PrimaryKeysEqual(entity.PrimaryKeys, entityCopy.PrimaryKeys));
            Assert.IsTrue(U.PrimaryKeysEqual(entity.RelationalEntity1.PrimaryKeys, entityCopy.RelationalEntity1.PrimaryKeys));
            Assert.IsTrue(U.PrimaryKeysEqual(entity.AssociativeEntities.First().PrimaryKeys, entityCopy.AssociativeEntities.First().PrimaryKeys));
            Assert.IsTrue(U.PrimaryKeysEqual(entity.AssociativeEntities.First().RelationalEntity2.PrimaryKeys, entityCopy.AssociativeEntities.First().RelationalEntity2.PrimaryKeys));

            Assert.AreEqual(EntityState.Persisted, entityCopy.State);
            Assert.AreEqual(EntityState.Persisted, entityCopy.RelationalEntity1.State);
            Assert.AreEqual(EntityState.Persisted, entityCopy.AssociativeEntities.First().State);
            Assert.AreEqual(EntityState.Persisted, entityCopy.AssociativeEntities.First().RelationalEntity2.State);
        }

        [TestMethod]
        public void UpdatePrimaryKeys_flat_object()
        {
            MockAssociativeEntity entity = new MockAssociativeEntity();
            int entity1Id = 1;
            int entity2Id = 2;
            Tuple<string, object>[] primaryKeys = new Tuple<string, object>[] { new Tuple<string, object>("RelationalEntity1Id", entity1Id), new Tuple<string, object>("RelationalEntity2Id", entity2Id) };

            U.UpdatePrimaryKeys(primaryKeys, entity);

            Assert.AreEqual(entity1Id, entity.RelationalEntity1Id);
            Assert.AreEqual(entity2Id, entity.RelationalEntity2Id);
        }

        [TestMethod]
        public void UpdatePrimaryKeys_long_to_int()
        {
            MockAssociativeEntity entity = new MockAssociativeEntity();
            long entity1Id = 1;
            long entity2Id = 2;
            Tuple<string, object>[] primaryKeys = new Tuple<string, object>[] { new Tuple<string, object>("RelationalEntity1Id", entity1Id), new Tuple<string, object>("RelationalEntity2Id", entity2Id) };

            U.UpdatePrimaryKeys(primaryKeys, entity);

            Assert.AreEqual(entity1Id, entity.RelationalEntity1Id);
            Assert.AreEqual(entity2Id, entity.RelationalEntity2Id);
        }
    }
}
