using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SqlOrganize.Exceptions;
using System.Data.Common;
using System.Text.RegularExpressions;
using Utils;

namespace SqlOrganize
{

    /// <summary>
    /// Selección de datos de una entidad
    /// </summary>
    /// <remarks>
    /// Los fields se traducen con los metodos de mapeo, deben indicarse con el prefijo. Ej "($ingreso = %p1) AND (MAX($persona-nombres) = %p1)"
    /// </remarks>
    public abstract class EntitySql
    {
        /// <summary>
        /// coneccion opcional
        /// </summary>
        protected DbConnection? connection;


        public Db Db { get; }


        public string entityName { get; set; }

        public string? where { get; set; } = "";
        


        public string? having { get; set; }

        public string? fields { get; set; } = "";

        public string? select { get; set; } = "";

        public string? order { get; set; } = "";

        public int size { get; set; } = 100;

        public int page { get; set; } = 1;

        public string group { get; set; } = "";

        public List<object> parameters = new List<object> { };

        public Dictionary<string, object> parametersDict = new ();




        public EntitySql(Db db, string entityName)
        {
            Db = db;
            this.entityName = entityName;
        }

        public EntitySql SetConn(DbConnection connection)
        {
            this.connection = connection;
            return this;
        }

        public EntitySql And(string w)
        {
            if(where.IsNullOrEmpty())
                where += w;
            else
                where = "(" + where + ") AND (" + w + ")";
            return this;
        }

        public EntitySql Or(string w)
        {
            if (where.IsNullOrEmpty())
                where += w;
            else
                where = "(" + where + ") OR (" + w + ")";
            return this;
        }

        public EntitySql Where(string w)
        {
            where += w;
            return this;
        }

        public EntitySql SearchObj(object param)
        {
            var d = param.Dict();
            return Search(d);
        }

        /// <summary>
        /// Crear condicion de busqueda del diccionario
        /// </summary>
        /// <param name="param">Diccionario fieldName : Valor</param>
        /// <returns>this</returns>
        /// <remarks>Filtra los campos que pertenecen a la entidad</remarks>
        public EntitySql Search(IDictionary<string, object?> param)
        {
            var count = parameters.Count;
            foreach(var (key, value) in param)
                if (Db.FieldNamesRel(entityName).Contains(key) && !value.IsNullOrEmpty())
                {
                    if (!where.IsNullOrEmpty())
                        Where(" AND ");
                    Where("$" + key + " = @" + count.ToString());
                    Parameters(value!);
                    count++;
                }
            return this;
        }

        public EntitySql UniqueObj(object obj)
        {
            var d = obj.Dict();
            return Unique(d);
        }

        public EntitySql Unique(EntityValues values)
        {
            return Unique(values.Values());
        }

        public EntitySql Unique(IDictionary<string, object?> row)
        {
            if (row.IsNullOrEmpty())
                throw new UniqueException("El parametro de condicion unica esta vacio");

            List<string> whereUniqueList = new();
            foreach (string fieldName in Db.Entity(entityName).unique)
            {
                foreach (var (key, value) in row)
                {
                    if ((key == fieldName) && (!value.IsNullOrEmptyOrDbNull()))
                    {
                        var v = (value == null) ? DBNull.Value : value;
                        whereUniqueList.Add("$" + key + " = @" + parameters.Count);
                        parameters.Add(v);
                        break;
                    }
                }
            }

            string w = "";
            if (whereUniqueList.Count > 0)
                w = "(" + String.Join(") OR (", whereUniqueList) + ")";

            string ww;
            foreach(var um in Db.Entity(entityName).uniqueMultiple)
            {
                ww = UniqueMultiple(um, row);
                if (!ww.IsNullOrEmpty())
                    w += (w.IsNullOrEmpty()) ? ww : " OR " + ww;
            }

            ww = UniqueMultiple(Db.Entity(entityName).pk, row);
            if (!ww.IsNullOrEmpty())
                w += (w.IsNullOrEmpty()) ? ww : " OR " + ww;

            if (w.IsNullOrEmpty())
                throw new UniqueException("No se pudo definir condicion de campo unico con el parametro indicado");

            where += (where.IsNullOrEmpty()) ? w : " AND (" + w + ")";

            return this;
        }

       

