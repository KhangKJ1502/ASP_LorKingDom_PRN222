namespace BLL.DTOs
{
    public class CheckoutDto
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";

        // Address
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }

        // Shipping & Payment
        public string ShippingMethod { get; set; } = "standard"; // standard, express
        public string PaymentMethod { get; set; } = "cod"; // cod
        public string? PromoCode { get; set; }
    }
}
