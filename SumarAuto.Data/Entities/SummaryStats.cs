namespace SumarAuto.Data.Entities
{
    public class SummaryStats
    {
        public int AvailableProducts { get; set; }
        public int ReadyStock { get; set; }
        public int InTransit { get; set; }
        public int SpecialOffers { get; set; }
        public decimal AvailableCredit { get; set; }
        public string Currency { get; set; } = "AED";
        public string UserCompanyName { get; set; } = "Al Khaleej Auto";
        public string UserInitials { get; set; } = "AK";
    }
}
