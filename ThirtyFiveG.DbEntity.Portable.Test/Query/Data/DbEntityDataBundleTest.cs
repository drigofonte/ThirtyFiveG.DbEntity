using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using ThirtyFiveG.DbEntity.Query.Data;

namespace ThirtyFiveG.DbEntity.Portable.Test.Query.Data
{
    [TestClass]
    public class DbEntityDataBundleTest
    {
        [TestMethod]
        public void GetPrimaryKeys()
        {
            MockEntity entity1 = new MockEntity() { id = 1 };
            MockEntity entity2 = new MockEntity() { id = 2 };
            List<MockEntity> entities = new List<MockEntity>() { entity1, entity2 };
            DbEntityDataBundle dataBundle = DbEntityDataBundle.GetInstance(1, 0, entities);

            IEnumerable<Tuple<string, object>[]> primaryKeys = dataBundle.GetPrimaryKeys();

            Assert.AreEqual(2, primaryKeys.Count());
            Assert.IsTrue(primaryKeys.Any(ks => ks.All(k => k.Item2.Equals(entity1.id))));
            Assert.IsTrue(primaryKeys.Any(ks => ks.All(k => k.Item2.Equals(entity2.id))));
        }

        [TestMethod]
        public void GetInstance_single()
        {
            MockEntity entity = new MockEntity() { id = 1 };
            DbEntityDataBundle dataBundle = DbEntityDataBundle.GetInstance(1, 0, entity);

            Assert.AreEqual(typeof(MockEntity), dataBundle.Type);
            Assert.AreEqual(1, dataBundle.Parameters.Count);
            Assert.IsTrue(dataBundle.Contains<MockEntity>(DbEntityDataBundle.EntityParameterKey));
            Assert.IsFalse(dataBundle.Contains<string>(DbEntityDataBundle.EntityJsonParameterKey));
            Assert.IsFalse(dataBundle.Contains<List<MockEntity>>(DbEntityDataBundle.EntitiesParameterKey));
            Assert.IsFalse(dataBundle.Contains<string>(DbEntityDataBundle.EntitiesJsonParameterKey));
            Assert.AreEqual(entity, dataBundle.GetParameter<MockEntity>(DbEntityDataBundle.EntityParameterKey));
        }

        [TestMethod]
        public void GetInstance_multiple()
        {
            MockEntity entity1 = new MockEntity() { id = 1 };
            MockEntity entity2 = new MockEntity() { id = 2 };
            List<MockEntity> entities = new List<MockEntity>() { entity1, entity2 };
            DbEntityDataBundle dataBundle = DbEntityDataBundle.GetInstance(1, 0, entities);

            Assert.AreEqual(typeof(MockEntity), dataBundle.Type);
            Assert.AreEqual(1, dataBundle.Parameters.Count);
            Assert.IsTrue(dataBundle.Contains<List<MockEntity>>(DbEntityDataBundle.EntitiesParameterKey));
            Assert.IsFalse(dataBundle.Contains<MockEntity>(DbEntityDataBundle.EntityParameterKey));
            Assert.IsFalse(dataBundle.Contains<string>(DbEntityDataBundle.EntityJsonParameterKey));
            Assert.IsFalse(dataBundle.Contains<string>(DbEntityDataBundle.EntitiesJsonParameterKey));
            Assert.AreEqual(2, dataBundle.GetParameter<List<MockEntity>>(DbEntityDataBundle.EntitiesParameterKey).Count());
            Assert.IsTrue(dataBundle.GetParameter<List<MockEntity>>(DbEntityDataBundle.EntitiesParameterKey).Contains(entity1));
            Assert.IsTrue(dataBundle.GetParameter<List<MockEntity>>(DbEntityDataBundle.EntitiesParameterKey).Contains(entity2));
        }

