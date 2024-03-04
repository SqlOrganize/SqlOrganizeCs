using SqlOrganize;
using Utils;
using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace SqlOrganizeSs
{
    public class EntityPersistSs : EntityPersist
    {

        public EntityPersistSs(Db db) : base(db)
        {
        }

        protected override EntityPersist _Update(string _entityName, IDictionary<string, object?> row)
        {
            Entity e = Db.Entity(_entityName);
            sql += @"
UPDATE " + e.alias + @" SET
";
            List<string> fieldNames = Db.FieldNamesAdmin(_entityName);

            foreach (string fieldName in fieldNames)
                if (row.ContainsKey(fieldName))
                {
                    sql += fieldName + " = @" + count + ", ";
                    count++;
                    parameters.Add(row[fieldName]);
                }
            sql = sql.RemoveLastChar(',');
            sql += " FROM " + e.schemaNameAlias + @"
";
            return this;
        }

        public override EntityPersist Transaction()
        {
            using SqlCommand command = new();

            if (connection.IsNullOrEmpty())
            {
                connection = new SqlConnection(Db.config.connectionString);
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
                connection = new SqlConnection(Db.config.connectionString);
                connection.Open();
                _TransactionSplit();
                connection.Close();
            }
            else
                _TransactionSplit();

            return this;
        }

    }

}
