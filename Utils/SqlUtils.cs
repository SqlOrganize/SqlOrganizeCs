using System.Data.Common;
using System.Reflection.PortableExecutable;

namespace Utils
{
    public static class SqlUtils
    {

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

        public static List<T> ColumnValues<T>(this DbDataReader reader, string columnName)
        {
            var result = new List<T>();
            while (reader.Read())
                result.Add((T)reader[columnName]);
            return result;
        }

        public static T[] ColumnValues<T>(this DbDataReader reader, int columnNumber)
        {
            var result = new List<T>();
            while (reader.Read())
                result.Add((T)reader.GetValue(columnNumber));
            return result.ToArray();
        }

        public static bool IsNullOrEmptyOrDbNull(this object? value)
        {
            return value == System.DBNull.Value || value.IsNullOrEmpty();
        }

        public static bool IsDbNull(this object? value)
        {
            return (value == System.DBNull.Value);
        }

        
    }


}
