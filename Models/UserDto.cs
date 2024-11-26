using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace JobTracker.Models
{
    [FirestoreData]
    public class UserDto
    {
        [Required, EmailAddress]
        public required string Email { get; set; }

        [Required]
        public required string Password { get; set; }
    }
}