using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.ComponentModel;
using System.Data.Common;
using Utils;

namespace SqlOrganize
{
    /// <summary>
    /// Unificar metodos para ejecutar consultas a la base de datos
    /// </summary>    
    public abstract class Query
    {
        /// <summary>conexion opcional, si no existe al ejecutar se crea</summary>       
        public DbConnection? connection;

        /// <summary>transaccion opcional, si no existe y la necesita, la crea
        public DbTransaction? transaction;

        /// Contenedor principal del proyecto
        /// </summary>
        public Db db { get; }

        /// <summary>
        /// Parametros de las consultas
        /// </summary>
        public List<object?> parameters { get; set; } = new List<object?>();

        /// <summary>
        /// Parametros de las consultas
        /// </summary>
        public Dictionary<string, object> parametersDict { get; set; } = new ();

        /// <summary>
        /// Consultas en SQL
        /// </summary>
        public string sql { get; set; } = "";

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_db">Contenedor principal del proyecto</param>
        public Query(Db _db)
        {
            db = _db;
        }

        /// <summary>
        /// Constructor para EntityPersist
        /// </summary>
        /// <param name="_db">Contenedor principal del proyecto</param>
        public Query(Db _db, EntityPersist persist)
        {
            db = _db;
            sql = persist.Sql();
            parameters = persist.parameters;
        }

        /// <summary>
        /// Constructor para EntitySelect
        /// </summary>
        /// <param name="_db">Contenedor principal del proyecto</param>
        public Query(Db _db, EntitySql select)
        {
            db = _db;
            sql = select.Sql();
            parameters = select.parameters;
            parametersDict = select.parametersDict;
        }

        /// <summary>
        /// Ejecutar sql y devolver resultado
        /// </summary>
        /// <returns>Resultado como List -Dictionary -string, object- -</returns>
        /// <remarks>Convert the result to json with "JsonConvert.SerializeObject(data, Formatting.Indented)"</remarks>
        public IEnumerable<Dictionary<string, object?>> ColOfDict()
        {
            using DbCommand command = NewCommand();
            if (connection.IsNullOrEmpty())
            {
                using DbConnection conn = NewConnection();
                conn.Open();
                Exec(conn, command);
                using DbDataReader reader = command.ExecuteReader();
                return reader.Serialize();
            }
            else
            {
                Exec(connection!, command);
                using DbDataReader reader = command.ExecuteReader();
                return reader.Serialize();
            }
        }

        public IEnumerable<T> ColOfObj<T>() where T : class, new()
        {
            using DbCommand command = NewCommand();
            if (connection.IsNullOrEmpty())
            {
                using DbConnection conn = NewConnection();
                conn.Open();
                Exec(conn, command);
                using DbDataReader reader = command.ExecuteReader();
                return reader.ColOfObj<T>();
            }
            else
            {
                Exec(connection!, command);
                using DbDataReader reader = command.ExecuteReader();
                return reader.ColOfObj<T>();
            }
        }

        public Dictionary<string, object?>? Dict()
        {
            using DbCommand command = NewCommand();
            if (connection.IsNullOrEmpty())
            {
                using DbConnection conn = NewConnection();
                conn.Open();
                Exec(conn, command);
                using DbDataReader reader = command.ExecuteReader(System.Data.CommandBehavior.SingleResult);
                return reader.SerializeRow();
            }
            else
            {
                Exec(connection!, command);
                using DbDataReader reader = command.ExecuteReader(System.Data.CommandBehavior.SingleResult);
                return reader.SerializeRow();
            }
        }

        public T? Obj<T>() where T : class, new()
        {
            using DbCommand command = NewCommand();
            if (connection.IsNullOrEmpty())
            {
                using DbConnection conn = NewConnection();
                conn.Open();
                Exec(conn, command);
                using DbDataReader reader = command.ExecuteReader(System.Data.CommandBehavior.SingleResult);
                return reader.Obj<T>();
            }
            else
            {
                Exec(connection!, command);
                using DbDataReader reader = command.ExecuteReader(System.Data.CommandBehavior.SingleResult);
                return reader.Obj<T>();
            }
        }

        public IEnumerable<T> Column<T>(string columnName)
        {
            using DbCommand command = NewCommand();
            if (connection.IsNullOrEmpty())
            {
                using DbConnection conn = NewConnection();
                conn.Open();
                Exec(conn, command);
                using DbDataReader reader = command.ExecuteReader();
                return reader.ColumnValues<T>(columnName);
            }
            else
            {
                Exec(connection!, command);
                using DbDataReader reader = command.ExecuteReader();
                return reader.ColumnValues<T>(columnName);
            }
        }

        public IEnumerable<T> Column<T>(int columnNumber = 0)
        {
            using DbCommand command = NewCommand();
            if (connection.IsNullOrEmpty())
            {
                using DbConnection conn = NewConnection();
                conn.Open();
                Exec(conn, command);
                using DbDataReader reader = command.ExecuteReader();
                return reader.ColumnValues<T>(columnNumber);
            }
            else
            {
                Exec(connection!, command);
                using DbDataReader reader = command.ExecuteReader();
                return reader.ColumnValues<T>(columnNumber);
            }
        }

