using Microsoft.Data.SqlClient;
using SqlOrganize;
using System.Data.Common;
using Utils;

namespace SqlOrganizeSs
{
    /// <summary>
    /// Ejecucion de consultas a la base de datos
    /// </summary>
    public class QuerySs : Query
    {
        public QuerySs(Db db) : base(db)
        {
        }

        public override IEnumerable<Dictionary<string, object?>> ColOfDict()
        {
            using SqlCommand command = new();
            if (connection.IsNullOrEmpty())
            {
                using SqlConnection conn = new(db.config.connectionString);
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
            using SqlCommand command = new();
            if (connection.IsNullOrEmpty())
            {
                using SqlConnection conn = new(db.config.connectionString);
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
            using SqlCommand command = new();
            if (connection.IsNullOrEmpty())
            {
                using SqlConnection conn = new(db.config.connectionString);
                conn.Open();
                Exec(conn, command);
                using DbDataReader reader = command.ExecuteReader(System.Data.CommandBehavior.SingleResult);
                return reader.SerializeRow();
            }
            else
            {
                Exec(connection!, command);
                using SqlDataReader reader = command.ExecuteReader(System.Data.CommandBehavior.SingleResult);
                return reader.SerializeRow();
            }
        }

        public override T? Obj<T>() where T : class
        {
            using SqlCommand command = new SqlCommand();
            if (connection.IsNullOrEmpty())
            {
                using SqlConnection conn = new SqlConnection(db.config.connectionString);
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
            using SqlCommand command = new SqlCommand();
            if (connection.IsNullOrEmpty())
            {
                using SqlConnection conn = new SqlConnection(db.config.connectionString);
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
            using SqlCommand command = new();
            if (connection.IsNullOrEmpty())
            {
                using SqlConnection conn = new(db.config.connectionString);
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
            using SqlCommand command = new();
            if (connection.IsNullOrEmpty())
            {
                using SqlConnection conn = new((string)db.config.connectionString);
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
            using SqlCommand command = new();
            if (connection.IsNullOrEmpty())
            {
                using SqlConnection conn = new(db.config.connectionString);
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
            using SqlCommand command = new();
            if (connection.IsNullOrEmpty())
            {
                using SqlConnection conn = new(db.config.connectionString);
                conn.Open();
                Exec(conn, command);
                conn.Close();
            }
            else
                if (!transaction.IsNullOrEmpty())
                    Exec(connection!, transaction!, command);
                else
                    Exec(connection!, command);
        }

        protected override void AddWithValue(DbCommand command, string columnName, object value)
        {
            (command as SqlCommand)!.Parameters.AddWithValue(columnName, value);
        }


        public override List<string> GetTableNames()
        {
            using SqlConnection connection = new SqlConnection(db.config.connectionString);
            connection.Open();
            using SqlCommand command = new SqlCommand();
            command.CommandText = @"
                SELECT TABLE_NAME
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_CATALOG=@dbName
				ORDER BY TABLE_NAME ASC;";
            command.Connection = connection;
            command.Parameters.AddWithValue("dbName", db.config.dbName);
            command.ExecuteNonQuery();
            using SqlDataReader reader = command.ExecuteReader();
            return SqlUtils.ColumnValues<string>(reader, "TABLE_NAME");
        }
    }

}
