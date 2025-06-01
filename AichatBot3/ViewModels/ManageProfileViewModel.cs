using System.ComponentModel.DataAnnotations;

namespace AichatBot3.ViewModels
{
    public class ManageProfileViewModel
    {
        // Data to display
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsPhoneConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public string? ProfilePictureUrl { get; set; }

        // For uploading a new picture
        [Display(Name = "New Profile Picture")]
        public IFormFile? ProfilePictureFile { get; set; } // Nullable for display

        // For status messages (optional)
        public string? StatusMessage { get; set; }
    }
}