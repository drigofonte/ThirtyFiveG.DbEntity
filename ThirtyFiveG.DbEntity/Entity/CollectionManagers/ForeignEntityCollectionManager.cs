using System;
using System.Collections.Generic;
using System.Linq;
using ThirtyFiveG.DbEntity.Common;

namespace ThirtyFiveG.DbEntity.Entity.CollectionManagers
{
    public class ForeignEntityCollectionManager<TEntity> : IForeignEntityCollectionManager<TEntity>
        where TEntity : class, IDbEntity, IDbEntityBasic, new()
    {
        #region Private variables
        private Func<ICollection<TEntity>> _collection;
        private Action<IDbEntity, TEntity> _setForeignKeys;
        private int _userId;
        #endregion

        #region Constructor
        public ForeignEntityCollectionManager(int userId, IDbEntity collectionOwner, Func<ICollection<TEntity>> collection, Action<IDbEntity, TEntity> setForeignKeys)
        {
            _userId = userId;
            CollectionOwner = collectionOwner;
            _collection = collection;
            _setForeignKeys = setForeignKeys;
        }
        #endregion

        #region Public properties
        public ICollection<TEntity> Collection { get { return _collection(); } }
        public IDbEntity CollectionOwner { get; private set; }
        #endregion

        #region Public methods
        public TEntity Add()
        {
            TEntity added = DbEntityRepository.CreateInstance<TEntity>(_userId);
            Add(added);
            return added;
        }

        public TEntity Add(TEntity entity)
        {
            if (!Contains(entity))
                Collection.Add(entity);

            entity.IsDeleted = false;
            if (entity.State == EntityState.New)
            {
                entity.CreatedByID = _userId;
                _setForeignKeys(CollectionOwner, entity);
            }
            return entity;
        }

        public bool Contains(TEntity entity)
        {
            bool contains = false;
            if (entity.State == EntityState.New)
                contains = Collection.Any(e => !e.IsDeleted && e.Guid.Equals(entity.Guid));
            else
                contains = Collection.Any(e => !e.IsDeleted && DbEntityUtilities.PrimaryKeysEqual(e.PrimaryKeys, entity.PrimaryKeys));
            return contains;
        }

        public TEntity Get(Tuple<string, object>[] primaryKeys)
        {
            return Collection.SingleOrDefault(e => /*!e.IsDeleted && */DbEntityUtilities.PrimaryKeysEqual(e.PrimaryKeys, primaryKeys));
        }

        public TEntity Get(string guid)
        {
            return Collection.SingleOrDefault(e => !e.IsDeleted && e.Guid.Equals(guid));
        }

        public TEntity Remove(TEntity entity)
        {
            TEntity removed = default(TEntity);
            if (entity.State == EntityState.New)
            {
                removed = Get(entity.Guid);
                Collection.Remove(removed);
            } else
            {
                removed = Get(entity.PrimaryKeys);
                removed.IsDeleted = true;
            }
            return removed;

        }
        #endregion
    }
}
