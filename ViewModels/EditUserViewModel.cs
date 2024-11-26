namespace JobTracker.ViewModels
{
    public class EditUserViewModel
    {
        public string Uid { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; }
        public List<string> AllRoles { get; set; } // A list of all available roles to choose from
    }
}
