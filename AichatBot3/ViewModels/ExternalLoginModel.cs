using System.ComponentModel.DataAnnotations;

namespace AichatBot3.ViewModels
{
    public class ExternalLoginModel
    {
        [Required]
        public string? Email { get; set; }

        [Required]
        public string? ProviderDisplayName { get; set; }

        public string? ReturnUrl { get; set; }


    }

}

