using System.ComponentModel.DataAnnotations;

namespace AichatBot3.ViewModels
{
    public class EnableAuthenticatorViewModel
    {
        public string SharedKey { get; set; }
        public string AuthenticatorUri { get; set; }
        [Required]
        [Display(Name = "Verification Code")]
        public string Code { get; set; }
    }
}
