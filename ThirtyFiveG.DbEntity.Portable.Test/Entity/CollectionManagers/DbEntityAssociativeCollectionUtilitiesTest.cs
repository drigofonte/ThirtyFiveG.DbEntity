using Microsoft.VisualStudio.TestTools.UnitTesting;

using U = ThirtyFiveG.DbEntity.Entity.CollectionManagers.DbEntityAssociativeCollectionUtilities;

namespace ThirtyFiveG.DbEntity.Portable.Test.Entity.CollectionManagers
{
    [TestClass]
    public class DbEntityAssociativeCollectionUtilitiesTest
    {
        [TestMethod]
        public void MatchesAssociativeEntity_new_entity_true()
        {
            MockEntity entity = new MockEntity();
            Assert.IsTrue(U.MatchAssociativeEntities(entity, entity.id, entity, entity.id));
        }

        [TestMethod]
        public void MatchesAssociativeEntity_new_entity_false()
        {
            MockEntity existingEntity = new MockEntity();
            MockEntity entityToMatch = new MockEntity();
            Assert.IsFalse(U.MatchAssociativeEntities(existingEntity, existingEntity.id, entityToMatch, entityToMatch.id));
        }

        [TestMethod]
        public void MatchesAssociativeEntity_persisted_entity_true()
        {
            MockEntity existingEntity = new MockEntity() { id = int.MaxValue };
            existingEntity.MarkPersisted();
            MockEntity entityToMatch = new MockEntity() { id = existingEntity.id };
            entityToMatch.MarkPersisted();
            Assert.IsTrue(U.MatchAssociativeEntities(existingEntity, existingEntity.id, entityToMatch, entityToMatch.id));
        }

        [TestMethod]
        public void MatchesAssociativeEntity_persisted_entity_false()
        {
            MockEntity existingEntity = new MockEntity() { id = int.MaxValue };
            existingEntity.MarkPersisted();
            MockEntity entityToMatch = new MockEntity() { id = int.MinValue };
            entityToMatch.MarkPersisted();
            Assert.IsFalse(U.MatchAssociativeEntities(existingEntity, existingEntity.id, entityToMatch, entityToMatch.id));
        }
    }
}
