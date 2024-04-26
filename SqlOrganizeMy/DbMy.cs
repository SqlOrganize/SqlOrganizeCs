using Microsoft.Extensions.Caching.Memory;
using MySql.Data.MySqlClient;
using SqlOrganize;

namespace SqlOrganizeMy
{
    /// <summary>
    /// Contenedor principal para mysql
    /// </summary>
    public class DbMy : Db
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
        public DbMy(Config config, Schema schema, IMemoryCache? cache = null) : base(config, schema, cache)
        {
            /*
            prueba de conexion
            Las conexiones se realizan directamente cuando se requiere la eje-
            cucion de una consulta a la base de datos.
            Este codigo esta como referencia, se deja como ejemplo por si es
            necesario verificar la conexion                       
            var conn = new MySqlConnection();
            conn.ConnectionString = (string)config.connectionString;
            conn.Open();
            conn.Close();
            */
        }

        public override EntityPersist Persist()
        {
            return new EntityPersistMy(this);
        }

        public override EntitySql Sql(string entity_name)
        {
            return new EntitySqlMy(this, entity_name);
        }

        public override Query Query()
        {
            return new QueryMy(this);
        }

        public override Query Query(EntitySql sql)
        {
            return new QueryMy(this, sql);
        }

        public override Query Query(EntityPersist persist)
        {
            return new QueryMy(this, persist);
        }

        public override long GetMaxValue(string entityName, string fieldName)
        {
            return Sql(entityName).Select("CAST ( ISNULL( MAX($" + fieldName + "), 0) AS bigint)").Value<long>();
        }
    }
}
