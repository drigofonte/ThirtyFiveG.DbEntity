using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using ThirtyFiveG.DbEntity.Entity;

namespace ThirtyFiveG.DbEntity.Extensions
{
    public static class DbContextExtensions
    {
        private static readonly IDictionary<Type, IEnumerable<string>> KeysForTypes = new Dictionary<Type, IEnumerable<string>>();

        #region Private static methods
        private static Type GetBaseTypeFor(Type type)
        {
            if (type.BaseType != null && type.Namespace == "System.Data.Entity.DynamicProxies")
                return type.BaseType;
            return type;
        }
        #endregion

        #region Public static methods
        public static PropertyInfo GetForeignKeyProperty(this DbContext context, PropertyInfo property)
        {
            AssociationType foreignKey = ForeignKeyForNavigationProperty(context, property.DeclaringType, property);
            PropertyInfo foreignKeyProperty = foreignKey.GetForeignKeyProperty(property.DeclaringType);
            return foreignKeyProperty;
        }

        public static object[] KeyValuesFor(this DbContext context, object entity)
        {
            Contract.Requires(context != null);
            Contract.Requires(entity != null);

            var entry = context.Entry(entity);
            return KeysFor(context, entity.GetType())
                .Select(k => entry.Property(k).CurrentValue)
                .ToArray();
        }

        public static object[] KeysFor(this DbContext context, Type entityType, Tuple<string, object>[] primaryKeys)
        {
            List<string> keysFor = KeysFor(context, entityType).ToList();
            object[] keys = new object[keysFor.Count()];
            for (int i = 0; i < keysFor.Count; i++)
            {
                keys[i] = primaryKeys.Single(k => k.Item1.Equals(keysFor.ElementAt(i))).Item2;
            }
            return keys;
        }
        public static object[] KeysFor(this DbContext context, IDbEntity entity)
        {
            return KeysFor(context, entity.GetType(), entity.PrimaryKeys);
        }

        public static IEnumerable<string> KeysFor(this DbContext context, Type entityType)
        {
            Contract.Requires(context != null);
            Contract.Requires(entityType != null);

            IEnumerable<string> keysForType;
            if (!KeysForTypes.TryGetValue(entityType, out keysForType))
            {
                entityType = ObjectContext.GetObjectType(entityType);

                MetadataWorkspace metadataWorkspace =
                    ((IObjectContextAdapter)context).ObjectContext.MetadataWorkspace;
                ObjectItemCollection objectItemCollection =
                    (ObjectItemCollection)metadataWorkspace.GetItemCollection(DataSpace.OSpace);

                EntityType ospaceType = metadataWorkspace
                    .GetItems<EntityType>(DataSpace.OSpace)
                    .SingleOrDefault(t => objectItemCollection.GetClrType(t) == entityType);

                if (ospaceType == null)
                {
                    throw new ArgumentException(
                        string.Format(
                            "The type '{0}' is not mapped as an entity type.",
                            entityType.Name),
                        "entityType");
                }

                keysForType = ospaceType.KeyMembers.Select(k => k.Name);
                if (!KeysForTypes.Keys.Contains(entityType))
                    KeysForTypes.Add(entityType, keysForType);
            }

            return keysForType;
        }

        public static AssociationType ForeignKeyForNavigationProperty(this DbContext context, Type type, PropertyInfo navigationProperty)
        {
            MetadataWorkspace metadata = ((IObjectContextAdapter)context).ObjectContext.MetadataWorkspace;
            ObjectItemCollection objectItemCollection = metadata.GetItemCollection(DataSpace.OSpace) as ObjectItemCollection;
            EntityType entityType = metadata.GetItems<EntityType>(DataSpace.OSpace).SingleOrDefault(e => objectItemCollection.GetClrType(e) == GetBaseTypeFor(type));
            EntitySet entitySet = metadata.GetItems<EntityContainer>(DataSpace.CSpace).Single().EntitySets.Single(s => s.ElementType.Name == entityType.Name);
            EntitySetMapping mapping = metadata.GetItems<EntityContainerMapping>(DataSpace.CSSpace).Single().EntitySetMappings.Single(s => s.EntitySet == entitySet);
            string entityIdentity = mapping.EntityTypeMappings.First().EntityType.ToString();
            entityType = metadata.GetItem<EntityType>(entityIdentity, DataSpace.CSpace);
            return entityType.NavigationProperties.Single(p => p.Name.Equals(navigationProperty.Name)).ToEndMember.DeclaringType as AssociationType;
        }

        public static AssociationType ForeignKeyFor(this DbContext context, Type source, Type target)
        {
            MetadataWorkspace metadata = ((IObjectContextAdapter)context).ObjectContext.MetadataWorkspace;
            ItemCollection objectItemCollection = ((ObjectItemCollection)metadata.GetItemCollection(DataSpace.OSpace));
            EntityType sourceItem = objectItemCollection.Where(o => o is EntityType && ((EntityType)o).GetReferenceType().ElementType.FullName == GetBaseTypeFor(source).FullName).FirstOrDefault() as EntityType;
            NavigationProperty property = (sourceItem as EntityType).NavigationProperties.Where(p => ((p.ToEndMember.MetadataProperties["TypeUsage"].Value as TypeUsage).EdmType.MetadataProperties["ElementType"].Value as EntityType).FullName == GetBaseTypeFor(target).FullName).FirstOrDefault();
            return metadata.GetItems<AssociationType>(DataSpace.CSpace).FirstOrDefault(a => a.IsForeignKey && a.ReferentialConstraints[0].ToRole.Name.Equals(property.FromEndMember.Name) && a.ReferentialConstraints[0].FromRole.Name.Equals(property.ToEndMember.Name));
        }

        public static string GetTableName(this DbContext context, DbEntityEntry entry)
        {
            var metadata = ((IObjectContextAdapter)context).ObjectContext.MetadataWorkspace;

            // Get the part of the model that contains info about the actual CLR types
            var objectItemCollection = ((ObjectItemCollection)metadata.GetItemCollection(DataSpace.OSpace));

            Type type = GetBaseTypeFor(entry.Entity.GetType());

            // Get the entity type from the model that maps to the CLR type
            var entityType = metadata
                    .GetItems<EntityType>(DataSpace.OSpace)
                    .Single(e => objectItemCollection.GetClrType(e) == type);

            // Get the entity set that uses this entity type
            var entitySet = metadata
                .GetItems<EntityContainer>(DataSpace.CSpace)
                .Single()
                .EntitySets
                .Single(s => s.ElementType.Name == entityType.Name);

            // Find the mapping between conceptual and storage model for this entity set
            var mapping = metadata.GetItems<EntityContainerMapping>(DataSpace.CSSpace)
                    .Single()
                    .EntitySetMappings
                    .Single(s => s.EntitySet == entitySet);

            // Find the storage entity set (table) that the entity is mapped
            EntitySet table = mapping
                .EntityTypeMappings.Single()
                .Fragments.Single()
                .StoreEntitySet;

            // Return the table name from the storage entity set
            return (string)table.MetadataProperties["Table"].Value ?? table.Name;
        }
        #endregion
    }
}
