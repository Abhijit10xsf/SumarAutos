using System;

namespace SumarAuto.Data.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string EmailId { get; set; }

        public string Email
        {
            get => EmailId;
            set => EmailId = value;
        }

        // Display compatibility accessors
        public string AccountId => Username;
        public string CompanyName { get => Username; set { } }
        public string ContactPerson { get => Username; set { } }
        public string Phone { get => ""; set { } }
        public string TrnNumber { get => ""; set { } }
        public string TradeLicenseNumber { get => ""; set { } }
        public string Country => "UAE";
        public string City { get => "Dubai"; set { } }
        public decimal AvailableCredit => 0.00m;
        public DateTime CreatedAt => DateTime.Now;
        public bool IsActive => true;

        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Username)) return "U";
                return Username.Substring(0, Math.Min(2, Username.Length)).ToUpper();
            }
        }
    }
}
