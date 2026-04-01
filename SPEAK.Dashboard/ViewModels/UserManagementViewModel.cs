namespace SPEAK.Dashboard.ViewModels
{
    public class UserManagementViewModel
    {
        public List<UserManagementItemViewModel> Users { get; set; } = new();
    }

    public class UserManagementItemViewModel
    {
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string Status { get; set; } = null!;   // "Active" or "Disabled"
        public DateTime CreatedAt { get; set; }
    }
}
