using System.Collections.Immutable;
using System.Globalization;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using ConfigurationManager = System.Configuration.ConfigurationManager;
using System.Data.Common;
using System.Reflection;

namespace WEB.SERVICE
{
    public class BulkMySql
    {
        public static string BulkDelete<P>(DBM dbm, P parameter, string tableName) where P : class
        {
            return BulkDelete(dbm, parameter, tableName, out _);
        }
        public static string BulkDelete<P>(DBM dbm, P parameter, string tableName, out int rowEffected) where P : class
        {
            rowEffected = 0;
            try
            {
                var propParam = typeof(P).GetProperties();
                if (!propParam.Any()) return "ERROR : Parameter empty";

                if (dbm.conn.State != ConnectionState.Open)
                    dbm.conn.Open();
                List<string> paramDelete = new List<string>();

                foreach (PropertyInfo prop in propParam)
                {
                    string value = "";
                    var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    switch (type.ToString())
                    {
                        case TypeStr.String:
                        case TypeStr.Guid:
                            value = $"BINARY '{prop.GetValue(parameter)}'";
                            break;
                        default:
                            value = $"{prop.GetValue(parameter)}";
                            break;
                    }
                    paramDelete.Add($"{prop.Name} = {value}");
                }
                string cmdText = $"DELETE FROM {tableName} WHERE {string.Join(" AND ", paramDelete)}";
                using MySqlDataAdapter adapter = new();
                adapter.DeleteCommand = new MySqlCommand(cmdText, (MySqlConnection)dbm.conn);

                rowEffected = adapter.DeleteCommand.ExecuteNonQuery();
                return "";
            }
            finally
            {
                dbm.CallDispose();
            }
        }
        public static string BulkUpdate(DBM dbm, DataTable table, string tableName)
        {
            List<string> keyMap = new List<string>();
            return BulkUpdate(dbm, table, tableName, keyMap);
        }
        public static string BulkUpdate(DBM dbm, DataTable table, string tableName, IEnumerable<string> keymap)
        {
            try
            {
                GetPrimaryKeys(tableName, out List<string> keys);
                if (!keys.Any() && !keymap.Any())
                    throw ThrowException($"BulkMySql Error : No Primary key was found ");

                dbm.OpenConnection();

                List<string> listColumnName = new List<string>();
                foreach (DataColumn column in table.Columns)
                {
                    listColumnName.Add(column.ColumnName);
                }
                string cmdtxt = $"select {string.Join(",", listColumnName)} from `{tableName}` limit 0";
                using MySqlDataAdapter adapter = new(cmdtxt, dbm.conn as MySqlConnection);
                adapter.UpdateBatchSize = 10000;
                adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

                using DataTable data = new DataTable();
                adapter.Fill(data);
                //table.Select()

                using (MySqlCommandBuilder commandBuilder = new MySqlCommandBuilder(adapter))
                {

                    data.Merge(table);

                    int num = adapter.Update(data);
                    if (num != table.Rows.Count)
                        throw ThrowException($"BulkMySql. Merge data error - rowInsert: {num} - tableRows: {table.Rows.Count}");
                }
                return "";
            }
            finally
            {
                dbm.CallDispose();
            }
        }
        public static string GetPrimaryKeys(string tableName, out List<string> keys)
        {
            keys = new List<string>();
            using DBM dbm = new DBM();
            dbm.OpenConnection();
            string cmdText = $"select * from `{tableName}` LIMIT 0";
            using MySqlDataAdapter adapter = new MySqlDataAdapter(cmdText, (MySqlConnection)dbm.conn);
            adapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

            using DataTable dataTable = new DataTable();
            adapter.Fill(dataTable);
            foreach (DataColumn col in dataTable.Columns)
            {
                keys.Add(col.ColumnName);
            }
            return "";
        }

