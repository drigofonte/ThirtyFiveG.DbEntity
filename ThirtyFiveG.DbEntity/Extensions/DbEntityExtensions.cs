using ThirtyFiveG.Commons.Common;
using ThirtyFiveG.DbEntity.Entity;

namespace ThirtyFiveG.DbEntity.Extensions
{
    public static class DbEntityExtensions
    {
        public static object Eval(this IDbEntity entity, string expression)
        {
            return DataBinder.Eval(entity, expression);
        }
    }
}
