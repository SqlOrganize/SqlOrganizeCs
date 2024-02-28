
using System.Reflection;
using System.Text.RegularExpressions;
using Utils;

namespace SqlOrganize
{
    /*
    Values of entity

    Define metodos basicos para administrar valores:
    
    -sset: Seteo con cast y formateo
    -set: Seteo directo
    -check: Validar valor
    -default: Asignar valor por defecto
    -get: Retorno directo
    -json: Transformar a json
    -sql: Transformar a sql
    */
    public class EntityValues : EntityFieldId
    {
        protected Logging logging = new Logging();

        protected IDictionary<string, object?> values = new Dictionary<string, object?>();

        public EntityValues(Db _db, string _entityName, string? _fieldId = null) : base(_db, _entityName, _fieldId)
        {
        }

        public Logging Logging { get { return logging; } }

        public IDictionary<string, object?> Values()
        {
            return values;
        }

        public EntityValues Values(IDictionary<string, object?> row)
        {
            values = row;
            return this;
        }

        public EntityValues ValuesObj(object obj)
        {
            values = obj.Dict() ?? new Dictionary<string, object?>();
            return this;
        }
     
        /// <summary>
        /// Existe valor de field
        /// </summary>
        public bool ContainsValue(string fieldId)
        {
            return values.ContainsKey(fieldId);
        }
        
        public EntityValues Clear()
        {
            values.Clear();
            return this;
        }

        public EntityValues SetObj(object o)
        {
            var d = o.Dict();
            return Set(d);
        }

        public EntityValues Sset(IDictionary<string, object?> row)
        {
            foreach (var fieldName in db.FieldNames(entityName))
                if (row.ContainsKey(Pf() + fieldName))
                    Sset(fieldName, row[Pf() + fieldName]);

            return this;
        }


        public EntityValues Set(IDictionary<string, object?> row)
        {
            foreach (var fieldName in db.FieldNames(entityName))
                if (row.ContainsKey(Pf() + fieldName))
                    Set(fieldName, row[Pf() + fieldName]);

            return this;
        }

        public EntityValues Set(string fieldName, object? value)
        {
            string fn = fieldName;
            if (!Pf().IsNullOrEmpty() && fieldName.Contains(Pf()))
                fn = fieldName.Replace(Pf(), "");
            values[fn] = value;
            return this;
        }

        public EntityValues Remove(string fieldName)
        {
            values.Remove(fieldName);
            return this;
        }

        public object Get(string fieldName)
        {
            return values[fieldName]!;
        }

        public object? GetOrNull(string fieldName)
        {
            return (values.ContainsKey(fieldName) && !values[fieldName]!.IsNullOrEmptyOrDbNull()) ?
                 values[fieldName] : null;

        }



        public IDictionary<string, object?> Get()
        {
            Dictionary<string, object?> response = new();
            foreach (var fieldName in db.FieldNames(entityName))
                if (values.ContainsKey(fieldName))
                    response[Pf() + fieldName] = values[fieldName]!;

            return response;
        }

        /// <summary>
        /// Retornar formato SQL
        /// </summary>
        /// <param name="fieldName">n</param>
        /// <returns>Formato SQL para el fieldName</returns>
        /// <remarks>La conversion de formato es realizada directamente por la libreria SQL, pero para ciertos casos puede ser necesario <br/></remarks>
        public object Sql(string fieldName)
        {
            if (!values.ContainsKey(fieldName))
                throw new Exception("Se esta intentando obtener valor de un campo no definido");

            var value = values[fieldName];

            if (value == null)
                return "null";

            Field field = db.Field(entityName, fieldName);

            switch (field.dataType) //solo funciona para tipos especificos, para mapear correctamente deberia almacenarse en field, el tipo original sql.
            {
                case "varchar":
                    return "'" + (string)value + "'";

                case "datetime": //puede que no funcione correctamente, es necesario almacenar el tipo original sql
                    return "'" + ((DateTime)value).ToString("u");

                default:
                    return value;

            }

        }


