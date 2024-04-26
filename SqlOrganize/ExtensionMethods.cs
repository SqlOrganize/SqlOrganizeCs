using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlOrganize
{
    public static class ExtensionMethods
    {

        public static IEnumerable<Dictionary<string, object?>> ColOfDictCache(this EntitySql esql)
        {
            return esql.Db.Cache(esql).ColOfDictCache();
        }

        public static IDictionary<string, object?>? DictCache(this EntitySql esql)
        {
            return esql.Db.Cache(esql).DictCache();
        }

        public static T? Obj<T>(this EntitySql esql) where T : class, new()
        {
            return esql.Db.Query(esql).Obj<T>();
        }

        public static IEnumerable<T> Column<T>(this EntitySql esql, string columnName)
        {
            return esql.Db.Query(esql).Column<T>(columnName);
        }

        public static IEnumerable<T> Column<T>(this EntitySql esql, int columnNumber = 0)
        {
            return esql.Db.Query(esql).Column<T>(columnNumber);
        }


    }
}
