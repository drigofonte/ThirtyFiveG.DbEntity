using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using ThirtyFiveG.DbEntity.Validation;
using ThirtyFiveG.DbEntity.Query;
using ThirtyFiveG.DbEntity.Event;
using ThirtyFiveG.DbEntity.Entity;
using ThirtyFiveG.Validation;

namespace ThirtyFiveG.DbEntity.Portable.Test.Entity
{
    [TestClass]
    public class BaseDbEntityTest
    {
        [TestMethod]
        public async Task Push_entity_pushed_changes_reset()
        {
            IDictionary<string, Tuple<string, object>[]> primaryKeys = new Dictionary<string, Tuple<string, object>[]>()
            {
                { ".", new Tuple<string, object>[] { new Tuple<string, object>("id", 1) } }
            };
            Mock<IDataAccessLayer> mockDal = new Mock<IDataAccessLayer>();
            mockDal.Setup(m => m.UpdateEntity(It.IsAny<Type>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).Returns(Task.FromResult(primaryKeys));
            MockEntity entity = new MockEntity();
            entity.BeginEdit();
            entity.IntProperty = 1;
            string changesAsJson = entity.ChangesAsJson();

            await entity.PushAsync(mockDal.Object, 0, 1);

            mockDal.Verify(m => m.UpdateEntity(It.IsAny<Type>(), It.Is<string>(s => s.Equals(changesAsJson)), It.IsAny<IEnumerable<string>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once());
            Assert.AreEqual(0, entity.DbEntityChanges().Count());
        }

        [TestMethod]
        public void DbEntityPropertyChanged()
        {
            int before = 1;
            int after = 2;
            MockEntity entity = new MockEntity() { IntProperty = before };

            object eventBefore = null;
            object eventAfter = null;
            string eventPropertyName = null;
            entity.DbEntityPropertyChanged += (object sender, DbEntityPropertyChangedEventArgs e) =>
            {
                eventPropertyName = e.PropertyName;
                eventBefore = e.Before;
                eventAfter = e.After;
            };

            entity.IntProperty = after;

            Assert.AreEqual("IntProperty", eventPropertyName);
            Assert.AreEqual(before, eventBefore);
            Assert.AreEqual(after, eventAfter);
        }

        [TestMethod]
        public void BeginEdit()
        {
            bool isEventRaised = false;
            MockEntity entity = new MockEntity();
            entity.Editing += (s, e) => { isEventRaised = true; };
            entity.BeginEdit();

            Assert.IsTrue(isEventRaised);
        }

        [TestMethod]
        public void BeginEdit_tracked_entity_mark_entity_as_editable()
        {
            MockEntity entity = new MockEntity();
            entity.IsTracked = true;

            Assert.IsFalse(entity.IsEditing);

            entity.BeginEdit();

            Assert.IsTrue(entity.IsEditing);

            entity.EndEdit();

            Assert.IsFalse(entity.IsEditing);
        }

        [TestMethod]
        public void BeginEdit_entity_tracked_by_parent_entity_do_not_initiate_tracker()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity = new MockEntity();
            entity.RelationalEntity1 = relationalEntity;
            entity.BeginEdit();
            relationalEntity.BeginEdit();

            relationalEntity.IntProperty = 1;

            Assert.AreEqual(1, entity.DbEntityChanges().Count());
            Assert.IsTrue(relationalEntity.IsTracked);
            Should.Throw<ArgumentException>(() => { relationalEntity.DbEntityChanges(); });
        }

        [TestMethod]
        public void BeginEdit_record_edit_duration()
        {
            MockEntity entity = new MockEntity();
            entity.BeginEdit();
            int editDuration = 100;
            Thread.Sleep(editDuration);
            double actualEditDuration = entity.EditDuration;

            // Assert with a 10% margin of error to account for execution time
            double marginEditDuration = editDuration * 1.15;
            Assert.IsTrue(actualEditDuration >= editDuration && actualEditDuration <= marginEditDuration);
        }

        [TestMethod]
        public void BeginEdit_merge_changes_one_to_many_navigation_property()
        {
            MockEntity entity = new MockEntity();
            entity.BeginEdit();

            MockEntity relationalEntity = new MockEntity();
            relationalEntity.BeginEdit();

            relationalEntity.RelationalEntity1 = new MockEntity();
            relationalEntity.RelationalEntity1.StringProperty = "Address Line 1";
            entity.RelationalEntity1 = relationalEntity;
            relationalEntity.IntProperty = 1;

            Assert.AreEqual(4, entity.PropertyChanges().Count());
            Assert.IsTrue(entity.PropertyChanges().Any(c => c.DbEntityPropertyPath(entity).Equals(".RelationalEntity1")));
            Assert.IsTrue(entity.PropertyChanges().Any(c => c.DbEntityPropertyPath(entity).Equals(".RelationalEntity1.RelationalEntity1")));
            Assert.IsTrue(entity.PropertyChanges().Any(c => c.DbEntityPropertyPath(entity).Equals(".RelationalEntity1.RelationalEntity1.StringProperty")));
            Assert.IsTrue(entity.PropertyChanges().Any(c => c.DbEntityPropertyPath(entity).Equals(".RelationalEntity1.IntProperty")));
            Should.Throw<ArgumentNullException>(() => relationalEntity.DbEntityChanges());
            Assert.IsTrue(entity.IsTracked);
            Assert.IsTrue(entity.IsEditing);
            Assert.IsTrue(relationalEntity.IsTracked);
            Assert.IsFalse(relationalEntity.IsEditing);
        }

        [TestMethod]
        public void BeginEdit_merge_changes_add_entity_to_many_to_x_navigation_property()
        {
            MockEntity entity = new MockEntity();
            entity.BeginEdit();

            MockAssociativeEntity associativeEntity = new MockAssociativeEntity();
            associativeEntity.BeginEdit();

            associativeEntity.IntProperty = 2;
            entity.AssociativeEntities.Add(associativeEntity);
            associativeEntity.BoolProperty = true;

            Assert.AreEqual(3, entity.PropertyChanges().Count());
            Assert.IsTrue(entity.PropertyChanges().Any(c => c.DbEntityPropertyPath(entity).Equals(".AssociativeEntities[Guid=" + associativeEntity.Guid + "]")));
            Assert.IsTrue(entity.PropertyChanges().Any(c => c.DbEntityPropertyPath(entity).Equals(".AssociativeEntities[Guid=" + associativeEntity.Guid + "].IntProperty")));
            Assert.IsTrue(entity.PropertyChanges().Any(c => c.DbEntityPropertyPath(entity).Equals(".AssociativeEntities[Guid=" + associativeEntity.Guid + "].BoolProperty")));
            Should.Throw<ArgumentNullException>(() => associativeEntity.DbEntityChanges());
            Assert.IsTrue(entity.IsTracked);
            Assert.IsTrue(entity.IsEditing);
            Assert.IsTrue(associativeEntity.IsTracked);
            Assert.IsFalse(associativeEntity.IsEditing);
        }

        [TestMethod]
        public void BeginEdit_merge_changes_set_collection_for_many_to_x_navigation_property()
        {
            MockEntity entity = new MockEntity();
            entity.BeginEdit();

            MockAssociativeEntity associativeEntity = new MockAssociativeEntity();
            associativeEntity.BeginEdit();

            associativeEntity.IntProperty = 2;
            entity.AssociativeEntities = new HashSet<MockAssociativeEntity>() { associativeEntity };
            associativeEntity.BoolProperty = true;

            Assert.AreEqual(3, entity.PropertyChanges().Count());
            Assert.IsTrue(entity.PropertyChanges().Any(c => c.DbEntityPropertyPath(entity).Equals(".AssociativeEntities")));
            Assert.IsTrue(entity.PropertyChanges().Any(c => c.DbEntityPropertyPath(entity).Equals(".AssociativeEntities[Guid=" + associativeEntity.Guid + "].IntProperty")));
            Assert.IsTrue(entity.PropertyChanges().Any(c => c.DbEntityPropertyPath(entity).Equals(".AssociativeEntities[Guid=" + associativeEntity.Guid + "].BoolProperty")));
            Should.Throw<ArgumentNullException>(() => associativeEntity.DbEntityChanges());
            Assert.IsTrue(entity.IsTracked);
            Assert.IsTrue(entity.IsEditing);
            Assert.IsTrue(associativeEntity.IsTracked);
            Assert.IsFalse(associativeEntity.IsEditing);
        }

        [TestMethod]
        public void EndEdit_reset_edit_duration()
        {
            MockEntity entity = new MockEntity();
            entity.BeginEdit();
            int editDuration = 100;
            Thread.Sleep(editDuration);

            entity.EndEdit();
            Assert.AreEqual(0, entity.EditDuration);
        }

        [TestMethod]
        public void DiscardEdit()
        {
            bool isEventRaised = false;
            int before = 1;
            int after = 2;
            MockEntity entity = new MockEntity() { IntProperty = before };
            entity.Undo += (s, e) => { isEventRaised = true; };
            entity.BeginEdit();

            entity.IntProperty = after;
            entity.DiscardEdit();

            Assert.AreEqual(before, entity.IntProperty);
            Assert.IsFalse(entity.IsEditing);
            Assert.IsTrue(isEventRaised);
        }

        [TestMethod]
        public void DiscardEdit_before_begin_edit()
        {
            MockEntity entity = new MockEntity() { IntProperty = 1 };

            Should.Throw<ArgumentNullException>(() => entity.DiscardEdit());
        }

        [TestMethod]
        public void UndoEdit()
        {
            bool isEventRaised = false;
            int before = 1;
            int after = 2;
            MockEntity entity = new MockEntity() { IntProperty = before };
            entity.Undo += (s, e) => { isEventRaised = true; };
            entity.BeginEdit();

            entity.IntProperty = after;
            entity.UndoEdit();

            Assert.IsTrue(isEventRaised);
            Assert.AreEqual(before, entity.IntProperty);
            Assert.IsTrue(entity.IsEditing);
        }

        [TestMethod]
        public void UndoEdit_before_begin_edit()
        {
            MockEntity entity = new MockEntity() { IntProperty = 1 };

            Should.Throw<ArgumentNullException>(() => entity.UndoEdit());
        }

        [TestMethod]
        public void UndoAllEdit()
        {
            int before = 1;
            int after = 2;
            MockEntity entity = new MockEntity() { IntProperty = before };
            entity.BeginEdit();

            entity.IntProperty = after;
            entity.UndoAllEdit();

            Assert.AreEqual(before, entity.IntProperty);
            Assert.IsTrue(entity.IsEditing);
        }

        [TestMethod]
        public void UndoAllEdit_before_begin_edit()
        {
            MockEntity entity = new MockEntity() { IntProperty = 1 };

            Should.Throw<ArgumentNullException>(() => entity.UndoAllEdit());
        }

        [TestMethod]
        public void HasChanges_editing_no_changes()
        {
            MockEntity entity = new MockEntity();
            entity.BeginEdit();

            Assert.IsFalse(entity.HasChanges);
        }

        [TestMethod]
        public void HasChanges_editing_changes()
        {
            MockEntity entity = new MockEntity();
            entity.BeginEdit();

            entity.IntProperty = 1;

            Assert.IsTrue(entity.HasChanges);
        }

        [TestMethod]
        public void HasChanges_not_editing()
        {
            MockEntity entity = new MockEntity();
            entity.IntProperty = 1;

            Assert.IsFalse(entity.HasChanges);
        }

        [TestMethod]
        public async Task Push_include_duration()
        {
            IDictionary<string, Tuple<string, object>[]> primaryKeys = new Dictionary<string, Tuple<string, object>[]>()
            {
                { ".", new Tuple<string, object>[] { new Tuple<string, object>("id", 1) } }
            };
            Mock<IDataAccessLayer> mockDal = new Mock<IDataAccessLayer>();
            mockDal.Setup(m => m.UpdateEntity(It.IsAny<Type>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).Returns(Task.FromResult(primaryKeys));
            MockEntity entity = new MockEntity();
            entity.BeginEdit();
            int editDuration = 100;
            Thread.Sleep(editDuration);

            await entity.PushAsync(mockDal.Object, 0, 0);

            mockDal.Verify(m => m.UpdateEntity(It.IsAny<Type>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<int>(), It.Is<int>(i => i > 0), It.IsAny<int>()), Times.Once());
        }

        [TestMethod]
        public void DbEntityDeleted()
        {
            MockEntity entity = new MockEntity();
            bool eventRaised = false;
            entity.DbEntityDeleted += (object sender, EventArgs e) =>
            {
                eventRaised = true;
            };
            entity.IsDeleted = true;

            Assert.IsTrue(eventRaised);
        }

        [TestMethod]
        public void DbEntityUndeleted()
        {
            MockEntity entity = new MockEntity() { IsDeleted = true };
            bool eventRaised = false;
            entity.DbEntityUndeleted += (object sender, EventArgs e) =>
            {
                eventRaised = true;
            };
            entity.IsDeleted = false;

            Assert.IsTrue(eventRaised);
        }

        [TestMethod]
        public void IsEditable_false_prevents_entity_from_being_edited() 
        {
            MockEntity entity = new MockEntity();
            entity.IsEditable = false;

            Should.Throw<ArgumentException>(() => entity.BeginEdit());
        }

        [TestMethod]
        public void Push_update_primary_keys_without_tracking()
        {
            Mock<IDataAccessLayer> mockDal = new Mock<IDataAccessLayer>();
            IDictionary<string, Tuple<string, object>[]> keys = new Dictionary<string, Tuple<string, object>[]>()
            {
                { ".", new Tuple<string, object>[] { new Tuple<string, object>("id", int.MaxValue) } }
            };
            mockDal.Setup(m => m.UpdateEntity(typeof(MockEntity), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).Returns(Task.FromResult(keys));
            MockEntity entity = new MockEntity();
            entity.BeginEdit();
            entity.PushAsync(mockDal.Object, 0, 0).GetAwaiter().GetResult();

            Assert.AreEqual(0, entity.DbEntityChanges().Count());
            Assert.AreEqual(entity.id, keys.First().Value.First().Item2);
            Assert.AreEqual(EntityState.Persisted, entity.State);
        }

        [TestMethod]
        public void CanPush_not_editing()
        {
            MockEntity entity = new MockEntity();
            Assert.IsFalse(entity.CanPush);
        }

        [TestMethod]
        public void CanPush_editing()
        {
            MockEntity entity = new MockEntity();
            entity.BeginEdit();
            Assert.IsTrue(entity.CanPush);
        }

        [TestMethod]
        public void CanPush_merge_entity()
        {
            MockEntity relationalEntity = new MockEntity();
            relationalEntity.BeginEdit();
            MockEntity entity = new MockEntity();
            entity.BeginEdit();
            entity.RelationalEntities.Add(relationalEntity);

            Assert.IsFalse(relationalEntity.CanPush);
        }

        [TestMethod]
        public void RaisePushed()
        {
            bool isEventRaised = false;
            MockEntity entity = new MockEntity();
            entity.Pushed += (s, e) => { isEventRaised = true; };
            entity.BeginEdit();
            IDictionary<string, Tuple<string, object>[]> result = new Dictionary<string, Tuple<string, object>[]>();
            Mock<IDataAccessLayer> mockDal = new Mock<IDataAccessLayer>();
            mockDal.Setup(m => m.UpdateEntity(typeof(MockEntity), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).Returns(Task.FromResult(result));

            entity.PushAsync(mockDal.Object, 0, 0).GetAwaiter().GetResult();

            Assert.IsTrue(isEventRaised);
        }

        [TestMethod]
        public void BeforePushActions()
        {
            bool isActionInvoked = false;
            MockEntity entity = new MockEntity();
            entity.BeforePushActions.Add((e) => isActionInvoked = true);
            entity.BeginEdit();
            IDictionary<string, Tuple<string, object>[]> result = new Dictionary<string, Tuple<string, object>[]>();
            Mock<IDataAccessLayer> mockDal = new Mock<IDataAccessLayer>();
            mockDal.Setup(m => m.UpdateEntity(typeof(MockEntity), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).Returns(Task.FromResult(result));

            entity.PushAsync(mockDal.Object, 0, 0).GetAwaiter().GetResult();

            Assert.IsTrue(isActionInvoked);
        }
 
        [TestMethod]
        public void Validate_do_not_clear_previously_matched_rules_that_still_match_after_new_rules_are_matched()
        {
            IEnumerable<IValidationResult> results = Enumerable.Empty<IValidationResult>();
            MockEntity mockEntity = new MockEntity();
            mockEntity.Validated += (s, e) => { results = e.Data; };
            mockEntity.BeginEdit();

            mockEntity.RelationalEntity1 = new MockEntity(new HashSet<IValidationRule>() { new ValidationRule("StringProperty", string.Empty, (e) => true, ValidationResultType.Error) }) { StringProperty = "Changed" };
            mockEntity.RelationalEntity2 = new MockEntity(new HashSet<IValidationRule>() { new ValidationRule("StringProperty", string.Empty, (e) => true, ValidationResultType.Error) }) { StringProperty = "Changed" };

            Assert.AreEqual(2, mockEntity.ValidationResults.Count());
            Assert.IsTrue(mockEntity.ValidationResults.Any(r => r.PropertyPath.Equals(".RelationalEntity1.StringProperty")));
            Assert.IsTrue(mockEntity.ValidationResults.Any(r => r.PropertyPath.Equals(".RelationalEntity2.StringProperty")));

            Assert.AreEqual(2, results.Count());
            Assert.IsTrue(results.Any(r => r.PropertyPath.Equals(".RelationalEntity1.StringProperty")));
            Assert.IsTrue(results.Any(r => r.PropertyPath.Equals(".RelationalEntity2.StringProperty")));
        }
    }
}
