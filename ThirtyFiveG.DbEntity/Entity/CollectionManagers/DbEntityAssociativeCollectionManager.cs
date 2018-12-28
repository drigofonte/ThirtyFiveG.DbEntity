using System;
using System.Collections.Generic;
using System.Linq;
using ThirtyFiveG.Commons.Collections;
using ThirtyFiveG.DbEntity.Common;

namespace ThirtyFiveG.DbEntity.Entity.CollectionManagers
{
    public class DbEntityAssociativeCollectionManager<TEntity, TAssociative> : IDbEntityAssociativeCollectionManager<TEntity, TAssociative>
        where TEntity : class, IDbEntity, IDbEntityBasic
        where TAssociative : class, IDbEntity, IDbEntityBasic, new()
    {
        #region Private variables
        private int _userId;
        private Action<IDbEntity, TEntity, TAssociative> _setForeignKeys;
        private Action<IDbEntity, TEntity, TAssociative> _setForeignEntities;
        private Func<IDbEntity, TAssociative, bool> _matchAssociativeEntityOwner;
        private Func<TEntity, TAssociative, bool> _matchAssociativeEntity;
        private Func<ICollection<TAssociative>> _associativeEntities;
        #endregion

        #region Constructor
        public DbEntityAssociativeCollectionManager(int userId, IDbEntity associativeCollectionOwner, Func<ICollection<TAssociative>> associativeEntities, Action<IDbEntity, TEntity, TAssociative> setForeignEntities, Action<IDbEntity, TEntity, TAssociative> setForeignKeys, Func<IDbEntity, TAssociative, bool> matchAssociativeEntityOwner, Func<TEntity, TAssociative, bool> matchAssociativeEntity)
        {
            _userId = userId;
            AssociativeCollectionOwner = associativeCollectionOwner;
            _associativeEntities = associativeEntities;
            _setForeignEntities = setForeignEntities;
            _setForeignKeys = setForeignKeys;
            _matchAssociativeEntityOwner = matchAssociativeEntityOwner;
            _matchAssociativeEntity = matchAssociativeEntity;
        }
        #endregion

        #region Public properties
        public IDbEntity AssociativeCollectionOwner { get; private set; }
        public ICollection<TAssociative> AssociativeCollection { get { return _associativeEntities(); } }
        #endregion

        #region Private methods
        private Func<TAssociative, bool> Matches(TEntity entity)
        {
            return e => _matchAssociativeEntityOwner(AssociativeCollectionOwner, e)
                        && _matchAssociativeEntity(entity, e);
        }

        private TAssociative CreateAssociativeEntity(TEntity entity)
        {
            TAssociative associativeEntity = DbEntityRepository.CreateInstance<TAssociative>(_userId);
            _setForeignKeys(AssociativeCollectionOwner, entity, associativeEntity);

            if (entity.State == EntityState.New)
            {
                _setForeignEntities(AssociativeCollectionOwner, entity, associativeEntity);
                entity.CreatedByID = _userId;
            }

            return associativeEntity;
        }

        private void RemoveAssociativeEntities(ICollection<TAssociative> associativeEntities)
        {
            ICollection<TAssociative> newAssociativeEntities = associativeEntities.Where(e => e.State == EntityState.New).ToList();
            Remove(_associativeEntities(), newAssociativeEntities);
            newAssociativeEntities.Clear();

            ICollection<TAssociative> persistedAssociativeEntities = associativeEntities.Where(e => e.State == EntityState.Persisted).ToList();
            foreach (TAssociative e in persistedAssociativeEntities)
                RemoveAssociativeEntity(e);
            persistedAssociativeEntities.Clear();
        }

        private void RemoveAssociativeEntity(TAssociative associativeEntity)
        {
            if (associativeEntity.State == EntityState.Persisted)
                associativeEntity.IsDeleted = true;
            else
                _associativeEntities().Remove(associativeEntity);
        }

        private void Add(ICollection<TAssociative> existingAssociativeEntities, IEnumerable<TAssociative> newAssociativeEntities)
        {
            ObservableRangeCollection<TAssociative> c = existingAssociativeEntities as ObservableRangeCollection<TAssociative>;
            if (c != null)
                c.AddRange(newAssociativeEntities);
            else
                foreach (TAssociative e in newAssociativeEntities)
                    existingAssociativeEntities.Add(e);
        }

        private void Remove(ICollection<TAssociative> existingAssociativeEntities, IEnumerable<TAssociative> associativeEntitiesToRemove)
        {
            ObservableRangeCollection<TAssociative> c = existingAssociativeEntities as ObservableRangeCollection<TAssociative>;
            if (c != null)
                c.RemoveRange(associativeEntitiesToRemove);
            else
                foreach (TAssociative e in associativeEntitiesToRemove)
                    existingAssociativeEntities.Remove(e);
        }
        #endregion

        #region Public methods
        public virtual TAssociative Add(TEntity entity)
        {
            TAssociative associativeEntity = _associativeEntities()
                .SingleOrDefault(Matches(entity));
            if (associativeEntity == null)
            {
                associativeEntity = CreateAssociativeEntity(entity);
                _associativeEntities().Add(associativeEntity);
            }
            associativeEntity.IsDeleted = false;
            return associativeEntity;
        }

        public IEnumerable<TAssociative> Add(IEnumerable<TEntity> entities)
        {
            ICollection<TAssociative> associativeEntities = new List<TAssociative>();
            ICollection<TAssociative> existingAssociativeEntities = _associativeEntities();
            foreach(TEntity entity in entities)
            {
                TAssociative associativeEntity = existingAssociativeEntities.SingleOrDefault(Matches(entity));
                if (associativeEntity == null)
                {
                    associativeEntity = CreateAssociativeEntity(entity);
                    associativeEntities.Add(associativeEntity);
                }
                associativeEntity.IsDeleted = false;
            }
            Add(existingAssociativeEntities, associativeEntities);
            return associativeEntities;
        }

        public virtual bool Contains(TEntity entity)
        {
            TAssociative associativeEntity = _associativeEntities().SingleOrDefault(Matches(entity));
            return associativeEntity != null && !associativeEntity.IsDeleted;
        }

        public virtual TAssociative Get(TEntity entity)
        {
            TAssociative associativeEntity = _associativeEntities().SingleOrDefault(Matches(entity));
            if (associativeEntity != null && associativeEntity.IsDeleted)
                associativeEntity = null;
            return associativeEntity;
        }

        public virtual TAssociative Remove(TEntity entity)
        {
            TAssociative associativeEntity = _associativeEntities().SingleOrDefault(Matches(entity));
            if (associativeEntity != null)
                RemoveAssociativeEntity(associativeEntity);
            return associativeEntity;
        }

        public virtual void Remove(IEnumerable<TEntity> entities)
        {
            ICollection<TAssociative> associativeEntities = new List<TAssociative>();
            foreach(TEntity e in entities)
                associativeEntities.Add(Get(e));
            RemoveAssociativeEntities(associativeEntities);
            associativeEntities.Clear();
        }

        public virtual void RemoveAll()
        {
            RemoveAssociativeEntities(_associativeEntities());
        }
        #endregion
    }
}
