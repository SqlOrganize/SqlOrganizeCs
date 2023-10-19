namespace ModelOrganize
{
    public class Config
    {
        public string connectionString { get; set; }
        public string dbName { get; set; }

        /// <summary>
        /// Indica el lugar donde se generaran los archivos json para documentar el modelo
        /// </summary>
        public string docPath { get; set; } = "./Doc/";

        /// <summary>
        /// Alias reservados que no deben ser incluidos en el modelo
        /// </summary>
        public List<string> reservedAlias { get; set; } = new List<string>();

        /// <summary>
        /// Entidades reservadas que no seran incluidas en el modelo
        /// </summary>
        public List<string> reservedEntities { get; set; } = new List<string>();


        /// <summary>
        /// Indica el lugar donde se tomara la configuracion para generar el modelo
        /// </summary>
        public string configPath { get; set; } = "./Config";

        /// <summary>
        /// Indica el lugar donde se almacenaran las clases de datos
        /// </summary>
        public string dataClassesPath { get; set; } = "./App/Data";

        /// <summary>
        /// Indica el namespace utilizado para las clases de datos
        /// </summary>
        public string dataClassesNamespace { get; set; } = "App.Data";

        /// <summary>
        /// Indica el lugar donde se almacenara la clase que describe el esquema de la base de datos
        /// </summary>
        public string schemaClassPath { get; set; } = "./";

        /// Indica el namespace asignado a la clase Schema
        public string schemaClassNamespace { get; set; } = "App";

        /// <summary>
        /// Referencia para definir los alias e identificadores de fields 
        /// </summary>
        public string idSource { get; set; } = "entity_name"; //field_name or entity_name
    }
}
