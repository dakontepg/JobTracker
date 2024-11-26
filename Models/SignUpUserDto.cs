using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace JobTracker.Models
{
    [FirestoreData]
    public class SignUpUserDto
    {
        public SignUpUserDto(){}

        [Required, EmailAddress]
        public required string Email {get; set;}

        [Required]
        public required string Password {get; set;}

        [Required, Compare(nameof(Password), ErrorMessage = "The passwords didn't match")]
        public required string ConfirmPassword {get; set;}
    }
}