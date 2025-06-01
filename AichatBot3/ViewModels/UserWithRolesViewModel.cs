namespace AichatBot3.ViewModels
{
    public class UserWithRolesViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; }
        public bool TwoFactorEnabled  { get; set; }
        public List<string> Roles { get; set; }

    }
}
