using MySql.Data.MySqlClient;
using SqlOrganize;
using System.Data.Common;
using Utils;

namespace SqlOrganizeMy
{
    /// <summary>
    /// Ejecucion de consultas a la base de datos
    /// </summary>
    public class QueryMy : Query
    {

        public QueryMy(Db db) : base(db)
        {
        }


        public override IEnumerable<Dictionary<string, object?>> ColOfDict()
        {
            using MySqlCommand command = new ();
            if (connection.IsNullOrEmpty())
            {
                using MySqlConnection conn = new (db.config.connectionString);
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

        public override IEnumerable<T> ColOfObj<T>()
        {
            using MySqlCommand command = new ();
            if (connection.IsNullOrEmpty())
            {
                using MySqlConnection conn = new (db.config.connectionString);
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

        public override Dictionary<string, object?>? Dict()
        {
            using MySqlCommand command = new ();
            if (connection.IsNullOrEmpty())
            {
                using MySqlConnection conn = new (db.config.connectionString);
                conn.Open();
                Exec(conn, command);
                using DbDataReader reader = command.ExecuteReader(System.Data.CommandBehavior.SingleResult);
                return reader.SerializeRow();
            }
            else
            {
                Exec(connection!, command);
                using MySqlDataReader reader = command.ExecuteReader(System.Data.CommandBehavior.SingleResult);
                return reader.SerializeRow();
            }
        }

        public override T? Obj<T>() where T : class
        {
            using MySqlCommand command = new MySqlCommand();
            if (connection.IsNullOrEmpty())
            {
                using MySqlConnection conn = new MySqlConnection(db.config.connectionString);
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

        public override IEnumerable<T> Column<T>(string columnName)
        {
            using MySqlCommand command = new MySqlCommand();
            if (connection.IsNullOrEmpty())
            {
                using MySqlConnection conn = new MySqlConnection(db.config.connectionString);
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

        public override IEnumerable<T> Column<T>(int columnNumber = 0)
        {
            using MySqlCommand command = new ();
            if (connection.IsNullOrEmpty())
            {
                using MySqlConnection conn = new (db.config.connectionString);
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

        public override T Value<T>(string columnName)
        {
            using MySqlCommand command = new ();
            if (connection.IsNullOrEmpty())
            {
                using MySqlConnection conn = new ((string)db.config.connectionString);
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

        public override T Value<T>(int columnNumber = 0)
        {
            using MySqlCommand command = new ();
            if (connection.IsNullOrEmpty())
            {
                using MySqlConnection conn = new (db.config.connectionString);
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


        public override void Exec()
        {
            using MySqlCommand command = new ();
            if (connection.IsNullOrEmpty())
            {
                using MySqlConnection conn = new (db.config.connectionString);
                conn.Open();
                Exec(conn, command);
                conn.Close();
            } else
                Exec(connection!, command);
        }
       

        protected override void AddWithValue(DbCommand command, string columnName, object value)
        {
            (command as MySqlCommand)!.Parameters.AddWithValue(columnName, value);
        }
    }

}
