using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Utils;

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

        public static IEnumerable<Dictionary<string, object?>> CacheByIds(this EntitySql esql, params object[] ids)
        {
            return esql.Db.Cache(esql).CacheByIds(ids);
        }

        public static IDictionary<string, object?>? CacheById(this EntitySql esql, object id)
        {
            return esql.Db.Cache(esql).CacheById(id);
        }

        public static IDictionary<string, object>? _CacheById(this EntitySql esql, object id)
        {
            return esql.Db.Cache(esql)._CacheById(id);
        }


        public static List<IDictionary<string, object?>> _CacheByIds(this EntitySql esql, params object[] ids)
        {
            return esql.Db.Cache(esql)._CacheByIds(ids);
        }


        public static IEnumerable<Dictionary<string, object?>> ColOfDict(this EntitySql esql)
        {
            var query = esql.Db.Query(esql);
            using DbConnection connection = query.OpenConnection();
            return query.ColOfDict();
        }

        public static T? Obj<T>(this EntitySql esql) where T : class, new()
        {
            var query = esql.Db.Query(esql);
            using DbConnection connection = query.OpenConnection();
            return query.Obj<T>();
        }

        public static IEnumerable<T> Column<T>(this EntitySql esql, string columnName)
        {
            var query = esql.Db.Query(esql);
            using DbConnection connection = query.OpenConnection();
            return query.Column<T>(columnName);
        }

        public static IEnumerable<T> Column<T>(this EntitySql esql, int columnNumber = 0)
        {
            var query = esql.Db.Query(esql);
            using DbConnection connection = query.OpenConnection();
            return query.Column<T>(columnNumber);
        }


        public static T Value<T>(this EntitySql esql, string columnName)
        {
            var query = esql.Db.Query(esql);
            using DbConnection connection = query.OpenConnection();
            return query.Value<T>(columnName);
        }

        public static T Value<T>(this EntitySql esql, int columnNumber = 0)
        {
            var query = esql.Db.Query(esql);
            using DbConnection connection = query.OpenConnection();
            return query.Value<T>(columnNumber);
        }


        /// <summary>Ejecución persistencia</summary>
        public static EntityPersist Exec(this EntityPersist persist)
        {
            var query = persist.Db.Query(persist);
            using DbConnection connection = query.OpenConnection();
            query.BeginTransaction();
            try
            {
                query.ExecTransaction();
                query.CommitTransaction();
            }
            catch (Exception ex)
            {
                query.RollbackTransaction();
                throw;
            }
            return persist;
        }

        public static EntityPersist AddTo(this EntityPersist persist, List<EntityPersist> persists)
        {
            persists.Add(persist);
            return persist;
        }

        public static EntityPersist Transaction(this EntityPersist persist)
        {
            return persist.Exec();
        }

        /// <summary>Ejecución de IEnumerable de persistencias</summary>
        public static IEnumerable<EntityPersist> Exec(this IEnumerable<EntityPersist> persists)
        {
            if (persists.IsNullOrEmpty())
                return persists;

            var query = persists.ElementAt(0).Db.Query();
            using DbConnection connection = query.OpenConnection();
            query.BeginTransaction();
            try
            {
                foreach (EntityPersist persist in persists)
                {
                    query.SetEntityPersist(persist);
                    query.ExecTransaction();
                }

                query.CommitTransaction();
            } catch (Exception ex)
            {
                query.RollbackTransaction();
                throw ex;
            }

            return persists;
        }

        public static IEnumerable<EntityPersist> Transaction(this IEnumerable<EntityPersist> persists)
        {
            return persists.Exec();
        }


        public static void RemoveCache(this IEnumerable<EntityPersist> persists)
        {

            if (persists.IsNullOrEmpty())
                return;

            persists.ElementAt(0).RemoveCacheQueries();

            foreach (EntityPersist persist in persists)
                persist.RemoveCacheDetail();
        }

        public static EntityPersist RemoveCacheQueries(this EntityPersist persist)
        {

            List<string> queries;
            object _queries;

            bool res = persist.Db.cache!.TryGetValue("queries", out _queries);
            if (res)
            {
                if (_queries is JArray)
                    queries = (_queries as JArray).ToObject<List<string>>();
                else
                    queries = (List<string>)_queries;

                foreach (string q in (queries as List<string>)!)
                    persist.Db.cache.Remove(q);
            }

            return persist;
        }

        /// <summary>
        /// Remover de la cache todas las consultas y las entidades indicadas en el parametro
        /// </summary>
        public static EntityPersist RemoveCache(this EntityPersist persist)
        {
            persist.RemoveCacheQueries();
            return persist.RemoveCacheDetail();
        }

        public static EntityPersist RemoveCacheDetail(this EntityPersist persist)
        {
            foreach (var d in persist.detail)
                persist.Db.cache!.Remove(d.entityName + d.id);
            return persist;
        }

        public static EntityPersist RemoveCache(this EntityPersist persist, string entityName, object id)
        {
            persist.RemoveCacheQueries();
            persist.Db.cache!.Remove(entityName + id);
            return persist;
        }

        public static EntityPersist RemoveCache(this EntityPersist persist, EntityValues values)
        {
            persist.RemoveCacheQueries();
            persist.Db.cache!.Remove(values.entityName + values.Get(persist.Db.config.id));
            return persist;
        }


    }
}
