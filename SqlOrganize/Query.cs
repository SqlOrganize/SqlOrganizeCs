 using System.Collections;
using System.Data;
using System.Data.Common;
using Utils;

namespace SqlOrganize
{
    /// <summary>
    /// Simplificar ejecucion de consultas a la base de datos y definicion de de parametros en las consultas de sql
    /// </summary>    
    public abstract class Query : IDisposable
    {
        private bool disposed = false;

        /// <summary>conexion</summary>
        public DbConnection connection;

        /// <summary>transaccion</summary>
        public DbTransaction? transaction;

        /// <summary>Contenedor principal del proyecto</summary>
        public Db db { get; }

        /// <summary>Parametros de las consultas</summary>
        /// <remarks>Las consulas sql definidas con Query, pueden utilizar parametros que deben ser identificados con un número entero secuencial. <br/>
        /// Cada parametro dentro de la lista de parameters será asociado al número entero definido en el sql</remarks>
        /// <example>
        ///     sql = "SELECT .. WHERE something = @0 AND $something_else = @1<br/> //la sintaxis debe ser compatible con el motor de base de datos
        ///     parameters = [value0, value1] //los valores pueden ser de cualquier tipo, que será reformateado para adaptarlo a las necesidades
        /// </example>
        /// 
        public List<object?> parameters { get; set; } = new List<object?>();

        /// <summary>Consultas en SQL</summary>
        public string sql { get; set; } = "";

        /// <summary>Constructor</summary>
        /// <param name="_db">Contenedor principal del proyecto</param>
        public Query(Db _db)
        {
            db = _db;
        }
    
        ~Query()
        {
            Dispose();
        }

       

        // Implement IDisposable interface
        public void Dispose()
        {
            CloseConnection();
            if (connection != null)
            {
                connection.Dispose();
                connection = null;
            }
            GC.SuppressFinalize(this);
        }

              

        /// <summary>
        /// Ejecutar sql y devolver resultado
        /// </summary>
        /// <returns>Resultado como List -Dictionary -string, object- -</returns>
        /// <remarks>Convert the result to json with "JsonConvert.SerializeObject(data, Formatting.Indented)"</remarks>
        public IEnumerable<Dictionary<string, object?>> ColOfDict()
        {
            using DbCommand command = NewCommand();
            Exec(connection!, command);
            using DbDataReader reader = command.ExecuteReader();
            return reader.Serialize();
            
        }

        public IEnumerable<T> ColOfObj<T>() where T : class, new()
        {
            using DbCommand command = NewCommand();
            Exec(connection!, command);
            using DbDataReader reader = command.ExecuteReader();
            return reader.ColOfObj<T>();
        }

        public Dictionary<string, object?>? Dict()
        {
            using DbCommand command = NewCommand();
            Exec(connection!, command);
            using DbDataReader reader = command.ExecuteReader(System.Data.CommandBehavior.SingleResult);
            return reader.SerializeRow();
        }

        public T? Obj<T>() where T : class, new()
        {
            using DbCommand command = NewCommand();
            Exec(connection!, command);
            using DbDataReader reader = command.ExecuteReader(System.Data.CommandBehavior.SingleResult);
            return reader.Obj<T>();
        }

        public IEnumerable<T> Column<T>(string columnName)
        {
            using DbCommand command = NewCommand();
            Exec(connection!, command);
            using DbDataReader reader = command.ExecuteReader();
            return reader.ColumnValues<T>(columnName);
        }

        public IEnumerable<T> Column<T>(int columnNumber = 0)
        {
            using DbCommand command = NewCommand();
            Exec(connection!, command);
            using DbDataReader reader = command.ExecuteReader();
            return reader.ColumnValues<T>(columnNumber);
        }

        /// <summary>Value</summary>
        /// <remarks>La consulta debe retornar 1 o mas valores</remarks>
        public T? Value<T>(string columnName)
        {
            using DbCommand command = NewCommand();
            Exec(connection!, command);
            using DbDataReader reader = command.ExecuteReader(System.Data.CommandBehavior.SingleResult);
            return reader.Read() ? (T)reader[columnName] : default(T);
        }

        /// <summary>Value</summary>
        /// <remarks>La consulta debe retornar 1 o mas valores</remarks>
        public T? Value<T>(int columnNumber = 0)
        {
            using DbCommand command = NewCommand();
            Exec(connection!, command);
            using DbDataReader reader = command.ExecuteReader(System.Data.CommandBehavior.SingleResult);
            return (reader.Read()) ? (T)reader.GetValue(columnNumber) : default(T);
        }

        /// <summary>
        /// Verifica conexion, si no existe la crea
        /// </summary>
        public void Exec()
        {
            using var command = NewCommand();
            Exec(connection!, command);
        }

        public void ExecTransaction()
        {
            using var command = NewCommand();
            Exec(connection!, transaction!, command);
        }

        public abstract DbConnection NewConnection();

        public DbConnection OpenConnection()
        {
            if (connection == null)
                connection = NewConnection();

            if (connection.State == ConnectionState.Closed)
                connection.Open();

            return connection;
        }

        // Method to close the connection
        public void CloseConnection()
        {
            if (connection != null)
            { 
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }

        public void BeginTransaction()
        {
            transaction = connection!.BeginTransaction();
        }

        public void CommitTransaction()
        {
            transaction!.Commit();
        }

        public void RollbackTransaction()
        {
            transaction!.Rollback();
        }

        public abstract DbCommand NewCommand();

        protected abstract void AddWithValue(DbCommand command, string columnName, object value);

        /// <summary>Ejecutar command con transaction</summary>
        /// <param name="connection">Conexión abierta</param>
        /// <param name="command">Comando</param>
        protected void Exec(DbConnection connection, DbTransaction transaction, DbCommand command)
        {
            command.Transaction = transaction;
            Exec(connection, command);
        }

        /// <summary>Ejecutar command</summary>
        /// <param name="connection">Conexión abierta</param>
        /// <param name="command">Comando</param>
        protected void Exec(DbConnection connection, DbCommand command)
        {
            command.Connection = connection;

            #region Procesar parameters
            for (var i = parameters.Count - 1; i >= 0; i--) //recorremos la lista al revez para evitar renombrar parametros no deseados con nombre similar
            {
                if (!sql.Contains("@" + i.ToString())) //control de que el sql posea el parametro
                    continue;

                int j = 0;
                List<Tuple<string, object>> _parameters = new();
                if (parameters[i] is IEnumerable<object>)
                {
                    foreach (object item in parameters[i] as IEnumerable<object>)
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
                    AddWithValue(command, "@_" + i.ToString(), p);
                }
            }
            #endregion  

            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        public abstract string[] GetTableNames();
     

        #region metodos especiales que generan sql y devuelven directamente el valor
        /// <summary>
        /// Cada motor debe tener su propia forma de definir Next Value!!! Derivar metodo a subclase
        /// </summary>
        /// <returns></returns>
        public object GetNextValue(string entityName)
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
        public object GetMaxValue(string entityName, string fieldName)
        {
            return db.Sql(entityName).SelectMaxValue(fieldName).Value<object>();
        }
        #endregion
    }
}
 
