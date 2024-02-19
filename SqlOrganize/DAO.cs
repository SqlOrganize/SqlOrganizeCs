using SqlOrganize.Exceptions;
using Utils;

namespace SqlOrganize
{

    /// <summary>
    /// Metodos de acceso a datos de uso general
    /// </summary>
    public class DAO
    {
        protected Db Db;

        public DAO(Db db) {
            this.Db = db;
        }

        public IEnumerable<Dictionary<string, object?>> SearchObj(string entityName, object param)
        {
            return Db.Query(entityName).SearchObj(param).Size(0).ColOfDictCache();
        }

        public IEnumerable<Dictionary<string, object?>> SearchKeyValue(string entityName, string key, object value)
        {
            return Db.Query(entityName).
                Where(key + " = @0").
                Parameters(value).
                Size(0).
                ColOfDictCache();
        }

        public EntityPersist UpdateValueRel(string entityName, string key, object? value, IDictionary<string, object?> source)
        {
            return Db.Persist().UpdateValueRel(entityName, key, value, source).Exec().RemoveCache();
        }

        public IDictionary<string, object?> Get(string entityName, object id)
        {
            return Db.Query(entityName).CacheByIds( id ).ElementAt(0);
        }

        public IDictionary<string, object?>? RowByFieldValue(string entityName, string fieldName, object value)
        {
            return Db.Query(entityName).Where("$" + fieldName + " = @0").Parameters(value).DictCache();
        }

        public IDictionary<string, object?>? RowByUniqueWithoutIdIfExists(string entityName, IDictionary<string, object?> source)
        {

            var q = Db.Query(entityName).Unique(source);

            if (source.ContainsKey(Db.config.id) && !source[Db.config.id]!.IsNullOrEmptyOrDbNull())
                q.And("$" + Db.config.id + " != @"+q.parameters.Count()).Parameters(source[Db.config.id]!);

            return q.DictCache();
        }


        public IDictionary<string, object?>? RowByUnique(EntityValues ev)
        {
            return RowByUnique(ev.entityName, ev.Values());
        }

        public IDictionary<string, object?>? RowByUnique(string entityName, IDictionary<string, object?> source)
        {
            EntityQuery q = Db.Query(entityName).Unique(source);
            IEnumerable<Dictionary<string, object?>> rows = q.ColOfDict();

            if (rows.Count() > 1)
                throw new Exception("La consulta por campos unicos retorno mas de un resultado");

            if (rows.Count() == 1)
                return rows.ElementAt(0);

            else
                return null;
        }

        public void Persist(EntityValues v)
        {
            if (v.Get(Db.config.id).IsNullOrEmptyOrDbNull())
            {
                v.Default().Reset();
                Db.Persist().Insert(v).Exec().RemoveCache();
            }
            else
            {
                v.Reset();
                Db.Persist().Update(v).Exec().RemoveCache();
            }
        }


        public IDictionary<string, object?>? RowByUniqueFieldOrValues(string fieldName, EntityValues values)
        {
            try { 
                if (Db.Field(values.entityName, fieldName).IsUnique())
                    return RowByFieldValue(values.entityName, fieldName, values.Get(fieldName));
                else
                    return RowByUniqueWithoutIdIfExists(values.entityName, values.Values());
            } catch (UniqueException ex)
            {
                return null;
            }
        }



    }
}