        protected string UniqueMultiple(List<string> fields, IDictionary<string, object?> param)
        {
            if (fields.IsNullOrEmpty())
                return "";

            bool existsUniqueMultiple = true;
            List<string> whereMultipleList = new();
            foreach(string field in fields)
            {
                if (!existsUniqueMultiple) 
                    break;

                existsUniqueMultiple = false;

                foreach(var (key, value) in param)
                    if (key == field)
                    {
                        var v = (value == null) ? DBNull.Value : value;
                        existsUniqueMultiple = true;
                        whereMultipleList.Add("$" + key + " = @" + parameters.Count);
                        parameters.Add(v);
                        break;
                    }
                
            }
            if(existsUniqueMultiple && whereMultipleList.Count > 0)
                return "(" + String.Join(") AND (", whereMultipleList) + ")";                

            return "";
        }

        public EntitySql Fields()
        {
            fields += string.Join(", ", Db.FieldNamesRel(entityName));
            return this;
        }

        public EntitySql Fields(string f)
        {
            fields += f;
            return this;
        }

        public EntitySql Fields(params string[] f)
        {
            fields += String.Join(", ", f);
            return this;
        }

        public EntitySql Select(string f)
        {
            select += f;
            return this;
        }

        public EntitySql Parameters(params object[] parameters)
        {
            this.parameters.AddRange(parameters.ToList());
            return this;
        }

        public EntitySql Parameters(Dictionary<string, object> parameters)
        {
            this.parametersDict.Merge(parameters);
            return this;
        }

        protected string TraduceFields(string _sql)
        {
            if (_sql.IsNullOrEmpty())
                return "";

            List<string> fields = _sql!.Replace("$", "").Split(',').ToList().Select(s => s.Trim()).ToList();

            #region procesar *
            List<string> fieldNamesToDelete = new();
            for (int i = 0; i < fields.Count; i++)
            {
                if (fields[i].Contains("*"))
                {
                    fieldNamesToDelete.Add(fields[i]);
                    var en = entityName;
                    var fid = "";
                    if (fields[i].Contains("-"))
                    {
                        List<string> ff = fields[i].Split("-").ToList();
                        en = Db.Entity(entityName).relations[ff[0]].refEntityName;
                        fid = ff[0] + "-";
                    }

                    List<string> fns = (List<string>)Db.FieldNames(en).AddPrefixToEnum(fid);
                    fields.AddRange(fns);
                }
            }

            foreach (var fntd in fieldNamesToDelete)
                fields.Remove(fntd);
            #endregion

            #region definir sql
            string sql = "";

            foreach (var fieldName in fields)
            {
                if (fieldName.Contains("-"))
                {
                    List<string> ff = fieldName.Split("-").ToList();
                    sql += Db.Mapping(Db.Entity(entityName).relations[ff[0]].refEntityName, ff[0]).Map(ff[1]) + " AS '" + fieldName + "', ";
                } else
                    sql += Db.Mapping(entityName).Map(fieldName) + " AS '" + fieldName + "', ";
            }
            sql = sql.RemoveLastChar(',');
            #endregion

            return sql;
        }

        protected string Traduce(string _sql, bool fieldAs = false )
        {
            string sql = "";
            int field_start = -1;

            for (int i = 0; i < _sql.Length; i++)
            {
                if (_sql[i] == '$')
                {
                    field_start = i;
                    continue;
                }

                if (field_start != -1)
                {
                    if ((_sql[i] != ' ') && (_sql[i] != ')') && (_sql[i] != ',')) continue;
                    sql += Traduce_(_sql, field_start, i - field_start - 1, fieldAs);
                    field_start = -1;
                }

                sql += _sql[i];

            }

            if (field_start != -1)
                sql += Traduce_(_sql, field_start, _sql.Length - field_start - 1, fieldAs);


            return sql;
        }

