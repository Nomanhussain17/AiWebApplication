namespace AichatBot3.ViewModels
{
    public class Enable2FAViewModel
    {
        public bool IsEmailConfirmed { get; set; }
        public string PhoneNumber { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public string Code { get; set; }

    }

}
