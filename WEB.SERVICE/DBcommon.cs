using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace WEB.SERVICE
{
    public class DBcommon
    {
        public static Exception ThrowException(string msg, Exception? e = null)
        {
            return new Exception(msg, e);
        }
        public static Type GetNullableType(Type type)
        {
            return Nullable.GetUnderlyingType(type) ?? type;
        }
        public static PropertyInfo[] GetProperties<T>()
        {
            PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return properties;
        }
        public static void PrintValues(DataTable table, string label)
        {
            Console.WriteLine(label);
            foreach (DataColumn col in table.Columns)
                Console.Write("\t " + col.ColumnName);
            Console.WriteLine();
            foreach (DataRow row in table.Rows)
            {
                foreach (DataColumn col in table.Columns)
                {
                    Console.Write("\t " + row[col].ToString());
                }
                Console.WriteLine();
            }
        }
    }
    public class TypeStr
    {
        public const string Boolean = "System.Boolean";
        public const string Guid = "System.Guid";
        public const string String = "System.String";
    }
}