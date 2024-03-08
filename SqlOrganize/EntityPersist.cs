using System.Data.Common;
using System.Transactions;
using Utils;

namespace SqlOrganize
{
    public abstract class EntityPersist
    {
        /// <summary>conexion opcional</summary>
        protected DbConnection? connection;
        
        /// <summary>transaccion opcional</summary>
        protected DbTransaction transaction;

        public Db Db { get; }

        public List<object?> parameters { get; set; } = new List<object?> { };

        public int count = 0;

        public string sql { get; set; } = "";

        /// <summary>
        /// Sintesis de elementos persistidos
        /// </summary>
        /// <remarks>
        /// Para poder identificar rapidamente todas las entidades que se modificaron de la base de datos
        /// </remarks>
        public List<(string entityName, object id, string action)> detail = new();


        public EntityPersist SetConn(DbConnection connection)
        {
            this.connection = connection;
            return this;
        }

        public EntityPersist(Db db)
        {
            Db = db;
        }

        public EntityPersist Parameters(params object[] parameters)
        {
            this.parameters.AddRange(parameters.ToList());
            return this;
        }

        /// <summary>
        /// Se separa el método WhereIds para procesar la cantidad de parametros
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="_entityName"></param>
        /// <returns></returns>
        protected void WhereIds(string entityName, params object[] ids)
        {
            string idMap = Db.Mapping(entityName!).Map(Db.config.id);

            if ((ids.Count() + count) > 2100) //SQL Server no admite mas de 2100 parametros, se define consulta alternativa para estos casos
            {
                List<object> ids_ = new();
                var v = Db.Values(entityName!);
                foreach (var id in ids)
                {
                    v.Set(Db.config.id, id);
                    var id_ = v.Sql(Db.config.id);
                    ids_.Add(id_);
                    detail.Add((entityName!, id, "delete"));

                }
                sql += @"WHERE " + idMap + " IN (" + String.Join(",", ids_) + @");
";
            }
            else
            {
                sql += @"WHERE " + idMap + " IN (@" + count + @");
";
                count++;
                parameters.Add(ids.ToList());

                foreach (var id in ids)
                    detail.Add((entityName!, id, "delete"));
            }
        }

        public EntityPersist DeleteIds(string _entityName, params object[] ids)
        {
            Entity e = Db.Entity(_entityName);
  
            sql += @"
DELETE " + e.alias + " FROM " + e.name + " " + e.alias + @"
";

            WhereIds(_entityName, ids);

            return this;
        }

        abstract protected EntityPersist _Update(string _entityName, IDictionary<string, object?> row);

        public EntityPersist Update(EntityValues values)
        {
            return Update(values.entityName, values.Values());
        }

        public EntityPersist Update(string _entityName, IDictionary<string, object?> row)
        {
            _Update(_entityName, row);
            string id = Db.Mapping(_entityName!).Map(Db.config.id);
            sql += @"
WHERE " + id + " = @" + count + @";
";
            count++;
            parameters.Add(row[Db.config.id]!);
            detail.Add((_entityName!, row[Db.config.id]!, "update"));
            return this;
        }


        public EntityPersist UpdateIds(string _entityName, Dictionary<string, object?> row, params object[] ids)
        {
            _Update(_entityName, row);

            string idMap = Db.Mapping(_entityName!).Map(Db.config.id);

            if ((ids.Count() + count) > 2100) //SQL Server no admite mas de 2100 parametros, se define consulta alternativa para estos casos
            {
                List<object> ids_ = new();
                var v = Db.Values(_entityName!);
                foreach (var id in ids)
                {
                    v.Set(Db.config.id, id);
                    var id_ = v.Sql(Db.config.id);
                    ids_.Add(id_);
                    detail.Add((_entityName!, id, "update"));

                }
                sql += @"WHERE " + idMap + " IN (" + String.Join(", ", ids_) + @");
";
            } else
            {
                sql += @"WHERE " + idMap + " IN (@" + count + @");
";
                count++;
                parameters.Add(ids);

                foreach (var id in ids)
                    detail.Add((_entityName!, id, "update"));
            }
            
            return this;
        }


        /// <summary>
        /// Actualizar valores de todas las entradas de una tabla
        /// </summary>
        /// <param name="row"></param>
        /// <param name="_entityName"></param>
        /// <remarks>USAR CON PRECAUCIÓN!!!</remarks>
        /// <returns></returns>
        public EntityPersist UpdateAll(string _entityName, Dictionary<string, object?> row)
        {
            var ids = Db.Query(_entityName).Fields(Db.config.id).Size(0).Column<object>();
            return (ids.Count() > 0) ? UpdateIds(_entityName, row, ids.ToArray()) : this;
        }