        [TestMethod]
        public void OnSerializing_empty()
        {
            List<MockEntity> entities = new List<MockEntity>();
            DbEntityDataBundle dataBundle = new DbEntityDataBundle();
            dataBundle.Type = typeof(MockEntity);
            dataBundle.AddParameter(DbEntityDataBundle.EntitiesParameterKey, entities);

            dataBundle.OnSerializing(default(StreamingContext));

            Assert.AreEqual(1, dataBundle.Parameters.Count);
            Assert.IsFalse(dataBundle.Contains<List<MockEntity>>(DbEntityDataBundle.EntitiesParameterKey));
            Assert.IsFalse(dataBundle.Contains<MockEntity>(DbEntityDataBundle.EntityParameterKey));
            Assert.IsFalse(dataBundle.Contains<string>(DbEntityDataBundle.EntityJsonParameterKey));
            Assert.IsTrue(dataBundle.Contains<string>(DbEntityDataBundle.EntitiesJsonParameterKey));
            Assert.IsFalse(string.IsNullOrEmpty(dataBundle.GetParameter<string>(DbEntityDataBundle.EntitiesJsonParameterKey)));
            Assert.AreEqual("[]", dataBundle.GetParameter<string>(DbEntityDataBundle.EntitiesJsonParameterKey));
        }

        [TestMethod]
        public void OnSerializing_entities()
        {
            MockEntity entity1 = new MockEntity() { id = 1 };
            MockEntity entity2 = new MockEntity() { id = 2 };
            List<MockEntity> entities = new List<MockEntity>() { entity1, entity2 };
            DbEntityDataBundle dataBundle = new DbEntityDataBundle();
            dataBundle.Type = typeof(MockEntity);
            dataBundle.AddParameter(DbEntityDataBundle.EntitiesParameterKey, entities);

            dataBundle.OnSerializing(default(StreamingContext));

            Assert.AreEqual(1, dataBundle.Parameters.Count);
            Assert.IsFalse(dataBundle.Contains<List<MockEntity>>(DbEntityDataBundle.EntitiesParameterKey));
            Assert.IsFalse(dataBundle.Contains<MockEntity>(DbEntityDataBundle.EntityParameterKey));
            Assert.IsFalse(dataBundle.Contains<string>(DbEntityDataBundle.EntityJsonParameterKey));
            Assert.IsTrue(dataBundle.Contains<string>(DbEntityDataBundle.EntitiesJsonParameterKey));
            Assert.IsFalse(string.IsNullOrEmpty(dataBundle.GetParameter<string>(DbEntityDataBundle.EntitiesJsonParameterKey)));
        }

        [TestMethod]
        public void OnSerializing_entity()
        {
            MockEntity entity = new MockEntity() { id = 1 };
            DbEntityDataBundle dataBundle = new DbEntityDataBundle();
            dataBundle.Type = typeof(MockEntity);
            dataBundle.AddParameter(DbEntityDataBundle.EntityParameterKey, entity);

            dataBundle.OnSerializing(default(StreamingContext));

            Assert.AreEqual(1, dataBundle.Parameters.Count);
            Assert.IsFalse(dataBundle.Contains<List<MockEntity>>(DbEntityDataBundle.EntitiesParameterKey));
            Assert.IsFalse(dataBundle.Contains<MockEntity>(DbEntityDataBundle.EntityParameterKey));
            Assert.IsTrue(dataBundle.Contains<string>(DbEntityDataBundle.EntityJsonParameterKey));
            Assert.IsFalse(dataBundle.Contains<string>(DbEntityDataBundle.EntitiesJsonParameterKey));
            Assert.IsFalse(string.IsNullOrEmpty(dataBundle.GetParameter<string>(DbEntityDataBundle.EntityJsonParameterKey)));
        }