        /// <summary>
        /// Seteo "lento", con verificacion y convercion de tipo de datos.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <remarks>Este metodo se encuentra en construccion!!! A medida que se van procesando los datos se agregan</remarks>
        public EntityValues Sset(string fieldName, object? value)
        {
            var method = "Sset_" + fieldName;
            Type thisType = this.GetType();
            MethodInfo m = thisType.GetMethod(method);
            if (!m.IsNullOrEmpty())
                m!.Invoke(this, new object[] { value });

            Field field = db.Field(entityName, fieldName);
            if (value == null)
            {
                values[fieldName] = null;
                return this;
            }

            switch (field.type)
            {
                case "string":
                    values[fieldName] = (string)value;
                    break;

                case "decimal":
                    if (value is decimal)
                        values[fieldName] = (Decimal)value;
                    else
                    {
                        var v = value.ToString()!;
                        values[fieldName] =  (v == "") ? null : Decimal.Parse(v);
                    }

                    break;

                case "int":
                case "Int32":
                    if (value is Int32)
                        values[fieldName] = (Int32)value;
                    else
                    {
                        var v = value.ToString()!;
                        values[fieldName] = (v == "") ? null : Int32.Parse(v);
                    }
                    break;

                case "bool":
                    if (value is bool)
                        values[fieldName] = (bool)value;
                    else
                        values[fieldName] = (value as string)!.ToBool();
                    break;

                case "DateTime":
                    if (value is DateTime)
                        values[fieldName] = (DateTime)value;
                    else
                        values[fieldName] = DateTime.Parse(value.ToString()!);
                    break;
            }

            return this;
        }

        /// <summary>
        /// Resetear valores definidos
        /// </summary>
        /// <returns></returns>
        public EntityValues Reset()
        {
            List<string> fieldNames = new List<string>(db.FieldNames(entityName));
            fieldNames.Remove(db.config.id); //id debe dejarse para el final porque puede depender de otros valores

            foreach (var fieldName in fieldNames)
                if (values.ContainsKey(fieldName))
                    Reset(fieldName);

            if (values.ContainsKey(db.config.id))
                Reset(db.config.id);

            return this;
        }

        /// <summary>
        /// Reasigna fieldName
        /// </summary>
        /// <param name="fieldName"></param>
        /// <remarks>fieldName debe estar definido obligatoriamente</remarks>
        /// <returns></returns>
        public EntityValues Reset(string fieldName)
        {
            var method = "Reset_" + fieldName;
            Type thisType = this.GetType();
            MethodInfo m = thisType.GetMethod(method);
            if (!m.IsNullOrEmpty())
            {
                m!.Invoke(this, new object[] { });
                return this;
            }
            Field field = db.Field(entityName, fieldName);

            foreach (var (resetKey, resetValue) in field.resets)
            {
                switch (resetKey)
                {
                    case "trim":
                        if (!values[fieldName].IsNullOrEmpty() && !values[fieldName].IsDbNull())
                            values[fieldName] = ((string)values[fieldName]).Trim(((string)resetValue).ToChar());
                        break;
                    case "removeMultipleSpaces":
                        if (!values[fieldName].IsNullOrEmpty() && !values[fieldName].IsDbNull())
                            values[fieldName] = Regex.Replace((string)values[fieldName], @"\s+", " ");
                        break;
                    case "nullIfEmpty":
                        if (values[fieldName].IsNullOrEmpty())
                            values[fieldName] = null;
                        break;
                }
            }

            return this;
        }

        /// <summary>
        /// Asignar valor por defecto para aquellos valores no definidos
        /// </summary>
        /// <returns></returns>
        public EntityValues Default()
        {
            foreach (var fieldName in db.FieldNames(entityName))
                if (!values.ContainsKey(fieldName))
                    Default(fieldName);

            return this;
        }

