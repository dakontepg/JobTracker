using System.Collections.Generic;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;

namespace JobTracker.Models
{
    [FirestoreData]
    public class User
    {
        [FirestoreProperty]
        public required string Uid {get; set;}

        [FirestoreProperty]
        public required string Email {get; set;}

        [BindProperty]
        [FirestoreProperty]
        public List<string> Roles {get; set;}

        public virtual Role Role {get; set;}
    }
}