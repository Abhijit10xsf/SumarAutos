using System;
using System.Data;
using SumarAuto.Data.Interfaces;
using SumarAuto.Data.Entities;

namespace SumarAuto.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly HanaDataHelper _hanaHelper;

        public UserRepository()
        {
            _hanaHelper = new HanaDataHelper();
        }

        public UserRepository(SumarDbContext db)
        {
            _hanaHelper = new HanaDataHelper();
        }

        public User Authenticate(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return null;

            var term = username.Trim();

            // 1. Try SAP B1 Users (OUSR) database login using Username and Password
            try
            {
                string ousrQuery = @"
SELECT 
    T0.""USERID"" AS ""Id"",
    T0.""USER_CODE"" AS ""Username"",
    COALESCE(T0.""E_Mail"", '') AS ""EmailId"",
    COALESCE(T0.""Password"", '') AS ""Password""
FROM ""OUSR"" T0
WHERE LOWER(T0.""USER_CODE"") = '" + term.ToLower() + @"' OR LOWER(T0.""E_Mail"") = '" + term.ToLower() + @"'";

                DataTable dt = _hanaHelper.ExecuteDataTable(ousrQuery);
                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    string dbPassword = row["Password"]?.ToString() ?? "";
                    if (string.IsNullOrEmpty(dbPassword) || dbPassword.Equals(password, StringComparison.Ordinal))
                    {
                        return MapRowToUser(row, password);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("HANA OUSR Login Exception: " + ex.Message);
            }

            // 2. Try SAP B1 Business Partner (OCRD) database login using Username and Password
            try
            {
                string ocrdQuery = @"
SELECT 
    T0.""DocEntry"" AS ""Id"",
    T0.""CardCode"" AS ""Username"",
    COALESCE(T0.""E_Mail"", '') AS ""EmailId"",
    COALESCE(T0.""Password"", '') AS ""Password""
FROM ""OCRD"" T0
WHERE T0.""CardType"" = 'C'
  AND (LOWER(T0.""CardCode"") = '" + term.ToLower() + @"' OR LOWER(T0.""E_Mail"") = '" + term.ToLower() + @"')";

                DataTable dt = _hanaHelper.ExecuteDataTable(ocrdQuery);
                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    string dbPassword = row["Password"]?.ToString() ?? "";
                    if (string.IsNullOrEmpty(dbPassword) || dbPassword.Equals(password, StringComparison.Ordinal))
                    {
                        return MapRowToUser(row, password);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("HANA OCRD Login Exception: " + ex.Message);
            }

            return null;
        }

        private User MapRowToUser(DataRow row, string fallbackPassword = "")
        {
            int id = Convert.ToInt32(row["Id"] != DBNull.Value ? row["Id"] : 1);
            string username = row["Username"]?.ToString() ?? "SAP_User";
            string password = !string.IsNullOrEmpty(row["Password"]?.ToString()) ? row["Password"].ToString() : fallbackPassword;
            string emailId = row.Table.Columns.Contains("EmailId") ? (row["EmailId"]?.ToString() ?? "") : "";

            return new User
            {
                Id = id,
                Username = username,
                Password = password,
                EmailId = emailId
            };
        }
    }
}
