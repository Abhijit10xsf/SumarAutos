using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.Linq;

namespace SumarAuto.Data
{
    public class HanaDataHelper
    {
        public static string GetHanaConnectionString()
        {
            try
            {
                var connStr = ConfigurationManager.AppSettings["HanaCon"]
                           ?? ConfigurationManager.AppSettings["HanaConnection"];

                if (string.IsNullOrWhiteSpace(connStr) && ConfigurationManager.ConnectionStrings["HanaCon"] != null)
                {
                    connStr = ConfigurationManager.ConnectionStrings["HanaCon"].ConnectionString;
                }

                if (!string.IsNullOrWhiteSpace(connStr))
                {
                    if (connStr.IndexOf("Driver=", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        string server = ExtractKey(connStr, "Server") ?? ExtractKey(connStr, "SERVERNODE") ?? "192.168.1.15:30015";
                        string uid = ExtractKey(connStr, "UserID") ?? ExtractKey(connStr, "UID") ?? "SYSTEM";
                        string pwd = ExtractKey(connStr, "Password") ?? ExtractKey(connStr, "PWD") ?? "Abcd@1234";
                        string schema = ExtractKey(connStr, "Current Schema") ?? ExtractKey(connStr, "CS") ?? "TESTDBSUMARLIVE20241221";

                        return $"Driver={{HDBODBC}};SERVERNODE={server};UID={uid};PWD={pwd};CS={schema}";
                    }
                    return connStr;
                }
            }
            catch { }

            // Default fallback SAP HANA ODBC Connection String
            return "Driver={HDBODBC};SERVERNODE=192.168.1.15:30015;UID=SYSTEM;PWD=Abcd@1234;CS=TESTDBSUMARLIVE20241221";
        }

        private static string ExtractKey(string connStr, string key)
        {
            if (string.IsNullOrWhiteSpace(connStr)) return null;
            var parts = connStr.Split(';');
            foreach (var part in parts)
            {
                var kv = part.Split('=');
                if (kv.Length >= 2 && kv[0].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    return string.Join("=", kv.Skip(1)).Trim();
                }
            }
            return null;
        }

        public static IDbConnection GetConnection()
        {
            string connStr = GetHanaConnectionString();

            // If connection string contains "Driver=" or "HDBODBC", use OdbcConnection for SAP HANA
            if (connStr.IndexOf("Driver=", StringComparison.OrdinalIgnoreCase) >= 0 ||
                connStr.IndexOf("HDBODBC", StringComparison.OrdinalIgnoreCase) >= 0 ||
                connStr.IndexOf("SERVERNODE=", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new OdbcConnection(connStr);
            }

            // Otherwise fallback to SqlClient
            return new SqlConnection(connStr);
        }

        public DataTable ExecuteDataTable(string query, Dictionary<string, object> parameters = null)
        {
            DataTable dt = new DataTable();
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = query;
                    if (parameters != null)
                    {
                        foreach (var kvp in parameters)
                        {
                            var p = cmd.CreateParameter();
                            p.ParameterName = kvp.Key;
                            p.Value = kvp.Value ?? DBNull.Value;
                            cmd.Parameters.Add(p);
                        }
                    }

                    if (conn is OdbcConnection odbcConn)
                    {
                        using (var da = new OdbcDataAdapter((OdbcCommand)cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                    else if (conn is SqlConnection sqlConn)
                    {
                        using (var da = new SqlDataAdapter((SqlCommand)cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }
            }
            return dt;
        }

        public object ExecuteScalar(string query, Dictionary<string, object> parameters = null)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = query;
                    if (parameters != null)
                    {
                        foreach (var kvp in parameters)
                        {
                            var p = cmd.CreateParameter();
                            p.ParameterName = kvp.Key;
                            p.Value = kvp.Value ?? DBNull.Value;
                            cmd.Parameters.Add(p);
                        }
                    }
                    return cmd.ExecuteScalar();
                }
            }
        }
    }
}
