using System.ComponentModel.DataAnnotations;

namespace AichatBot3.ViewModels
{
    public class EditRoleViewModel
    {
        public string Id { get; set; } // To identify the role being edited

        [Required]
        [Display(Name = "Role Name")]
        public string RoleName { get; set; }
    }
}
