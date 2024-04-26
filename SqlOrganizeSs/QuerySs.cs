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

	public QuerySs(Db db, EntitySql sql) : base(db, sql)
        {
        }

        public QuerySs(Db db, EntityPersist persist) : base(db, persist)
        {
        }

        public override DbCommand NewCommand()
        {
            return new SqlCommand();
        }

        public override DbConnection OpenConnection()
        {
            connection = new SqlConnection(db.config.connectionString);
            connection.Open();
            return connection;
        }

        protected override void AddWithValue(DbCommand command, string columnName, object value)
        {
            (command as SqlCommand)!.Parameters.AddWithValue(columnName, value);
        }

        public override List<string> GetTableNames()
        {
            using DbConnection connection = OpenConnection();
            using DbCommand command = NewCommand();
            command.CommandText = @"
                SELECT TABLE_NAME
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_CATALOG=@dbName
				ORDER BY TABLE_NAME ASC;";
            command.Connection = connection;
            command.Parameters.AddWithValue("dbName", db.config.dbName);
            command.ExecuteNonQuery();
            using DbDataReader reader = command.ExecuteReader();
            return SqlUtils.ColumnValues<string>(reader, "TABLE_NAME");
        }
    }

}
