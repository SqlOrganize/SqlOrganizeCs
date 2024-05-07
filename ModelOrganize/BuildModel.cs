using Newtonsoft.Json;
using Utils;

namespace ModelOrganize
{
    public abstract class BuildModel
    {
        public Config Config { get; }

        public List<Table> Tables { get; } = new();

        public Dictionary<string, Entity> entities { get; } = new();

        public Dictionary<string, Dictionary<string, Field>> fields { get; set; } = new();


        protected void resetField(Field f)
        {
            f.checks = new()
                    {
                        { "type", f.type },
                    };

            if (f.notNull)
                f.checks["required"] = true;

            if (f.type == "string")
            {
                f.resets = new()
                        {
                            { "trim", ' '},
                            { "removeMultipleSpaces", true },
                        };
                if (!f.notNull)
                    f.resets["nullIfEmpty"] = true;
            }

            if (f.type == "bool" && f.defaultValue is not null)
                f.defaultValue = ((string)f.defaultValue).ToBool();

            if (f.type == "string" && f.defaultValue is not null)
                f.defaultValue = f.defaultValue.ToString()!.Trim('\'');
        }

        protected void defineField(Column c, Field f)
        {
            if (!c.CHARACTER_MAXIMUM_LENGTH.IsNullOrEmpty() && !c.CHARACTER_MAXIMUM_LENGTH.IsDbNull() && Convert.ToInt64(c.CHARACTER_MAXIMUM_LENGTH) > 0)
                f.maxLength = Convert.ToUInt64(c.CHARACTER_MAXIMUM_LENGTH)!;
            else if (!c.MAX_LENGTH.IsNullOrEmpty() && !c.MAX_LENGTH.IsDbNull())
                f.maxLength = Convert.ToUInt64(c.MAX_LENGTH)!;

            f.dataType = c.DATA_TYPE!;
            switch (c.DATA_TYPE)
            {


                case "varbinary":
                case "binary":
                    if (f.maxLength == 1)
                        f.type = "byte";
                    else
                        f.type = "Byte[]";
                    break;

                case "image":
                case "rowversion":
                    f.type = "Byte[]";
                    break;

                case "money":
                case "smallmoney":
                case "numeric":
                case "decimal":
                    f.type = "decimal";
                    break;

                case "varchar":
                case "char":
                case "nchar":
                case "nvarchar":
                case "text":
                case "mediumtext":
                case "tinytext":
                case "longtext":
                    f.type = "string";
                    break;
                case "real":
                    f.type = "Single";
                    break;

                case "float":
                    f.type = "Double";
                    break;

                case "bit":
                    f.type = "bool";
                    break;

                case "datetime":
                case "smalldatetime":
                case "timestamp":
                case "date":
                case "time":
                    f.type = "DateTime";
                    break;

                case "smallint":
                case "year":
                    f.type = (c.IS_UNSIGNED == 1) ? "ushort" : "short";
                    break;

                case "int":
                case "mediumint":
                    f.type = (c.IS_UNSIGNED == 1) ? "uint" : "int";
                    break;

                case "tinyint": 
                    if (f.maxLength == 1)
                        f.type = "bool";
                    else
                        f.type = "byte";
                    break;

                case "bigint":
                    f.type = (c.IS_UNSIGNED == 1) ? "ulong" : "long";
                    break;

                case "uniqueidentifier":
                    f.type = "Guid";
                    break;

                case "sql_variant":
                case "table":
                case "cursor":
                case "xml":
                    f.type = "object";
                    break;

                default:
                    f.type = c.DATA_TYPE!;
                    break;
            }

            if (!c.COLUMN_DEFAULT.IsNullOrEmpty() && !c.COLUMN_DEFAULT.IsDbNull() && (c.COLUMN_DEFAULT as string) != "NULL")
                f.defaultValue = c.COLUMN_DEFAULT;

            if (!c.REFERENCED_TABLE_NAME.IsNullOrEmpty())
                f.refEntityName = c.REFERENCED_TABLE_NAME;

            if (!c.REFERENCED_COLUMN_NAME.IsNullOrEmpty()) //se compara por REFERENCED_TABLE_NAME para REFERENCED_COLUMN_NAME
                f.refFieldName = c.REFERENCED_COLUMN_NAME;

            f.notNull = (c.IS_NULLABLE == 1) ? false : true;


        }

