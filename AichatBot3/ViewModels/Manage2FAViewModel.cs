namespace AichatBot3.ViewModels
{
    public class Manage2FAViewModel
    {
        public bool Is2FAEnabled { get; set; }
        public string? Preferred2FAMethod { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public string? QRCodeImageUrl { get; set; }
        public string? SecretKey { get; set; }
    }
}
