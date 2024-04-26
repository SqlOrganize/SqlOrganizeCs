using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelOrganize
{
    public class Field
    {
        public string name { get; set; }

        public string? alias { get; set; }

        /* 
        nombre de la entidad 
        */
        public string entityName { get; set; }

        /* 
        si es clave foranea: Nombre de la entidad referenciada por la clave foranea 
        */
        public string? refEntityName { get; set; }

        /* 
        si es clave foranea: Nombre del field al que hace referencia de la entidad referenciada
        */
        public string? refFieldName { get; set; }

      
        /// <summary>
        /// Tipo de datos del motor
        /// </summary>
        public string dataType { get; set; }

        /// <summary>
        /// tipo de datos del lenguaje
        /// </summary>
        /// <remarks></remarks>
        public string type { get; set; }

        /* 
        string con el tipo de field
            "pk": Clave primaria
            "nf": Field normal
            "mo": Clave foranea muchos a uno
            "oo": Clave foranea uno a uno
        */

        /// <summary>
        /// es not null?
        /// </summary>
        public bool notNull { get; set; }

        /// <summary> Valor por defecto </summary>
        /// <remarks> Valores especiales: <br/>
        /// _Guid: GUID para tipos string o Guid <br/>
        /// _Random: Random string o Random int dependiendo del tipo <br/>
        /// _Current_year: Año actual para tipo short <br/>
        /// _Current_semester: Semestre actual para tipo short <br/>
        /// _New: nuevo valor para tipo Guid <br/>
        /// _Max: Valor maximo <br/>
        /// _Next: Siguiente valor<br/>
        /// </remarks>
        public object? defaultValue { get; set; } = null;


        /// <summary>
        /// Generar valor por defecto en la clase de datos, si existe
        /// </summary>
        /// <remarks>Para ciertos atributos de control puede requerirse que no se inicien en la clase de datos, un ejemplo es el atributo de alta que si no esta definido se puede utilizar para saber si la entidad es nueva y aun no ha sido persistida.</remarks>
        public bool defaultValueClassData { get; set; } = true;


        /// <summary>
        /// longitud maxima
        /// </summary>
        public ulong? maxLength { get; set; } = null;


        /// <summary>
        /// Lista de chequeos
        /// </summary>
        /// <example>
        /// [
        ///     field_name:true, //metodo exclusivo definido por el usuario
        ///     Type:"string",
        ///     Required:true,
        /// ]
        /// </example>
        public Dictionary<string, object> checks = new();

        /// <summary>
        /// Lista de reasignaciones
        /// </summary>
        /// <example>
        /// [
        ///     field_name:true, //metodo exclusivo definido por el usuario
        ///     Trim:" ",
        ///     Ltrim:" ", //no implementado
        ///     Rtrim:" ", //no implementado
        ///     RemoveMultipleSpaces:object?, 
        ///     NullIfEmpty:true, //si es vacio se asigna en null
        ///     DefaultIfNull:true, //si es null se asigna valor por defecto
        ///     SetDefault:true, //siempre setea valor por defecto, por mas que el valor ya exista
        /// ]
        /// </example>
        public Dictionary<string, object?> resets = new();

    }
}