        /// <summary>
        /// Definir datos del esquema y arbol de relaciones
        /// </summary>
        /// <param name="config">Configuracion</param>
        public BuildModel(Config config)
        {
            Config = config;

            #region Definicion inicial de tables y columns
            List<string> tableAlias = new List<string>(Config.reservedAlias);

            foreach (string tableName in GetTableNames())
            {
                if (Config.reservedEntities.Contains(tableName!))
                    continue;

                Table table = new();
                table.Name = tableName;
                table.Alias = GetAlias(tableName, tableAlias, 4);
                tableAlias.Add(table.Alias);
                table.Columns = GetColumns(table.Name);

                List<string> fieldAlias = new List<string>(Config.reservedAlias);
                foreach (Column col in table.Columns)
                {
                    if (col.IS_FOREIGN_KEY == 1 && !Config.reservedEntities.Contains(col.REFERENCED_TABLE_NAME!)) {
                        string idSource = ((Config.idSource == "field_name") ? col.COLUMN_NAME : col.REFERENCED_TABLE_NAME)!;
                        col.Alias = GetAlias(idSource, fieldAlias, 3);
                        fieldAlias.Add(col.Alias);
                    }
                    table.ColumnNames.Add(col.COLUMN_NAME);

                    if (col.IS_FOREIGN_KEY == 1 && !Config.reservedEntities.Contains(col.REFERENCED_TABLE_NAME!))
                        table.Fk.Add(col.COLUMN_NAME);
                    if (col.IS_PRIMARY_KEY == 1)
                        table.Pk.Add(col.COLUMN_NAME);
                    if (col.IS_NULLABLE == 0)
                        table.NotNull.Add(col.COLUMN_NAME);

                    
                }

                Tables.Add(table);
            }
            #endregion

            #region Definicion de campos unicos de tables
            foreach (var table in Tables)
            {
                var infoUnique = GetInfoUnique(table.Name!);

                foreach(var (constraintName, columnNames) in infoUnique!)
                {
                    if (columnNames.Count > 1) {
                        table.UniqueMultiple.Add(columnNames);
                    }
                    else
                    {
                        table.Unique.Add(columnNames[0]);
                    }
                }

            }

            #endregion

            #region Definicion de entities
            foreach (Table t in Tables)
            {
                if (Config.reservedEntities.Contains(t.Name!))
                    continue;

                var e = new Entity();
                e.name = t.Name!;
                e.alias = t.Alias!;
                e.fields = t.ColumnNames;
                e.pk = t.Pk;
                e.fk = t.Fk;
                e.unique = t.Unique;
                e.uniqueMultiple = t.UniqueMultiple;
                e.notNull = t.NotNull;
                entities[e.name!] = e;
            }
            #endregion

            #region Definir id de entities
            foreach (var (name, e) in entities)
                e.id = DefineId(e);
            #endregion

            #region Redefinicion de entities en base a configuracion
           if (File.Exists(config.configPath + "entities.json"))
            {
                using (StreamReader r = new StreamReader(config.configPath + "entities.json"))
                {
                    Dictionary<string, EntityAux> entitiesAux = JsonConvert.DeserializeObject<Dictionary<string, EntityAux>>(r.ReadToEnd())!;
                    foreach (KeyValuePair<string, EntityAux> e in entitiesAux)
                    {
                        if (!entities.ContainsKey(e.Key))
                            continue;

                        entities[e.Key].CopyValues(e.Value, sourceNotNull:true);

                        var f = new List<string>();
                        f.AddRange(entities[e.Key].fields);
                        if(!e.Value.fieldsAdd.IsNullOrEmpty())
                            f.AddRange(e.Value.fieldsAdd);
                        if (!e.Value.fieldsSub.IsNullOrEmpty())
                            f = f.Except(e.Value.fieldsSub).ToList();
                        entities[e.Key].fields = f;

                        entities[e.Key].fk = entities[e.Key].fk.Intersect(f).ToList();
                        entities[e.Key].unique = entities[e.Key].unique.Intersect(f).ToList();
                        entities[e.Key].notNull = entities[e.Key].notNull.Intersect(f).ToList();

                        f = new List<string>();
                        f.AddRange(entities[e.Key].fk);
                        if (!e.Value.fkAdd.IsNullOrEmpty())
                            f.AddRange(e.Value.fkAdd);
                        if (!e.Value.fkSub.IsNullOrEmpty())
                            f = f.Except(e.Value.fkSub).ToList();
                        entities[e.Key].fk = f;

                        f = new List<string>();
                        f.AddRange(entities[e.Key].unique);
                        if (!e.Value.uniqueAdd.IsNullOrEmpty())
                            f.AddRange(e.Value.uniqueAdd);
                        if (!e.Value.uniqueSub.IsNullOrEmpty())
                            f = f.Except(e.Value.uniqueSub).ToList();
                        entities[e.Key].unique = f;

                        f = new List<string>();
                        f.AddRange(entities[e.Key].notNull);

                        if (!e.Value.notNullAdd.IsNullOrEmpty())
                            f.AddRange(e.Value.notNullAdd);

                        if (!e.Value.notNullSub.IsNullOrEmpty())
                            f = f.Except(e.Value.notNullSub).ToList();

                        entities[e.Key].notNull = f;
                    }
                }
            }
            #endregion

            #region Definicion de fields
            foreach (Table t in Tables)
            {
                if (Config.reservedEntities.Contains(t.Name!))
                    continue;

                foreach (Column c in t.Columns)
                {
                    if (!entities[t.Name!]!.fields.Contains(c.COLUMN_NAME))
                        continue;

                    var f = new Field();
                    f.entityName = t.Name!;
                    f.name = c.COLUMN_NAME;
                    f.alias = c.Alias;

                    defineField(c, f);
                    resetField(f);

                    if (!fields.ContainsKey(t.Name!))
                        fields[t.Name!] = new();

                    fields[t.Name!][f.name] = f;
                }
            }
            #endregion

            #region Redefinicion de fields en base a configuracion
            foreach (string entityName in entities.Keys)
                if (fields.ContainsKey(entityName))
                    if (File.Exists(config.configPath + "fields/" + entityName + ".json"))
                        using (StreamReader r = new StreamReader(config.configPath + "fields/" + entityName + ".json"))
                        {
                            Dictionary<string, FieldAux> fieldsAux = JsonConvert.DeserializeObject<Dictionary<string, FieldAux>>(r.ReadToEnd())!;
                            foreach (KeyValuePair<string, FieldAux> e in fieldsAux)
                            {
                                if (fields[entityName].ContainsKey(e.Key))
                                {
                                    fields[entityName][e.Key].CopyValues(e.Value, targetNull:false, sourceNotNull:true, compareNotNull:false);

                                    resetField(fields[entityName][e.Key]);

                                    Dictionary<string, object> f = fields[entityName][e.Key].checks;
                                    if (!e.Value.checks.IsNullOrEmpty())
                                        f = e.Value.checks;
                                    if (!e.Value.checksAdd.IsNullOrEmpty())
                                        f.Merge(e.Value.checks);
                                    if (!e.Value.checksSub.IsNullOrEmpty())
                                        foreach (string k in e.Value.checksSub)
                                            f.Remove(k);
                                    fields[entityName][e.Key].checks = f;

                                    f = fields[entityName][e.Key].resets;
                                    if (!e.Value.resets.IsNullOrEmpty())
                                        f = e.Value.resets;
                                    if (!e.Value.resetsAdd.IsNullOrEmpty())
                                        f.Merge(e.Value.checks);
                                    if (!e.Value.resetsSub.IsNullOrEmpty())
                                        foreach (string k in e.Value.resetsSub)
                                            f.Remove(k);
                                    fields[entityName][e.Key].resets = f;
                                }
                            }
                        }
            #endregion

            #region Definicion de tree y relations de entities            
            /*
             * La definicion de tree y relations no se debe modificar 
             * en la configuracion, ya que se basa en otras llaves 
             * (entity.fk y field.ref*)
             */
            foreach (var (name, e) in entities)
            {
                var bet = new BuildEntityTree(config, entities, fields, e.name!);
                e.tree = bet.Build();
                RelationsRecursive(e.relations, e.tree);

            }
            #endregion



        }

