namespace SqlOrganize.Sql
{

    /// <summary>
    /// Configuración
    /// </summary>
    public class Config
    {
        /// <summary>
        /// String de conexión con la base de datos
        /// </summary>
        public virtual string connectionString { get; set; }
        
        /// <summary>
        /// String de concatenacion
        /// </summary>
        /// <remarks>
        /// Se utiliza para ciertas operaciones como por ejemplo la concatenación de campos
        /// </remarks>
        public virtual string concatString { get; set; } = "~";
        
        /// <summary>
        /// fk asociada a id
        /// </summary>
        /// <remarks>
        /// Para algunas base de datos las fk están directamente asociadas a las Id.<br/>
        /// Se debe indicar para que las operaciones sean mas eficientes
        /// </remarks>
        public virtual bool fkId { get; set; } = true;

        /// <summary>
        /// Nombre del identificador único de las tablas       
        /// </summary>
        /// <remarks>
        /// Todas las tablas deben tener un identificador, que puede ser real o ficticio<br/>
        /// El identificador ficticio se define como "_Id"
        /// </remarks>
        public virtual string id { get; set; } = "_Id";

        /// <summary>
        /// Nombre de la base de datos
        /// </summary>       
        public virtual string dbName { get; set; }
    }
}
