using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Data.SqlClient;

namespace SumarAuto.Client.Helper
{
    public class DataHelper
    {
        private string connectionString;

        public DataHelper()
        {
            try
            {
                connectionString = System.Configuration.ConfigurationManager.AppSettings["HanaCon"]
                                ?? System.Configuration.ConfigurationManager.AppSettings["HanaConnection"];
                
                if (string.IsNullOrWhiteSpace(connectionString) && System.Configuration.ConfigurationManager.ConnectionStrings["DBEntities"] != null)
                {
                    connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DBEntities"].ConnectionString;
                }
            }
            catch
            {
                connectionString = "Driver={HDBODBC32};UID=SYSTEM;PWD=Abcd@1234;SERVERNODE=192.168.51.19:30015;CS={DEVDB}";
            }
        }

        private IDbConnection GetConnection(string overrideConnStr = null)
        {
            string connStr = overrideConnStr ?? connectionString;
            if (string.IsNullOrWhiteSpace(connStr))
            {
                connStr = "Driver={HDBODBC32};UID=SYSTEM;PWD=Abcd@1234;SERVERNODE=192.168.51.19:30015;CS={DEVDB}";
            }

            if (connStr.IndexOf("Driver=", StringComparison.OrdinalIgnoreCase) >= 0 ||
                connStr.IndexOf("HDBODBC", StringComparison.OrdinalIgnoreCase) >= 0 ||
                connStr.IndexOf("SERVERNODE=", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new OdbcConnection(connStr);
            }
            return new SqlConnection(connStr);
        }

        public int ExecuteNonQuery(string sQry)
        {
            int i = 0;
            using (var conn = GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = sQry;
                        i = cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            return i;
        }

        public object ExecuteScalar(string sQry)
        {
            object objVar = null;
            using (var conn = GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = sQry;
                        objVar = cmd.ExecuteScalar();
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            return objVar;
        }

        public DataSet getDataSet(string sQry)
        {
            DataSet dsData = new DataSet();
            using (var conn = GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = sQry;
                        if (conn is OdbcConnection odbcConn)
                        {
                            using (var hda = new OdbcDataAdapter((OdbcCommand)cmd))
                            {
                                hda.Fill(dsData, "ds");
                            }
                        }
                        else if (conn is SqlConnection sqlConn)
                        {
                            using (var hda = new SqlDataAdapter((SqlCommand)cmd))
                            {
                                hda.Fill(dsData, "ds");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            return dsData;
        }

        public DataSet getDataSetByCompany(string sQry, string companyKey)
        {
            string compConnStr = System.Configuration.ConfigurationManager.AppSettings[companyKey]?.ToString();
            DataSet dsData = new DataSet();
            using (var conn = GetConnection(compConnStr))
            {
                try
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = sQry;
                        if (conn is OdbcConnection odbcConn)
                        {
                            using (var hda = new OdbcDataAdapter((OdbcCommand)cmd))
                            {
                                hda.Fill(dsData, "ds");
                            }
                        }
                        else if (conn is SqlConnection sqlConn)
                        {
                            using (var hda = new SqlDataAdapter((SqlCommand)cmd))
                            {
                                hda.Fill(dsData, "ds");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            return dsData;
        }

        public DataTable ExecuteDataTable(string sQry, List<IDataParameter> parameters = null)
        {
            DataTable dt = new DataTable();
            using (var conn = GetConnection())
            {
                try
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = sQry;
                        if (parameters != null)
                        {
                            foreach (var p in parameters)
                            {
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
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            return dt;
        }

        public List<decimal> ExecuteDecimalList(string query)
        {
            List<decimal> list = new List<decimal>();
            DataSet ds = getDataSet(query);

            if (ds != null && ds.Tables.Count > 0)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    list.Add(row[0] == DBNull.Value ? 0m : Convert.ToDecimal(row[0]));
                }
            }

            return list;
        }
    }
}