        /// <summary>Value</summary>
        /// <remarks>La consulta debe retornar 1 o mas valores</remarks>
        public T Value<T>(string columnName)
        {
            using DbCommand command = NewCommand();
            if (connection.IsNullOrEmpty())
            {
                using DbConnection conn = NewConnection();
                conn.Open();
                Exec(conn, command);
                using DbDataReader reader = command.ExecuteReader(System.Data.CommandBehavior.SingleResult);
                return reader.Read() ? (T)reader[columnName] : default(T);
            }
            else
            {
                Exec(connection!, command);
                using DbDataReader reader = command.ExecuteReader(System.Data.CommandBehavior.SingleResult);
                return reader.Read() ? (T)reader[columnName] : default(T);
            }
        }



        /// <summary>Value</summary>
        /// <remarks>La consulta debe retornar 1 o mas valores</remarks>
        public T Value<T>(int columnNumber = 0)
        {
            using DbCommand command = NewCommand();
            if (connection.IsNullOrEmpty())
            {
                using DbConnection conn = NewConnection();
                conn.Open();
                Exec(conn, command);
                using DbDataReader reader = command.ExecuteReader(System.Data.CommandBehavior.SingleResult);
                return (reader.Read()) ? (T)reader.GetValue(columnNumber) : default(T);
            }
            else
            {
                Exec(connection!, command);
                using DbDataReader reader = command.ExecuteReader(System.Data.CommandBehavior.SingleResult);
                return (reader.Read()) ? (T)reader.GetValue(columnNumber) : default(T);
            }
        }

        /// <summary>
        /// Verifica conexion, si no existe la crea
        /// </summary>
        public void Exec()
        {
            using var command = NewCommand();
            if (connection.IsNullOrEmpty())
            {
                using var conn = NewConnection();
                conn.Open();
                Exec(conn, command);
                conn.Close();
            }
            else
                Exec(connection!, command);
        }

        public abstract DbConnection NewConnection();
        public abstract DbCommand NewCommand();
        protected abstract void AddWithValue(DbCommand command, string columnName, object value);

        /// <summary>
        /// Ejecutar command con transaction
        /// </summary>
        /// <param name="connection">Conexión abierta</param>
        /// <param name="command">Comando</param>
        protected void Exec(DbConnection connection, DbTransaction transaction, DbCommand command)
        {
            command.Transaction = transaction;
            Exec(connection, command);
        }

        /// <summary>
        /// Ejecutar command
        /// </summary>
        /// <param name="connection">Conexión abierta</param>
        /// <param name="command">Comando</param>
        protected void Exec(DbConnection connection, DbCommand command)
        {
            command.Connection = connection;

            #region Transformar parametersDict to parameters
            if (parametersDict.Keys.Count > 0)
            {
                //debe recorrerse de forma ordenada por longitud, si un campo se llama "persona" y otro "persona_adicional"  y no se recorre ordenado descendiente, el resultado es erroneo.
                var keys = parametersDict.Keys.SortByLength("DESC");

                var j = parameters.Count;

                foreach (string key in keys)
                    while (sql.Contains("@" + key))
                    {
                        sql = sql.Replace("@" + key, "@" + j.ToString());
                        parameters.Add(parametersDict[key]);
                        j++;
                    }
            }
            #endregion

            #region Procesar parameters
            for (var i = parameters.Count - 1; i >= 0; i--) //recorremos la lista al revez para evitar renombrar parametros no deseados con nombre similar
            {
                if (!sql.Contains("@" + i.ToString())) //control de que el sql posea el parametro
                    continue;

                var list = parameters[i] as IList;
                int j = 0;
                List<Tuple<string, object>> _parameters = new();
                if (list != null)
                {
                    foreach (object item in list)
                    {
                        var t = Tuple.Create($"@_{i}_{j}", item); //se le asigna un "_" adicional al nuevo nombre para evitar ser renombrado nuevamente.
                        _parameters.Add(t);
                        j++;
                    }

                    sql = sql.ReplaceFirst("@" + i.ToString(), string.Join(",", _parameters.Select(x => x.Item1)));
                    foreach (var parameter in _parameters)
                        AddWithValue(command, parameter.Item1, parameter.Item2);
                }
                else
                {
                    var p = (parameters[i] == null) ? DBNull.Value : parameters[i];
                    sql = sql.Replace("@" + i.ToString(), "@_" + i.ToString()); //renombro para evitar doble asignacion
                    AddWithValue(command, "_" + i.ToString(), p);
                }
            }
            #endregion  

            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        public abstract List<string> GetTableNames();



     

        #region metodos especiales que generan sql y devuelven directamente el valor
        /// <summary>
        /// Cada motor debe tener su propia forma de definir Next Value!!! Derivar metodo a subclase
        /// </summary>
        /// <returns></returns>
        public ulong GetNextValue(string entityName)
        {
            var q = db.Query();
            q.connection = connection;
            q.sql = @"
                            SELECT auto_increment 
                            FROM INFORMATION_SCHEMA.TABLES 
                            WHERE TABLE_NAME = @0";
            q.parameters.Add(entityName);
            return q.Value<ulong>();
        }

        /// <summary>
        /// Cada motor debe tener su propia forma de definir Max Value!!! Derivar metodo a subclase
        /// </summary>
        /// <returns></returns>
        public long GetMaxValue(string entityName, string fieldName)
        {
            EntitySql sql = db.Sql(entityName).Select("MAX($" + fieldName + ")");
            return db.Query(sql).Value<long>();
        }
        #endregion
    }
}
 
