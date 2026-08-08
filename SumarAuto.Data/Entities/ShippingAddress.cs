namespace SumarAuto.Data.Entities
{
    public class ShippingAddress
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string AddressTitle { get; set; }
        public string RecipientName { get; set; }
        public string Phone { get; set; }
        public string StreetAddress { get; set; }
        public string City { get; set; }
        public string Emirate { get; set; }
        public string Country { get; set; } = "UAE";
        public bool IsDefault { get; set; } = true;
    }
}