        public static string BulkUpdate(DBM dbm, DataTable table, string tableName, string primaryKey)
        {
            return BulkMergeMysql(dbm, table, tableName, primaryKey, false);
        }
        public static string BulkMerge(DBM dbm, DataTable table, string tableName, string primaryKey)
        {
            return BulkMergeMysql(dbm, table, tableName, primaryKey, true);
        }
        private static string BulkMergeMysql(DBM dbm, DataTable table, string tableName, string primaryKey, bool isMerge)
        {
            try
            {
                if (table.Rows.Count == 0 || table.Columns.Count == 0)
                    throw ThrowException("BulkMysql Error: Datatable is empty");

                if (dbm.conn.State != ConnectionState.Open)
                    dbm.conn.Open();

                List<string> valuePrimaryKey = new List<string>();
                foreach (DataRow r in table.Rows)
                {
                    valuePrimaryKey.Add(Convert.ToString(r[primaryKey]) ?? "");
                }

                List<string> listColumnName = new List<string>();
                foreach (DataColumn column in table.Columns)
                {
                    listColumnName.Add(column.ColumnName);
                }
                string arrPrimary = string.Join(",", valuePrimaryKey.Select(k => $"'{k}'"));

                string cmdtxt = $"select {string.Join(",", listColumnName)} from `{tableName}` WHERE {primaryKey} In ({arrPrimary})";

                using MySqlDataAdapter adapter = new(cmdtxt, dbm.conn as MySqlConnection);
                adapter.UpdateBatchSize = 10000;
                DataTable data = new DataTable();
                adapter.Fill(data);

                using (MySqlCommandBuilder commandBuilder = new MySqlCommandBuilder(adapter))
                {

                    data.PrimaryKey = new DataColumn[] { data.Columns[primaryKey] };
                    if (data.PrimaryKey.Length == 0)
                        throw ThrowException("BulkMerge Error: Primery Key Is Not Valid");

                    //adapter.UpdateCommand = commandBuilder.GetUpdateCommand();

                    if (!isMerge && table.Rows.Count > data.Rows.Count)
                        throw ThrowException($"BulkMySql. Merge data error - rowInsert: {table.Rows.Count}, Data found: {data.Rows.Count}");
                    data.Merge(table);

                    data = data.DefaultView.ToTable(true);

                    int num = adapter.Update(data);
                    if (num != table.Rows.Count)
                        throw ThrowException($"BulkMySql. Merge data error - rowInsert: {num} - tableRows: {table.Rows.Count}");

                }

                return "";
            }
            finally
            {
                table.Dispose();
                dbm.CallDispose();
            }
        }
        public static Exception ThrowException(string msg, Exception? e = null)
        {
            return new Exception(msg, e);
        }
        public static string BulkInsert(DBM dbm, DataTable table, string tableName)
        {
            return BulkInsert(dbm, table, tableName, out _);
        }
        public static string BulkInsert(DBM dbm, DataTable table, string tableName, out int rowEffected)
        {
            rowEffected = 0;
            if (table.Columns.Count == 0)
                return "BulkMysql Error : Datatable is empty";
            try
            {
                List<string> list = new List<string>();
                foreach (DataColumn column in table.Columns)
                {
                    list.Add(column.ColumnName);
                }
                dbm.OpenConnection();
                string cmdtxt = $"select {string.Join(",", list)} from `{tableName}` limit 0";
                using MySqlDataAdapter adapter = new(cmdtxt, dbm.conn as MySqlConnection);
                adapter.UpdateBatchSize = 10000;
                using (MySqlCommandBuilder commandBuilder = new MySqlCommandBuilder(adapter))
                {
                    commandBuilder.SetAllValues = true;
                    rowEffected = adapter.Update(table);
                    if (rowEffected != table.Rows.Count)
                    {
                        return $"BulkMySql. Insert data error - rowInsert: {rowEffected} - tableRows: {table.Rows.Count}";
                    }
                }
                return "";
            }
            finally
            {
                table.Dispose();
                dbm.CallDispose();
            }
        }
    }
}