namespace AichatBot3.ViewModels
{
    public class UserTableViewModel
    {
        public List<UserWithRolesViewModel> Users { get; set; } // List for the current page
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalItemCount { get; set; }
        // Calculate total pages (using integer division trick)
        public int TotalPages => (TotalItemCount + PageSize - 1) / PageSize;
        public string CurrentSearchTerm { get; set; }

        public UserTableViewModel() // Initialize list
        {
            Users = new List<UserWithRolesViewModel>();
        }
    }
}