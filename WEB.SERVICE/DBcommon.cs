using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace WEB.SERVICE
{
    public static class ListExtensions
    {
        public static DataTable ToDataTable<T>(this IEnumerable<T> list) where T : class
        {
            DataTable dataTable = new DataTable();
            // Lấy danh sách các thuộc tính từ đối tượng đầu tiên trong List<T>
            if (typeof(T).IsArray) throw new Exception("ERROR TYPE");
            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                var typeColum = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                switch (Type.GetTypeCode(typeColum))
                {
                    case TypeCode.Boolean:
                        dataTable.Columns.Add(new DataColumn()
                        {
                            AllowDBNull = true,
                            ColumnName = property.Name,
                            DataType = typeof(ulong)
                        });
                        break;
                    default:
                        dataTable.Columns.Add(new DataColumn
                        {
                            AllowDBNull = true,
                            ColumnName = property.Name,
                            DataType = typeColum
                        });
                        break;
                }
            }

            // Thêm các dòng từ List vào DataTable
            foreach (var item in list)
            {
                DataRow row = dataTable.NewRow();
                foreach (var property in properties)
                {
                    row[property.Name] = property.GetValue(item) ?? DBNull.Value;
                }
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }
    }
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