        /// <summary>
        /// Actualizar un unico campo
        /// </summary>
        /// <param name="key">Nombre del campo a actualizar</param>
        /// <param name="value">Valor del campo a actualizar</param>
        /// <param name="id">Identificacion de la fila a actualizar</param>
        /// <param name="_entityName">Nombre de la entidad, si no se especifica se toma el atributo</param>
        /// <returns>Mismo objeto</returns>
        public EntityPersist UpdateValueIds(string _entityName, string key, object? value, params object[] ids)
        {
            Dictionary<string, object?> row = new Dictionary<string, object?>()
            {
                { key, value }
            };
            return UpdateIds(_entityName, row, ids);
        }

        /// <summary>
        /// Actualizar un unico campo
        /// </summary>
        /// <param name="key">Nombre del campo a actualizar</param>
        /// <param name="value">Valor del campo a actualizar</param>
        /// <param name="id">Identificacion de la fila a actualizar</param>
        /// <param name="_entityName">Nombre de la entidad, si no se especifica se toma el atributo</param>
        /// <returns>Mismo objeto</returns>
        public EntityPersist UpdateValueAll(string _entityName, string key, object value)
        {
            Dictionary<string, object> row = new Dictionary<string, object>() { { key, value } };
            return UpdateAll(_entityName, row);
        }


        /// <summary>
        /// Actualiza valor local o de relacion
        /// </summary>
        /// <param name="key">Nombre del campo a actualizar</param>
        /// <param name="value">Nuevo valor del campo a actualizar</param>
        /// <param name="source">Fuente con todos los valores sin actualizar</param>
        /// <param name="_entityName">Opcional nombre de la entidad, si no existe toma el atributo</param>
        /// <returns>Mismo objeto</returns>
        public EntityPersist UpdateValueRel(string _entityName, string key, object? value, IDictionary<string, object?> source)
        {
            string idKey = Db.config.id;
            if (key.Contains("__"))
            {
                int indexSeparator = key.IndexOf("__");
                string fieldId = key.Substring(0, indexSeparator);
                _entityName = Db.Entity(_entityName!).relations[fieldId].refEntityName;
                idKey = fieldId + "__" + Db.config.id;
                key = key.Substring(indexSeparator + "__".Length); //se suma la cantidad de caracteres del separador
            }

            List<object> ids = new() { source[idKey]! };
            return UpdateValueIds(_entityName, key, value, ids);
        }

        public EntityPersist InsertObj(string _entityName, object obj)
        {
            IDictionary<string, object?> dict = obj.Dict();
            return Insert(_entityName, dict);
        }

        public EntityPersist Insert(EntityValues v)
        {
            if (v.GetOrNull(Db.config.id).IsNullOrEmptyOrDbNull())
                v.SetDefault(Db.config.id);
            return Insert(v.entityName, v.Values()!);
        }


