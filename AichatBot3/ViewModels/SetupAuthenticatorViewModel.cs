namespace AichatBot3.ViewModels
{
    public class SetupAuthenticatorViewModel
    {
        public string SharedKey { get; set; }
        public string AuthenticatorUri { get; set; }
        public string QrCodeImageUrl { get; set; }
        public string Code { get; set; } // The 6-digit TOTP code user enters

    }
}
