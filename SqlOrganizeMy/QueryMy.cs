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

        public QueryMy(Db db, EntitySql sql) : base(db, sql)
        {
        }

        public QueryMy(Db db, EntityPersist persist) : base(db, persist)
        {
        }

        public override DbCommand NewCommand()
        {
            return new MySqlCommand();
        }

        public override DbConnection OpenConnection()
        {
            connection = new MySqlConnection(db.config.connectionString);
            connection.Open();
            return connection;
        }

        protected override void AddWithValue(DbCommand command, string columnName, object value)
        {
            (command as MySqlCommand)!.Parameters.AddWithValue(columnName, value);
        }

        public override List<string> GetTableNames()
        {
            using DbConnection connection = OpenConnection();
            using DbCommand command = NewCommand();
            command.CommandText = @"SHOW TABLES FROM " + db.config.dbName;
            command.Connection = connection;
            command.ExecuteNonQuery();
            using DbDataReader reader = command.ExecuteReader();
            return SqlUtils.ColumnValues<string>(reader, 0);
        }
    }

}
