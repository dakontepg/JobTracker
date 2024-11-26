using Google.Cloud.Firestore;

namespace JobTracker.Models
{
    [FirestoreData]
    public class JobOp
    {
        public JobOp(){}

        [FirestoreProperty]
        public required int Id {get; set;}

        [FirestoreProperty]
        public required string OpName {get; set;}

        [FirestoreProperty]
        public required bool Active {get; set;}
    }
}