        /// <summary>
        /// Fuerza la asignacion de valor por defecto
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public EntityValues SetDefault(string fieldName)
        {
            if (values.ContainsKey(fieldName))
                Remove(fieldName);
            return Default(fieldName);
        }

        /// <summary>
        /// Reset _Id
        /// </summary>
        /// <remarks>_Id depende de otros valores de la misma entidad, se reasigna luego de definir el resto de los valores</remarks>
        /// <example>db.Values("entityName").Set(source).Set("_Id", null).Reset("_Id"); //inicializa y reasigna _Id individualmente //<br/>
        /// db.Values("entityName").Set(source).Default().Reset() //inicializa y reasigna _Id conjuntamente</example>
        /// <returns></returns>
        public EntityValues Reset__Id()
        {
            List<string> fieldsId = db.Entity(entityName).id;
            foreach (string fieldName in fieldsId)
                if (!values.ContainsKey(fieldName) || values[fieldName].IsNullOrEmpty())
                    return this; //no se reasigna si no esta definido o si es distinto de null

            if (fieldsId.Count == 1)
            {
                values["_Id"] = values[fieldsId[0]].ToString();
                return this;
            }

            List<string> valuesId = new();
            foreach (string fieldName in fieldsId)
                valuesId.Add(values[fieldName].ToString()!);

            values["_Id"] = String.Join(db.config.concatString, valuesId);
            return this;
        }




        /// <summary>
        /// Definir valor por defecto
        /// </summary>
        /// <param name="fieldName">Nombre del field al cual se va a definir valor por defecto</param>
        /// <remarks>Solo se define valor por defecto si el field no se encuentra en atributo values</remarks>
        /// <returns>Mismo objeto</returns>
        public EntityValues Default(string fieldName)
        {
            if (values.ContainsKey(fieldName))
                return this;

            var method = "Default_" + fieldName;
            Type thisType = this.GetType();
            MethodInfo m = thisType.GetMethod(method);
            if (!m.IsNullOrEmpty()) {
                m!.Invoke(this, new object[] { });
                return this;
            }

            values[fieldName] = DefaultField(fieldName);
            return this;
        }

        /// <summary>
        /// Verificar campos
        /// </summary>
        /// <returns>true si la verificacion es correcta, false caso contrario</returns>
        /// <remarks>Para obtener los errores utilizar logging.ToString()</remarks>
        public bool Check()
        {
            logging.Clear();
            foreach (var fieldName in db.FieldNames(entityName))
                if (values.ContainsKey(fieldName))
                    Check(fieldName);

            return !logging.HasErrors();
        }

        /// <summary>
        /// Validar valor del field
        /// </summary>
        /// <param name="fieldName">Nombre del field a validar</param>
        /// <returns>Resultado de la validacion</returns>
        public bool Check(string fieldName)
        {
            logging.ClearByKey(fieldName);
            var method = "Check_" + fieldName;
            Type thisType = this.GetType();
            MethodInfo? m = thisType.GetMethod(method);
            if (!m.IsNullOrEmpty())
                return (bool)m!.Invoke(this, null);

            Field field = db.Field(entityName, fieldName);
            Validation v = new(Get(fieldName));
            v.Clear();
            foreach (var (checkMethod, param) in field.checks)
            {
                switch (checkMethod)
                {
                    case "type":
                        v.Type((string)param);
                        break;
                    case "required":
                        if ((bool)param)
                            v.Required();
                        break;

                }
            }

            foreach (var error in v.errors)
                logging.AddErrorLog(key: fieldName, type: error.type, msg: error.msg);

            return !v.HasErrors();
        }

        public EntityValues SetNotNull(IDictionary<string, object?> row)
        {
            foreach (var fieldName in db.FieldNames(entityName))
                if (row.ContainsKey(Pf() + fieldName))
                    if (row[Pf() + fieldName] != null && !row[Pf() + fieldName].IsDbNull())
                        Set(fieldName, row[Pf() + fieldName]);

            return this;
        }


