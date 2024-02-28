using System;
using System.Collections;
using System.Data.Common;
using Utils;

namespace SqlOrganize
{
    /// <summary>
    /// Unificar metodos para ejecutar consultas a la base de datos
    /// </summary>    
    public abstract class Query
    {
        /// <summary>
        /// conexion opcional, si no existe al ejecutar se crea
        /// </summary>       
        public DbConnection? connection;


        /// Contenedor principal del proyecto
        /// </summary>
        public Db db { get; }

        /// <summary>
        /// Parametros de las consultas
        /// </summary>
        public List<object?> parameters { get; set; }  = new List<object?>();

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
        /// Ejecutar sql y devolver resultado
        /// </summary>
        /// <returns>Resultado como List -Dictionary -string, object- -</returns>
        /// <remarks>Convert the result to json with "JsonConvert.SerializeObject(data, Formatting.Indented)"</remarks>
        public abstract IEnumerable<Dictionary<string, object?>> ColOfDict();

        public abstract IEnumerable<T> ColOfObj<T>() where T : class, new();

        public abstract IDictionary<string, object?>? Dict();

        public abstract T Obj<T>() where T : class, new();

        public abstract IEnumerable<T> Column<T>(string columnName);

        public abstract IEnumerable<T> Column<T>(int columnValue = 0);

        public abstract T Value<T>(string columnName);

        public abstract T Value<T>(int columnValue = 0);
        
        /// <summary>
        /// Verifica conexion, si no existe la crea
        /// </summary>
        public abstract void Exec();

        protected abstract void AddWithValue(DbCommand command, string columnName, object value);


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
            for (var i = parameters.Count-1; i >= 0; i--) //recorremos la lista al revez para evitar renombrar parametros no deseados con nombre similar
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
                    AddWithValue(command, "_"+i.ToString(), p);
                }
            }
            #endregion

            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
    }
}
 
