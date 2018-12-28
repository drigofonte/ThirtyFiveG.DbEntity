using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using ThirtyFiveG.DbEntity.Entity;
using ThirtyFiveG.DbEntity.Common;
using ThirtyFiveG.DbEntity.Tracking;
using ThirtyFiveG.DbEntity.Validation;
using ThirtyFiveG.Validation;
using System.Threading;

namespace ThirtyFiveG.DbEntity.Portable.Test.Tracking
{
    [TestClass]
    public class PropertyChangeTrackerTest
    {
        [TestMethod]
        public void Undo_once()
        {
            int firstEditValue = 1;
            int secondEditValue = 2;
            MockEntity entity = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.IntProperty = firstEditValue;
            entity.IntProperty = secondEditValue;

            // Assert changes were definitely applied
            Assert.AreEqual(secondEditValue, entity.IntProperty);

            tracker.Undo();

            // Assert changes were reverted
            Assert.AreEqual(firstEditValue, entity.IntProperty);
            Assert.AreEqual(2, tracker.AllChanges().Count());
        }

        [TestMethod]
        public void Undo_grouped()
        {
            int originalIntProperty = 1;
            int editedIntProperty = 2;
            string originalStringProperty = "3";
            string editedStringProperty = "4";
            MockEntity entity = new MockEntity() { IntProperty = originalIntProperty, StringProperty = originalStringProperty };
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            tracker.Grouped(e =>
            {
                (e as MockEntity).IntProperty = editedIntProperty;
                (e as MockEntity).StringProperty = editedStringProperty;
            });

            tracker.Undo();

            Assert.AreEqual(2, tracker.AllChanges().Count());
            Assert.AreEqual(0, tracker.Changes().Count());
            Assert.AreEqual(originalIntProperty, entity.IntProperty);
            Assert.AreEqual(originalStringProperty, entity.StringProperty);
        }

        [TestMethod]
        public void Undo_grouped_change_add_new_grouped_change_undo_grouped_change()
        {
            int originalIntProperty = 1;
            int editedIntProperty = 2;
            string originalStringProperty = "3";
            string editedStringProperty = "4";
            double originalDoubleProperty = 5;
            double editedDoubleProperty = 6;
            float originalFloatProperty = 7;
            float editedFloatProperty = 8;
            MockEntity entity = new MockEntity() { IntProperty = originalIntProperty, StringProperty = originalStringProperty, DoubleProperty = originalDoubleProperty, FloatProperty = originalFloatProperty };
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            tracker.Grouped(e =>
            {
                (e as MockEntity).IntProperty = editedIntProperty;
                (e as MockEntity).StringProperty = editedStringProperty;
            });

            tracker.Undo();

            tracker.Grouped(e =>
            {
                (e as MockEntity).DoubleProperty = editedDoubleProperty;
                (e as MockEntity).FloatProperty = editedFloatProperty;
            });

            tracker.Undo();

            Assert.AreEqual(2, tracker.AllChanges().Count());
            Assert.AreEqual(0, tracker.Changes().Count());
            Assert.AreEqual(originalIntProperty, entity.IntProperty);
            Assert.AreEqual(originalStringProperty, entity.StringProperty);
            Assert.AreEqual(originalDoubleProperty, entity.DoubleProperty);
            Assert.AreEqual(originalFloatProperty, entity.FloatProperty);
        }

        [TestMethod]
        public void Undo_grouped_after_grouped_change()
        {
            int originalIntProperty = 1;
            int editedIntProperty = 2;
            string originalStringProperty = "3";
            string editedStringProperty = "4";
            double originalDoubleProperty = 5;
            double editedDoubleProperty = 6;
            float originalFloatProperty = 7;
            float editedFloatProperty = 8;
            MockEntity entity = new MockEntity() { IntProperty = originalIntProperty, StringProperty = originalStringProperty, DoubleProperty = originalDoubleProperty, FloatProperty = originalFloatProperty };
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            tracker.Grouped(e =>
            {
                (e as MockEntity).IntProperty = editedIntProperty;
                (e as MockEntity).StringProperty = editedStringProperty;
            });

            tracker.Grouped(e =>
            {
                (e as MockEntity).DoubleProperty = editedDoubleProperty;
                (e as MockEntity).FloatProperty = editedFloatProperty;
            });

            tracker.Undo();

            Assert.AreEqual(4, tracker.AllChanges().Count());
            Assert.AreEqual(2, tracker.Changes().Count());
            Assert.AreEqual(editedIntProperty, entity.IntProperty);
            Assert.AreEqual(editedStringProperty, entity.StringProperty);
            Assert.AreEqual(originalDoubleProperty, entity.DoubleProperty);
            Assert.AreEqual(originalFloatProperty, entity.FloatProperty);
        }

        [TestMethod]
        public void Undo_grouped_after_ungrouped_change()
        {
            int originalIntProperty = 1;
            int editedIntProperty = 2;
            string originalStringProperty = "3";
            string editedStringProperty = "4";
            double originalDoubleProperty = 5;
            double editedDoubleProperty = 6;
            MockEntity entity = new MockEntity() { IntProperty = originalIntProperty, StringProperty = originalStringProperty, DoubleProperty = originalDoubleProperty };
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            entity.IntProperty = editedIntProperty;

            tracker.Grouped(e =>
            {
                (e as MockEntity).StringProperty = editedStringProperty;
                (e as MockEntity).DoubleProperty = editedDoubleProperty;
            });

            tracker.Undo();

            Assert.AreEqual(3, tracker.AllChanges().Count());
            Assert.AreEqual(1, tracker.Changes().Count());
            Assert.AreEqual(editedIntProperty, entity.IntProperty);
            Assert.AreEqual(originalDoubleProperty, entity.DoubleProperty);
            Assert.AreEqual(originalStringProperty, entity.StringProperty);
        }

        [TestMethod]
        public void Undo_ungrouped_after_grouped_change()
        {
            int originalIntProperty = 1;
            int editedIntProperty = 2;
            string originalStringProperty = "3";
            string editedStringProperty = "4";
            double originalDoubleProperty = 5;
            double editedDoubleProperty = 6;
            MockEntity entity = new MockEntity() { IntProperty = originalIntProperty, StringProperty = originalStringProperty, DoubleProperty = originalDoubleProperty };
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            tracker.Grouped(e =>
            {
                (e as MockEntity).StringProperty = editedStringProperty;
                (e as MockEntity).DoubleProperty = editedDoubleProperty;
            });

            entity.IntProperty = editedIntProperty;

            tracker.Undo();

            Assert.AreEqual(3, tracker.AllChanges().Count());
            Assert.AreEqual(2, tracker.Changes().Count());
            Assert.AreEqual(editedStringProperty, entity.StringProperty);
            Assert.AreEqual(editedDoubleProperty, entity.DoubleProperty);
            Assert.AreEqual(originalIntProperty, entity.IntProperty);
        }

        [TestMethod]
        public void Undo_once_track_new_change()
        {
            int firstEditValue = 1;
            int secondEditValue = 2;
            MockEntity entity = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.IntProperty = firstEditValue;
            entity.IntProperty = secondEditValue;

            tracker.Undo();
            entity.DoubleProperty = 1d;

            Assert.AreEqual(2, tracker.AllChanges().Count());
            Assert.AreEqual(".IntProperty", tracker.AllChanges().First().PropertyPath);
            Assert.AreEqual(".DoubleProperty", tracker.AllChanges().Last().PropertyPath);
        }

        [TestMethod]
        public void Undo_twice()
        {
            int firstEditValue = 1;
            int secondEditValue = 2;
            int thirdEditValue = 3;
            MockEntity entity = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.IntProperty = firstEditValue;
            entity.IntProperty = secondEditValue;
            entity.IntProperty = thirdEditValue;

            // Assert changes were definitely applied
            Assert.AreEqual(thirdEditValue, entity.IntProperty);

            tracker.Undo();
            tracker.Undo();

            // Assert changes were reverted
            Assert.AreEqual(firstEditValue, entity.IntProperty);
            Assert.AreEqual(3, tracker.AllChanges().Count());
        }

        [TestMethod]
        public void Undo_once_no_db_entity_changes_tracked()
        {
            MockEntity entity = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.IntProperty = 1;

            tracker.Undo();

            Assert.AreEqual(0, tracker.DbEntityChanges().Count());
        }

        [TestMethod]
        public void Undo_once_one_db_entity_changes_tracked()
        {
            MockEntity entity = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.IntProperty = 1;
            entity.DoubleProperty = 1;

            tracker.Undo();

            Assert.AreEqual(1, tracker.DbEntityChanges().Count());
            Assert.AreEqual(".IntProperty", tracker.DbEntityChanges().Single());
        }

        [TestMethod]
        public void Undo_once_track_new_change_one_db_entity_change_tracked()
        {
            MockEntity entity = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.IntProperty = 1;

            tracker.Undo();
            entity.DoubleProperty = 1;

            Assert.AreEqual(1, tracker.DbEntityChanges().Count());
            Assert.AreEqual(".DoubleProperty", tracker.DbEntityChanges().Single());
        }

        [TestMethod]
        public void Undo_many_to_one_un_nullified_stop_tracking()
        {
            MockEntity entity = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            MockEntity relationalEntity = new MockEntity();
            entity.RelationalEntity1 = relationalEntity;
            tracker.Undo();
            relationalEntity.StringProperty = "Site Name";

            Assert.AreEqual(1, tracker.AllChanges().Count());
            Assert.AreEqual(".RelationalEntity1", tracker.AllChanges().Single().PropertyPath);
            Assert.AreEqual(0, tracker.DbEntityChanges().Count());
        }

        [TestMethod]
        public void Undo_many_to_one_nullified_resume_tracking()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity = new MockEntity();
            entity.RelationalEntity1 = relationalEntity;
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            entity.RelationalEntity1 = null;
            tracker.Undo();
            relationalEntity.StringProperty = "Site Name";

            Assert.AreEqual(1, tracker.AllChanges().Count());
            Assert.AreEqual(1, tracker.DbEntityChanges().Count());
            Assert.AreEqual(".RelationalEntity1.StringProperty", tracker.DbEntityChanges().Single());
        }

        [TestMethod]
        public void Undo_one_to_many_added_stop_tracking()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity1 = new MockEntity();
            MockEntity relationalEntity2 = new MockEntity() { RelationalEntity1 = relationalEntity1 };
            string entityPath = ".RelationalEntities[Guid=" + relationalEntity2.Guid + "]";
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            entity.RelationalEntities.Add(relationalEntity2);
            tracker.Undo();
            relationalEntity2.BoolProperty = true;
            relationalEntity1.StringProperty = "Contact Name";

