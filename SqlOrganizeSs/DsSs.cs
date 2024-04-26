using Microsoft.Extensions.Caching.Memory;
﻿using Microsoft.Data.SqlClient;
using SqlOrganize;

namespace SqlOrganizeSs
{
    /// <summary>
    /// Contenedor principal para sql server
    /// </summary>
    /// <remarks>
    /// Sql Server agrega espacios en blanco adicionales cuando se utiliza 
    /// CONCAT y CONCAT_WS.<br/>
    /// </remarks>
    public class DbSs : Db
    {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="config">
        /// Configuracion
        /// </param>
        /// <example>
        ///   connectionString = "server=127.0.0.1;uid=root;pwd=12345;database=test"
        /// </example>
        public DbSs(Config config, Schema schema, IMemoryCache? cache = null) : base(config, schema, cache)
        {

            if (config.dbName.IsNullOrEmpty())
            {
                string connectionString = config.connectionString;

                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);

                config.dbName = builder.InitialCatalog;
            }
        }

        public override EntityPersist Persist()
        {
            return new EntityPersistSs(this);
        }

        public override EntitySql Sql(string entity_name)
        {
            return new EntitySqlSs(this, entity_name);
        }

        public override Query Query()
        {
            return new QuerySs(this);
        }

        public override Query Query(EntitySql sql)
        {
            return new QuerySs(this, sql);
        }

        public override Query Query(EntityPersist persist)
        {
            return new QuerySs(this, persist);
        }

        public override long GetMaxValue(string entityName, string fieldName)
        {
            return Sql(entityName).Select("CAST ( ISNULL( MAX($" + fieldName + "), 0) AS bigint)").Value<long>();
        }
    }
}
