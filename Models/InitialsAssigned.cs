using Google.Cloud.Firestore;

namespace JobTracker.Models
{
    [FirestoreData]
    public class InitialsAssigned
    {
        public InitialsAssigned(){}

        [FirestoreProperty]
        public string? InitialsAssignedId {get; set;}

        [FirestoreProperty]
        public required string InitialsAssignedName {get; set;}

        [FirestoreProperty]
        public required bool Active {get; set;}
    }
}