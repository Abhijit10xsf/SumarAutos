using System;

namespace SumarAuto.Data.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string AccountId { get; set; }
        public string CompanyName { get; set; }
        public string ContactPerson { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Phone { get; set; }
        public string TrnNumber { get; set; }
        public string TradeLicenseNumber { get; set; }
        public string Country { get; set; } = "UAE";
        public string City { get; set; } = "Dubai";
        public decimal AvailableCredit { get; set; } = 86420.00m;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CompanyName)) return "TP";
                var parts = CompanyName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();
                return (parts[0][0].ToString() + parts[1][0].ToString()).ToUpper();
            }
        }
    }
}
