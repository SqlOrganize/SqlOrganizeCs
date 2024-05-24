using Microsoft.Data.SqlClient;
using SqlOrganize;
using System.Data;
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

        public override DbCommand NewCommand()
        {
            return new SqlCommand();
        }

        public override DbConnection NewConnection()
        {
            connection = new SqlConnection(db.config.connectionString);
            return connection;
        }


        protected override void AddWithValue(DbCommand command, string columnName, object value)
        {
            (command as SqlCommand)!.Parameters.AddWithValue(columnName, value);
        }

        public override List<string> GetTableNames()
        {
            using DbConnection connection = OpenConnection();
            using DbCommand command = (SqlCommand)NewCommand();
            command.CommandText = @"
                SELECT TABLE_NAME
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_CATALOG=@dbName
				ORDER BY TABLE_NAME ASC;";
            command.Connection = connection;
            ((SqlCommand)command).Parameters.AddWithValue("dbName", db.config.dbName);
            command.ExecuteNonQuery();
            using DbDataReader reader = command.ExecuteReader();
            return SqlUtils.ColumnValues<string>(reader, "TABLE_NAME");
        }
    }

}