        /// <summary>
        /// Comparacion de EntityValues
        /// </summary>
        /// <param name="val"></param>
        /// <param name="ignoreFields"></param>
        /// <param name="ignoreNull"></param>
        /// <param name="ignoreNonExistent"></param>
        /// <returns></returns>
        public IDictionary<string, object> Compare(EntityValues val, IEnumerable<string>? ignoreFields = null, bool ignoreNull = true, bool ignoreNonExistent = true)
        {
            return Compare(val.values!, ignoreFields, ignoreNull, ignoreNonExistent);
        }


        public IDictionary<string, object?> CompareFields(EntityValues val, IEnumerable<string> fieldsToCompare, bool ignoreNull = true, bool ignoreNonExistent = true)
        {
            return CompareFields(val.values!, fieldsToCompare, ignoreNull, ignoreNonExistent);
        }


        /// <summary>
        /// Comparar valores con los indicados en parametro
        /// </summary>
        /// <param name="val">Valores externos a persistir<</param>
        /// <param name="ignoreFields">Campos que seran ignorados en la comparacion<</param>
        /// <param name="ignoreNull">Si el campo del parametro es nulo, sera ignorado en la comparacion<</param>
        /// <param name="ignoreNonExistent">Si el campo no esta definido localmente, sera ignorado en la comparacion</param>

        /// <returns>Valores del parametro que son diferentes o que no estan definidos localmente</returns>
        /// <remarks>Solo compara fieldNames</remarks>
        public virtual IDictionary<string, object?> Compare(IDictionary<string, object?> val, IEnumerable<string>? ignoreFields = null, bool ignoreNull = true, bool ignoreNonExistent = true)
        {
            Dictionary<string, object?> dict1_ = new(values);
            Dictionary<string, object?> dict2_ = new(val);
            Dictionary<string, object?> response = new();


            if (!ignoreFields.IsNullOrEmpty())
                foreach (var key in ignoreFields!)
                {
                    dict1_.Remove(key);
                    dict2_.Remove(key);
                }

            foreach (var fieldName in db.FieldNames(entityName)) {
                if (ignoreNonExistent && !dict1_.ContainsKey(fieldName))
                    continue;

                if (dict2_.ContainsKey(fieldName) && (ignoreNull && !dict2_[fieldName]!.IsDbNull()))
                    if (
                        !dict1_.ContainsKey(fieldName)
                        || (dict1_[fieldName].IsNullOrEmptyOrDbNull() && !dict2_[fieldName].IsNullOrEmptyOrDbNull())
                        || (!dict1_[fieldName].IsNullOrEmptyOrDbNull() && dict2_[fieldName].IsNullOrEmptyOrDbNull())
                        || !dict1_[fieldName]!.ToString()!.Equals(dict2_[fieldName]!.ToString()!)
                    )
                        response[fieldName] = dict2_[fieldName];
            }
            return response;
        }

        /// <summary>
        /// Comparar valores con los indicados en parametro
        /// </summary>
        /// <remarks>Es similar a compare, pero se debe indicar obligatoriamente los campos que se desea comparar</remarks>
        public virtual IDictionary<string, object?> CompareFields(IDictionary<string, object?> val, IEnumerable<string> fieldsToCompare, bool ignoreNull = true, bool ignoreNonExistent = true)
        {
            Dictionary<string, object?> dict1_ = new(values);
            Dictionary<string, object?> dict2_ = new(val);
            Dictionary<string, object?> response = new();

            foreach (var fieldName in db.FieldNames(entityName))
            {
                if (!fieldsToCompare.Contains(fieldName))
                    continue;

                if (ignoreNonExistent && !dict1_.ContainsKey(fieldName))
                    continue;

                if (dict2_.ContainsKey(fieldName) && (ignoreNull && dict2_[fieldName] != null && !dict2_[fieldName].IsDbNull()))
                    if (
                        !dict1_.ContainsKey(fieldName)
                        || !dict1_[fieldName].ToString().Equals(dict2_[fieldName].ToString())
                    )
                        response[fieldName] = dict2_[fieldName];
            }
            return response;
        }


