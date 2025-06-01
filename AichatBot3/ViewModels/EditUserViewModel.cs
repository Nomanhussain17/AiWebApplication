using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AichatBot3.ViewModels
{
    public class EditUserViewModel
    {
        public string Id { get; set; } // Hidden field

        [Required]
        [Display(Name = "Username")]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Email Confirmed")]
        public bool EmailConfirmed { get; set; }

        // For displaying existing roles and populating checkboxes
        public List<SelectListItem> AllRoles { get; set; }

        // For receiving the selected roles from the form
        public List<string> SelectedRoles { get; set; }

        public EditUserViewModel()
        {
            AllRoles = new List<SelectListItem>();
            SelectedRoles = new List<string>();
        }
    }
}