        [TestMethod]
        public void OnDeserialized_entities_empty()
        {
            DbEntityDataBundle dataBundle = new DbEntityDataBundle();
            dataBundle.Type = typeof(MockEntity);
            dataBundle.AddParameter(DbEntityDataBundle.EntitiesJsonParameterKey, "[]");

            dataBundle.OnDeserialized(default(StreamingContext));

            Assert.AreEqual(1, dataBundle.Parameters.Count);
            Assert.IsTrue(dataBundle.Contains<List<MockEntity>>(DbEntityDataBundle.EntitiesParameterKey));
            Assert.IsFalse(dataBundle.Contains<string>(DbEntityDataBundle.EntitiesJsonParameterKey));
            Assert.IsFalse(dataBundle.Contains<MockEntity>(DbEntityDataBundle.EntityParameterKey));
            Assert.IsFalse(dataBundle.Contains<string>(DbEntityDataBundle.EntityJsonParameterKey));
            Assert.AreEqual(0, dataBundle.GetParameter<List<MockEntity>>(DbEntityDataBundle.EntitiesParameterKey).Count());
        }

        [TestMethod]
        public void OnDeserialized_entities()
        {
            MockEntity entity1 = new MockEntity() { id = 1 };
            MockEntity entity2 = new MockEntity() { id = 2 };
            DbEntityDataBundle dataBundle = new DbEntityDataBundle();
            dataBundle.Type = typeof(MockEntity);
            dataBundle.AddParameter(DbEntityDataBundle.EntitiesJsonParameterKey, "[{\"$id\":\"" + entity1.Guid + "\",\"id\":" + entity1.id + "},{\"$id\":\"" + entity2.Guid + "\",\"id\":" + entity2.id + "}]");

            dataBundle.OnDeserialized(default(StreamingContext));

            Assert.AreEqual(1, dataBundle.Parameters.Count);
            Assert.IsTrue(dataBundle.Contains<List<MockEntity>>(DbEntityDataBundle.EntitiesParameterKey));
            Assert.IsFalse(dataBundle.Contains<string>(DbEntityDataBundle.EntitiesJsonParameterKey));
            Assert.IsFalse(dataBundle.Contains<MockEntity>(DbEntityDataBundle.EntityParameterKey));
            Assert.IsFalse(dataBundle.Contains<string>(DbEntityDataBundle.EntityJsonParameterKey));
            Assert.AreEqual(2, dataBundle.GetParameter<List<MockEntity>>(DbEntityDataBundle.EntitiesParameterKey).Count());
            Assert.IsTrue(dataBundle.GetParameter<List<MockEntity>>(DbEntityDataBundle.EntitiesParameterKey).Any(j => j.id == entity1.id));
            Assert.IsTrue(dataBundle.GetParameter<List<MockEntity>>(DbEntityDataBundle.EntitiesParameterKey).Any(j => j.id == entity2.id));
        }

        [TestMethod]
        public void OnDeserialized_entity()
        {
            MockEntity entity = new MockEntity() { id = 1 };
            DbEntityDataBundle dataBundle = new DbEntityDataBundle();
            dataBundle.Type = typeof(MockEntity);
            dataBundle.AddParameter(DbEntityDataBundle.EntityJsonParameterKey, "{\"$id\":\"" + entity.Guid + "\",\"id\":" + entity.id + "}");

            dataBundle.OnDeserialized(default(StreamingContext));

            Assert.AreEqual(1, dataBundle.Parameters.Count);
            Assert.IsFalse(dataBundle.Contains<List<MockEntity>>(DbEntityDataBundle.EntitiesParameterKey));
            Assert.IsFalse(dataBundle.Contains<string>(DbEntityDataBundle.EntitiesJsonParameterKey));
            Assert.IsTrue(dataBundle.Contains<MockEntity>(DbEntityDataBundle.EntityParameterKey));
            Assert.IsFalse(dataBundle.Contains<string>(DbEntityDataBundle.EntityJsonParameterKey));
            Assert.AreEqual(entity.id, dataBundle.GetParameter<MockEntity>(DbEntityDataBundle.EntityParameterKey).id);
        }
    }
}