        /// <summary>
        /// Obtener siguiente valor de la secuencia para mysql
        /// </summary>
        /// <remarks>Esta implementación funciona en mysql, llevar a subclase</br>
        /// Siempre devuelve el siguiente valor de la secuencia sin incrementar, si se utiliza en multiples transacciones de inserción consultar una sola vez e incrementar valor</remarks>
        /// <param name="field"></param>
        /// <returns></returns>
        public ulong GetNextValue(Field field)
        {
            var q = db.Query();
            q.sql = @"
                            SELECT auto_increment 
                            FROM INFORMATION_SCHEMA.TABLES 
                            WHERE TABLE_NAME = @0";
            q.parameters.Add(field.entityName);
            return q.Value<ulong>();
        }

        public EntityValues? ValuesTree(string fieldId)
        {
            Entity entity = db.Entity(entityName);
            EntityTree tree = entity.tree[fieldId];
            object? val = GetOrNull(tree.fieldName);
            if (!val.IsNullOrEmpty())
            {
                var data = db.Query(tree.refEntityName)._CacheById(val);
                return (!data.IsNullOrEmptyOrDbNull()) ? db.Values(tree.refEntityName).Set(data) : null;
            }
            return null;
        }

        public EntityValues? ValuesRel(string fieldId)
        {
            Entity entity = db.Entity(entityName);
            EntityRelation rel = entity.relations[fieldId];
            if(rel.parentId == null)
            {
                object? val = GetOrNull(rel.fieldName);
                if (!val.IsNullOrEmpty())
                {
                    var data = db.Query(rel.refEntityName)._CacheById(val!);
                    return db.Values(rel.refEntityName).Set(data!);
                }
            } 
            else
            {
                EntityValues? values = ValuesRel(rel.parentId);                
                if (!values.IsNullOrEmpty())
                    return values!.ValuesRel(fieldId);
            }
            return null;
        }


        /// <summary>Concatena strings indicados en el parametro</summary>
        public string ToStringFields(params string[] fields)
        {
            string s = "";
            foreach (string field in fields)
            {
                s += GetOrNull(field)?.ToString() ?? "?";
                s += ", ";
            }
            return s;
        }

        public string ToStringExcept(params string[] fields)
        {
            List<string> fieldNames = db.FieldNames(entityName);

            foreach (string field in fields)
                fieldNames.Remove(field);

            var label = "";
            foreach (string fieldName in fieldNames) { 
                label += GetOrNull(fieldName)?.ToString() ?? " ";
                label += ", ";
            }

            return label.RemoveMultipleSpaces().Trim();
        }

        public override string ToString()
        {
            List<string> fieldNames = ToStringWhat();

            var label = "";
            foreach (string fieldName in fieldNames)
            { 
                label += GetOrNull(fieldName)?.ToString() ?? " ";
                label += ", ";
            }

            return label.RemoveMultipleSpaces().Trim();
        }

        /// <summary>Retorna una lista de los fields de la entidad más adecuados para ser utilizados como Label</summary>
        protected List<string> ToStringWhat()
        {
            var entity = db.Entity(entityName);
            List<string> fields = new();
            foreach (string f in entity.unique)
                if (entity.notNull.Contains(f))
                    fields.Add(f);

            bool uniqueMultipleFlag = true;
            foreach (List<string> um in entity.uniqueMultiple)
            {
                foreach (string f in um)
                    if (!entity.notNull.Contains(f))
                    {
                        uniqueMultipleFlag = false;
                        break;
                    }

                if (uniqueMultipleFlag)
                    foreach (var f in um)
                        fields.Add(f);

                uniqueMultipleFlag = true;
            }

            if (fields.IsNullOrEmpty())
                fields = entity.notNull;

            if (fields.IsNullOrEmpty())
                fields = entity.fields;

            return fields;
        }

