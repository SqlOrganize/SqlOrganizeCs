using Newtonsoft.Json;
using System.Collections;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Reflection;

namespace SqlOrganize
{
    public static class Utils
    {
        /// <summary>Metodo para verificar si un object es null o empty</summary>
        /// <remarks>Abarca mas tipos que IsNullOrEmpty</br>
        /// Se llama IsNoE para diferenciar de IsNullOrEmpty</remarks>
        public static bool IsNoE(this object value)
        {
            if (value == null || value == DBNull.Value)
                return true;

            Type type = value.GetType();

            if (type == typeof(string))
                return string.IsNullOrEmpty(value as string);

            // Special case for numeric types to check for default value (0)
            //if (IsNumericType(type))
            //{
            //return value!.ToString()!.Equals("0");
            //}

            // Special case for collections to check for empty collection
            if (value is ICollection collection)
            {
                return collection.Count == 0;
            }

            // Handle value types and their default values
            return false;
        }

        public static T Obj<T>(this IDictionary<string, object?> source) where T : class, new()
        {
            var serialized = JsonConvert.SerializeObject(source);
            return JsonConvert.DeserializeObject<T>(serialized);
        }

        /// <summary>Convertir objeto a diccionario analizando el tipo exacto</summary>
        /// <returns></returns>
        public static IDictionary<string, object?> Dict(this object source, BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Instance)
        {
            var properties = source.GetType().GetProperties();
            Dictionary<string, object?> response = new();
            foreach (var propInfo in properties)
            {
                try
                {
                    response[propInfo.Name.Replace("__", "-")] = propInfo.GetValue(source, null); //se reemplaza caracter - por __
                }
                catch (TargetParameterCountException ex)
                {
                    continue;
                }
            }
            return response;
        }

        /// <summary>Convertir objeto a diccionario utilizando json</summary>
        /// <remarks>Puede definir diferentes tipos por ejemplo un int al serializar y deserealizar se traduce en long</remarks>
        public static IDictionary<string, T?> Dict<T>(this object source)
        {
            var json = JsonConvert.SerializeObject(source);
            return JsonConvert.DeserializeObject<Dictionary<string, T?>>(json);
        }


        public static IEnumerable<T> ColOfObj<T>(this IEnumerable<Dictionary<string, object?>> rows) where T : class, new()
        {
            var results = new List<T>();

            foreach (var row in rows)
                results.Add(row.Obj<T>());

            return results;
        }

        public static void AddRange<T>(this ObservableCollection<T> oc, IEnumerable<T> items)
        {
            foreach (var item in items)
                oc.Add(item);
        }

        public static void AddRange<T>(this ObservableCollection<T> oc, IEnumerable<Dictionary<string, object?>> items) where T : class, new()
        {
            foreach (var item in items)
            {
                T o = item.Obj<T>();
                oc.Add(o);
            }
        }



        /*
        https://stackoverflow.com/questions/41040189/fastest-way-to-map-result-of-sqldatareader-to-object
        
        Los caracteres especiales de fieldName son reemplazados por "__"
            Ej. persona-nombres > persona__nombres        
        */
        public static T? Obj<T>(this DbDataReader rd) where T : class, new()
        {
            return rd.SerializeRow()?.Obj<T>() ?? null;
        }

        /// <summary>
        /// Deprecated? Conviene transformar a Dict y luego a Obj por mas que haga un doble loop
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rd"></param>
        /// <returns></returns>
        public static IEnumerable<T> ColOfObj<T>(this DbDataReader rd) where T : class, new()
        {
            var results = new List<T>();
            var cols = rd.ColumnNames();

            while (rd.Read())
                results.Add(rd.SerializeRowCols(cols).Obj<T>());

            return results;
        }

        /*
        https://stackoverflow.com/questions/5083709/convert-from-sqldatareader-to-json 
        */
        public static IEnumerable<Dictionary<string, object?>> Serialize(this DbDataReader reader)
        {
            var cols = reader.ColumnNames();
            var results = new List<Dictionary<string, object?>>();

            while (reader.Read())
                results.Add(reader.SerializeRowCols(cols));

            return results;
        }
        public static Dictionary<string, object?>? SerializeRow(this DbDataReader reader)
        {
            var result = new Dictionary<string, object?>();
            if (!reader.Read()) return null;
            var cols = reader.ColumnNames();
            foreach (var col in cols)
                result.Add(col, reader[col]);
            return result;
        }

        public static Dictionary<string, object?> SerializeRowCols(this DbDataReader reader, List<string> cols)
        {
            Dictionary<string, object?>  result = new ();
            foreach (var col in cols)
                result.Add(col, reader[col]);
            return result;
        }

        public static List<string> ColumnNames(this DbDataReader reader)
        {
            return Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
        }

        public static IEnumerable<T> ColumnValues<T>(this DbDataReader reader, string columnName)
        {
            var result = new List<T>();
            while (reader.Read())
                result.Add((T)reader[columnName]);
            return result;
        }

        public static IEnumerable<T> ColumnValues<T>(this DbDataReader reader, int columnNumber)
        {
            var result = new List<T>();
            while (reader.Read())
                result.Add((T)reader.GetValue(columnNumber));
            return result;
        }

        public static bool IsDbNull(this object? value)
        {
            return (value == System.DBNull.Value);
        }
    }
}
