using System;
using System.Collections.Generic;
using System.Linq;
using ThirtyFiveG.DbEntity.Entity;

namespace ThirtyFiveG.DbEntity.Entity.CollectionManagers
{
    public class DualDbEntityAssociativeCollectionManager<TEntity1, TEntity2, TAssociative> : IDualDbEntityAssociativeCollectionManager<TEntity1, TEntity2, TAssociative>
        where TEntity1 : class, IDbEntity, IDbEntityBasic
        where TEntity2 : class, IDbEntity, IDbEntityBasic
        where TAssociative : class, IDbEntity, IDbEntityBasic, new()
    {
        #region Private variables
        private int _userId;
        private Action<IDbEntity, TEntity1, TEntity2, TAssociative> _setForeignKeys;
        private Action<IDbEntity, TEntity1, TAssociative> _setEntity1ForeignEntity;
        private Action<IDbEntity, TEntity2, TAssociative> _setEntity2ForeignEntity;
        private Func<IDbEntity, TAssociative, bool> _matchAssociativeEntityOwner;
        private Func<TEntity1, TAssociative, bool> _matchAssociativeEntity1;
        private Func<TEntity2, TAssociative, bool> _matchAssociativeEntity2;
        private Func<ICollection<TAssociative>> _associativeEntities;
        #endregion

        #region Constructor
        public DualDbEntityAssociativeCollectionManager(int userId, IDbEntity associativeCollectionOwner, Func<ICollection<TAssociative>> associativeEntities, Action<IDbEntity, TEntity1, TAssociative> setEntity1ForeignEntity, Action<IDbEntity, TEntity2, TAssociative> setEntity2ForeignEntity, Action<IDbEntity, TEntity1, TEntity2, TAssociative> setForeignKeys, Func<IDbEntity, TAssociative, bool> matchAssociativeEntityOwner, Func<TEntity1, TAssociative, bool> matchAssociativeEntity1, Func<TEntity2, TAssociative, bool> matchAssociativeEntity2)
        {
            _userId = userId;
            AssociativeCollectionOwner = associativeCollectionOwner;
            _associativeEntities = associativeEntities;
            _setEntity1ForeignEntity = setEntity1ForeignEntity;
            _setEntity2ForeignEntity = setEntity2ForeignEntity;
            _setForeignKeys = setForeignKeys;
            _matchAssociativeEntityOwner = matchAssociativeEntityOwner;
            _matchAssociativeEntity1 = matchAssociativeEntity1;
            _matchAssociativeEntity2 = matchAssociativeEntity2;
        }
        #endregion

        #region Public properties
        public IDbEntity AssociativeCollectionOwner { get; private set; }
        #endregion

        #region Private methods
        private Func<TAssociative, bool> Matches(TEntity1 entity1, TEntity2 entity2)
        {
            return e => _matchAssociativeEntityOwner(AssociativeCollectionOwner, e) 
                        && _matchAssociativeEntity1(entity1, e) 
                        && _matchAssociativeEntity2(entity2, e);
        }

        private Func<TAssociative, bool> Matches(TEntity1 entity1)
        {
            return e => _matchAssociativeEntityOwner(AssociativeCollectionOwner, e)
                        && _matchAssociativeEntity1(entity1, e);
        }

        private Func<TAssociative, bool> Matches(TEntity2 entity2)
        {
            return e => _matchAssociativeEntityOwner(AssociativeCollectionOwner, e)
                        && _matchAssociativeEntity2(entity2, e);
        }

        private void Remove(TAssociative associativeEntity)
        {
            if (associativeEntity != null)
            {
                if (associativeEntity.State == EntityState.Persisted)
                    associativeEntity.IsDeleted = true;
                else
                    _associativeEntities().Remove(associativeEntity);
            }
        }
        #endregion

        #region Public methods
        public virtual TAssociative Add(TEntity1 entity1, TEntity2 entity2)
        {
            TAssociative associativeEntity = _associativeEntities()
                .SingleOrDefault(Matches(entity1, entity2));
            if (associativeEntity == null)
            {
                associativeEntity = new TAssociative();
                _associativeEntities().Add(associativeEntity);
                _setForeignKeys(AssociativeCollectionOwner, entity1, entity2, associativeEntity);
                associativeEntity.CreatedByID = _userId;
                if (entity1.State == EntityState.New)
                    _setEntity1ForeignEntity(AssociativeCollectionOwner, entity1, associativeEntity);
                if (entity2.State == EntityState.New)
                    _setEntity2ForeignEntity(AssociativeCollectionOwner, entity2, associativeEntity);
            }
            associativeEntity.IsDeleted = false;
            return associativeEntity;
        }

        public virtual TAssociative Get(TEntity1 entity1, TEntity2 entity2)
        {
            TAssociative associativeEntity = _associativeEntities().SingleOrDefault(Matches(entity1, entity2));
            if (associativeEntity != null && associativeEntity.IsDeleted)
                associativeEntity = null;
            return associativeEntity;
        }

        public virtual bool Contains(TEntity1 entity1, TEntity2 entity2)
        {
            TAssociative associativeEntity = _associativeEntities().SingleOrDefault(Matches(entity1, entity2));
            return associativeEntity != null && !associativeEntity.IsDeleted;
        }

        public virtual bool Contains(TEntity1 entity1)
        {
            IEnumerable<TAssociative> matching = _associativeEntities().Where(Matches(entity1));
            return matching.Count() > 0 && matching.Any(e => !e.IsDeleted);
        }

        public virtual bool Contains(TEntity2 entity2)
        {
            IEnumerable<TAssociative> matching = _associativeEntities().Where(Matches(entity2));
            return matching.Count() > 0 && matching.Any(e => !e.IsDeleted);
        }

        public virtual TAssociative Remove(TEntity1 entity1, TEntity2 entity2)
        {
            TAssociative associativeEntity = _associativeEntities().SingleOrDefault(Matches(entity1, entity2));
            Remove(associativeEntity);
            return associativeEntity;
        }

        public virtual IEnumerable<TAssociative> Remove(TEntity1 entity1)
        {
            ICollection<TAssociative> associativeEntities = _associativeEntities().Where(Matches(entity1)).ToList();
            foreach(TAssociative e in associativeEntities)
                Remove(e);
            return associativeEntities;
        }

        public virtual IEnumerable<TAssociative> Remove(TEntity2 entity2)
        {
            ICollection<TAssociative> associativeEntities = _associativeEntities().Where(Matches(entity2)).ToList();
            foreach (TAssociative e in associativeEntities)
                Remove(e);
            return associativeEntities;
        }
        #endregion
    }
}
