using JobTracker.Models;

namespace JobTracker.ViewModels
{
    public class AdministrationViewModel
    {
        public JobOp JobOp {get; set;}
        public Role Role {get; set;}
        public User User {get; set;}
        public InitialsAssigned InitialsAssigned {get; set;}

        public AdministrationViewModel(){}

        public AdministrationViewModel(JobOp jobOp)
        {
            this.JobOp = jobOp;
        }

        public AdministrationViewModel(Role role)
        {
            this.Role = role;
        }

        public AdministrationViewModel(User user)
        {
            this.User = user;
        }

        public AdministrationViewModel(InitialsAssigned initialsAssigned)
        {
            this.InitialsAssigned = initialsAssigned;
        }

    }

}