        protected string Traduce_(string _sql, int fieldStart, int fieldEnd, bool fieldAs)
        {
            var fieldName = _sql.Substring(fieldStart + 1, fieldEnd);

            string ff = "";
            if (fieldName.Contains('-'))
            {
                List<string> fff = fieldName.Split("-").ToList();
                ff += Db.Mapping(Db.Entity(entityName).relations[fff[0]].refEntityName, fff[0]).Map(fff[1]);
            }
            else
                ff += Db.Mapping(entityName).Map(fieldName);

            if (fieldAs) ff += " AS '" + fieldName + "'";
            return ff;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_size"></param>
        /// <returns></returns>
        public EntitySql Size(int _size)
        {
            size = _size;
            return this;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="_page"></param>
        /// <returns></returns>
        public EntitySql Page(int _page)
        {
            page = _page;
            return this;
        }

        public EntitySql Order(string _order)
        {
            order = _order;
            return this;
        }

        public EntitySql Having(string h)
        {
            having += h;
            return this;
        }

        public EntitySql Group(string g)
        {
            group += g;
            return this;
        }

        protected string SqlJoin()
        {
            string sql = "";
            if (!Db.Entity(entityName).tree.IsNullOrEmpty())
                sql += SqlJoinFk(Db.Entity(entityName).tree!, "");
            return sql;
        }

        protected string SqlJoinFk(Dictionary<string, EntityTree> tree, string table_id)
        {
            if (table_id.IsNullOrEmpty())
                table_id = Db.Entity(entityName).alias;

            string sql = "";
            string schema_name;
            foreach (var (field_id, entity_tree) in tree) {
                schema_name = Db.Entity(entity_tree.refEntityName).schemaName;
                sql += "LEFT OUTER JOIN " + schema_name + " AS " + field_id + " ON (" + table_id + "." + entity_tree.fieldName + " = " + field_id + "." + entity_tree.refFieldName + @")
";

                if (!entity_tree.children.IsNullOrEmpty()) sql += SqlJoinFk(entity_tree.children, field_id);
            }
            return sql;
        }

        public string Sql()
        {
            var sql = "SELECT DISTINCT ";
            sql += SqlFields();
            sql += SqlFrom();
            sql += SqlJoin();
            sql += SqlWhere();
            sql += SqlGroup();
            sql += SqlHaving();
            sql += SqlOrder();
            sql += SqlLimit();

            return sql;
        }

        protected string SqlWhere()
        {
            return (where.IsNullOrEmpty()) ? "" : "WHERE " + Traduce(where!) + @"
";
        }

        protected string SqlGroup()
        {
            return (group.IsNullOrEmpty()) ? "" : "GROUP BY " + Traduce(group!) + @"
";
        }

        protected string SqlHaving()
        {
            return (having.IsNullOrEmpty()) ? "" : "HAVING " + Traduce(having!) + @"
";
        }

        protected abstract string SqlOrder();

        protected virtual string SqlFields()
        {
            if(this.fields.IsNullOrEmpty() && this.select.IsNullOrEmpty() && this.group.IsNullOrEmpty())
                this.Fields();

            string f = TraduceFields(this.fields);

            f += Concat(Traduce(this.select), @",
", "", !f.IsNullOrEmpty());

            f += Concat(Traduce(this.group, true), @",
", "", !f.IsNullOrEmpty());

            return f + @"
";
        }

        protected string SqlFrom()
        {
            return @"FROM " + Db.Entity(entityName).schemaName + " AS " + Db.Entity(entityName).alias + @"
";
        }

        protected abstract string SqlLimit();
       
        protected string Concat(string? value, string connect_true, string connect_false = "", bool connect_cond = true)
        {
            if (value.IsNullOrEmpty()) return "";

            string connect = "";
            if (connect_cond)
                connect = connect_true;
            else
                connect = connect_false;

            return connect + " " + value;
        }

        public override string ToString()
        {
            return Regex.Replace(entityName + where + having + fields + select + order + size + page + JsonConvert.SerializeObject(parameters), @"\s+", "");
        }

        public abstract EntitySql Clone();

        protected EntitySql _Clone(EntitySql eq)
        {
            eq.connection = connection;
            eq.entityName = entityName;
            eq.size = size;
            eq.where = where;
            eq.page = page;
            eq.parameters = parameters;
            eq.parametersDict = parametersDict;
            eq.group = group;
            eq.having = having;
            eq.fields = fields;
            eq.select = select;
            eq.order = order;
            return eq;
        }



        

 



    }


   
}
