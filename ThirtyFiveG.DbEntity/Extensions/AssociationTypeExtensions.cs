using System;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using System.Reflection;

namespace ThirtyFiveG.DbEntity.Extensions
{
    public static class AssociationTypeExtensions
    {
        public static PropertyInfo GetForeignKeyProperty(this AssociationType foreignKey, Type type)
        {
            if (foreignKey != null)
            {
                string foreignKeyName = foreignKey.ReferentialConstraints[0].ToProperties[0].Name;
                return type.GetProperties().FirstOrDefault(p => p.Name.Equals(foreignKeyName));
            }
            return null;
        }
    }
}
