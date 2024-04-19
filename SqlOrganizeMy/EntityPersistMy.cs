using MySql.Data.MySqlClient;
using Org.BouncyCastle.Tls;
using SqlOrganize;
using System.Data.Common;
using Utils;

namespace SqlOrganizeMy
{
    public class EntityPersistMy : EntityPersist
    {
        public EntityPersistMy(Db db) : base(db)
        {
        }

        protected override EntityPersist _Update(string _entityName, IDictionary<string, object?> row)
        {
            string sna = Db.Entity(_entityName!).schemaNameAlias;
            sql += @"
UPDATE " + sna + @" SET
";
            List<string> fieldNames = Db.FieldNamesAdmin(_entityName!);

            foreach (string fieldName in fieldNames)
                if (row.ContainsKey(fieldName))
                {
                    sql += fieldName + " = @" + count + ", ";
                    count++;
                    parameters.Add(row[fieldName]);
                }
            sql = sql.RemoveLastChar(',');
            return this;
        }


        public override EntityPersist Transaction()
        {
            using MySqlCommand command = new();

            if (connection.IsNullOrEmpty())
            {
                connection = new MySqlConnection(Db.config.connectionString);
                connection.Open();
                _Transaction();
                connection.Close();
            }
            else
                _Transaction();

            return this;
        }

        public override EntityPersist TransactionSplit()
        {
            if (connection.IsNullOrEmpty())
            {
                connection = new MySqlConnection(Db.config.connectionString);
                connection.Open();
                _TransactionSplit();
                connection.Close();
                connection = null;
            }
            else
                _TransactionSplit();

            return this;
        }

    }

}
