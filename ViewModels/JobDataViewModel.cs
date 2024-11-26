using JobTracker.Models;

namespace JobTracker.ViewModels
{
    public class JobDataViewModel
    {
        public required DemoJobData DemoJobData {get; set;}

        public required JobOp JobOp {get; set;}

        public required InitialsAssigned InitialsAssigned {get; set;}

        public JobDataViewModel(){}

        public JobDataViewModel(DemoJobData demoJobData, JobOp jobOp, InitialsAssigned initialsAssigned)
        {
            this.DemoJobData = demoJobData;
            this.JobOp = jobOp;
            this.InitialsAssigned = initialsAssigned;
        }
    }
}