        /// <summary>
        /// Definir alias para tablas y campos
        /// </summary>
        /// <param name="name">Nombre para el cual se definira alias</param>
        /// <param name="reserved">Palabras reservadas que no seran definidas como alias</param>
        /// <param name="length">Longitud del alias</param>
        /// <param name="separator">String utilizado para separar palabras de 'name'</param>
        /// <returns></returns>
        protected string GetAlias(string name, List<string> reserved, int length = 3, string separator = "_")
        {
            var n = name.Trim('_');
            string[] words = n.Split(separator);

            string nameAux = "";

            if (n.Length < length)
                length = n.Length;

            if (words.Length > 1)
                foreach (string word in words)
                    nameAux += word[0];

            string aliasAux = name.Substring(0, length);

            int c = 0;

            while (reserved.Contains(aliasAux))
            {
                c++;
                aliasAux = aliasAux.Substring(0, length - 1);
                aliasAux += c.ToString();
            }

            return aliasAux;
        }

        protected abstract List<string> GetTableNames();

        protected abstract IEnumerable<Column> GetColumns(string tableName);

        protected abstract Dictionary<string, List<string>> GetInfoUnique(string tableName);


        protected void RelationsRecursive(Dictionary<string, EntityRelation> rel, Dictionary<string, EntityTree> tree, string? parentId = null)
        {
            foreach (var (fieldId, t) in tree)
            {
                EntityRelation r = new()
                {
                    fieldName = t.fieldName,
                    refEntityName = t.refEntityName,
                    refFieldName = t.refFieldName,
                    parentId = parentId
                };
                rel[fieldId] = r;
                RelationsRecursive(rel, t.children, fieldId);
            }
        }


