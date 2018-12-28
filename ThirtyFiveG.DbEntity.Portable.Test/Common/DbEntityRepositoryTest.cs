using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using ThirtyFiveG.DbEntity.Common;
using ThirtyFiveG.DbEntity.Entity;
using ThirtyFiveG.DbEntity.Projection.Entity;
using ThirtyFiveG.DbEntity.Query;

namespace ThirtyFiveG.DbEntity.Portable.Test.Common
{
    [TestClass]
    public class DbEntityRepositoryTest
    {
        [TestMethod]
        public void MarkPersisted_flat_entity()
        {
            MockEntity entity = new MockEntity();
            Assert.AreEqual(EntityState.New, entity.State);

            Tuple<string, object>[] primaryKeys = new Tuple<string, object>[] { new Tuple<string, object>("Key", "Value") };
            Mock<IDataAccessLayer> mockDal = new Mock<IDataAccessLayer>();
            mockDal.Setup(m => m.GetEntity<MockEntity, IDbEntityProjection>(primaryKeys)).Returns(Task.FromResult(entity));
            DbEntityRepository.MarkPersisted(entity, entity.GetType());

            Assert.AreEqual(EntityState.Persisted, entity.State);
        }

        [TestMethod]
        public void MarkPersisted_flat_one_to_one_relation()
        {
            MockEntity entity = new MockEntity() { RelationalEntity1 = new MockEntity() };
            Assert.AreEqual(EntityState.New, entity.State);
            Assert.AreEqual(EntityState.New, entity.RelationalEntity1.State);

            Tuple<string, object>[] primaryKeys = new Tuple<string, object>[] { new Tuple<string, object>("Key", "Value") };
            Mock<IDataAccessLayer> mockDal = new Mock<IDataAccessLayer>();
            mockDal.Setup(m => m.GetEntity<MockEntity, IDbEntityProjection>(primaryKeys)).Returns(Task.FromResult(entity));
            DbEntityRepository.MarkPersisted(entity, entity.GetType());

            Assert.AreEqual(EntityState.Persisted, entity.State);
            Assert.AreEqual(EntityState.Persisted, entity.RelationalEntity1.State);
        }

        [TestMethod]
        public void MarkPersisted_flat_one_to_many_relation()
        {
            MockEntity entity = new MockEntity();
            entity.RelationalEntities.Add(new MockEntity());
            Assert.AreEqual(EntityState.New, entity.State);
            Assert.AreEqual(EntityState.New, entity.RelationalEntities.Single().State);

            Tuple<string, object>[] primaryKeys = new Tuple<string, object>[] { new Tuple<string, object>("Key", "Value") };
            Mock<IDataAccessLayer> mockDal = new Mock<IDataAccessLayer>();
            mockDal.Setup(m => m.GetEntity<MockEntity, IDbEntityProjection>(primaryKeys)).Returns(Task.FromResult(entity));
            DbEntityRepository.MarkPersisted(entity, entity.GetType());

            Assert.AreEqual(EntityState.Persisted, entity.State);
            Assert.AreEqual(EntityState.Persisted, entity.RelationalEntities.Single().State);
        }

        [TestMethod]
        public void MarkPersisted_flat_one_to_one_and_one_to_many_relations()
        {
            MockEntity entity = new MockEntity() { RelationalEntity1 = new MockEntity() };
            entity.RelationalEntities.Add(new MockEntity());
            Assert.AreEqual(EntityState.New, entity.State);
            Assert.AreEqual(EntityState.New, entity.RelationalEntity1.State);
            Assert.AreEqual(EntityState.New, entity.RelationalEntities.Single().State);

            Tuple<string, object>[] primaryKeys = new Tuple<string, object>[] { new Tuple<string, object>("Key", "Value") };
            Mock<IDataAccessLayer> mockDal = new Mock<IDataAccessLayer>();
            mockDal.Setup(m => m.GetEntity<MockEntity, IDbEntityProjection>(primaryKeys)).Returns(Task.FromResult(entity));
            DbEntityRepository.MarkPersisted(entity, entity.GetType());

            Assert.AreEqual(EntityState.Persisted, entity.State);
            Assert.AreEqual(EntityState.Persisted, entity.RelationalEntity1.State);
            Assert.AreEqual(EntityState.Persisted, entity.RelationalEntities.Single().State);
        }

