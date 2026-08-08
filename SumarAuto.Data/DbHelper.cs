using System;
using System.Configuration;
using System.Data.SqlClient;

namespace SumarAuto.Data
{
    public static class DbHelper
    {
        private static readonly string FallbackConnStr = @"Data Source=MI-LAPTOP\MSSQLSERVER02;Initial Catalog=SumarAuto;Persist Security Info=True;User ID=sa;Password=sa123;TrustServerCertificate=True;Pooling=True;";

        public static string ConnectionString
        {
            get
            {
                try
                {
                    var settings = ConfigurationManager.ConnectionStrings["DBEntities"];
                    if (settings != null && !string.IsNullOrWhiteSpace(settings.ConnectionString))
                    {
                        return settings.ConnectionString;
                    }
                }
                catch { }
                return FallbackConnStr;
            }
        }

        public static SqlConnection GetConnection()
        {
            var conn = new SqlConnection(ConnectionString);
            conn.Open();
            return conn;
        }
    }
}