            Assert.AreEqual(1, tracker.AllChanges().Count());
            Assert.AreEqual(entityPath, tracker.AllChanges().Single().EntityPath);
            Assert.AreEqual(0, tracker.DbEntityChanges().Count());
        }

        [TestMethod]
        public void Undo_one_to_many_removed_resume_tracking()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity1 = new MockEntity();
            MockEntity relationalEntity2 = new MockEntity() { RelationalEntity1 = relationalEntity1 };
            entity.RelationalEntities.Add(relationalEntity2);
            string entityPath = ".RelationalEntities[Guid=" + relationalEntity2.Guid + "]";
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            entity.RelationalEntities.Remove(relationalEntity2);
            tracker.Undo();
            relationalEntity2.BoolProperty = true;
            relationalEntity1.StringProperty = "Contact Name";

            Assert.AreEqual(2, tracker.AllChanges().Count());
            Assert.AreEqual(2, tracker.DbEntityChanges().Count());
            Assert.IsTrue(tracker.DbEntityChanges().Contains(entityPath + ".BoolProperty"));
            Assert.IsTrue(tracker.DbEntityChanges().Contains(entityPath + ".RelationalEntity1.StringProperty"));
        }

        [TestMethod]
        public void Undo_changes_on_nullified_entity()
        {
            MockEntity entity = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.IsEditing = true;
            MockEntity relationalEntity = new MockEntity();
            relationalEntity.BeginEdit();

            relationalEntity.StringProperty = "Changed";
            entity.RelationalEntity1 = relationalEntity;

            tracker.UndoAll();

            Assert.IsNull(entity.RelationalEntity1);
        }

        [TestMethod]
        public void Redo_changes_on_nullified_entity()
        {
            MockEntity entity = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.IsEditing = true;
            MockEntity relationalEntity = new MockEntity();
            relationalEntity.BeginEdit();

            string value = "Changed";
            relationalEntity.StringProperty = value;
            entity.RelationalEntity1 = relationalEntity;

            tracker.UndoAll();
            Assert.IsNull(entity.RelationalEntity1);

            tracker.RedoAll();
            Assert.IsNotNull(entity.RelationalEntity1);
            Assert.AreEqual(relationalEntity, entity.RelationalEntity1);
            Assert.AreEqual(value, entity.RelationalEntity1.StringProperty);
        }

        [TestMethod]
        public void Redo_no_changes()
        {
            MockEntity entity = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            Should.Throw<ArgumentException>(() => tracker.Redo());
        }

        [TestMethod]
        public void Redo_no_undone_changes()
        {
            MockEntity entity = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            entity.IntProperty = 1;

            Should.Throw<ArgumentException>(() => tracker.Redo());
        }

        [TestMethod]
        public void Undo_once_redo_once()
        {
            int firstEditValue = 1;
            int secondEditValue = 2;
            MockEntity entity = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.IntProperty = firstEditValue;
            entity.IntProperty = secondEditValue;

            // Assert changes were definitely applied
            Assert.AreEqual(secondEditValue, entity.IntProperty);

            tracker.Undo();
            tracker.Redo();

            // Assert changes were reverted
            Assert.AreEqual(secondEditValue, entity.IntProperty);
            Assert.AreEqual(2, tracker.AllChanges().Count());
        }

        [TestMethod]
        public void Undo_once_redo_once_grouped()
        {
            int originalIntProperty = 1;
            int editedIntProperty = 2;
            string originalStringProperty = "3";
            string editedStringProperty = "4";
            MockEntity entity = new MockEntity() { IntProperty = originalIntProperty, StringProperty = originalStringProperty };
            entity.MarkPersisted();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            tracker.Grouped(e =>
            {
                (e as MockEntity).IntProperty = editedIntProperty;
                (e as MockEntity).StringProperty = editedStringProperty;
            });

            tracker.Undo();
            tracker.Redo();

            Assert.AreEqual(2, tracker.AllChanges().Count());
            Assert.AreEqual(2, tracker.Changes().Count());
            Assert.AreEqual(editedIntProperty, entity.IntProperty);
            Assert.AreEqual(editedStringProperty, entity.StringProperty);
        }

        [TestMethod]
        public void Undo_once_redo_once_grouped_after_grouped()
        {
            int originalIntProperty = 1;
            int editedIntProperty = 2;
            string originalStringProperty = "3";
            string editedStringProperty = "4";
            double originalDoubleProperty = 5;
            double editedDoubleProperty = 6;
            float originalFloatProperty = 7;
            float editedFloatProperty = 8;
            MockEntity entity = new MockEntity() { IntProperty = originalIntProperty, StringProperty = originalStringProperty, DoubleProperty = originalDoubleProperty, FloatProperty = originalFloatProperty };
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            tracker.Grouped(e =>
            {
                (e as MockEntity).IntProperty = editedIntProperty;
                (e as MockEntity).StringProperty = editedStringProperty;
            });

            tracker.Grouped(e =>
            {
                (e as MockEntity).DoubleProperty = editedDoubleProperty;
                (e as MockEntity).FloatProperty = editedFloatProperty;
            });

            tracker.Undo();
            tracker.Redo();

            Assert.AreEqual(4, tracker.AllChanges().Count());
            Assert.AreEqual(4, tracker.Changes().Count());
            Assert.AreEqual(editedIntProperty, entity.IntProperty);
            Assert.AreEqual(editedStringProperty, entity.StringProperty);
            Assert.AreEqual(editedDoubleProperty, entity.DoubleProperty);
            Assert.AreEqual(editedFloatProperty, entity.FloatProperty);
        }

        [TestMethod]
        public void Undo_twice_redo_once_grouped_after_grouped()
        {
            int originalIntProperty = 1;
            int editedIntProperty = 2;
            string originalStringProperty = "3";
            string editedStringProperty = "4";
            double originalDoubleProperty = 5;
            double editedDoubleProperty = 6;
            float originalFloatProperty = 7;
            float editedFloatProperty = 8;
            MockEntity entity = new MockEntity() { IntProperty = originalIntProperty, StringProperty = originalStringProperty, DoubleProperty = originalDoubleProperty, FloatProperty = originalFloatProperty };
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            tracker.Grouped(e =>
            {
                (e as MockEntity).IntProperty = editedIntProperty;
                (e as MockEntity).StringProperty = editedStringProperty;
            });

            tracker.Grouped(e =>
            {
                (e as MockEntity).DoubleProperty = editedDoubleProperty;
                (e as MockEntity).FloatProperty = editedFloatProperty;
            });

            tracker.Undo();
            tracker.Undo();
            tracker.Redo();

            Assert.AreEqual(4, tracker.AllChanges().Count());
            Assert.AreEqual(2, tracker.Changes().Count());
            Assert.AreEqual(editedIntProperty, entity.IntProperty);
            Assert.AreEqual(editedStringProperty, entity.StringProperty);
            Assert.AreEqual(originalDoubleProperty, entity.DoubleProperty);
            Assert.AreEqual(originalFloatProperty, entity.FloatProperty);
        }

        [TestMethod]
        public void Undo_twice_redo_once_grouped_after_ungrouped()
        {
            int originalIntProperty = 1;
            int editedIntProperty = 2;
            string originalStringProperty = "3";
            string editedStringProperty = "4";
            double originalDoubleProperty = 5;
            double editedDoubleProperty = 6;
            MockEntity entity = new MockEntity() { IntProperty = originalIntProperty, StringProperty = originalStringProperty, DoubleProperty = originalDoubleProperty };
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            entity.IntProperty = editedIntProperty;

            tracker.Grouped(e =>
            {
                (e as MockEntity).StringProperty = editedStringProperty;
                (e as MockEntity).DoubleProperty = editedDoubleProperty;
            });

            tracker.Undo();
            tracker.Undo();
            tracker.Redo();

            Assert.AreEqual(3, tracker.AllChanges().Count());
            Assert.AreEqual(1, tracker.Changes().Count());
            Assert.AreEqual(editedIntProperty, entity.IntProperty);
            Assert.AreEqual(originalStringProperty, entity.StringProperty);
            Assert.AreEqual(originalDoubleProperty, entity.DoubleProperty);
        }

        [TestMethod]
        public void Undo_twice_redo_once_grouped_before_ungrouped()
        {
            int originalIntProperty = 1;
            int editedIntProperty = 2;
            string originalStringProperty = "3";
            string editedStringProperty = "4";
            double originalDoubleProperty = 5;
            double editedDoubleProperty = 6;
            MockEntity entity = new MockEntity() { IntProperty = originalIntProperty, StringProperty = originalStringProperty, DoubleProperty = originalDoubleProperty };
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            tracker.Grouped(e =>
            {
                (e as MockEntity).StringProperty = editedStringProperty;
                (e as MockEntity).DoubleProperty = editedDoubleProperty;
            });

            entity.IntProperty = editedIntProperty;

            tracker.Undo();
            tracker.Undo();
            tracker.Redo();

            Assert.AreEqual(3, tracker.AllChanges().Count());
            Assert.AreEqual(2, tracker.Changes().Count());
            Assert.AreEqual(originalIntProperty, entity.IntProperty);
            Assert.AreEqual(editedStringProperty, entity.StringProperty);
            Assert.AreEqual(editedDoubleProperty, entity.DoubleProperty);
        }

        [TestMethod]
        public void Undo_once_redo_once_track_new_change()
        {
            int firstEditValue = 1;
            int secondEditValue = 2;
            MockEntity entity = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.IntProperty = firstEditValue;
            entity.IntProperty = secondEditValue;

            tracker.Undo();
            tracker.Redo();

            entity.IntProperty = 1;

            Assert.AreEqual(3, tracker.AllChanges().Count());
        }

        [TestMethod]
        public void Undo_twice_redo_once()
        {
            int firstEditValue = 1;
            int secondEditValue = 2;
            int thirdEditValue = 3;
            MockEntity entity = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.IntProperty = firstEditValue;
            entity.IntProperty = secondEditValue;
            entity.IntProperty = thirdEditValue;

            // Assert changes were definitely applied
            Assert.AreEqual(thirdEditValue, entity.IntProperty);

            tracker.Undo();
            tracker.Undo();
            tracker.Redo();

            // Assert changes were reverted
            Assert.AreEqual(secondEditValue, entity.IntProperty);
            Assert.AreEqual(3, tracker.AllChanges().Count());
        }

        [TestMethod]
        public void Undo_twice_redo_twice()
        {
            int firstEditValue = 1;
            int secondEditValue = 2;
            int thirdEditValue = 3;
            MockEntity entity = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.IntProperty = firstEditValue;
            entity.IntProperty = secondEditValue;
            entity.IntProperty = thirdEditValue;

            // Assert changes were definitely applied
            Assert.AreEqual(thirdEditValue, entity.IntProperty);

            tracker.Undo();
            tracker.Undo();
            tracker.Redo();
            tracker.Redo();

            // Assert changes were reverted
            Assert.AreEqual(thirdEditValue, entity.IntProperty);
            Assert.AreEqual(3, tracker.AllChanges().Count());
        }

        [TestMethod]
        public void Undo_change_property_value_remove_last_edit_from_undo_list_save_new_edit()
        {
            int firstEditValue = 1;
            int secondEditValue = 2;
            int thirdEditValue = 3;
            MockEntity entity = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.IntProperty = firstEditValue;
            entity.IntProperty = secondEditValue;

            // Assert changes were definitely applied
            Assert.AreEqual(secondEditValue, entity.IntProperty);

            tracker.Undo();
            entity.IntProperty = thirdEditValue;
            tracker.Undo();

            // Assert changes were reverted
            Assert.AreEqual(firstEditValue, entity.IntProperty);
            Assert.AreEqual(2, tracker.AllChanges().Count());
        }

        [TestMethod]
        public void Undo_all()
        {
            int firstEditValue = 1;
            int secondEditValue = 2;
            int thirdEditValue = 3;
            MockEntity entity = new MockEntity() { IntProperty = 0 };
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            int originalValue = entity.IntProperty;
            entity.IntProperty = firstEditValue;
            entity.IntProperty = secondEditValue;
            entity.IntProperty = thirdEditValue;

            // Assert changes were definitely applied
            Assert.AreEqual(thirdEditValue, entity.IntProperty);

            tracker.UndoAll();

            // Assert changes were reverted
            Assert.AreEqual(originalValue, entity.IntProperty);
            Assert.AreEqual(3, tracker.AllChanges().Count());
        }

        [TestMethod]
        public void Undo_once_and_undo_all()
        {
            int firstEditValue = 1;
            int secondEditValue = 2;
            MockEntity entity = new MockEntity() { IntProperty = 0 };
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            int originalValue = entity.IntProperty;
            entity.IntProperty = firstEditValue;
            entity.IntProperty = secondEditValue;

            tracker.Undo();
            tracker.UndoAll();

            // Assert changes were reverted
            Assert.AreEqual(originalValue, entity.IntProperty);
            Assert.AreEqual(2, tracker.AllChanges().Count());
        }

        [TestMethod]
        public void Redo_all()
        {
            int firstEditValue = 1;
            int secondEditValue = 2;
            int thirdEditValue = 3;
            MockEntity entity = new MockEntity() { IntProperty = 0 };
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            int originalValue = entity.IntProperty;
            entity.IntProperty = firstEditValue;
            entity.IntProperty = secondEditValue;
            entity.IntProperty = thirdEditValue;

            // Assert changes were definitely applied
            Assert.AreEqual(thirdEditValue, entity.IntProperty);

            tracker.UndoAll();
            Assert.AreEqual(originalValue, entity.IntProperty);
            tracker.RedoAll();

            // Assert changes were reverted
            Assert.AreEqual(thirdEditValue, entity.IntProperty);
            Assert.AreEqual(3, tracker.AllChanges().Count());
        }

        [TestMethod]
        public void Redo_many_to_one_non_nullified_resume_tracking()
        {
            MockEntity entity = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            MockEntity relationalEntity = new MockEntity();
            entity.RelationalEntity1 = relationalEntity;
            tracker.Undo();
            tracker.Redo();
            relationalEntity.StringProperty = "Site Name";

            Assert.AreEqual(2, tracker.AllChanges().Count());
            Assert.AreEqual(2, tracker.DbEntityChanges().Count());
            Assert.IsTrue(tracker.DbEntityChanges().Contains(".RelationalEntity1"));
            Assert.IsTrue(tracker.DbEntityChanges().Contains(".RelationalEntity1.StringProperty"));
        }

        [TestMethod]
        public void Redo_many_to_one_nullified_stop_tracking()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity = new MockEntity();
            entity.RelationalEntity1 = relationalEntity;
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            entity.RelationalEntity1 = null;
            tracker.Undo();
            tracker.Redo();
            relationalEntity.StringProperty = "Site Name";

            Assert.AreEqual(1, tracker.AllChanges().Count());
            Assert.AreEqual(".RelationalEntity1", tracker.AllChanges().Single().PropertyPath);
            Assert.AreEqual(1, tracker.DbEntityChanges().Count());
        }

        [TestMethod]
        public void Redo_one_to_many_added_resume_tracking()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity = new MockEntity();
            MockAssociativeEntity associativeEntity = new MockAssociativeEntity() { RelationalEntity2 = relationalEntity };
            string entityPath = ".AssociativeEntities[Guid=" + associativeEntity.Guid + "]";
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            entity.AssociativeEntities.Add(associativeEntity);
            tracker.Undo();
            tracker.Redo();
            associativeEntity.BoolProperty = true;
            relationalEntity.StringProperty = "Contact Name";

            Assert.AreEqual(3, tracker.AllChanges().Count());
            Assert.AreEqual(3, tracker.DbEntityChanges().Count());
            Assert.IsTrue(tracker.DbEntityChanges().Contains(entityPath));
            Assert.IsTrue(tracker.DbEntityChanges().Contains(entityPath + ".BoolProperty"));
            Assert.IsTrue(tracker.DbEntityChanges().Contains(entityPath + ".RelationalEntity2.StringProperty"));
        }

        [TestMethod]
        public void Redo_one_to_many_removed_stop_tracking()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity = new MockEntity();
            MockAssociativeEntity associativeEntity = new MockAssociativeEntity() { RelationalEntity2 = relationalEntity };
            entity.AssociativeEntities.Add(associativeEntity);
            string entityPath = ".AssociativeEntities[Guid=" + associativeEntity.Guid + "]";
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            entity.AssociativeEntities.Remove(associativeEntity);
            tracker.Undo();
            tracker.Redo();
            associativeEntity.BoolProperty = true;
            relationalEntity.StringProperty = "Contact Name";

            Assert.AreEqual(1, tracker.AllChanges().Count());
            Assert.AreEqual(0, tracker.DbEntityChanges().Count());
        }

        [TestMethod]
        public void Undo_index()
        {
            int firstEditValue = 1;
            int secondEditValue = 2;
            int thirdEditValue = 3;
            MockEntity entity = new MockEntity() { IntProperty = 0 };
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            int originalValue = entity.IntProperty;
            entity.IntProperty = firstEditValue;
            entity.IntProperty = secondEditValue;
            entity.IntProperty = thirdEditValue;

            // Assert changes were definitely applied
            Assert.AreEqual(thirdEditValue, entity.IntProperty);

            tracker.Undo(1);

            // Assert changes were reverted
            Assert.AreEqual(firstEditValue, entity.IntProperty);
            Assert.AreEqual(3, tracker.AllChanges().Count());
        }

        [TestMethod]
        public void Track_initialised_no_property_tracked()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            Assert.AreEqual(0, tracker.AllChanges().Count());
        }

        [TestMethod]
        public void Track_flat_property_change_tracked()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.IntProperty = 1;

            Assert.AreEqual(1, tracker.AllChanges().Count());
            Assert.AreEqual(".IntProperty", tracker.AllChanges().Single().PropertyPath);
        }

        [TestMethod]
        public void Track_property_changed_to_same_value_no_change_tracked()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            entity.IntProperty = 1;
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.IntProperty = entity.IntProperty;

            Assert.AreEqual(0, tracker.AllChanges().Count());
        }

        [TestMethod]
        public void Track_new_relational_property_changed()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.RelationalEntity1 = new MockEntity();

            Assert.AreEqual(1, tracker.AllChanges().Count());
            Assert.AreEqual(".RelationalEntity1", tracker.AllChanges().Single().PropertyPath);
        }

        [TestMethod]
        public void Track_new_relational_sub_property_changed()
        {
            MockEntity entity = new MockEntity() { RelationalEntity1 = new MockEntity() };
            entity.MarkPersisted();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.RelationalEntity1 = new MockEntity();
            entity.RelationalEntity1.StringProperty = "Changed";

            Assert.AreEqual(2, tracker.AllChanges().Count());
            Assert.IsTrue(tracker.AllChanges().Select(c => c.PropertyPath).Contains(".RelationalEntity1"));
            Assert.IsTrue(tracker.AllChanges().Select(c => c.PropertyPath).Contains(".RelationalEntity1.StringProperty"));
        }

        [TestMethod]
        public void Track_relational_sub_property_changed()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            entity.RelationalEntity1 = new MockEntity();
            entity.RelationalEntity1.MarkPersisted();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.RelationalEntity1.StringProperty = "Changed";

            Assert.AreEqual(1, tracker.AllChanges().Count());
            Assert.AreEqual(".RelationalEntity1.StringProperty", tracker.AllChanges().Single().PropertyPath);
        }

        [TestMethod]
        public void Track_collection_relational_property_changed()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.RelationalEntities = new HashSet<MockEntity>();

            Assert.AreEqual(1, tracker.AllChanges().Count());
            Assert.IsTrue(tracker.AllChanges().Single().IsDbEntityEnumerable);
            Assert.AreEqual(".RelationalEntities", tracker.AllChanges().Single().PropertyPath);
        }

        [TestMethod]
        public void Track_collection_relational_sub_property_changed()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            MockEntity relationalEntity = new MockEntity();
            relationalEntity.MarkPersisted();
            entity.RelationalEntities = new HashSet<MockEntity>() { relationalEntity };
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.RelationalEntities.First().BoolProperty = true;

            Assert.AreEqual(1, tracker.AllChanges().Count());
            Assert.AreEqual(".RelationalEntities[Guid=" + relationalEntity.Guid + "].BoolProperty", tracker.AllChanges().Single().PropertyPath);
        }

        [TestMethod]
        public void Track_collection_relational_sub_relation_sub_property_changed()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            MockEntity relationalEntity = new MockEntity();
            relationalEntity.MarkPersisted();
            MockAssociativeEntity associativeEntity = new MockAssociativeEntity() { RelationalEntity2 = relationalEntity };
            associativeEntity.MarkPersisted();
            entity.AssociativeEntities = new HashSet<MockAssociativeEntity>() { associativeEntity };
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.AssociativeEntities.First().RelationalEntity2.StringProperty = "Changed";

            Assert.AreEqual(1, tracker.AllChanges().Count());
            Assert.AreEqual(".AssociativeEntities[Guid=" + associativeEntity.Guid + "].RelationalEntity2.StringProperty", tracker.AllChanges().Single().PropertyPath);
        }

        [TestMethod]
        public void Track_collection_relation_new_entity_added()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            MockAssociativeEntity associativeEntity = new MockAssociativeEntity();
            associativeEntity.MarkPersisted();
            entity.AssociativeEntities = new HashSet<MockAssociativeEntity>() { };
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.AssociativeEntities.Add(associativeEntity);

            Assert.AreEqual(1, tracker.AllChanges().Count());
            Assert.IsTrue(tracker.AllChanges().Single().IsDbEntityEnumerable);
            Assert.AreEqual(".AssociativeEntities[Guid=" + associativeEntity.Guid + "]", tracker.AllChanges().Single().DbEntityPropertyPath(entity));
            Assert.AreEqual(1, tracker.DbEntityChanges().Count());
        }


        [TestMethod]
        public void Track_collection_relation_sub_sub_collection_relation_new_entity_added()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            MockEntity relationalEntity = new MockEntity() { id = 1, RelationalEntities = new HashSet<MockEntity>() };
            relationalEntity.MarkPersisted();
            MockAssociativeEntity associativeEntity = new MockAssociativeEntity() { RelationalEntity2 = relationalEntity, RelationalEntity2Id = relationalEntity.id, RelationalEntity1Id = entity.id };
            associativeEntity.MarkPersisted();
            entity.AssociativeEntities.Add(associativeEntity);
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            MockEntity relationalEntity1 = new MockEntity();
            relationalEntity.RelationalEntities.Add(relationalEntity1);

            Assert.AreEqual(1, tracker.AllChanges().Count());
            Assert.AreEqual(".AssociativeEntities[Guid=" + associativeEntity.Guid + ",RelationalEntity2Id=" + relationalEntity.id + "].RelationalEntity2.RelationalEntities[Guid=" + relationalEntity1.Guid + "]", tracker.AllChanges().Single().DbEntityPropertyPath(entity));
            Assert.AreEqual(1, tracker.DbEntityChanges().Count());
        }

        [TestMethod]
        public void Track_collection_relation_sub_sub_collection_relation_multiple_new_entities_added_distinct_changes()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            MockEntity relationalEntity = new MockEntity() { id = 1, RelationalEntities = new HashSet<MockEntity>() };
            relationalEntity.MarkPersisted();
            MockAssociativeEntity associativeEntity = new MockAssociativeEntity() { RelationalEntity2 = relationalEntity, RelationalEntity2Id = relationalEntity.id, RelationalEntity1Id = entity.id };
            associativeEntity.MarkPersisted();
            entity.AssociativeEntities.Add(associativeEntity);
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            MockEntity relationalEntity1 = new MockEntity();
            MockEntity relationalEntity2 = new MockEntity();
            relationalEntity.RelationalEntities.Add(relationalEntity1);
            relationalEntity.RelationalEntities.Add(relationalEntity2);

            Assert.AreEqual(2, tracker.DbEntityChanges().Count());
            Assert.IsTrue(tracker.DbEntityChanges().Contains(".AssociativeEntities[Guid=" + associativeEntity.Guid + ",RelationalEntity2Id=" + relationalEntity.id+"].RelationalEntity2.RelationalEntities[Guid=" + relationalEntity1.Guid + "]"));
            Assert.IsTrue(tracker.DbEntityChanges().Contains(".AssociativeEntities[Guid=" + associativeEntity.Guid + ",RelationalEntity2Id=" + relationalEntity.id + "].RelationalEntity2.RelationalEntities[Guid=" + relationalEntity2.Guid + "]"));
        }

        [TestMethod]
        public void Track_turn_off_changes_tracking()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            tracker.Untracked(e =>
            {
                (e as MockEntity).IntProperty = 1;
            });

            Assert.AreEqual(0, tracker.AllChanges().Count());
        }

        [TestMethod]
        public void Track_grouped_changes()
        {
            MockEntity entity = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            string groupGuid = tracker.Grouped(e =>
            {
                (e as MockEntity).IntProperty = 1;
                (e as MockEntity).StringProperty = "Changed";
            });

            Assert.AreEqual(2, tracker.AllChanges().Count());
            Assert.IsTrue(tracker.AllChanges().Select(c => c.PropertyPath).Contains(".IntProperty"));
            Assert.IsTrue(tracker.AllChanges().Select(c => c.PropertyPath).Contains(".StringProperty"));
            Assert.AreEqual(groupGuid, tracker.AllChanges().First().GroupGuid);
            Assert.AreEqual(groupGuid, tracker.AllChanges().Last().GroupGuid);
        }

        [TestMethod]
        public void Track_grouped_changes_after_ungrouped_changes()
        {
            MockEntity entity = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            entity.IntProperty = 1;
            string groupGuid = tracker.Grouped(e =>
            {
                (e as MockEntity).StringProperty = "Changed";
                (e as MockEntity).DoubleProperty = 3;
            });

            Assert.AreEqual(3, tracker.AllChanges().Count());
            Assert.IsTrue(tracker.AllChanges().Select(c => c.PropertyPath).Contains(".IntProperty"));
            Assert.IsTrue(tracker.AllChanges().Select(c => c.PropertyPath).Contains(".StringProperty"));
            Assert.IsTrue(tracker.AllChanges().Select(c => c.PropertyPath).Contains(".DoubleProperty"));
            Assert.IsFalse(tracker.AllChanges().Single(c => c.PropertyPath.Equals(".IntProperty")).IsGrouped);
            Assert.IsNull(tracker.AllChanges().Single(c => c.PropertyPath.Equals(".IntProperty")).GroupGuid);
            Assert.AreEqual(groupGuid, tracker.AllChanges().Single(c => c.PropertyPath.Equals(".StringProperty")).GroupGuid);
            Assert.AreEqual(groupGuid, tracker.AllChanges().Single(c => c.PropertyPath.Equals(".DoubleProperty")).GroupGuid);
        }

        [TestMethod]
        public void Track_grouped_changes_before_ungrouped_changes()
        {
            MockEntity entity = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            string groupGuid = tracker.Grouped(e =>
            {
                (e as MockEntity).StringProperty = "Changed";
                (e as MockEntity).DoubleProperty = 3;
            });
            entity.IntProperty = 1;

            Assert.AreEqual(3, tracker.AllChanges().Count());
            Assert.IsTrue(tracker.AllChanges().Select(c => c.PropertyPath).Contains(".IntProperty"));
            Assert.IsTrue(tracker.AllChanges().Select(c => c.PropertyPath).Contains(".StringProperty"));
            Assert.IsTrue(tracker.AllChanges().Select(c => c.PropertyPath).Contains(".DoubleProperty"));
            Assert.AreEqual(groupGuid, tracker.AllChanges().Single(c => c.PropertyPath.Equals(".StringProperty")).GroupGuid);
            Assert.AreEqual(groupGuid, tracker.AllChanges().Single(c => c.PropertyPath.Equals(".DoubleProperty")).GroupGuid);
            Assert.IsFalse(tracker.AllChanges().Single(c => c.PropertyPath.Equals(".IntProperty")).IsGrouped);
            Assert.IsNull(tracker.AllChanges().Single(c => c.PropertyPath.Equals(".IntProperty")).GroupGuid);
        }

        [TestMethod]
        public void Track_relational_property_changed_sub_relational_properties_tracked()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.RelationalEntity1 = new MockEntity() { RelationalEntity1 = new MockEntity() };
            entity.RelationalEntity1.StringProperty = "Changed";
            entity.RelationalEntity1.RelationalEntity1.IntProperty = 222;

            Assert.AreEqual(3, tracker.AllChanges().Count());
            Assert.IsTrue(tracker.AllChanges().Select(c => c.PropertyPath).Contains(".RelationalEntity1"));
            Assert.IsTrue(tracker.AllChanges().Select(c => c.PropertyPath).Contains(".RelationalEntity1.StringProperty"));
            Assert.IsTrue(tracker.AllChanges().Select(c => c.PropertyPath).Contains(".RelationalEntity1.RelationalEntity1.IntProperty"));
        }

        [TestMethod]
        public void Track_self_referencing_relational_sub_property()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            MockEntity relationalEntity = new MockEntity();
            relationalEntity.MarkPersisted();
            relationalEntity.RelationalEntity1 = entity;
            entity.RelationalEntities.Add(relationalEntity);
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            // Getting past this point already means all is well. The tracker did not go into an infinite loop trying to track the relational entity

            entity.StringProperty = "Changed";
            relationalEntity.StringProperty = "Changed";

            Assert.AreEqual(2, tracker.AllChanges().Count());
            Assert.IsTrue(tracker.AllChanges().Select(c => c.PropertyPath).Contains(".StringProperty"));
            Assert.IsTrue(tracker.AllChanges().Select(c => c.PropertyPath).Contains(".RelationalEntities[Guid=" + relationalEntity.Guid + "].StringProperty"));
        }

        [TestMethod]
        public void Track_string_changes_do_not_track_every_user_key_stroke_only_keep_the_last_change()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.StringProperty = "a";
            entity.StringProperty = "ab";
            entity.StringProperty = "abc";

            Assert.AreEqual(1, tracker.AllChanges().Count());
            Assert.IsTrue(tracker.AllChanges().Select(c => c.PropertyPath).Contains(".StringProperty"));
            Assert.AreEqual(null, tracker.AllChanges().Single().Before);
        }

        [TestMethod]
        public void Track_new_entity_added_to_collection_track_new_entity_changes()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            MockEntity relationalEntity = new MockEntity();
            relationalEntity.MarkPersisted();
            MockAssociativeEntity associativeEntity = new MockAssociativeEntity() { RelationalEntity2 = relationalEntity };
            associativeEntity.MarkPersisted();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.AssociativeEntities.Add(associativeEntity);
            associativeEntity.BoolProperty = true;

            Assert.AreEqual(2, tracker.AllChanges().Count());
            Assert.IsTrue(tracker.AllChanges().Select(c => c.PropertyPath).Contains(".AssociativeEntities[Guid=" + associativeEntity.Guid + "]."));
            Assert.IsTrue(tracker.AllChanges().Select(c => c.PropertyPath).Contains(".AssociativeEntities[Guid=" + associativeEntity.Guid + "].BoolProperty"));
        }

        [TestMethod]
        public void Track_string_changes_do_not_track_every_user_key_stroke_only_keep_the_last_change_multiple_property_changes()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.StringProperty = string.Empty;
            entity.StringProperty = null;
            entity.StringProperty = "a";
            entity.StringProperty = "ab";
            entity.StringProperty2 = "another string field";
            entity.StringProperty = "abc";

            Assert.AreEqual(5, tracker.AllChanges().Count());
            Assert.IsTrue(tracker.AllChanges().Select(c => c.PropertyPath).Contains(".StringProperty"));
            Assert.IsTrue(tracker.AllChanges().Select(c => c.PropertyPath).Contains(".StringProperty2"));
        }

        [TestMethod]
        public void Stop_tracking_and_empty_all_changes_tracked()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            MockEntity relationalEntity = new MockEntity();
            relationalEntity.MarkPersisted();
            MockAssociativeEntity associativeEntity = new MockAssociativeEntity() { RelationalEntity2 = relationalEntity };
            associativeEntity.MarkPersisted();
            entity.AssociativeEntities = new HashSet<MockAssociativeEntity>() { associativeEntity };
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.AssociativeEntities.First().RelationalEntity2.StringProperty = "Changed";
            MockAssociativeEntity newAssociativeEntity = new MockAssociativeEntity();
            entity.AssociativeEntities.Add(newAssociativeEntity);
            entity.RelationalEntity1 = new MockEntity() { RelationalEntity1 = new MockEntity() };
            entity.RelationalEntity1.StringProperty = "Changed";
            entity.RelationalEntity1.RelationalEntity1.IntProperty = 222;

            Assert.AreEqual(5, tracker.DbEntityChanges().Count());
            Assert.IsTrue(tracker.DbEntityChanges().Contains(".AssociativeEntities[Guid=" + newAssociativeEntity.Guid + "]"));
            Assert.IsTrue(tracker.DbEntityChanges().Contains(".AssociativeEntities[Guid=" + associativeEntity.Guid + "].RelationalEntity2.StringProperty"));
            Assert.IsTrue(tracker.DbEntityChanges().Contains(".RelationalEntity1"));
            Assert.IsTrue(tracker.DbEntityChanges().Contains(".RelationalEntity1.StringProperty"));
            Assert.IsTrue(tracker.DbEntityChanges().Contains(".RelationalEntity1.RelationalEntity1.IntProperty"));

            tracker.Stop();
            tracker.PurgeChanges();
            // Force the parent entity to be marked as tracking changes in order to test that neither the 'PropertyChanged' nor the 'CollectionChanged' events are triggering the recording of property changes
            entity.IsTrackingChanges = true;

            entity.AssociativeEntities.First().RelationalEntity2.StringProperty = "Changed Again";
            entity.AssociativeEntities.Add(new MockAssociativeEntity());
            entity.RelationalEntity1 = new MockEntity() { RelationalEntity1 = new MockEntity() };
            entity.RelationalEntity1.StringProperty = "Changed Again";
            entity.RelationalEntity1.RelationalEntity1.IntProperty = 1;

            Assert.AreEqual(0, tracker.AllChanges().Count());
        }

        [TestMethod]
        public void Track_entity_removed_from_collection_stop_tracking()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            MockEntity relationalEntity = new MockEntity();
            relationalEntity.MarkPersisted();
            MockAssociativeEntity associativeEntity = new MockAssociativeEntity() { RelationalEntity2 = relationalEntity };
            associativeEntity.MarkPersisted();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            // Assert changes are being tracked
            entity.AssociativeEntities.Add(associativeEntity);
            associativeEntity.RelationalEntity2.StringProperty = "Changed";
            Assert.AreEqual(2, tracker.AllChanges().Count());

            // Assert changes have been removed
            entity.AssociativeEntities.Remove(associativeEntity);
            Assert.AreEqual(3, tracker.AllChanges().Count());

            // Assert changes are no longer being tracked
            associativeEntity.RelationalEntity2.StringProperty = "Changed Again";
            Assert.AreEqual(3, tracker.AllChanges().Count());
        }

        [TestMethod]
        public void Track_entity_property_nullified_stop_tracking()
        {
            MockEntity relationalEntity = new MockEntity();
            relationalEntity.MarkPersisted();
            MockEntity entity = new MockEntity() { RelationalEntity1 = relationalEntity };
            entity.MarkPersisted();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            // Assert changes are being tracked
            relationalEntity.StringProperty = "Changed";
            Assert.AreEqual(1, tracker.AllChanges().Count());
            Assert.IsTrue(tracker.AllChanges().Any(c => c.PropertyPath.Equals(".RelationalEntity1.StringProperty")));

            // Assert changes have been removed
            entity.RelationalEntity1 = null;
            Assert.AreEqual(2, tracker.AllChanges().Count());
            Assert.IsTrue(tracker.AllChanges().Any(c => c.PropertyPath.Equals(".RelationalEntity1")));
            Assert.IsTrue(tracker.AllChanges().Any(c => c.PropertyPath.Equals(".RelationalEntity1.StringProperty")));

            // Assert changes are no longer being tracked
            relationalEntity.StringProperty = "Changed Again";
            Assert.AreEqual(2, tracker.AllChanges().Count());
            Assert.IsTrue(tracker.AllChanges().Any(c => c.PropertyPath.Equals(".RelationalEntity1")));
            Assert.IsTrue(tracker.AllChanges().Any(c => c.PropertyPath.Equals(".RelationalEntity1.StringProperty")));
        }

        [TestMethod]
        public void Track_linker_entity_changed_before_foreign_keys_are_set()
        {
            MockEntity entity = new MockEntity() { id = 1 };
            entity.MarkPersisted();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            MockAssociativeEntity associativeEntity = new MockAssociativeEntity();
            entity.AssociativeEntities.Add(associativeEntity);
            associativeEntity.RelationalEntity1Id = entity.id;
            MockEntity relationalEntity = new MockEntity() { id = 2 };
            associativeEntity.RelationalEntity2 = relationalEntity;
            associativeEntity.RelationalEntity2Id = relationalEntity.id;

            Assert.AreEqual(4, tracker.AllChanges().Count());
            Assert.IsTrue(tracker.AllChanges().Select(c => c.PropertyPath).Contains(".AssociativeEntities[Guid=" + associativeEntity.Guid + "]."));
            Assert.IsTrue(tracker.AllChanges().Select(c => c.PropertyPath).Contains(".AssociativeEntities[Guid=" + associativeEntity.Guid + "].RelationalEntity1Id"));
            Assert.IsTrue(tracker.AllChanges().Select(c => c.PropertyPath).Contains(".AssociativeEntities[Guid=" + associativeEntity.Guid + "].RelationalEntity2Id"));
            Assert.IsTrue(tracker.AllChanges().Select(c => c.PropertyPath).Contains(".AssociativeEntities[Guid=" + associativeEntity.Guid + "].RelationalEntity2"));
            Assert.IsTrue(tracker.DbEntityChanges().Contains(".AssociativeEntities[RelationalEntity1Id=" + entity.id + ",RelationalEntity2Id=" + relationalEntity.id + "]"));
            Assert.IsTrue(tracker.DbEntityChanges().Contains(".AssociativeEntities[RelationalEntity1Id=" + entity.id + ",RelationalEntity2Id=" + relationalEntity.id + "].RelationalEntity1Id"));
            Assert.IsTrue(tracker.DbEntityChanges().Contains(".AssociativeEntities[RelationalEntity1Id=" + entity.id + ",RelationalEntity2Id=" + relationalEntity.id + "].RelationalEntity2Id"));
            Assert.IsTrue(tracker.DbEntityChanges().Contains(".AssociativeEntities[RelationalEntity1Id=" + entity.id + ",RelationalEntity2Id=" + relationalEntity.id + "].RelationalEntity2"));
        }

        [TestMethod]
        public void Track_new_one_to_many_relation_deleted_no_changes_returned()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            MockEntity relationalEntity = new MockEntity();
            entity.RelationalEntities.Add(relationalEntity);
            relationalEntity.IsDeleted = true;
            relationalEntity.BoolProperty = true;

            // Deleted new entity, no changes returned
            Assert.AreEqual(0, tracker.DbEntityChanges().Count());

            relationalEntity.IsDeleted = false;

            // Undeleted new entity, previous changes returned
            Assert.AreEqual(2, tracker.DbEntityChanges().Count());
            Assert.IsTrue(tracker.DbEntityChanges().Contains(".RelationalEntities[Guid=" + relationalEntity.Guid + "]"));
            Assert.IsTrue(tracker.DbEntityChanges().Contains(".RelationalEntities[Guid=" + relationalEntity.Guid + "].BoolProperty"));
        }

        [TestMethod]
        public void Track_new_one_to_many_relation_deleted_no_changes_new_non_deleted_contact_added_changes()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            MockEntity relationalEntity1 = new MockEntity();
            entity.RelationalEntities.Add(relationalEntity1);
            relationalEntity1.IsDeleted = true;
            relationalEntity1.BoolProperty = true;

            MockEntity relationalEntity2 = new MockEntity();
            entity.RelationalEntities.Add(relationalEntity2);
            relationalEntity2.BoolProperty = true;

            // Deleted new entity, no changes returned
            Assert.AreEqual(2, tracker.DbEntityChanges().Count());
            Assert.IsTrue(tracker.DbEntityChanges().Contains(".RelationalEntities[Guid=" + relationalEntity2.Guid + "]"));
            Assert.IsTrue(tracker.DbEntityChanges().Contains(".RelationalEntities[Guid=" + relationalEntity2.Guid + "].BoolProperty"));

            relationalEntity1.IsDeleted = false;

            // Undeleted new entity, previous changes returned
            Assert.AreEqual(4, tracker.DbEntityChanges().Count());
            Assert.IsTrue(tracker.DbEntityChanges().Contains(".RelationalEntities[Guid=" + relationalEntity1.Guid + "]"));
            Assert.IsTrue(tracker.DbEntityChanges().Contains(".RelationalEntities[Guid=" + relationalEntity2.Guid + "]"));
            Assert.IsTrue(tracker.DbEntityChanges().Contains(".RelationalEntities[Guid=" + relationalEntity1.Guid + "].BoolProperty"));
            Assert.IsTrue(tracker.DbEntityChanges().Contains(".RelationalEntities[Guid=" + relationalEntity2.Guid + "].BoolProperty"));
        }

        [TestMethod]
        public void Track_new_one_to_many_relation_deleted_no_changes_returned_changes_purged_deleted_entity_changes_not_purged()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            MockEntity relationalEntity = new MockEntity();
            entity.RelationalEntities.Add(relationalEntity);
            relationalEntity.IsDeleted = true;
            relationalEntity.BoolProperty = true;

            // Deleted new entity, no changes returned
            Assert.AreEqual(0, tracker.DbEntityChanges().Count());

            tracker.PurgeChanges();
            relationalEntity.IsDeleted = false;

            // Undeleted new entity, previous changes returned
            Assert.AreEqual(2, tracker.DbEntityChanges().Count());
            Assert.IsTrue(tracker.DbEntityChanges().Contains(".RelationalEntities[Guid=" + relationalEntity.Guid + "]"));
            Assert.IsTrue(tracker.DbEntityChanges().Contains(".RelationalEntities[Guid=" + relationalEntity.Guid + "].BoolProperty"));
        }

        [TestMethod]
        public void Track_persisted_one_to_many_relation_deleted_changes_returned()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            MockEntity relationalEntity = new MockEntity();
            relationalEntity.MarkPersisted();
            entity.RelationalEntities.Add(relationalEntity);
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            relationalEntity.IsDeleted = true;
            relationalEntity.BoolProperty = true;

            // Deleted new entity, no changes returned
            Assert.AreEqual(2, tracker.DbEntityChanges().Count());

            tracker.PurgeChanges();

            Assert.AreEqual(0, tracker.DbEntityChanges().Count());
        }

        [TestMethod]
        public void Track_mark_all_objects_as_tracked()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity1 = new MockEntity();
            MockAssociativeEntity associativeEntity = new MockAssociativeEntity() { RelationalEntity2 = relationalEntity1 };
            entity.AssociativeEntities.Add(associativeEntity);
            MockEntity relationalEntity2 = new MockEntity();
            entity.RelationalEntity1 = relationalEntity2;
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            Assert.IsTrue(entity.IsTracked);
            Assert.IsTrue(relationalEntity1.IsTracked);
            Assert.IsTrue(relationalEntity2.IsTracked);
            Assert.IsTrue(associativeEntity.IsTracked);
        }

        [TestMethod]
        public void Track_mark_all_objects_as_not_tracked()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity1 = new MockEntity();
            MockAssociativeEntity associativeEntity = new MockAssociativeEntity() { RelationalEntity2 = relationalEntity1 };
            entity.AssociativeEntities.Add(associativeEntity);
            MockEntity relationalEntity2 = new MockEntity();
            entity.RelationalEntity1 = relationalEntity2;
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            tracker.Stop();

            Assert.IsFalse(entity.IsTracked);
            Assert.IsFalse(relationalEntity1.IsTracked);
            Assert.IsFalse(relationalEntity2.IsTracked);
            Assert.IsFalse(associativeEntity.IsTracked);
        }

        [TestMethod]
        public void Track_changes_logged()
        {
            Assert.Fail();
        }

        [TestMethod]
        public void Track_projection_properties_only()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity1 = new MockEntity();
            MockAssociativeEntity associativeEntity = new MockAssociativeEntity() { RelationalEntity2 = relationalEntity1 };
            entity.AssociativeEntities.Add(associativeEntity);
            MockEntity relationalEntity2 = new MockEntity();
            entity.RelationalEntity1 = relationalEntity2;
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity, typeof(MockEntityProjection1));
            tracker.Start();

            entity.IntProperty = 1;
            relationalEntity1.StringProperty = "Changed";
            relationalEntity2.StringProperty = "Changed";
            associativeEntity.BoolProperty = true;

            Assert.AreEqual(3, tracker.DbEntityChanges().Count());
            Assert.IsTrue(tracker.DbEntityChanges().Contains(".IntProperty"));
            Assert.IsTrue(tracker.DbEntityChanges().Contains(".RelationalEntity1.StringProperty"));
            Assert.IsTrue(tracker.DbEntityChanges().Contains(".AssociativeEntities[Guid="+associativeEntity.Guid+"].BoolProperty"));
        }

        [TestMethod]
        public void Track__flatten_new_entity_changes__new_many_to_one_relational_entity()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            entity.RelationalEntity1 = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            entity.RelationalEntity1.StringProperty = "Changed";

            Assert.AreEqual(1, tracker.DbEntityChanges(true).Count());
            Assert.IsTrue(tracker.DbEntityChanges(true).Contains(".RelationalEntity1"));
        }

        [TestMethod]
        public void Track__flatten_new_entity_changes__new_entity_new_many_to_one_relational_entity()
        {
            MockEntity entity = new MockEntity();
            entity.RelationalEntity1 = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            entity.StringProperty = "Changed";
            entity.RelationalEntity1.StringProperty = "Changed";

            Assert.AreEqual(1, tracker.DbEntityChanges(true).Count());
            Assert.IsTrue(tracker.DbEntityChanges(true).Contains(".RelationalEntity1"));
        }

        [TestMethod]
        public void Track__flatten_new_entity_changes__new_one_to_many_relational_entity()
        {
            MockEntity entity = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            entity.RelationalEntities.Add(new MockEntity());
            entity.RelationalEntities.First().BoolProperty = true;
            entity.RelationalEntities.First().RelationalEntity1 = new MockEntity();
            entity.RelationalEntities.First().RelationalEntity1.StringProperty = "Changed";

            Assert.AreEqual(1, tracker.DbEntityChanges(true).Count());
            Assert.IsTrue(tracker.DbEntityChanges(true).Contains(".RelationalEntities"));
        }

        [TestMethod]
        public void Track__flatten_new_entity_changes__new_one_to_many_relational_entity_multiple_hops()
        {
            MockEntity entity = new MockEntity();
            entity.RelationalEntities.Add(new MockEntity());
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            entity.RelationalEntities.First().RelationalEntities.Add(new MockEntity());
            entity.RelationalEntities.First().RelationalEntities.First().StringProperty = "Changed";
            entity.RelationalEntities.First().RelationalEntities.First().IntProperty = 1;
            entity.RelationalEntities.First().RelationalEntities.First().DoubleProperty = 2d;
            entity.IntProperty = 1;

            Assert.AreEqual(1, tracker.DbEntityChanges(true).Count());
            Assert.IsTrue(tracker.DbEntityChanges(true).Contains(".RelationalEntities[Guid=" + entity.RelationalEntities.First().Guid+ "].RelationalEntities"));
        }

        [TestMethod]
        public void DbEntityChanges_isolate_one_to_many_relational_entity_changes()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity = new MockEntity();
            entity.RelationalEntities.Add(relationalEntity);
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            entity.IntProperty = 1;
            relationalEntity.StringProperty = "Changed";

            Assert.AreEqual(1, tracker.DbEntityChanges(false, relationalEntity).Count());
            Assert.IsTrue(tracker.DbEntityChanges(false, relationalEntity).Contains(".StringProperty"));
        }

        [TestMethod]
        public void DbEntityChanges_isolate_one_to_one_relational_entity_changes()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity = new MockEntity();
            entity.RelationalEntity1 = relationalEntity;
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            entity.IntProperty = 1;
            relationalEntity.StringProperty = "Changed";

            Assert.AreEqual(1, tracker.DbEntityChanges(false, relationalEntity).Count());
            Assert.IsTrue(tracker.DbEntityChanges(false, relationalEntity).Contains(".StringProperty"));
        }

        [TestMethod]
        public void PurgeChanges_isolate_one_to_many_relational_entity_changes()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity = new MockEntity();
            entity.RelationalEntities.Add(relationalEntity);
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            entity.IntProperty = 1;
            relationalEntity.StringProperty = "Changed";
            entity.BoolProperty = true;

            tracker.PurgeChanges(relationalEntity);
            entity.DoubleProperty = 3d;

            Assert.AreEqual(3, tracker.DbEntityChanges(false).Count());
            Assert.IsTrue(tracker.DbEntityChanges(false).Contains(".IntProperty"));
            Assert.IsTrue(tracker.DbEntityChanges(false).Contains(".BoolProperty"));
            Assert.IsTrue(tracker.DbEntityChanges(false).Contains(".DoubleProperty"));
        }

        [TestMethod]
        public void PurgeChanges_isolate_one_to_one_relational_entity_changes()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity = new MockEntity();
            entity.RelationalEntity1 = relationalEntity;
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            entity.IntProperty = 1;
            relationalEntity.StringProperty = "Changed";

            tracker.PurgeChanges(relationalEntity);

            Assert.AreEqual(1, tracker.DbEntityChanges(false).Count());
            Assert.IsTrue(tracker.DbEntityChanges(false).Contains(".IntProperty"));
        }

        [TestMethod]
        public void DbEntityAsJson_isolate_one_to_many_relational_entity_changes()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity = new MockEntity();
            entity.RelationalEntities.Add(relationalEntity);
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            entity.IntProperty = 1;
            relationalEntity.StringProperty = "Changed";

            Assert.AreEqual("{\"$id\":\"" + relationalEntity.Guid + "\",\"Guid\":\"" + relationalEntity.Guid + "\",\"StringProperty\":\"" + relationalEntity.StringProperty + "\"}", tracker.DbEntityAsJson(relationalEntity));
        }

        [TestMethod]
        public void DbEntityAsJson_isolate_one_to_one_relational_entity_changes()
        {
            MockEntity entity = new MockEntity();
            MockEntity relationalEntity = new MockEntity();
            entity.RelationalEntity1 = relationalEntity;
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            entity.IntProperty = 1;
            relationalEntity.StringProperty = "Changed";

            Assert.AreEqual("{\"$id\":\"" + relationalEntity.Guid + "\",\"Guid\":\"" + relationalEntity.Guid + "\",\"StringProperty\":\"" + relationalEntity.StringProperty + "\"}", tracker.DbEntityAsJson(relationalEntity));
        }

        [TestMethod]
        public void DbEntityChanges_one_to_many_relational_entity_changes()
        {
            MockEntity entity = new MockEntity();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            MockEntity relationalEntity = DbEntityRepository.CreateInstance<MockEntity>(1);
            MockAssociativeEntity associativeEntity = DbEntityRepository.CreateInstance<MockAssociativeEntity>(1);
            entity.AssociativeEntities.Add(associativeEntity);
            associativeEntity.RelationalEntity2 = relationalEntity;

            IEnumerable<string> changes = tracker.DbEntityChanges(true, associativeEntity);

            Assert.AreEqual(7, changes.Count());
            Assert.IsTrue(changes.Contains(".RelationalEntity2"));
            Assert.IsTrue(changes.Contains(".RelationalEntity2.CreatedByID"));
            Assert.IsTrue(changes.Contains(".RelationalEntity2.RecordCreated"));
            Assert.IsTrue(changes.Contains(".RelationalEntity2.LastModifiedDate"));
            Assert.IsTrue(changes.Contains(".CreatedByID"));
            Assert.IsTrue(changes.Contains(".RecordCreated"));
            Assert.IsTrue(changes.Contains(".LastModifiedDate"));
        }

        [TestMethod]
        public void Track__reuse_tracked_object_with_different_tracking_projections_on_different_trackers()
        {
            MockEntity entity = new MockEntity();
            PropertyChangeTracker entityTracker = new PropertyChangeTracker(entity, typeof(MockEntityProjection4));
            entityTracker.Start();

            MockEntity relationalEntity1 = new MockEntity();
            PropertyChangeTracker relationalEntity1Tracker = new PropertyChangeTracker(relationalEntity1);
            relationalEntity1Tracker.Start();

            MockEntity relationalEntity2 = new MockEntity();
            relationalEntity2.RelationalEntities.Add(new MockEntity());

            // TODO: Modify the definition of 'IsValid' in the tracker to account for the projection properties, instead of the original type properties (i.e. any property not in the projection is not valid)
            entity.IntProperty = 1;
            entityTracker.Untracked((e) => entity.RelationalEntity1 = relationalEntity2);
            relationalEntity1Tracker.Untracked((e) => relationalEntity1.RelationalEntity1 = relationalEntity2);

            Assert.AreEqual(1, entityTracker.DbEntityChanges().Count());
            Assert.IsTrue(entityTracker.DbEntityChanges().Contains(".IntProperty"));
            Assert.AreEqual(0, relationalEntity1Tracker.DbEntityChanges().Count());
        }

        [TestMethod]
        public void RemoveValidationRules()
        {
            string guid = Guid.NewGuid().ToString();
            Mock<IValidationRule> validationRule = new Mock<IValidationRule>();
            validationRule.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule.Setup(m => m.GetPropertyPath(It.IsAny<string>())).Returns(".RelationalEntities[Guid=" + guid + "].StringProperty");
            MockEntity mockRelationalEntity = new MockEntity(new HashSet<IValidationRule>() { validationRule.Object }, guid);
            MockEntity mockEntity = new MockEntity();
            mockEntity.RelationalEntities.Add(mockRelationalEntity);
            PropertyChangeTracker tracker = new PropertyChangeTracker(mockEntity);
            tracker.Start();

            mockEntity.RelationalEntities.Remove(mockRelationalEntity);

            // There is nothing to check. It should just work without failing when removing the mock relational entity validation rules from the tracker.
        }

        [TestMethod]
        public void Validate_start_tracker_load_entity_rules()
        {
            Mock<IValidationRule> validationRule1 = new Mock<IValidationRule>();
            validationRule1.SetupGet(m => m.PropertyName).Returns("StringProperty");
            Mock<IValidationRule> validationRule2 = new Mock<IValidationRule>();
            validationRule2.SetupGet(m => m.PropertyName).Returns("StringProperty");
            Mock<IValidationRule> validationRule3 = new Mock<IValidationRule>();
            validationRule3.SetupGet(m => m.PropertyName).Returns("StringProperty");
            MockEntity mockEntity = new MockEntity(new HashSet<IValidationRule>() { validationRule1.Object });
            mockEntity.RelationalEntity1 = new MockEntity(new HashSet<IValidationRule>() { validationRule2.Object });
            mockEntity.RelationalEntities.Add(new MockEntity(new HashSet<IValidationRule>() { validationRule3.Object }));
            PropertyChangeTracker tracker = new PropertyChangeTracker(mockEntity);
            tracker.Start();

            // Make sure that all validation rules are loaded and are indexed with their appropriate path
            Assert.AreEqual(3, tracker.ValidationRules.Count);
            Assert.IsTrue(tracker.ValidationRules.Keys.Contains(".StringProperty"));
            Assert.AreEqual(validationRule1.Object, tracker.ValidationRules[".StringProperty"].Single());
            Assert.IsTrue(tracker.ValidationRules.Keys.Contains(".RelationalEntity1.StringProperty"));
            Assert.AreEqual(validationRule2.Object, tracker.ValidationRules[".RelationalEntity1.StringProperty"].Single());
            Assert.IsTrue(tracker.ValidationRules.Keys.Contains(".RelationalEntities[Guid="+mockEntity.RelationalEntities.Single().Guid+ "].StringProperty"));
            Assert.AreEqual(validationRule3.Object, tracker.ValidationRules[".RelationalEntities[Guid=" + mockEntity.RelationalEntities.Single().Guid + "].StringProperty"].Single());
        }

        [TestMethod]
        public void Validate_stop_tracker_unload_entity_rules()
        {
            string guid = Guid.NewGuid().ToString();
            Mock<IValidationRule> validationRule1 = new Mock<IValidationRule>();
            validationRule1.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule1.Setup(m => m.GetPropertyPath(It.IsAny<string>())).Returns(".StringProperty");
            Mock<IValidationRule> validationRule2 = new Mock<IValidationRule>();
            validationRule2.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule2.Setup(m => m.GetPropertyPath(It.IsAny<string>())).Returns(".RelationalEntity1.StringProperty");
            Mock<IValidationRule> validationRule3 = new Mock<IValidationRule>();
            validationRule3.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule3.Setup(m => m.GetPropertyPath(It.IsAny<string>())).Returns(".RelationalEntities[Guid="+guid+ "].StringProperty");
            MockEntity mockEntity = new MockEntity(new HashSet<IValidationRule>() { validationRule1.Object });
            mockEntity.RelationalEntity1 = new MockEntity(new HashSet<IValidationRule>() { validationRule2.Object });
            mockEntity.RelationalEntities.Add(new MockEntity(new HashSet<IValidationRule>() { validationRule3.Object }, guid));
            PropertyChangeTracker tracker = new PropertyChangeTracker(mockEntity);
            tracker.Start();
            tracker.Stop();

            Assert.AreEqual(0, tracker.ValidationRules.Count);
        }

        [TestMethod]
        public void Validate_flat_property()
        {
            IEnumerable<IValidationResult> validationResults = Enumerable.Empty<IValidationResult>();

            Mock<IValidationResult> validationResult = new Mock<IValidationResult>();
            Mock<IValidationRule> validationRule = new Mock<IValidationRule>();
            validationRule.SetupAllProperties();
            validationRule.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule.SetupGet(m => m.Matches).Returns((e) => true);
            validationRule.Setup(m => m.AsResult(It.IsAny<string>())).Returns(validationResult.Object);

            MockEntity mockEntity = new MockEntity(new HashSet<IValidationRule>() { validationRule.Object });
            PropertyChangeTracker tracker = new PropertyChangeTracker(mockEntity);
            tracker.Validated += (s, e) => { validationResults = e.Data; };
            tracker.Start();

            mockEntity.StringProperty = "Changed";

            Assert.AreEqual(1, validationResults.Count());
            Assert.AreEqual(validationResult.Object, validationResults.Single());
        }

        [TestMethod]
        public void Validate_flat_property_deleted_entity()
        {
            IEnumerable<IValidationResult> validationResults = Enumerable.Empty<IValidationResult>();

            Mock<IValidationResult> validationResult = new Mock<IValidationResult>();
            Mock<IValidationRule> validationRule = new Mock<IValidationRule>();
            validationRule.SetupAllProperties();
            validationRule.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule.SetupGet(m => m.Matches).Returns((e) => true);
            validationRule.Setup(m => m.AsResult(It.IsAny<string>())).Returns(validationResult.Object);

            MockEntity mockEntity = new MockEntity(new HashSet<IValidationRule>() { validationRule.Object });
            mockEntity.IsDeleted = true;
            PropertyChangeTracker tracker = new PropertyChangeTracker(mockEntity);
            tracker.Validated += (s, e) => { validationResults = e.Data; };
            tracker.Start();

            mockEntity.StringProperty = "Changed";

            Assert.AreEqual(0, validationResults.Count());
        }

        [TestMethod]
        public void Validate_many_to_one_relational_property()
        {
            IEnumerable<IValidationResult> validationResults = Enumerable.Empty<IValidationResult>();

            Mock<IValidationResult> validationResult1 = new Mock<IValidationResult>();
            Mock<IValidationRule> validationRule1 = new Mock<IValidationRule>();
            validationRule1.SetupAllProperties();
            validationRule1.SetupGet(m => m.PropertyName).Returns("StringProperty");

            Mock<IValidationResult> validationResult2 = new Mock<IValidationResult>();
            Mock<IValidationRule> validationRule2 = new Mock<IValidationRule>();
            validationRule2.SetupAllProperties();
            validationRule2.SetupGet(m => m.PropertyName).Returns("RelationalEntity1");
            validationRule2.SetupGet(m => m.Matches).Returns((e) => true);
            validationRule2.Setup(m => m.AsResult(It.IsAny<string>())).Returns(validationResult2.Object);

            Mock<IValidationResult> validationResult3 = new Mock<IValidationResult>();
            Mock<IValidationRule> validationRule3 = new Mock<IValidationRule>();
            validationRule3.SetupAllProperties();
            validationRule3.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule3.SetupGet(m => m.Matches).Returns((e) => true);
            validationRule3.Setup(m => m.AsResult(It.IsAny<string>())).Returns(validationResult3.Object);

            MockEntity mockEntity = new MockEntity(new HashSet<IValidationRule>() { validationRule1.Object, validationRule2.Object });
            mockEntity.RelationalEntity1 = new MockEntity(new HashSet<IValidationRule>() { validationRule3.Object });
            PropertyChangeTracker tracker = new PropertyChangeTracker(mockEntity);
            tracker.Validated += (s, e) => { validationResults = e.Data; };
            tracker.Start();

            mockEntity.RelationalEntity1.StringProperty = "Changed";

            Assert.AreEqual(2, validationResults.Count());
            Assert.IsTrue(validationResults.Contains(validationResult2.Object));
            Assert.IsTrue(validationResults.Contains(validationResult3.Object));
        }

        [TestMethod]
        public void Validate_deleted_many_to_one_relational_property()
        {
            IEnumerable<IValidationResult> validationResults = Enumerable.Empty<IValidationResult>();

            Mock<IValidationResult> validationResult1 = new Mock<IValidationResult>();
            Mock<IValidationRule> validationRule1 = new Mock<IValidationRule>();
            validationRule1.SetupAllProperties();
            validationRule1.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule1.Setup(m => m.GetPropertyPath(It.IsAny<string>())).Returns(".StringProperty");

            Mock<IValidationResult> validationResult2 = new Mock<IValidationResult>();
            Mock<IValidationRule> validationRule2 = new Mock<IValidationRule>();
            validationRule2.SetupAllProperties();
            validationRule2.SetupGet(m => m.PropertyName).Returns("RelationalEntity1");
            validationRule2.Setup(m => m.GetPropertyPath(It.IsAny<string>())).Returns(".RelationalEntity1");
            validationRule2.SetupGet(m => m.Matches).Returns((e) => true);
            validationRule2.Setup(m => m.AsResult(It.IsAny<string>())).Returns(validationResult2.Object);

            Mock<IValidationResult> validationResult3 = new Mock<IValidationResult>();
            Mock<IValidationRule> validationRule3 = new Mock<IValidationRule>();
            validationRule3.SetupAllProperties();
            validationRule3.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule3.SetupGet(m => m.Matches).Returns((e) => true);
            validationRule3.Setup(m => m.AsResult(It.IsAny<string>())).Returns(validationResult3.Object);

            MockEntity mockEntity = new MockEntity(new HashSet<IValidationRule>() { validationRule1.Object, validationRule2.Object });
            mockEntity.RelationalEntity1 = new MockEntity(new HashSet<IValidationRule>() { validationRule3.Object });
            mockEntity.RelationalEntity1.IsDeleted = true;
            PropertyChangeTracker tracker = new PropertyChangeTracker(mockEntity);
            tracker.Validated += (s, e) => { validationResults = e.Data; };
            tracker.Start();

            mockEntity.RelationalEntity1.StringProperty = "Changed";

            Assert.AreEqual(0, validationResults.Count());
        }

        [TestMethod]
        public void Validate_many_to_one_relational_entity()
        {
            IEnumerable<IValidationResult> validationResults = Enumerable.Empty<IValidationResult>();

            Mock<IValidationResult> validationResult1 = new Mock<IValidationResult>();
            Mock<IValidationRule> validationRule1 = new Mock<IValidationRule>();
            validationRule1.SetupAllProperties();
            validationRule1.SetupGet(m => m.PropertyName).Returns("StringProperty");

            Mock<IValidationResult> validationResult2 = new Mock<IValidationResult>();
            Mock<IValidationRule> validationRule2 = new Mock<IValidationRule>();
            validationRule2.SetupAllProperties();
            validationRule2.SetupGet(m => m.PropertyName).Returns("RelationalEntity1");
            validationRule2.SetupGet(m => m.Matches).Returns((e) => true);
            validationRule2.Setup(m => m.AsResult(It.IsAny<string>())).Returns(validationResult2.Object);

            Mock<IValidationResult> validationResult3 = new Mock<IValidationResult>();
            Mock<IValidationRule> validationRule3 = new Mock<IValidationRule>();
            validationRule3.SetupAllProperties();
            validationRule3.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule3.SetupGet(m => m.Matches).Returns((e) => true);
            validationRule3.Setup(m => m.AsResult(It.IsAny<string>())).Returns(validationResult3.Object);

            MockEntity mockEntity = new MockEntity(new HashSet<IValidationRule>() { validationRule1.Object, validationRule2.Object });
            PropertyChangeTracker tracker = new PropertyChangeTracker(mockEntity);
            tracker.Validated += (s, e) => { validationResults = e.Data; };
            tracker.Start();

            mockEntity.RelationalEntity1 = new MockEntity(new HashSet<IValidationRule>() { validationRule3.Object }) { StringProperty = "Changed" };

            Assert.AreEqual(2, validationResults.Count());
            Assert.IsTrue(validationResults.Contains(validationResult2.Object));
            Assert.IsTrue(validationResults.Contains(validationResult3.Object));
        }

        [TestMethod]
        public void Validate_one_to_many_relational_property()
        {
            IEnumerable<IValidationResult> validationResults = Enumerable.Empty<IValidationResult>();

            Mock<IValidationResult> validationResult1 = new Mock<IValidationResult>();
            Mock<IValidationRule> validationRule1 = new Mock<IValidationRule>();
            validationRule1.SetupAllProperties();
            validationRule1.SetupGet(m => m.PropertyName).Returns("StringProperty");

            Mock<IValidationResult> validationResult2 = new Mock<IValidationResult>();
            Mock<IValidationRule> validationRule2 = new Mock<IValidationRule>();
            validationRule2.SetupAllProperties();
            validationRule2.SetupGet(m => m.PropertyName).Returns("RelationalEntities");
            validationRule2.SetupGet(m => m.Matches).Returns((e) => true);
            validationRule2.Setup(m => m.AsResult(It.IsAny<string>())).Returns(validationResult2.Object);

            Mock<IValidationResult> validationResult3 = new Mock<IValidationResult>();
            Mock<IValidationRule> validationRule3 = new Mock<IValidationRule>();
            validationRule3.SetupAllProperties();
            validationRule3.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule3.SetupGet(m => m.Matches).Returns((e) => true);
            validationRule3.Setup(m => m.AsResult(It.IsAny<string>())).Returns(validationResult3.Object);

            MockEntity mockEntity = new MockEntity(new HashSet<IValidationRule>() { validationRule1.Object, validationRule2.Object });
            mockEntity.RelationalEntities.Add(new MockEntity(new HashSet<IValidationRule>() { validationRule3.Object }));
            PropertyChangeTracker tracker = new PropertyChangeTracker(mockEntity);
            tracker.Validated += (s, e) => { validationResults = e.Data; };
            tracker.Start();

            mockEntity.RelationalEntities.Single().StringProperty = "Changed";

            Assert.AreEqual(2, validationResults.Count());
            Assert.IsTrue(validationResults.Contains(validationResult2.Object));
            Assert.IsTrue(validationResults.Contains(validationResult3.Object));
        }

        [TestMethod]
        public void Validate_deleted_one_to_many_relational_property()
        {
            IEnumerable<IValidationResult> validationResults = Enumerable.Empty<IValidationResult>();

            Mock<IValidationResult> validationResult1 = new Mock<IValidationResult>();
            Mock<IValidationRule> validationRule1 = new Mock<IValidationRule>();
            validationRule1.SetupAllProperties();
            validationRule1.SetupGet(m => m.PropertyName).Returns("StringProperty");

            Mock<IValidationResult> validationResult2 = new Mock<IValidationResult>();
            Mock<IValidationRule> validationRule2 = new Mock<IValidationRule>();
            validationRule2.SetupAllProperties();
            validationRule2.SetupGet(m => m.PropertyName).Returns("RelationalEntities");
            validationRule2.SetupGet(m => m.Matches).Returns((e) => true);
            validationRule2.Setup(m => m.AsResult(It.IsAny<string>())).Returns(validationResult2.Object);

            Mock<IValidationResult> validationResult3 = new Mock<IValidationResult>();
            Mock<IValidationRule> validationRule3 = new Mock<IValidationRule>();
            validationRule3.SetupAllProperties();
            validationRule3.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule3.SetupGet(m => m.Matches).Returns((e) => true);
            validationRule3.Setup(m => m.AsResult(It.IsAny<string>())).Returns(validationResult3.Object);

            MockEntity mockEntity = new MockEntity(new HashSet<IValidationRule>() { validationRule1.Object, validationRule2.Object });
            mockEntity.RelationalEntities.Add(new MockEntity(new HashSet<IValidationRule>() { validationRule3.Object }));
            mockEntity.RelationalEntities.Single().IsDeleted = true;
            PropertyChangeTracker tracker = new PropertyChangeTracker(mockEntity);
            tracker.Validated += (s, e) => { validationResults = e.Data; };
            tracker.Start();

            mockEntity.RelationalEntities.Single().StringProperty = "Changed";

            Assert.AreEqual(1, validationResults.Count());
            Assert.IsTrue(validationResults.Contains(validationResult2.Object));
        }

        [TestMethod]
        public void Validate_one_to_many_relational_entity()
        {
            IEnumerable<IValidationResult> validationResults = Enumerable.Empty<IValidationResult>();

            Mock<IValidationResult> validationResult1 = new Mock<IValidationResult>();
            Mock<IValidationRule> validationRule1 = new Mock<IValidationRule>();
            validationRule1.SetupAllProperties();
            validationRule1.SetupGet(m => m.PropertyName).Returns("StringProperty");

            Mock<IValidationResult> validationResult2 = new Mock<IValidationResult>();
            Mock<IValidationRule> validationRule2 = new Mock<IValidationRule>();
            validationRule2.SetupAllProperties();
            validationRule2.SetupGet(m => m.PropertyName).Returns("RelationalEntities");
            validationRule2.SetupGet(m => m.Matches).Returns((e) => true);
            validationRule2.Setup(m => m.AsResult(It.IsAny<string>())).Returns(validationResult2.Object);

            Mock<IValidationResult> validationResult3 = new Mock<IValidationResult>();
            Mock<IValidationRule> validationRule3 = new Mock<IValidationRule>();
            validationRule3.SetupAllProperties();
            validationRule3.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule3.SetupGet(m => m.Matches).Returns((e) => true);
            validationRule3.Setup(m => m.AsResult(It.IsAny<string>())).Returns(validationResult3.Object);

            MockEntity mockEntity = new MockEntity(new HashSet<IValidationRule>() { validationRule1.Object, validationRule2.Object });
            PropertyChangeTracker tracker = new PropertyChangeTracker(mockEntity);
            tracker.Validated += (s, e) => { validationResults = e.Data; };
            tracker.Start();

            mockEntity.RelationalEntities.Add(new MockEntity(new HashSet<IValidationRule>() { validationRule3.Object }) { StringProperty = "Changed" });

            Assert.AreEqual(2, validationResults.Count());
            Assert.IsTrue(validationResults.Contains(validationResult2.Object));
            Assert.IsTrue(validationResults.Contains(validationResult3.Object));
        }

        [TestMethod]
        public void Validate_one_to_many_relational_entities()
        {
            IEnumerable<IValidationResult> validationResults = Enumerable.Empty<IValidationResult>();

            Mock<IValidationResult> validationResult1 = new Mock<IValidationResult>();
            Mock<IValidationRule> validationRule1 = new Mock<IValidationRule>();
            validationRule1.SetupAllProperties();
            validationRule1.SetupGet(m => m.PropertyName).Returns("RelationalEntities");
            validationRule1.SetupGet(m => m.Matches).Returns((e) => true);
            validationRule1.Setup(m => m.AsResult(It.IsAny<string>())).Returns(validationResult1.Object);

            Mock<IValidationResult> validationResult2 = new Mock<IValidationResult>();
            Mock<IValidationRule> validationRule2 = new Mock<IValidationRule>();
            validationRule2.SetupAllProperties();
            validationRule2.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule2.SetupGet(m => m.Matches).Returns((e) => true);
            validationRule2.Setup(m => m.AsResult(It.IsAny<string>())).Returns(validationResult2.Object);

            MockEntity mockEntity = new MockEntity(new HashSet<IValidationRule>() { validationRule1.Object });
            PropertyChangeTracker tracker = new PropertyChangeTracker(mockEntity);
            tracker.Validated += (s, e) => { validationResults = e.Data; };
            tracker.Start();

            mockEntity.RelationalEntities = new HashSet<MockEntity>() { new MockEntity(new HashSet<IValidationRule>() { validationRule2.Object }) { StringProperty = "Changed" } };

            Assert.AreEqual(2, validationResults.Count());
            Assert.IsTrue(validationResults.Contains(validationResult1.Object));
            Assert.IsTrue(validationResults.Contains(validationResult2.Object));
        }

        [TestMethod]
        public void Validate_specific_rules_flat_property()
        {
            bool isMatches1Run = false;
            Func<IDbEntity, bool> matches1 = (e) => isMatches1Run = true;
            Mock<IValidationRule> validationRule1 = new Mock<IValidationRule>();
            validationRule1.SetupAllProperties();
            validationRule1.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule1.SetupGet(m => m.Matches).Returns(matches1);

            bool isMatches2Run = false;
            Func<IDbEntity, bool> matches2 = (e) => isMatches2Run = true;
            Mock<IValidationRule> validationRule2 = new Mock<IValidationRule>();
            validationRule2.SetupAllProperties();
            validationRule2.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule2.SetupGet(m => m.Matches).Returns(matches2);

            bool isMatches3Run = false;
            Func<IDbEntity, bool> matches3 = (e) => isMatches3Run = true;
            Mock<IValidationRule> validationRule3 = new Mock<IValidationRule>();
            validationRule3.SetupAllProperties();
            validationRule3.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule3.SetupGet(m => m.Matches).Returns(matches3);

            MockEntity mockEntity = new MockEntity(new HashSet<IValidationRule>() { validationRule1.Object });
            mockEntity.RelationalEntity1 = new MockEntity(new HashSet<IValidationRule>() { validationRule2.Object });
            mockEntity.RelationalEntities.Add(new MockEntity(new HashSet<IValidationRule>() { validationRule3.Object }));
            PropertyChangeTracker tracker = new PropertyChangeTracker(mockEntity);
            tracker.Start();

            tracker.Validate("^.StringProperty$");

            Assert.IsTrue(isMatches1Run);
            Assert.IsFalse(isMatches2Run);
            Assert.IsFalse(isMatches3Run);
        }

        [TestMethod]
        public void Validate_specific_rules_many_to_one_property()
        {
            bool isMatches1Run = false;
            Func<IDbEntity, bool> matches1 = (e) => isMatches1Run = true;
            Mock<IValidationRule> validationRule1 = new Mock<IValidationRule>();
            validationRule1.SetupAllProperties();
            validationRule1.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule1.SetupGet(m => m.Matches).Returns(matches1);

            bool isMatches2Run = false;
            Func<IDbEntity, bool> matches2 = (e) => isMatches2Run = true;
            Mock<IValidationRule> validationRule2 = new Mock<IValidationRule>();
            validationRule2.SetupAllProperties();
            validationRule2.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule2.SetupGet(m => m.Matches).Returns(matches2);

            bool isMatches3Run = false;
            Func<IDbEntity, bool> matches3 = (e) => isMatches3Run = true;
            Mock<IValidationRule> validationRule3 = new Mock<IValidationRule>();
            validationRule3.SetupAllProperties();
            validationRule3.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule3.SetupGet(m => m.Matches).Returns(matches3);

            MockEntity mockEntity = new MockEntity(new HashSet<IValidationRule>() { validationRule1.Object });
            mockEntity.RelationalEntity1 = new MockEntity(new HashSet<IValidationRule>() { validationRule2.Object });
            mockEntity.RelationalEntities.Add(new MockEntity(new HashSet<IValidationRule>() { validationRule3.Object }));
            PropertyChangeTracker tracker = new PropertyChangeTracker(mockEntity);
            tracker.Start();

            tracker.Validate("^.RelationalEntity1.StringProperty$");

            Assert.IsFalse(isMatches1Run);
            Assert.IsTrue(isMatches2Run);
            Assert.IsFalse(isMatches3Run);
        }

        [TestMethod]
        public void Validate_specific_rules_one_to_many_property()
        {
            bool isMatches1Run = false;
            Func<IDbEntity, bool> matches1 = (e) => isMatches1Run = true;
            Mock<IValidationRule> validationRule1 = new Mock<IValidationRule>();
            validationRule1.SetupAllProperties();
            validationRule1.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule1.SetupGet(m => m.Matches).Returns(matches1);

            bool isMatches2Run = false;
            Func<IDbEntity, bool> matches2 = (e) => isMatches2Run = true;
            Mock<IValidationRule> validationRule2 = new Mock<IValidationRule>();
            validationRule2.SetupAllProperties();
            validationRule2.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule2.SetupGet(m => m.Matches).Returns(matches2);

            bool isMatches3Run = false;
            Func<IDbEntity, bool> matches3 = (e) => isMatches3Run = true;
            Mock<IValidationRule> validationRule3 = new Mock<IValidationRule>();
            validationRule3.SetupAllProperties();
            validationRule3.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule3.SetupGet(m => m.Matches).Returns(matches3);

            MockEntity mockEntity = new MockEntity(new HashSet<IValidationRule>() { validationRule1.Object });
            mockEntity.RelationalEntity1 = new MockEntity(new HashSet<IValidationRule>() { validationRule2.Object });
            mockEntity.RelationalEntities.Add(new MockEntity(new HashSet<IValidationRule>() { validationRule3.Object }));
            PropertyChangeTracker tracker = new PropertyChangeTracker(mockEntity);
            tracker.Start();

            tracker.Validate(".RelationalEntities\\[Guid=" + mockEntity.RelationalEntities.Single().Guid + "\\].StringProperty");

            Assert.IsFalse(isMatches1Run);
            Assert.IsFalse(isMatches2Run);
            Assert.IsTrue(isMatches3Run);
        }

        [TestMethod]
        public void Validate_specific_rules_wildcard()
        {
            bool isMatches1Run = false;
            Func<IDbEntity, bool> matches1 = (e) => isMatches1Run = true;
            Mock<IValidationRule> validationRule1 = new Mock<IValidationRule>();
            validationRule1.SetupAllProperties();
            validationRule1.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule1.SetupGet(m => m.Matches).Returns(matches1);

            bool isMatches2Run = false;
            Func<IDbEntity, bool> matches2 = (e) => isMatches2Run = true;
            Mock<IValidationRule> validationRule2 = new Mock<IValidationRule>();
            validationRule2.SetupAllProperties();
            validationRule2.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule2.SetupGet(m => m.Matches).Returns(matches2);

            bool isMatches3Run = false;
            Func<IDbEntity, bool> matches3 = (e) => isMatches3Run = true;
            Mock<IValidationRule> validationRule3 = new Mock<IValidationRule>();
            validationRule3.SetupAllProperties();
            validationRule3.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule3.SetupGet(m => m.Matches).Returns(matches3);

            MockEntity mockEntity = new MockEntity(new HashSet<IValidationRule>() { validationRule1.Object });
            mockEntity.RelationalEntity1 = new MockEntity(new HashSet<IValidationRule>() { validationRule2.Object });
            mockEntity.RelationalEntities.Add(new MockEntity(new HashSet<IValidationRule>() { validationRule3.Object }));
            PropertyChangeTracker tracker = new PropertyChangeTracker(mockEntity);
            tracker.Start();

            tracker.Validate(".*");

            Assert.IsTrue(isMatches1Run);
            Assert.IsTrue(isMatches2Run);
            Assert.IsTrue(isMatches3Run);
        }

        [TestMethod]
        public void Validate_multiple_specific_rules()
        {
            bool isMatches1Run = false;
            Func<IDbEntity, bool> matches1 = (e) => isMatches1Run = true;
            Mock<IValidationRule> validationRule1 = new Mock<IValidationRule>();
            validationRule1.SetupAllProperties();
            validationRule1.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule1.SetupGet(m => m.Matches).Returns(matches1);

            bool isMatches2Run = false;
            Func<IDbEntity, bool> matches2 = (e) => isMatches2Run = true;
            Mock<IValidationRule> validationRule2 = new Mock<IValidationRule>();
            validationRule2.SetupAllProperties();
            validationRule2.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule2.SetupGet(m => m.Matches).Returns(matches2);

            bool isMatches3Run = false;
            Func<IDbEntity, bool> matches3 = (e) => isMatches3Run = true;
            Mock<IValidationRule> validationRule3 = new Mock<IValidationRule>();
            validationRule3.SetupAllProperties();
            validationRule3.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule3.SetupGet(m => m.Matches).Returns(matches3);

            MockEntity mockEntity = new MockEntity(new HashSet<IValidationRule>() { validationRule1.Object });
            mockEntity.RelationalEntity1 = new MockEntity(new HashSet<IValidationRule>() { validationRule2.Object });
            mockEntity.RelationalEntities.Add(new MockEntity(new HashSet<IValidationRule>() { validationRule3.Object }));
            PropertyChangeTracker tracker = new PropertyChangeTracker(mockEntity);
            tracker.Start();

            tracker.Validate(new string[] { ".StringProperty", ".RelationalEntity1.StringProperty", ".RelationalEntities[Guid=" + mockEntity.RelationalEntities.Single().Guid + "].StringProperty" });

            Assert.IsTrue(isMatches1Run);
            Assert.IsTrue(isMatches2Run);
            Assert.IsTrue(isMatches3Run);
        }

        [TestMethod]
        public void Validate_filter_out_irrelevant_rules()
        {
            bool isMatches1Run = false;
            Func<IDbEntity, bool> matches1 = (e) => isMatches1Run = true;
            Mock<IValidationRule> validationRule1 = new Mock<IValidationRule>();
            validationRule1.SetupAllProperties();
            validationRule1.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule1.SetupGet(m => m.Matches).Returns(matches1);

            bool isMatches2Run = false;
            Func<IDbEntity, bool> matches2 = (e) => isMatches2Run = true;
            Mock<IValidationRule> validationRule2 = new Mock<IValidationRule>();
            validationRule2.SetupAllProperties();
            validationRule2.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule2.SetupGet(m => m.Matches).Returns(matches2);

            bool isMatches3Run = false;
            Func<IDbEntity, bool> matches3 = (e) => isMatches3Run = true;
            Mock<IValidationRule> validationRule3 = new Mock<IValidationRule>();
            validationRule3.SetupAllProperties();
            validationRule3.SetupGet(m => m.PropertyName).Returns("StringProperty");
            validationRule3.SetupGet(m => m.Matches).Returns(matches3);

            MockEntity mockEntity = new MockEntity(new HashSet<IValidationRule>() { validationRule1.Object });
            mockEntity.RelationalEntity1 = new MockEntity(new HashSet<IValidationRule>() { validationRule2.Object });
            mockEntity.RelationalEntity1.RelationalEntity1 = new MockEntity(new HashSet<IValidationRule>() { validationRule3.Object });

            PropertyChangeTracker tracker = new PropertyChangeTracker(mockEntity);
            tracker.Start();

            tracker.AddValidationRulesFilters(new HashSet<string>() { @"^.RelationalEntity1.(\w*)$" });
            mockEntity.RelationalEntity1.StringProperty = "Changed";

            Assert.IsFalse(isMatches1Run);
            Assert.IsTrue(isMatches2Run);
            Assert.IsFalse(isMatches3Run);
        }

        [TestMethod]
        public void IsDeleted_dot_true()
        {
            MockEntity mockEntity = new MockEntity();
            mockEntity.IsDeleted = true;

            PropertyChangeTracker tracker = new PropertyChangeTracker(mockEntity);
            tracker.Start();

            IDbEntity entity;
            Assert.IsTrue(tracker.IsDeleted(".", out entity));
            Assert.AreEqual(mockEntity, entity);
        }

        [TestMethod]
        public void IsDeleted_dot_false()
        {
            MockEntity mockEntity = new MockEntity();
            mockEntity.IsDeleted = false;

            PropertyChangeTracker tracker = new PropertyChangeTracker(mockEntity);
            tracker.Start();

            IDbEntity entity;
            Assert.IsFalse(tracker.IsDeleted(".", out entity));
            Assert.AreEqual(mockEntity, entity);
        }

        [TestMethod]
        public void IsDeleted_flat_property_true()
        {
            MockEntity mockEntity = new MockEntity();
            mockEntity.IsDeleted = true;

            PropertyChangeTracker tracker = new PropertyChangeTracker(mockEntity);
            tracker.Start();

            IDbEntity entity;
            Assert.IsTrue(tracker.IsDeleted(".StringProperty", out entity));
            Assert.AreEqual(mockEntity, entity);
        }

        [TestMethod]
        public void IsDeleted_flat_property_false()
        {
            MockEntity mockEntity = new MockEntity();
            mockEntity.IsDeleted = false;

            PropertyChangeTracker tracker = new PropertyChangeTracker(mockEntity);
            tracker.Start();

            IDbEntity entity;
            Assert.IsFalse(tracker.IsDeleted(".StringProperty", out entity));
            Assert.AreEqual(mockEntity, entity);
        }

        [TestMethod]
        public void IsDeleted_relational_entity_false()
        {
            MockEntity mockEntity = new MockEntity();
            mockEntity.IsDeleted = false;
            mockEntity.RelationalEntity1 = new MockEntity();
            mockEntity.RelationalEntity1.IsDeleted = false;

            PropertyChangeTracker tracker = new PropertyChangeTracker(mockEntity);
            tracker.Start();

            IDbEntity entity;
            Assert.IsFalse(tracker.IsDeleted(".RelationalEntity1.StringProperty", out entity));
            Assert.AreEqual(mockEntity.RelationalEntity1, entity);
        }

        [TestMethod]
        public void IsDeleted_parent_of_relational_entity_true()
        {
            MockEntity mockEntity = new MockEntity();
            mockEntity.IsDeleted = true;
            mockEntity.RelationalEntity1 = new MockEntity();
            mockEntity.RelationalEntity1.IsDeleted = false;

            PropertyChangeTracker tracker = new PropertyChangeTracker(mockEntity);
            tracker.Start();

            IDbEntity entity;
            Assert.IsTrue(tracker.IsDeleted(".RelationalEntity1.StringProperty", out entity));
            Assert.AreEqual(mockEntity, entity);
        }

        [TestMethod]
        public void IsDeleted_child_of_relational_entity_true()
        {
            MockEntity mockEntity = new MockEntity();
            mockEntity.IsDeleted = false;
            mockEntity.RelationalEntity1 = new MockEntity();
            mockEntity.RelationalEntity1.IsDeleted = true;

            PropertyChangeTracker tracker = new PropertyChangeTracker(mockEntity);
            tracker.Start();

            IDbEntity entity;
            Assert.IsTrue(tracker.IsDeleted(".RelationalEntity1.StringProperty", out entity));
            Assert.AreEqual(mockEntity.RelationalEntity1, entity);
        }

        [TestMethod]
        public void IsDeleted_associative_entity_false()
        {
            MockEntity mockEntity = new MockEntity();
            mockEntity.IsDeleted = false;
            MockEntity mockAssociativeEntity = new MockEntity();
            mockAssociativeEntity.IsDeleted = false;
            mockEntity.RelationalEntities.Add(mockAssociativeEntity);

            PropertyChangeTracker tracker = new PropertyChangeTracker(mockEntity);
            tracker.Start();

            IDbEntity entity;
            Assert.IsFalse(tracker.IsDeleted(".RelationalEntities[Guid=" + mockEntity.RelationalEntities.Single().Guid + "].StringProperty", out entity));
            Assert.AreEqual(mockAssociativeEntity, entity);
        }

        [TestMethod]
        public void IsDeleted_parent_of_associative_entity_true()
        {
            MockEntity mockEntity = new MockEntity();
            mockEntity.IsDeleted = true;
            MockEntity mockAssociativeEntity = new MockEntity();
            mockAssociativeEntity.IsDeleted = false;
            mockEntity.RelationalEntities.Add(mockAssociativeEntity);

            PropertyChangeTracker tracker = new PropertyChangeTracker(mockEntity);
            tracker.Start();

            IDbEntity entity;
            Assert.IsTrue(tracker.IsDeleted(".RelationalEntities[Guid=" + mockEntity.RelationalEntities.Single().Guid + "].StringProperty", out entity));
            Assert.AreEqual(mockEntity, entity);
        }

        [TestMethod]
        public void IsDeleted_child_of_associative_entity_true()
        {
            MockEntity mockEntity = new MockEntity();
            mockEntity.IsDeleted = false;
            MockEntity mockAssociativeEntity = new MockEntity();
            mockAssociativeEntity.IsDeleted = true;
            mockEntity.RelationalEntities.Add(mockAssociativeEntity);

            PropertyChangeTracker tracker = new PropertyChangeTracker(mockEntity);
            tracker.Start();

            IDbEntity entity;
            Assert.IsTrue(tracker.IsDeleted(".RelationalEntities[Guid=" + mockEntity.RelationalEntities.Single().Guid + "].StringProperty", out entity));
            Assert.AreEqual(mockAssociativeEntity, entity);
        }

        [TestMethod]
        public void DbEntityChanges_changes_before_timestamp_one_before_one_after()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.IntProperty = 1;

            long timestamp = DateTime.UtcNow.Ticks;
            // Force a timestamp difference between the timestamp and the new change
            Thread.Sleep(10);
            entity.StringProperty = "2";

            Assert.AreEqual(2, tracker.AllChanges().Count());
            Assert.AreEqual(1, tracker.DbEntityChanges(utcTimestamp: timestamp).Count());
            Assert.AreEqual(".IntProperty", tracker.DbEntityChanges(utcTimestamp: timestamp).Single());
        }

        [TestMethod]
        public void DbEntityChanges_changes_before_timestamp_all_after()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            long timestamp = DateTime.UtcNow.Ticks;
            // Force a timestamp difference between the timestamp and the new changes
            Thread.Sleep(10);
            entity.IntProperty = 1;
            entity.StringProperty = "2";

            Assert.AreEqual(2, tracker.AllChanges().Count());
            Assert.AreEqual(0, tracker.DbEntityChanges(utcTimestamp: timestamp).Count());
        }

        [TestMethod]
        public void DbEntityChanges_changes_before_timestamp_all_before()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.IntProperty = 1;
            entity.StringProperty = "2";

            // Force a timestamp difference between the timestamp and the new changes
            Thread.Sleep(10);
            long timestamp = DateTime.UtcNow.Ticks;

            Assert.AreEqual(2, tracker.AllChanges().Count());
            Assert.AreEqual(2, tracker.DbEntityChanges(utcTimestamp: timestamp).Count());
            Assert.AreEqual(".IntProperty", tracker.DbEntityChanges(utcTimestamp: timestamp).ElementAt(0));
            Assert.AreEqual(".StringProperty", tracker.DbEntityChanges(utcTimestamp: timestamp).ElementAt(1));
        }

        [TestMethod]
        public void PurgeChanges_changes_before_timestamp_one_before_one_after()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.IntProperty = 1;

            long timestamp = DateTime.UtcNow.Ticks;
            // Force a timestamp difference between the timestamp and the new change
            Thread.Sleep(10);
            entity.StringProperty = "2";
            Assert.AreEqual(2, tracker.AllChanges().Count());

            tracker.PurgeChanges(utcTimestamp: timestamp);

            Assert.AreEqual(1, tracker.AllChanges().Count());
            Assert.AreEqual(".StringProperty", tracker.DbEntityChanges().Single());
        }

        [TestMethod]
        public void PurgeChanges_changes_before_timestamp_all_after()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();

            long timestamp = DateTime.UtcNow.Ticks;
            // Force a timestamp difference between the timestamp and the new changes
            Thread.Sleep(10);
            entity.IntProperty = 1;
            entity.StringProperty = "2";

            tracker.PurgeChanges(utcTimestamp: timestamp);

            Assert.AreEqual(2, tracker.AllChanges().Count());
        }

        [TestMethod]
        public void PurgeChanges_changes_before_timestamp_all_before()
        {
            MockEntity entity = new MockEntity();
            entity.MarkPersisted();
            PropertyChangeTracker tracker = new PropertyChangeTracker(entity);
            tracker.Start();
            entity.IntProperty = 1;
            entity.StringProperty = "2";

            // Force a timestamp difference between the timestamp and the new changes
            Thread.Sleep(10);
            long timestamp = DateTime.UtcNow.Ticks;
            Assert.AreEqual(2, tracker.AllChanges().Count());

            tracker.PurgeChanges(utcTimestamp: timestamp);

            Assert.AreEqual(0, tracker.AllChanges().Count());
        }

        public class MockEntityProjection1
        {
            public int IntProperty { get; set; }
            public MockEntityProjection2 RelationalEntity1 { get; set; }
            public IEnumerable<MockEntityProjection3> AssociativeEntities { get; set; }

            public class MockEntityProjection2
            {
                public string StringProperty { get; set; }
            }

            public class MockEntityProjection3
            {
                public bool BoolProperty { get; set; }
            }
        }

        public class MockEntityProjection4
        {
            public int IntProperty { get; set; }
            public MockEntityProjection5 RelationalEntity1 { get; set; }
        }

        public class MockEntityProjection5
        {

        }
    }
}