        public void CreateFileEntitites()
        {
            if (!Directory.Exists(Config.docPath))
                Directory.CreateDirectory(Config.docPath);

            if (File.Exists(Config.docPath + "entities.json"))
                File.Delete(Config.docPath + "entities.json");

            var file = JsonConvert.SerializeObject(entities, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(Config.docPath + "entities.json", file);
        }

        public void CreateClassModel()
        {
            if (!Directory.Exists(Config.schemaClassPath))
                Directory.CreateDirectory(Config.schemaClassPath);

            using StreamWriter sw = File.CreateText(Config.schemaClassPath + "Schema.cs");

            var file = JsonConvert.SerializeObject(entities, Newtonsoft.Json.Formatting.Indented);
            sw.WriteLine("using System.Collections.Generic;");
            sw.WriteLine("");
            sw.WriteLine("namespace " + Config.schemaClassNamespace);
            sw.WriteLine("{");
            sw.WriteLine("    public class Schema : SqlOrganize.Schema");
            sw.WriteLine("    {");
            sw.WriteLine("        private string _entities = " + JsonConvert.ToString(file) + ";");
            sw.WriteLine("");
            sw.WriteLine("        private Dictionary<string, string> _fields = new() {");

            foreach (var (entityName, entity) in entities)
            {
                    file = JsonConvert.SerializeObject(fields[entityName], Newtonsoft.Json.Formatting.Indented);
                    sw.WriteLine("            { \"" + entityName + "\", " + JsonConvert.ToString(file) + " },");
            }

            sw.WriteLine("        };");
            sw.WriteLine("");
            sw.WriteLine("        protected override string entities => _entities;");
            sw.WriteLine("");
            sw.WriteLine("        protected override Dictionary<string, string> fields => _fields;");
            sw.WriteLine("");
            sw.WriteLine("    }");
            sw.WriteLine("}");
        }

        public void CreateFileFields()
        {
            if (!Directory.Exists(Config.docPath + "Fields/"))
                Directory.CreateDirectory(Config.docPath + "Fields/");

            foreach(var (entityName, field) in fields)
            {
                if (File.Exists(Config.docPath + entityName + ".json"))
                    File.Delete(Config.docPath + entityName + ".json");

                var file = JsonConvert.SerializeObject(fields[entityName], Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(Config.docPath + "Fields/" + entityName + ".json", file);
            }

        }

        public void CreateFileData()
        {
            _CreateFileData();
            _CreateFileDataRel();
        }

        public void _CreateFileData()
        {
            if (!Directory.Exists(Config.dataClassesPath))
                Directory.CreateDirectory(Config.dataClassesPath);

            foreach (var (entityName, entity) in entities)
            {
                using StreamWriter sw = File.CreateText(Config.dataClassesPath + entityName + ".cs");
                sw.WriteLine("#nullable enable");
                sw.WriteLine("using SqlOrganize;");
                sw.WriteLine("using System;");
                sw.WriteLine("using System.ComponentModel;");
                sw.WriteLine("using System.Collections.Generic;");
                sw.WriteLine("using System.Reflection;");
                sw.WriteLine("using Utils;");
                sw.WriteLine("");
                sw.WriteLine("namespace " + Config.dataClassesNamespace);
                sw.WriteLine("{");
                sw.WriteLine("    public class Data_" + entityName + " : SqlOrganize.Data");
                sw.WriteLine("    {");
                sw.WriteLine("");
                sw.WriteLine("        public Data_" + entityName + " ()");
                sw.WriteLine("        {");
                sw.WriteLine("            Initialize();");
                sw.WriteLine("        }");
                sw.WriteLine("");
                sw.WriteLine("        public Data_" + entityName + "(DataInitMode mode)");
                sw.WriteLine("        {");
                sw.WriteLine("            Initialize(mode);");
                sw.WriteLine("        }");
                sw.WriteLine("");
                sw.WriteLine("        protected virtual void Initialize(DataInitMode mode = DataInitMode.Null)");
                sw.WriteLine("        {");
                sw.WriteLine("            switch(mode)");
                sw.WriteLine("            {");
                sw.WriteLine("                case DataInitMode.Default:");
                sw.WriteLine("                case DataInitMode.DefaultMain:");

                foreach (var (fieldName, field) in fields[entityName])
                {
                    if (field.defaultValue != null && field.defaultValueClassData)
                    {
                        string df = "(" + field.type + "?)ContainerApp.db.Values(\"" + entityName + "\").Default(\"" + fieldName + "\").Get(\"" + fieldName + "\")";
                        sw.WriteLine("                    _" + fieldName + " = " + df + ";");
                    }
                }

                sw.WriteLine("                break;");
                sw.WriteLine("            }");
                sw.WriteLine("        }");

                sw.WriteLine("");

                sw.WriteLine("        public string? Label { get; set; }");
                sw.WriteLine("");

                Dictionary<string, Field> _fields = new(fields[entityName]);
                if (!_fields.ContainsKey(Config.id))
                {
                    Field _Id = new Field()
                    {
                        entityName = entityName,
                        name = Config.id,
                        type = "string"
                    };
                    _fields[Config.id] = _Id;
                }

                foreach (var (fieldName, field) in _fields)
                {
                    sw.WriteLine("        protected " + field.type + "? _" + fieldName + " = null;");
                    sw.WriteLine("        public " + field.type + "? " + fieldName);
                    sw.WriteLine("        {");
                    sw.WriteLine("            get { return _" + fieldName + "; }");
                    sw.WriteLine("            set { _" + fieldName + " = value; NotifyPropertyChanged(nameof(" + fieldName + ")); }");
                    sw.WriteLine("        }");
                }

                sw.WriteLine("        protected override string ValidateField(string columnName)");
                sw.WriteLine("        {");
                sw.WriteLine("");
                sw.WriteLine("            switch (columnName)");
                sw.WriteLine("            {");
                sw.WriteLine("");
                foreach (var (fieldName, field) in fields[entityName])
                {
                    sw.WriteLine("                case \"" + fieldName + "\":");

                    if (field.notNull)
                    {
                        sw.WriteLine("                    if (_" + fieldName + " == null)");
                        sw.WriteLine("                        return \"Debe completar valor.\";");

                    }
                    if (entity.unique.Contains(field.name))
                    {
                        sw.WriteLine("                    if (!_" + fieldName + ".IsNullOrEmptyOrDbNull()) {");
                        sw.WriteLine("                        var row = ContainerApp.db.Sql(\"" + entityName + "\").Where(\"$" + fieldName + " = @0\").Parameters(_" + fieldName + ").DictCache();");
                        sw.WriteLine("                        if (!row.IsNullOrEmpty() && !_" + Config.id + ".ToString().Equals(row![\"" + Config.id + "\"]!.ToString()))");
                        sw.WriteLine("                            return \"Valor existente.\";");
                        sw.WriteLine("                    }");
                    }
                    sw.WriteLine("                    return \"\";");
                    sw.WriteLine("");

                }
                sw.WriteLine("            }");
                sw.WriteLine("");
                sw.WriteLine("            return \"\";");
                sw.WriteLine("        }");
                sw.WriteLine("    }");
                sw.WriteLine("}");

            }
        }

        public void _CreateFileDataRel()
        {

            foreach (var (entityName, entity) in entities)
            {
                if (entities[entityName].relations.Count() == 0)
                    continue;

                using StreamWriter sw = File.CreateText(Config.dataClassesPath + entityName + "_r.cs");
                sw.WriteLine("#nullable enable");
                sw.WriteLine("using SqlOrganize;");
                sw.WriteLine("using System;");
                sw.WriteLine("using Newtonsoft.Json;");
                sw.WriteLine("");
                sw.WriteLine("namespace " + Config.dataClassesNamespace);
                sw.WriteLine("{");
                sw.WriteLine("    public class Data_" + entityName + "_r" + " : Data_" + entityName);
                sw.WriteLine("    {");

                sw.WriteLine("");

                sw.WriteLine("        public Data_" + entityName + "_r () : base()");
                sw.WriteLine("        {");
                sw.WriteLine("            Initialize();");
                sw.WriteLine("        }");
                sw.WriteLine("");
                sw.WriteLine("        public Data_" + entityName + "_r (DataInitMode mode) : base(mode)");
                sw.WriteLine("        {");
                sw.WriteLine("            Initialize(mode);");
                sw.WriteLine("        }");

                sw.WriteLine("");

                sw.WriteLine("        protected override void Initialize(DataInitMode mode = DataInitMode.Null)");
                sw.WriteLine("        {");
                sw.WriteLine("            base.Initialize(mode);");
                sw.WriteLine("            switch(mode)");
                sw.WriteLine("            {");
                sw.WriteLine("                case DataInitMode.Default:");

                foreach (var (fieldId, relation) in entities[entityName].relations)
                {
                    foreach (var (fieldName, field) in fields[relation.refEntityName])
                        if (field.defaultValue != null && field.defaultValueClassData)
                        {
                            string df = "(" + field.type + "?)ContainerApp.db.Values(\"" + relation.refEntityName + "\").Default(\"" + fieldName + "\").Get(\"" + fieldName + "\")";
                            sw.WriteLine("                    " + fieldId + "__" + fieldName + " = " + df + ";");
                        }

                    
                }
                sw.WriteLine("                break;");
                sw.WriteLine("            }");
                sw.WriteLine("        }");

                foreach (var (fieldId, relation) in entities[entityName].relations)
                {
                    var fs = "";

                    if (!relation.parentId.IsNullOrEmpty())
                        fs = relation.parentId + "__" + relation.fieldName;
                    else
                        fs = relation.fieldName;


                    sw.WriteLine("");
                    sw.WriteLine("        public string? " + fieldId + "__Label { get; set; }");
                    sw.WriteLine("");
                    foreach (var (fieldName, field) in fields[relation.refEntityName])
                    {
                        sw.WriteLine("        protected " + field.type + "? _" + fieldId + "__" + fieldName + " = null;");
                        sw.WriteLine("");
                        sw.WriteLine("        [JsonProperty(\"" + fieldId + "-" + fieldName + "\")]");
                        sw.WriteLine("        public " + field.type + "? " + fieldId + "__" + fieldName);
                        sw.WriteLine("        {");
                        sw.WriteLine("            get { return _" + fieldId + "__" + fieldName + "; }");
                        if(fieldName != relation.refFieldName)
                            sw.WriteLine("            set { _" + fieldId + "__" + fieldName + " = value; NotifyPropertyChanged(nameof(" + fieldId + "__" + fieldName + ")); }");
                        else
                            sw.WriteLine("            set { _" + fieldId + "__" + fieldName + " = value; " + fs + " = value; NotifyPropertyChanged(nameof(" + fieldId + "__" + fieldName + ")); }");

                        sw.WriteLine("        }");
                    }
                }
                sw.WriteLine("    }");
                sw.WriteLine("}");
            }
        }

        protected List<string> DefineId(Entity entity)
        {
            if (entity.pk.Count == 1)
                return entity.pk;

            foreach (string f in entity.unique)
                if (entity.notNull.Contains(f))
                    return new List<string> { f };



            bool uniqueMultipleFlag = true;
            foreach (List<string> um in entity.uniqueMultiple) { 
                foreach (string f in um)
                    if (!entity.notNull.Contains(f))
                    {
                        uniqueMultipleFlag = false;
                        break;
                    }
                if (uniqueMultipleFlag)
                    return um;

                uniqueMultipleFlag = true;
            }

            if (entity.notNull.Count > 1)
            {
                return entity.notNull;
            }

            return entity.fields;
        }

    }
}

