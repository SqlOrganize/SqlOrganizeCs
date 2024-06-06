namespace ModelOrganize
{
    public class Config
    {
        public virtual string connectionString { get; set; }
        public virtual string dbName { get; set; }

        /// <summary>
        /// Indica el lugar donde se generaran los archivos json para documentar el modelo
        /// </summary>
        public virtual string docPath { get; set; } = "./Doc/";

        /// <summary>Alias reservados que no deben ser incluidos en el modelo</summary>
        public virtual List<string> reservedAlias { get; set; } = new List<string>();

        /// <summary>Entidades reservadas que no seran incluidas en el modelo</summary>
        /// <remarks>Asignamos nombre habitual de tabla de auditorias</remarks>

        public virtual List<string> reservedEntities { get; set; } = new List<string>() { "AuditLog" };


        /// <summary>
        /// Indica el lugar donde se tomara la configuracion para generar el modelo
        /// </summary>
        public virtual string configPath { get; set; } = "./Config";

        /// <summary>
        /// Indica el lugar donde se almacenaran las clases de datos
        /// </summary>
        public virtual string dataClassesPath { get; set; } = "./App/Data";

        /// <summary>
        /// Indica el namespace utilizado para las clases de datos
        /// </summary>
        public virtual string dataClassesNamespace { get; set; } = "App.Data";

        /// <summary>
        /// Indica el lugar donde se almacenara la clase que describe el esquema de la base de datos
        /// </summary>
        public virtual string schemaClassPath { get; set; } = "./";

        /// Indica el namespace asignado a la clase Schema
        public virtual string schemaClassNamespace { get; set; } = "App";

        /// <summary>
        /// Referencia para definir los alias e identificadores de fields 
        /// </summary>
        public virtual string idSource { get; set; } = "entity_name"; //field_name or entity_name

        /// <summary>
        /// Nombre del identificador único de las tablas       
        /// </summary>
        /// <remarks>
        /// Todas las tablas deben tener un identificador, que puede ser real o ficticio<br/>
        /// El identificador ficticio se define como "_Id"
        /// </remarks>
        public virtual string id { get; set; } = "_Id";

    }
}