        [TestMethod]
        public void MarkPersisted_non_flat_one_to_one_and_one_to_one_relations()
        {
            MockEntity entity = new MockEntity() { RelationalEntity1 = new MockEntity() { RelationalEntity1 = new MockEntity() } };
            Assert.AreEqual(EntityState.New, entity.State);
            Assert.AreEqual(EntityState.New, entity.RelationalEntity1.State);
            Assert.AreEqual(EntityState.New, entity.RelationalEntity1.RelationalEntity1.State);

            Tuple<string, object>[] primaryKeys = new Tuple<string, object>[] { new Tuple<string, object>("Key", "Value") };
            Mock<IDataAccessLayer> mockDal = new Mock<IDataAccessLayer>();
            mockDal.Setup(m => m.GetEntity<MockEntity, IDbEntityProjection>(primaryKeys)).Returns(Task.FromResult(entity));
            DbEntityRepository.MarkPersisted(entity, entity.GetType());

            Assert.AreEqual(EntityState.Persisted, entity.State);
            Assert.AreEqual(EntityState.Persisted, entity.RelationalEntity1.State);
            Assert.AreEqual(EntityState.Persisted, entity.RelationalEntity1.RelationalEntity1.State);
        }

        [TestMethod]
        public void MarkPersisted_non_flat_one_to_many_and_one_to_one_relations()
        {
            MockEntity entity = new MockEntity();
            entity.RelationalEntities.Add(new MockEntity() { RelationalEntity1 = new MockEntity() });
            Assert.AreEqual(EntityState.New, entity.State);
            Assert.AreEqual(EntityState.New, entity.RelationalEntities.Single().State);
            Assert.AreEqual(EntityState.New, entity.RelationalEntities.Single().RelationalEntity1.State);

            Tuple<string, object>[] primaryKeys = new Tuple<string, object>[] { new Tuple<string, object>("Key", "Value") };
            Mock<IDataAccessLayer> mockDal = new Mock<IDataAccessLayer>();
            mockDal.Setup(m => m.GetEntity<MockEntity, IDbEntityProjection>(primaryKeys)).Returns(Task.FromResult(entity));
            DbEntityRepository.MarkPersisted(entity, entity.GetType());

            Assert.AreEqual(EntityState.Persisted, entity.State);
            Assert.AreEqual(EntityState.Persisted, entity.RelationalEntities.Single().State);
            Assert.AreEqual(EntityState.Persisted, entity.RelationalEntities.Single().RelationalEntity1.State);
        }

        [TestMethod]
        public void MarkPersisted_non_flat_one_to_one_and_one_to_many_relations()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity = new MockEntity();
            relationalEntity.RelationalEntities.Add(new MockEntity());
            entity.RelationalEntities.Add(new MockEntity() { RelationalEntity1 = relationalEntity });
            Assert.AreEqual(EntityState.New, entity.State);
            Assert.AreEqual(EntityState.New, entity.RelationalEntities.Single().State);
            Assert.AreEqual(EntityState.New, entity.RelationalEntities.Single().RelationalEntity1.State);
            Assert.AreEqual(EntityState.New, entity.RelationalEntities.Single().RelationalEntity1.RelationalEntities.Single().State);

            Tuple<string, object>[] primaryKeys = new Tuple<string, object>[] { new Tuple<string, object>("Key", "Value") };
            Mock<IDataAccessLayer> mockDal = new Mock<IDataAccessLayer>();
            mockDal.Setup(m => m.GetEntity<MockEntity, IDbEntityProjection>(primaryKeys)).Returns(Task.FromResult(entity));
            DbEntityRepository.MarkPersisted(entity, entity.GetType());

            Assert.AreEqual(EntityState.Persisted, entity.State);
            Assert.AreEqual(EntityState.Persisted, entity.RelationalEntities.Single().State);
            Assert.AreEqual(EntityState.Persisted, entity.RelationalEntities.Single().RelationalEntity1.State);
            Assert.AreEqual(EntityState.Persisted, entity.RelationalEntities.Single().RelationalEntity1.RelationalEntities.Single().State);
        }

        [TestMethod]
        public void MarkPersisted_non_flat_one_to_many_and_one_to_many_relations()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity = new MockEntity();
            relationalEntity.RelationalEntities.Add(new MockEntity());
            entity.RelationalEntities.Add(relationalEntity);
            Assert.AreEqual(EntityState.New, entity.State);
            Assert.AreEqual(EntityState.New, entity.RelationalEntities.Single().State);
            Assert.AreEqual(EntityState.New, entity.RelationalEntities.Single().RelationalEntities.Single().State);

            Tuple<string, object>[] primaryKeys = new Tuple<string, object>[] { new Tuple<string, object>("Key", "Value") };
            Mock<IDataAccessLayer> mockDal = new Mock<IDataAccessLayer>();
            mockDal.Setup(m => m.GetEntity<MockEntity, IDbEntityProjection>(primaryKeys)).Returns(Task.FromResult(entity));
            DbEntityRepository.MarkPersisted(entity, entity.GetType());

            Assert.AreEqual(EntityState.Persisted, entity.State);
            Assert.AreEqual(EntityState.Persisted, entity.RelationalEntities.Single().State);
            Assert.AreEqual(EntityState.Persisted, entity.RelationalEntities.Single().RelationalEntities.Single().State);
        }
    }
}
