using System.Collections;
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
using System.Configuration;

namespace WEB.SERVICE
{
    public class DBM : IDisposable
    {
        public static string ConnectionStr = ConfigurationManager.AppSettings["MYSQL"] ?? "";
        public DbConnection conn { get; set; }
        public DbTransaction? trans { get; set; }
        public DBM()
        {
            conn = CreateMysqlConnection();
        }
        public DBM(string connectionStr)
        {
            conn = new MySqlConnection(connectionStr);
        }
        private DbConnection CreateMysqlConnection()
        {
            return new MySqlConnection(ConnectionStr);
        }
        public void BeginTrans()
        {
            OpenConnection();

            trans = conn.BeginTransaction(IsolationLevel.Unspecified);
        }
        public void CommitTrans()
        {
            if (trans != null)
            {
                trans.Commit();
                trans = null;
            }
            CloseConnection();
        }
        public void RollBackTrans()
        {
            if (trans != null)
            {
                trans.Rollback();
                trans = null;
            }
            CloseConnection();
        }
        private static DbCommand PrepareCommand<P>(DBM dbm, string cmdTxt, CommandType commandType, P parameters, string prefixParam) //where P : class
        {
            if (parameters == null || !typeof(P).IsClass) throw new Exception("ERROR: Parameter must be not null and a reference type");
            var (conn, trans) = dbm;
            dbm.OpenConnection();
            DbCommand command = new MySqlCommand(cmdTxt, (MySqlConnection)dbm.conn);
            command.CommandType = commandType;
            if (trans != null)
                command.Transaction = (MySqlTransaction)trans;
            Addparameter(command, parameters, prefixParam);
            return command;
        }
        private static string PrepareCommand<P, T>(DBM dbm, string cmdtext, CommandType commandType, P parameters, out T result, Func<DbCommand, T> ExcuteCommand)
        {
            result = default;
            try
            {
                using DbCommand cmd = PrepareCommand(dbm, cmdtext, commandType, parameters, "p");
                cmd.Prepare();
                result = ExcuteCommand(cmd);
                return "";
            }
            finally
            {
                dbm.CallDispose();
            }
        }
        public static string ExecuteScalar<P>(DBM dbm, string store, P parameters, out object result)
        {
            return PrepareCommand(dbm, store, CommandType.StoredProcedure, parameters, out result, cmd =>
            {
                return cmd.ExecuteScalar();
            });
        }
        public static string ExecuteNonQuery<P>(DBM dbm, string store, P parameters)
        {
            return ExecuteNonQuery(dbm, store, parameters, out _);
        }
        public static string ExecuteNonQuery<P>(DBM dbm, string store, P parameters, out int rowEffected)
        {

            return PrepareCommand(dbm, store, CommandType.StoredProcedure, parameters, out rowEffected, cmd =>
            {
                return cmd.ExecuteNonQuery();
            });
        }

        public static DbConnection GetConnection()
        {
            return new MySqlConnection(ConnectionStr);
        }
        private static List<T> MapList<T>(DataTable dataTable) where T : new()
        {
            List<T> result = new List<T>();
            var properties = DBcommon.GetProperties<T>();

            foreach (DataRow row in dataTable.Rows)
            {
                T obj = new T();
                foreach (var property in properties)
                {
                    Type type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                    if (dataTable.Columns.Contains(property.Name) && row[property.Name] != DBNull.Value)
                    {
                        switch (Type.GetTypeCode(type))
                        {
                            case TypeCode.Boolean:
                                property.SetValue(obj, Convert.ToBoolean(row[property.Name]));
                                break;
                            default:
                                property.SetValue(obj, row[property.Name]);
                                break;
                        }
                    }
                }
                result.Add(obj);
            }
            dataTable.Dispose();
            return result;
        }

        private static void Addparameter<P>(IDbCommand command, P parameters, string prefixParam)
        {
            var properties = typeof(P).GetProperties();

            foreach (var property in properties)
            {
                MySqlParameter parameter = new MySqlParameter(prefixParam + property.Name, property.GetValue(parameters) ?? DBNull.Value);
                command.Parameters.Add(parameter);
            }
        }

