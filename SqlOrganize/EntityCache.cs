using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace SqlOrganize
{
    public class EntityCache
    {

        Db Db;
        EntitySql Sql;


        /// <summary>
        /// Constructor para EntitySelect
        /// </summary>
        /// <param name="_db">Contenedor principal del proyecto</param>
        public EntityCache(Db db, EntitySql sql)
        {
            Db = db;
            Sql = sql;
        }



        /// <summary>
        /// Metodo de busqueda rapida en cache
        /// </summary>
        /// <remarks>Solo analiza el atributo fields (devuelve relaciones)</remarks>
        /// <param name="ids"></param>
        /// <returns></returns>
        public IEnumerable<Dictionary<string, object?>> CacheByIds(params object[] ids)
        {
            if (Sql.fields.IsNullOrEmpty())
                Sql.Fields();

            List<string> _fields = Sql.fields!.Replace("$", "").Split(',').ToList().Select(s => s.Trim()).ToList();

            return PreColOfDictCacheRecursive(_fields, ids);
        }

        /// <summary>
        /// Metodo de busqueda rapida en cache
        /// </summary>
        /// <remarks>Solo analiza el atributo fields (devuelve relaciones)</remarks>
        /// <param name="ids"></param>
        /// <returns></returns>
        public IDictionary<string, object?>? CacheById(object id)
        {
            var list = CacheByIds(id);
            if (list.IsNullOrEmpty())
                return null;

            return list.ElementAt(0);
        }

        /// <summary>
        /// Metodo de busqueda rapida en cache
        /// </summary>
        /// <remarks>Solo analiza el atributo fields (NO devuelve relaciones)</remarks>
        /// <param name="ids"></param>
        /// <returns></returns>
        public IDictionary<string, object>? _CacheById(object id)
        {
            var list = _CacheByIds(id);
            if (list.IsNullOrEmpty())
                return null;

            return list.ElementAt(0);
        }


        /// <summary>
        /// Obtener campos de una entidad (sin relaciones)<br/>
        /// Si no encuentra valores en el Cache, realiza una consulta a la base de datos y lo almacena en Db.Cache.
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="ids"></param>
        /// <remarks>IMPORTANTE! No devuelve relaciones!!!</remarks>
        /// <returns></returns>
        public List<IDictionary<string, object?>> _CacheByIds(params object[] ids)
        {
            ids.Distinct();

            List<IDictionary<string, object?>> response = new(ids.Count()); //respuesta que sera devuelta

            List<object> searchIds = new(); //ids que no se encuentran en cache y deben ser buscados

            for (var i = 0; i < ids.Count(); i++)
            {
                object? data;
                if (Db.cache!.TryGetValue(Sql.entityName + ids.ElementAt(i), out data))
                {
                    if (data is JObject)
                        data = (data as JObject).ToObject<IDictionary<string, object?>>();

                    response.Insert(i, (IDictionary<string, object>)data!);
                }
                else
                {
                    response.Insert(i, null);
                    searchIds.Add(ids.ElementAt(i));
                }
            }

            if (searchIds.Count == 0)
                return response;

            IEnumerable<Dictionary<string, object?>> rows = Db.Sql(Sql.entityName).
                Size(0).
                Where("$" + Db.config.id + " IN (@0)").
                Parameters(searchIds).
                ColOfDict();

            if (rows.IsNullOrEmpty())
                throw new Exception("La consulta a traves de ids existentes no arrojo ningun resultado. Se estan usando ids correspondientes a otra entidad? Existe el id utilizado en la base de datos?");
            
            foreach (Dictionary<string, object?> row in rows)
            {
                int index = Array.IndexOf(ids.ToArray(), row[Db.config.id]);
                response[index] = Set(Sql.entityName, row);
            }

            return response;
        }



        /// <summary>
        /// Ejecuta consulta de datos (con relaciones).<br/>
        /// Verifica la cache para obtener el resultado de la consulta, si no existe en cache accede a la base de datos.
        /// </summary>
        protected IDictionary<string, object?> DictCacheQuery()
        {
            List<string> queries;
            if (!Db.cache.TryGetValue("queries", out queries))
                queries = new();

            IDictionary<string, object?> result;
            string queryKey = Sql!.ToString();
            if (!Db.cache.TryGetValue(queryKey, out result))
            {
                result = this.Dict();
                Db.cache.Set(queryKey, result);
                queries!.Add(queryKey);
                Db.cache.Set("queries", queries);
            }
            return result!;
        }

        /// <summary>
        /// Consulta de datos (uso de cache para consulta y resultados)<br/>
        /// </summary>
        /// <param name="query">Consulta</param>
        public IEnumerable<Dictionary<string, object?>> ColOfDictCache()
        {
            if (!Sql.select.IsNullOrEmpty() || !Sql.group.IsNullOrEmpty())
                return ColOfDictCacheQuery();

            if (Sql.fields.IsNullOrEmpty())
                Sql.Fields();

            List<string> _fields = Sql.fields!.Replace("$", "").Split(',').ToList().Select(s => s.Trim()).ToList();

            //si no se encuentra el Db.config.id, no se realiza cache.
            //Si por ejemplo se consultan solo campos de relacoines, no se aplicaria correctamente el distinct
            if (!_fields.Contains(Db.config.id))
                return ColOfDictCacheQuery();

            EntitySql sqlAux = Sql.Clone();
            sqlAux.fields = Db.config.id;

            IEnumerable<object> ids = Db.Cache(sqlAux).ColOfDictCacheQuery().ColOfVal<object>(Db.config.id);

            return PreColOfDictCacheRecursive(_fields, ids.ToArray());
        }

        /// <summary>
        /// Efectua una consulta a la base de datos, la almacena en cache.<br/>
        /// Dependiendo del tipo de consulta almacena la fila de resultado en cache.
        /// </summary>
        /// <param name="query">Consulta</param>
        /// <remarks>Cuando se esta seguro de que se desea consultar una sola fila. Utilizar este metodo para evitar que se tenga que procesar un tamaño grande de resultado</remarks>
        public IDictionary<string, object?>? DictCache()
        {
            if (!Sql.select.IsNullOrEmpty() || !Sql.group.IsNullOrEmpty())
                return DictCacheQuery();

            if (Sql.fields.IsNullOrEmpty())
                Sql.Fields();

            EntitySql esqlAux = Sql.Clone();
            esqlAux.fields = Db.config.id;

            string id = esqlAux.Value<string>();

            if (id.IsNullOrEmpty())
                return null;

            List<string> fields = Sql.fields!.Replace("$", "").Split(',').ToList().Select(s => s.Trim()).ToList();

            IEnumerable<Dictionary<string, object?>> response = PreColOfDictCacheRecursive(fields, id);

            return response.ElementAt(0);
        }


        /// <summary>
        /// Organiza los elementos a consultar y efectua la consulta a la base de datos.
        /// </summary>
        protected IEnumerable<Dictionary<string, object?>> PreColOfDictCacheRecursive(List<string> fields, params object[] ids)
        {
            FieldsOrganize fo = new(Db, Sql.entityName, fields);

            List<IDictionary<string, object?>> data = _CacheByIds(ids);

            List<Dictionary<string, object?>> response = new();

            for (var i = 0; i < data.Count; i++)
            {
                response.Add(new());
                for (var j = 0; j < fo.Fields.Count; j++)
                    response[i][fo.Fields[j]] = null;
                for (var j = 0; j < fo.FieldsMain.Count; j++)
                    response[i][fo.FieldsMain[j]] = data[i][fo.FieldsMain[j]];
            }

            return ColOfDictCacheRecursive(fo, response, 0);
        }

        /// <summary>
        /// Analiza la respuesta de una consulta y re organiza los elementos para armar el resultado
        /// </summary>
        protected IEnumerable<Dictionary<string, object?>> ColOfDictCacheRecursive(FieldsOrganize fo, IEnumerable<Dictionary<string, object?>> response, int index)
        {
            if (index >= fo.FieldsIdOrder.Count) return response;
            {
                if (response.Count() == 0) return response;

                string fieldId = fo.FieldsIdOrder[index];
                string refEntityName = Db.Entity(fo.EntityName).relations[fieldId].refEntityName;
                string? parentId = Db.Entity(fo.EntityName).relations[fieldId].parentId;
                string fieldName = Db.Entity(fo.EntityName).relations[fieldId].fieldName;
                string refFieldName = Db.Entity(fo.EntityName).relations[fieldId].refFieldName;
                string fkName = (!parentId.IsNullOrEmpty()) ? parentId + "-" + fieldName : fieldName;

                List<object> ids = response.ColOfVal<object>(fkName).Distinct().ToList();
                ids.RemoveAll(item => item == null || item == System.DBNull.Value);
                IEnumerable<IDictionary<string, object?>> data;
                if (ids.Count() == 1 && ids.ElementAt(0) == System.DBNull.Value)
                    return Enumerable.Empty<Dictionary<string, object?>>();
                else
                {
                    //Si las fk estan asociadas a una unica pk, debe indicarse para mayor eficiencia
                    if (Db.config.fkId)
                    {
                        data = Db.Sql(refEntityName)._CacheByIds(ids.ToArray());
                    }
                    else
                    {
                        //data = Db.Query(refEntityName).Where("$"+Db.config.id+" IN (@0)").Parameters(ids).ColOfDictCacheQuery();
                        data = Db.Sql(refEntityName).CacheByIds(ids.ToArray());
                    }
                }

                for (var i = 0; i < response.Count(); i++)
                {
                    if (response.ElementAt(i)[fkName].IsNullOrEmptyOrDbNull())
                        continue;

                    for (var j = 0; j < data.Count(); j++)
                    {
                        if (response.ElementAt(i)[fkName].ToString().Equals(data.ElementAt(j)[refFieldName].ToString()))
                        {
                            for (var k = 0; k < fo.FieldsRel[fieldId].Count; k++)
                            {
                                var n = fo.FieldsRel[fieldId][k];
                                response.ElementAt(i)[fieldId + "-" + n] = data.ElementAt(j)[n];
                            }
                        }
                    }
                }

                return (++index < fo.FieldsIdOrder.Count) ? ColOfDictCacheRecursive(fo, response, index) : response;
            }
        }

        /// <summary>
        /// Analiza una fila de resultado y la almacena en cache.
        /// </summary>
        /// <param name="entityName">Nombre de la entidad principal de la fila</param>
        /// <param name="row">Fila de datos (tupla)</param>
        /// <returns>Resultado filtrado solo para la entidad principal</returns>
        protected Dictionary<string, object?> Set(string entityName, Dictionary<string, object?> row)
        {
            if (!Db.Entity(entityName).relations.IsNullOrEmpty())
                SetRecursive(Db.Entity(entityName).relations!, row);

            Db.cache!.Set(entityName + row[Db.config.id]!.ToString(), row);
            return row;
        }

        /// <summary>
        /// Analiza una fila de resultado y la almacena en cache considerando cada entidad de las relaciones. 
        /// </summary>
        /// <param name="relations">Relaciones de una entidad</param>
        /// <param name="row">Fila de datos (tupla)</param>
        protected void SetRecursive(Dictionary<string, EntityRelation> relations, Dictionary<string, object?> row)
        {
            foreach (var (fieldId, rel) in relations)
            {
                var entityName = rel.refEntityName;
                Dictionary<string, object?> rowAux = new();
                string f = fieldId + "-";
                foreach (var (column, value) in row)
                {
                    if (column.Contains(f))
                    {
                        string ff = column.Substring(f.Length);
                        rowAux[ff] = value;
                        row.Remove(column);
                    }
                }
                if (rowAux.Count > 0)
                    Db.cache!.Set(entityName + rowAux[Db.config.id]!.ToString(), rowAux);
            }
        }

        public IEnumerable<Dictionary<string, object?>> ColOfDictCacheQuery()
        {
            List<string> queries;
            object _queries;

            bool res = Db.cache!.TryGetValue("queries", out _queries);
            if (res)
            {
                if (_queries is JArray)
                    queries = (_queries as JArray).ToObject<List<string>>();
                else
                    queries = (List<string>)_queries;
            }
            else
            {
                queries = new();
            }



            IEnumerable<Dictionary<string, object?>> result;
            object _result;
            string queryKey = Sql!.ToString();
            res = Db.cache!.TryGetValue(queryKey, out _result);

            if (res)
            {
                if (_result is JArray)
                    result = (_result as JArray).ToObject<IEnumerable<Dictionary<string, object>>>();
                else
                    result = (IEnumerable<Dictionary<string, object>>)_result;
            }
            else
            {
                result = Sql.ColOfDict();
                Db.cache!.Set(queryKey, result);
                queries!.Add(queryKey);
                Db.cache!.Set("queries", queries);
            }
            return result!;
        }
        
    }


    public class FieldsOrganize
    {
        Db Db;

        public string EntityName;

        /*
        Campos a consultar de relaciones, 
        Se pueden agregar fk adicionales necesarias para comparar
        */
        public List<string> Fields;

        /*
        Campos de relaciones
        Para facilitar el filtro de campos de relaciones se agregan agrupadas por fieldId
        */
        public Dictionary<string, List<string>> FieldsRel = new();

        /*
        FieldsId que deben ser consultados en el orden correspondiente
        */
        public List<string> FieldsIdOrder = new();

        /*
        Campos a consultar de la entidad principal "entityName", 
        */
        public List<string> FieldsMain = new();

        public FieldsOrganize(Db db, string entityName, List<string> fields)
        {
            Db = db;
            EntityName = entityName;
            Fields = fields;
            this.OrganizeRelations(0);
            this.OrganizeOrder(Db.Entity(entityName).tree);
        }
        /*
        Organizar fields 
        Se agregan los campos necesarios para consultar y comparar el arbol de fields
        */
        protected void OrganizeRelations(int index)
        {
            if (Fields[index].Contains("-"))
            {
                var f = Fields[index].Split("-");
                EntityRelation r = Db.Entity(EntityName).relations[f[0]];
                string fkName = (!r.parentId.IsNullOrEmpty()) ? r.parentId + "-" + r.fieldName : r.fieldName;

                if (!FieldsRel.ContainsKey(f[0]))
                    FieldsRel.Add(f[0], new List<string>());
                if (!FieldsRel[f[0]].Contains(f[1]))
                    FieldsRel[f[0]].Add(f[1]);
                if (!Fields.Contains(fkName))
                    Fields.Add(fkName);
            }
            else
                FieldsMain.Add(Fields[index]);

            if (++index < Fields.Count)
                OrganizeRelations(index);
        }

        protected void OrganizeOrder(Dictionary<string, EntityTree> tree)
        {
            foreach (var (fieldId, et) in tree)
            {
                bool recorrerChildren = false;
                for (var j = 0; j < Fields.Count; j++)
                {
                    if (Fields[j].Contains("-"))
                    {
                        var f = Fields[j].Split("-");
                        if (f[0] == fieldId && !FieldsIdOrder.Contains(fieldId))
                        {
                            FieldsIdOrder.Add(fieldId);
                            recorrerChildren = true;
                        }

                    }
                }
                if (recorrerChildren && !et.children.IsNullOrEmpty())
                    OrganizeOrder(et.children);
            }
        }
    }

}