        /// <summary>
        /// Insertar
        /// </summary>
        /// <param name="row"></param>
        /// <param name="_entityName"></param>
        /// <returns></returns>
        /// <remarks>Debe estar definido el id</remarks>
        public EntityPersist Insert(string _entityName, IDictionary<string, object?> row)
        {
            List<string> fieldNames = Db.FieldNamesAdmin(_entityName!);
            Dictionary<string, object> row_ = new();
            foreach (string key in row.Keys)
                if (fieldNames.Contains(key))
                    row_.Add(key, row[key]);

            string sn = Db.Entity(_entityName!).schemaName;
            sql += "INSERT INTO " + sn + @" (" + String.Join(", ", row_.Keys) + @") 
VALUES (";


            foreach (object value in row_.Values)
            {
                sql += "@" + count + ", ";
                parameters.Add(value);
                count++;
            }

            sql = sql.RemoveLastChar(',');
            sql += @");
";
            //EntityValues v = Db.Values(_entityName).Set(row_);
            //if (!v.values.ContainsKey(Db.config.id))
            //    v.Set(Db.config.id, null).Reset(Db.config.id);
            //row[Db.config.id] = v.Get(Db.config.id);
            detail.Add((_entityName!, row[Db.config.id], "insert"));

            return this;
        }

        public string Sql()
        {
            return sql;
        }
        

        public EntityPersist PersistObj(string entityName, object obj)
        {
            IDictionary<string, object?> row = obj.Dict();
            return Persist(entityName, row);
        }


        /// <summary>
        /// Verifica existencia de valor unico en base a la configuracion de la entidad
        /// Si encuentra resultado, actualiza
        /// Si no encuentra resultado, inserta
        /// </summary>
        /// <param name="row">Conjunto de valores a persistir</param>
        /// <param name="_entityName">Nombre de la entidad, si no existe toma el atributo</param>
        /// <returns>El mismo objeto</returns>
        /// <exception cref="Exception">Si encuentra mas de un conjunto de valores a partir de los campos unicos</exception>
        /// <exception cref="Exception">Si encuentra errores de configuracion en los campos a actualizar</exception>
        /// <exception cref="Exception">Si encuentra errores de configuracion en los campos a insertar</exception>
        public EntityPersist Persist(string _entityName, IDictionary<string, object?> row)
        {
            EntityValues v = Db.Values(_entityName!).Set(row);
            return Persist(v);
        }
        


        /// <summary>
        /// Persistencia de una instancia de EntityValues
        /// </summary>
        /// <remarks>Se define el comportamiento básico de persistencia</remarks>
        /// <param name="v"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public EntityPersist Persist(EntityValues v)
        {
            v.Reset();
            EntityQuery q = Db.Query(v.entityName!).Unique(v.Values());
            IEnumerable<Dictionary<string, object?>> rows = q.ColOfDict();

            if (rows.Count() > 1)
                throw new Exception("La consulta por campos unicos retorno mas de un resultado");

            if (rows.Count() == 1)
            {
                //Se controla la existencia de id diferente? No! Se reasigna el id, dejo el codigo comentado
                //if (v.values.ContainsKey(Db.config.id) && v.Get(Db.config.id).ToString() != rows.ElementAt(0)[Db.config.id].ToString())
                //    throw new Exception("Los id son diferentes");

                v.Set(Db.config.id, rows.ElementAt(0)[Db.config.id]);
                if (!v.Check())
                    throw new Exception("Los campos a actualizar poseen errores: " + v.Logging.ToString());

                return Update(v.entityName, v.Values()!);
            }

            if (!v.Values().ContainsKey(Db.config.id) || v.Values()[Db.config.id].IsNullOrEmptyOrDbNull())
                v.SetDefault(Db.config.id);
                    
            v.Default().Reset(Db.config.id).Check();

            if (v.Logging.HasErrors())
                throw new Exception("Los campos a insertar poseen errores: " + v.Logging.ToString());

            return Insert(v.entityName, v.Values());
        }

        /// <summary>
        /// Ejecuta verificando conexion, si no existe la crea
        /// </summary>
        /// <returns></returns>
        public EntityPersist Exec()
        {
            var q = Db.Query();
            q.connection = connection;
            q.transaction = transaction;
            q.sql = sql;
            q.parameters = parameters;
            q.Exec();
            return this;
        }

        /// <summary>
        /// Ejecuta, abriendo una transaccion, realiza commit al finalizar o rollback si falla
        /// Si no existe conexion la crea
        /// </summary>
        /// <returns></returns>
        abstract public EntityPersist Transaction();

        /// <summary>
        /// Metodo especial de ejecucion de transacciones que realiza un split en el sql para ejecutar las transacciones una por una.
        /// </summary>
        /// <remarks>Desventaja: Procesa todos los parametros por cada consulta</remarks>
        abstract public EntityPersist TransactionSplit();

        /// <summary>Transaction Split, debe existir una conexion abierta obligatorimienta</summary>
        /// <remarks>Desventaja: Procesa todos los parametros por cada consulta</remarks>
        protected void _TransactionSplit()
        {
            using DbTransaction tran = connection!.BeginTransaction();

            try
            {
                string[] sqls = sql.Split(";");

                foreach (string s in sqls)
                {
                    if (s.Trim().IsNullOrEmpty())
                        continue;

                    var qu = Db.Query();
                    qu.connection = connection;
                    qu.transaction = tran;
                    qu.sql = s;
                    qu.parameters = parameters;
                    qu.Exec();
                }

                tran.Commit();
            }

            catch (Exception)
            {
                tran.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Ejecuta, abriendo una transaccion, realiza commit al finalizar o rollback si falla
        /// Debe existir una conexion abierta
        /// </summary>
        /// <returns></returns>
        protected void _Transaction()
        {
            using DbTransaction tran = connection.BeginTransaction();
            try
            {
                transaction = tran;
                Exec();
                tran.Commit();
            }
            catch (Exception)
            {
                tran.Rollback();
                throw;
            }
        }




        public EntityPersist RemoveCacheQueries()
        {
            object queries;

            if (Db.Cache.TryGetValue("queries", out queries))
            {
                foreach (string q in (queries as List<string>)!)
                    Db.Cache.Remove(q);

                Db.Cache.Remove("queries");
            }
            return this;

        }

        /// <summary>
        /// Remover de la cache todas las consultas y las entidades indicadas en el parametro
        /// </summary>
        public EntityPersist RemoveCache()
        {
            RemoveCacheQueries();
            foreach (var d in detail)
                Db.Cache!.Remove(d.entityName + d.id);
            return this;
        }

        public EntityPersist RemoveCache(string entityName, object id)
        {
            RemoveCacheQueries();
            Db.Cache!.Remove(entityName + id);
            return this;
        }

        public EntityPersist RemoveCache(EntityValues values)
        {
            RemoveCacheQueries();
            Db.Cache!.Remove(values.entityName + values.Get(Db.config.id));
            return this;
        }
    }

}
 