        public static void MapToObjectDT<T>(IDataReader reader, out List<T> values) where T : new()
        {
            values = new List<T>();

            var properties = DBcommon.GetProperties<T>();// typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            using DataTable table = new DataTable();
            table.Load(reader);

            values = MapList<T>(table);
        }
        public static void MapToList<T>(IDataReader dr, out List<T> values) where T : new()
        {
            values = new List<T>();
            try
            {
                List<string> columnName = Enumerable.Range(0, dr.FieldCount).Select(dr.GetName).ToList();
                PropertyInfo[] properties = DBcommon.GetProperties<T>();
                while (dr.Read())
                {
                    T obj = new T();
                    foreach (var property in properties)
                    {
                        var type = DBcommon.GetNullableType(property.PropertyType);
                        var IsContain = columnName.Contains(property.Name);
                        if (IsContain && dr[property.Name] != DBNull.Value)
                        {
                            switch (Type.GetTypeCode(type))
                            {
                                case TypeCode.Boolean:
                                    property.SetValue(obj, Convert.ToBoolean(dr[property.Name]));
                                    break;
                                default:
                                    property.SetValue(obj, dr[property.Name]);
                                    break;
                            }
                        }
                    }
                    values.Add(obj);
                }
            }
            catch (Exception e)
            {
                throw new Exception("ERROR CONVERT DataType MYSQL ", e);
            }
            finally
            {
                dr.Close();
            }
        }

        private static string ExecuteReader<P, T>(string cmdTxt, P parameter, string prefixParam, CommandType commandType, IEnumerable<string>? varOutPut, out List<object> valueOutput, out List<T> result) where T : new()
        {
            result = new List<T>();
            valueOutput = new List<object>();

            using DBM dbm = new DBM();
            using MySqlCommand cmd = (MySqlCommand)PrepareCommand(dbm, cmdTxt, commandType, parameter, prefixParam);
            AddOutPutParameter(cmd, varOutPut);

            using DbDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

            MapToList(dr, out result);

            GetOutPutValue(cmd, varOutPut, out valueOutput);
            return "";
        }
        public static string ExecuteReader<P, T>(string cmdTxt, P parameter, CommandType commandType, out List<T> result) where T : new()
        {
            return ExecuteReader(cmdTxt, parameter, "@", commandType, null, out _, out result);
        }
        private static string ExecuteReaderStore<P, T>(string store, P parameter, out List<T> result) where T : new()
        {
            return ExecuteReader(store, parameter, "p", CommandType.StoredProcedure, null, out _, out result);
        }
        public static string GetList<P, T>(string store, P parameter, out List<T> result) where T : new()
        {
            string msg = ExecuteReaderStore(store, parameter, out result);
            if (msg.Length > 0) return msg;
            return "";
        }
        public static string GetOne<P, T>(string store, P parameter, out T result) where T : new()
        {
            result = new T();
            string msg = ExecuteReaderStore(store, parameter, out List<T> data);
            if (msg.Length > 0) return msg;
            result = data.FirstOrDefault();
            return "";
        }
        public static string GetOneByColumnName<P, T>(string tableName, P value, out T result) where T : new()
        {
            string condition = "";
            result = new T();
            foreach (var prop in typeof(P).GetProperties())
            {
                var type = DBcommon.GetNullableType(prop.PropertyType);
                switch (type.ToString())
                {
                    case TypeStr.String or TypeStr.Guid:
                        condition = $"{prop.Name} = BINARY '{prop.GetValue(prop.GetValue(value))}'";
                        break;
                    default:
                        condition = $"{prop.Name} = {prop.GetValue(value)}";
                        break;
                }
            }

            string cmdText = $"SELECT * FROM `{tableName}` WHERE {condition} LIMIT 1";
            ExecuteReader(cmdText, new { }, CommandType.Text, out List<T> data);
            result = data.FirstOrDefault();
            return "";
        }
        public static string GetList<P, T>(string store, P parameter, IEnumerable<string> varOutPut, out List<object> valueOutput, out List<T> result) where T : new()
        {
            return ExecuteReader(store, parameter, "p", CommandType.StoredProcedure, varOutPut, out valueOutput, out result);
        }
        private static void AddOutPutParameter(DbCommand command, IEnumerable<string>? varOutPut)
        {
            if (varOutPut != null && varOutPut.Any())
                foreach (var v in varOutPut)
                {
                    command.Parameters.Add(new MySqlParameter
                    {
                        ParameterName = v,
                        Direction = ParameterDirection.Output
                    });
                }
        }
        private static void GetOutPutValue(MySqlCommand command, IEnumerable<string>? varOutPut, out List<object> valueOutPut)
        {
            valueOutPut = new();
            if (varOutPut != null && varOutPut.Any())
                foreach (var varOut in varOutPut)
                {
                    valueOutPut.Add(command.Parameters[varOut].Value);
                }
        }

        public void Dispose()
        {
            conn.Dispose();
            trans?.Dispose();

            GC.SuppressFinalize(this);
        }
        public void CallDispose()
        {
            if (trans == null) Dispose();
        }

        public void CloseConnection()
        {
            if (conn.State != ConnectionState.Closed)
                conn.Close();
        }
        public void OpenConnection()
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();
        }

        public void Deconstruct(out DbConnection connect, out DbTransaction transac)
        {
            connect = conn;
            transac = trans;
        }
    }
}