using System;
using System.Data;
using System.Linq;
using SumarAuto.Data.Interfaces;
using SumarAuto.Data.Entities;

namespace SumarAuto.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly SumarDbContext _db;
        private readonly HanaDataHelper _hanaHelper;

        public UserRepository()
        {
            _db = new SumarDbContext();
            _hanaHelper = new HanaDataHelper();
        }

        public UserRepository(SumarDbContext db)
        {
            _db = db ?? new SumarDbContext();
            _hanaHelper = new HanaDataHelper();
        }

        public User Authenticate(string emailOrAccount, string password)
        {
            if (string.IsNullOrWhiteSpace(emailOrAccount) || string.IsNullOrWhiteSpace(password))
                return null;

            var term = emailOrAccount.Trim();

            // Try SAP B1 Business Partner (OCRD) HANA database login first
            try
            {
                string hanaQuery = @"
SELECT 
    T0.""DocEntry"" AS ""Id"",
    T0.""CardCode"" AS ""AccountId"",
    T0.""CardName"" AS ""CompanyName"",
    COALESCE(T0.""CntctPrsn"", '') AS ""ContactPerson"",
    COALESCE(T0.""E_Mail"", '') AS ""Email"",
    COALESCE(T0.""Password"", '') AS ""Password"",
    COALESCE(T0.""Phone1"", '') AS ""Phone"",
    COALESCE(T0.""LicTradNum"", '') AS ""TradeLicenseNumber"",
    COALESCE(T0.""City"", 'Dubai') AS ""City"",
    COALESCE(T0.""Country"", 'UAE') AS ""Country"",
    COALESCE(T0.""CreditLine"", 50000.00) AS ""AvailableCredit"",
    T0.""validFor"" AS ""ValidFor""
FROM ""OCRD"" T0
WHERE T0.""CardType"" = 'C'
  AND (LOWER(T0.""E_Mail"") = '" + term.ToLower() + @"' OR LOWER(T0.""CardCode"") = '" + term.ToLower() + @"')";

                DataTable dt = _hanaHelper.ExecuteDataTable(hanaQuery);
                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    string dbPassword = row["Password"]?.ToString() ?? "";
                    string validFor = row["ValidFor"]?.ToString() ?? "Y";

                    if (validFor.Equals("Y", StringComparison.OrdinalIgnoreCase))
                    {
                        // If password is set in SAP B1 OCRD, validate it; if blank in SAP B1 DB, allow standard user authentication
                        if (string.IsNullOrEmpty(dbPassword) || dbPassword.Equals(password, StringComparison.Ordinal))
                        {
                            return MapRowToUser(row, password);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("HANA User Authenticate Exception: " + ex.Message);
            }

            // Fallback to local DB authentication
            var termLower = term.ToLower();
            return _db.Users.FirstOrDefault(u =>
                (u.Email.ToLower() == termLower || u.AccountId.ToLower() == termLower) &&
                u.Password == password &&
                u.IsActive
            );
        }

        public User GetUserByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;

            try
            {
                string hanaQuery = @"
SELECT 
    T0.""DocEntry"" AS ""Id"",
    T0.""CardCode"" AS ""AccountId"",
    T0.""CardName"" AS ""CompanyName"",
    COALESCE(T0.""CntctPrsn"", '') AS ""ContactPerson"",
    COALESCE(T0.""E_Mail"", '') AS ""Email"",
    COALESCE(T0.""Password"", '') AS ""Password"",
    COALESCE(T0.""Phone1"", '') AS ""Phone"",
    COALESCE(T0.""LicTradNum"", '') AS ""TradeLicenseNumber"",
    COALESCE(T0.""City"", 'Dubai') AS ""City"",
    COALESCE(T0.""Country"", 'UAE') AS ""Country"",
    COALESCE(T0.""CreditLine"", 50000.00) AS ""AvailableCredit"",
    T0.""validFor"" AS ""ValidFor""
FROM ""OCRD"" T0
WHERE T0.""CardType"" = 'C' AND LOWER(T0.""E_Mail"") = '" + email.Trim().ToLower() + @"'";

                DataTable dt = _hanaHelper.ExecuteDataTable(hanaQuery);
                if (dt != null && dt.Rows.Count > 0)
                {
                    return MapRowToUser(dt.Rows[0]);
                }
            }
            catch { }

            var term = email.Trim().ToLower();
            return _db.Users.FirstOrDefault(u => u.Email.ToLower() == term);
        }

        public User GetUserById(int id)
        {
            try
            {
                string hanaQuery = @"
SELECT 
    T0.""DocEntry"" AS ""Id"",
    T0.""CardCode"" AS ""AccountId"",
    T0.""CardName"" AS ""CompanyName"",
    COALESCE(T0.""CntctPrsn"", '') AS ""ContactPerson"",
    COALESCE(T0.""E_Mail"", '') AS ""Email"",
    COALESCE(T0.""Password"", '') AS ""Password"",
    COALESCE(T0.""Phone1"", '') AS ""Phone"",
    COALESCE(T0.""LicTradNum"", '') AS ""TradeLicenseNumber"",
    COALESCE(T0.""City"", 'Dubai') AS ""City"",
    COALESCE(T0.""Country"", 'UAE') AS ""Country"",
    COALESCE(T0.""CreditLine"", 50000.00) AS ""AvailableCredit"",
    T0.""validFor"" AS ""ValidFor""
FROM ""OCRD"" T0
WHERE T0.""CardType"" = 'C' AND T0.""DocEntry"" = " + id;

                DataTable dt = _hanaHelper.ExecuteDataTable(hanaQuery);
                if (dt != null && dt.Rows.Count > 0)
                {
                    return MapRowToUser(dt.Rows[0]);
                }
            }
            catch { }

            return _db.Users.FirstOrDefault(u => u.Id == id);
        }

        public bool Register(User newUser, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (newUser == null)
            {
                errorMessage = "Invalid user details.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(newUser.Email))
            {
                errorMessage = "Email address is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(newUser.CompanyName))
            {
                errorMessage = "Company Name is required.";
                return false;
            }

            if (_db.Users.Any(u => u.Email.Equals(newUser.Email, StringComparison.OrdinalIgnoreCase)))
            {
                errorMessage = "An account with this email address already exists.";
                return false;
            }

            int maxId = _db.Users.Any() ? _db.Users.Max(u => u.Id) : 0;
            newUser.AccountId = $"B2B-{10950 + maxId + 1}";
            newUser.AvailableCredit = 50000.00m;
            newUser.CreatedAt = DateTime.Now;
            newUser.IsActive = true;

            _db.Users.Add(newUser);
            _db.SaveChanges();
            return true;
        }

        private User MapRowToUser(DataRow row, string fallbackPassword = "")
        {
            int id = Convert.ToInt32(row["Id"] != DBNull.Value ? row["Id"] : 1);
            string accountId = row["AccountId"]?.ToString() ?? "B2B-10951";

            if (id == 0)
            {
                id = Math.Abs(accountId.GetHashCode());
            }

            return new User
            {
                Id = id,
                AccountId = accountId,
                CompanyName = row["CompanyName"]?.ToString() ?? "B2B Client",
                ContactPerson = row["ContactPerson"]?.ToString() ?? "",
                Email = row["Email"]?.ToString() ?? "",
                Password = !string.IsNullOrEmpty(row["Password"]?.ToString()) ? row["Password"].ToString() : fallbackPassword,
                Phone = row["Phone"]?.ToString() ?? "",
                TradeLicenseNumber = row["TradeLicenseNumber"]?.ToString() ?? "",
                City = row["City"]?.ToString() ?? "Dubai",
                Country = row["Country"]?.ToString() ?? "UAE",
                AvailableCredit = Convert.ToDecimal(row["AvailableCredit"] != DBNull.Value ? row["AvailableCredit"] : 50000.00m),
                IsActive = true,
                CreatedAt = DateTime.Now
            };
        }
    }
}
