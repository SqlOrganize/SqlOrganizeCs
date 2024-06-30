using Newtonsoft.Json.Linq;
using SqlOrganize.Exceptions;
using System.Data.Common;
using Utils;

namespace SqlOrganize
{
    public static class ExtensionMethods
    {
        #region EntitySql + Query
        /// <summary>Ejecucion rapida de EntitySql</summary>
        public static IEnumerable<Dictionary<string, object?>> ColOfDict(this EntitySql esql)
        {
            using Query query = esql.Query();
            using DbConnection connection = query.OpenConnection();
            return query.ColOfDict();
        }

        /// <summary>Ejecucion rapida de EntitySql</summary>
        public static IDictionary<string, object?>? Dict(this EntitySql esql)
        {
            using Query query = esql.Query();
            using DbConnection connection = query.OpenConnection();
            return query.Dict();
        }


        public static T? Obj<T>(this EntitySql esql) where T : class, new()
        {
            using Query query = esql.Query();
            using DbConnection connection = query.OpenConnection();
            return query.Obj<T>();
        }

        public static IEnumerable<T> Column<T>(this EntitySql esql, string columnName)
        {
            using Query query = esql.Query();
            using DbConnection connection = query.OpenConnection();
            return query.Column<T>(columnName);
        }

        public static IEnumerable<T> Column<T>(this EntitySql esql, int columnNumber = 0)
        {
            using Query query = esql.Query();
            using DbConnection connection = query.OpenConnection();
            return query.Column<T>(columnNumber);
        }


        public static T? Value<T>(this EntitySql esql, string columnName)
        {
            using Query query = esql.Query();
            using DbConnection connection = query.OpenConnection();
            return query.Value<T>(columnName);
        }

        public static T? Value<T>(this EntitySql esql, int columnNumber = 0)
        {
            using Query query = esql.Query();
            using DbConnection connection = query.OpenConnection();
            return query.Value<T>(columnNumber);
        }
        public static IDictionary<string, object?>? RowByUnique(this EntitySql entitySql, IDictionary<string, object?> source)
        {
            IEnumerable<Dictionary<string, object?>> rows = entitySql.Unique(source).ColOfDict();

            if (rows.Count() > 1)
                throw new Exception("La consulta por campos unicos retorno mas de un resultado");

            if (rows.Count() == 1)
                return rows.ElementAt(0);

            else
                return null;
        }
        #endregion

        #region EntityPersist + Query
        /// <summary>Ejecución persistencia</summary>
        public static EntityPersist Exec(this EntityPersist persist)
        {
            var query = persist.Query();
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
                    persist.Query(query).ExecTransaction();
                }

                query.CommitTransaction();
            }
            catch (Exception ex)
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
        
        public static EntityPersist TransferOm(this EntityPersist persist, string entityName, object origenId, object destinoId)
        {
            List<Field> fieldsOmPersona = persist.Db.Entity(entityName).FieldsOm();
            foreach (var field in fieldsOmPersona)
            {
                object[] ids = persist.Db.Sql(field.entityName).Where(field.name + " = @0").Parameters(origenId).Column<object>("id").ToArray();
                if(ids.Any())
                    persist.UpdateValueIds(field.entityName, field.name, destinoId!, ids);
            }
            return persist;
        }
        #endregion

        #region EntityPersist + EntityValues
        public static EntityPersist Insert(this EntityValues values)
        {
            return values.db.Persist().Insert(values);
        }

        public static EntityPersist Update(this EntityValues values)
        {
            return values.db.Persist().Update(values);
        }

        public static EntityValues Insert(this EntityValues values, EntityPersist persist)
        {
            persist.Insert(values);
            return values;
        }

        public static EntityValues Update(this EntityValues values, EntityPersist persist)
        {
            persist.Update(values);
            return values;
        }

        public static IDictionary<string, object?>? RowByFieldValue(this EntityValues entityValues, string fieldName)
        {
            return entityValues.db.Sql(entityValues.entityName).Equal(fieldName, entityValues.Get(fieldName)).Cache().Dict();
        }

        public static EntityPersist PersistId(this EntityValues v)
        {
            EntityPersist p;
            if (v.Get(v.db.config.id).IsNullOrEmptyOrDbNull())
            {
                v.Default().Reset();
                p = v.db.Persist().Insert(v).Exec().RemoveCache();
            }
            else
            {
                v.Reset();
                p = v.db.Persist().Update(v).Exec().RemoveCache();
            }

            return p;
        }

        public static EntityPersist Persist(this EntityValues v)
        {
            var row = v.RowByUnique();

            EntityPersist p;

            if (row.IsNullOrEmptyOrDbNull())
            {
                v.Default().Reset();
                return v.db.Persist().Insert(v);
            }
            else
            {
                v.Reset();
                return v.db.Persist().Update(v);
            }

        }

        public static EntityValues Persist(this EntityValues v, EntityPersist persist)
        {
            persist.Persist(v);
            return v;
        }

        public static EntityValues PersistCondition(this EntityValues v, EntityPersist persist, object? condition)
        {
            persist.PersistCondition(v, condition);
            return v;
        }
        #endregion

        #region EntityPersist + Cache
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
        #endregion

        #region EntityPersist + Data
        public static EntityPersist Delete(this Data data)
        {
            data.Dict();
            return data.db.Persist().DeleteIds(data["entityName"], data["id"]);
        }

        #endregion

        #region EntitySql + EntityValues
        public static IDictionary<string, object?>? RowByUniqueFieldOrValues(this EntityValues values, string fieldName)
        {
            try
            {
                if (values.db.Field(values.entityName, fieldName).IsUnique())
                    return values.RowByFieldValue(fieldName);
                else
                    return values.RowByUniqueWithoutIdIfExists();
            }
            catch (UniqueException ex)
            {
                return null;
            }
        }
        public static IDictionary<string, object?>? RowByUniqueWithoutIdIfExists(this EntityValues values)
        {
            return values.db.Sql(values.entityName).RowByUniqueWithoutIdIfExists(values.Values());
        }
        public static IDictionary<string, object?>? RowByUniqueWithoutIdIfExists(this EntitySql entitySql, IDictionary<string, object?> source)
        {
            entitySql.Unique(source);

            if (source.ContainsKey(entitySql.Db.config.id) && !source[entitySql.Db.config.id]!.IsNullOrEmptyOrDbNull())
                entitySql.And("$" + entitySql.Db.config.id + " != @" + entitySql.parameters.Count()).Parameters(source[entitySql.Db.config.id]!);

            return entitySql.Cache().Dict();
        }

        public static IDictionary<string, object?>? RowByUnique(this EntityValues ev)
        {
            return ev.db.Sql(ev.entityName).RowByUnique(ev.Values());
        }
        #endregion

        #region EntitySql
        public static IEnumerable<Dictionary<string, object?>> SearchKeyValue(this EntitySql entitySql, string key, object value)
        {
            return entitySql.
                Where(key + " = @0").
                Parameters(value).
                Size(0).
                Cache().ColOfDict();
        }
        #endregion

        #region Data
        /// <summary> Metodos especiales para facilitar el uso de subclases sin necesidad de cast </summary>
        public static T DefaultData<T>(this T data) where T : Data
        {
            data.Default();
            return data;
        }
        #endregion

    }
}
