﻿using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Protocols;
using Newtonsoft.Json.Linq;
using SqlOrganize;
using Utils;

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
        public DbSs(Config config, Schema schema, MemoryCache? cache = null) : base(config, schema, cache)
        {

            if (config.dbName.IsNullOrEmpty())
            {
                string connectionString = config.connectionString;

                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);

                config.dbName = builder.InitialCatalog;
            }
        }

        public override EntityPersist Persist(string entityName)
        {
            return new EntityPersistSs(this, entityName);
        }

        public override EntityQuery Query(string entity_name)
        {
            return new EntityQuerySs(this, entity_name);
        }

        public override Query Query()
        {
            return new QuerySs(this);
        }

    }
}
