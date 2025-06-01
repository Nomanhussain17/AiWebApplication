using System.ComponentModel.DataAnnotations;

namespace AichatBot3.ViewModels
{
    public class LoginWith2faViewModel
    {
        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Authentication Code")]
        public string TwoFactorCode { get; set; }

        [Display(Name = "Remember this machine")]
        public bool RememberMachine { get; set; }

        public bool RememberMe { get; set; }

        public string ReturnUrl { get; set; }

        //public string SelectedProvider { get; set; } // "Email" or "Authenticator"


    }

}
