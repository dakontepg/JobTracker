using Google.Cloud.Firestore;

namespace JobTracker.Models
{
    [FirestoreData]
    public class Role
    {
        public Role(){}

        [FirestoreProperty]
        public required string RoleId {get; set;}

        [FirestoreProperty]
        public required string RoleName {get; set;}
    }
}