        public (string? fieldId, string fieldName, string entityName, object? value) ParentVariables(string mainEntityName)
        {
            object? value;
            string fieldName;
            string entityName = mainEntityName;
            string? newFieldId = null;

            string? parentId = db.Entity(mainEntityName).relations[fieldId!].parentId;
            if (parentId != null)
            {
                //sea por ejemplo alumnoT.personaF (con fieldId alumno) = personaT.id (con fieldId = persona), entones:
                //parentFieldName = personaF
                //value = personaValues.values["id"]
                //fieldId = alumno
                //fieldName = personaF
                //entityName = alumnoT
                string parentFieldName = db.Entity(mainEntityName).relations[fieldId!].fieldName;
                value = values[db.Entity(mainEntityName).relations[fieldId!].refFieldName];
                newFieldId = parentId;
                fieldName = parentFieldName;
                entityName = db.Entity(mainEntityName).relations[parentId].refEntityName;

            }
            else
            {
                fieldName = db.Entity(mainEntityName).relations[fieldId!].fieldName;
                value = values[db.Entity(mainEntityName).relations[fieldId!].refFieldName];
            }

            return (newFieldId, fieldName, entityName, value);
        }

        /// <summary>
        /// Valor por defecto de field
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public object? DefaultField(string fieldName)
        {
            var field = db.Field(entityName, fieldName);

            if (field.defaultValue is null)
                return null;

            switch (field.type)
            {
                case "string":
                    if (field.defaultValue.ToString()!.ToLower().Contains("guid"))
                        return (Guid.NewGuid()).ToString();

                    //generate random strings
                    else if (field.defaultValue.ToString()!.ToLower().Contains("random"))
                    {
                        string param = field.defaultValue.ToString()!.SubstringBetween("(", ")");
                        return ValueTypesUtils.RandomString(Int32.Parse(param));
                    }
                    else
                        return field.defaultValue;
                case "DateTime":
                    if (field.defaultValue.ToString()!.ToLower().Contains("cur") ||
                        field.defaultValue.ToString()!.ToLower().Contains("getdate")
                    )
                        return DateTime.Now;
                    else
                        return field.defaultValue;

                case "sbyte":
                    return Convert.ToSByte(DefaultFieldInt(field));

                case "byte":
                    return Convert.ToByte(DefaultFieldInt(field));

                case "long":
                    return Convert.ToInt64(DefaultFieldInt(field));

                case "ulong":
                    return Convert.ToUInt64(DefaultFieldInt(field));

                case "int":
                case "nint":
                    return Convert.ToInt32(DefaultFieldInt(field));

                case "uint":
                case "nuint":
                    return Convert.ToUInt32(DefaultFieldInt(field));

                case "short":
                    //el tipo YEAR de mysql es mapeado a short
                    if (field.defaultValue.ToString()!.ToLower().Contains("current_year"))
                         return Convert.ToInt16(DateTime.Now.Year);

                    if (field.defaultValue.ToString()!.ToLower().Contains("current_semester"))
                        return DateTime.Now.ToSemester();

                    return Convert.ToInt16(DefaultFieldInt(field));

                case "ushort":
                    return Convert.ToUInt16(DefaultFieldInt(field));

                default:
                    return field.defaultValue;
            }
        }

        protected object? DefaultFieldInt(Field field)
        {
            if (field.defaultValue.ToString()!.ToLower().Contains("next"))
            {
                ulong next = db.Query(field.entityName).GetNextValue();
                return next;
            }
            else if (field.defaultValue.ToString()!.ToLower().Contains("max"))
            {
                long max = db.Query(field.entityName).GetMaxValue(field.name);
                return max + 1;
            }
            else
            {
                return field.defaultValue;
            }
        }
    }


}
