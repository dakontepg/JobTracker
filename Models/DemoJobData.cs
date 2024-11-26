using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;

namespace JobTracker.Models
{
    [FirestoreData]
    public class DemoJobData
    {
        public DemoJobData(){}

        [FirestoreProperty]
        public string? JobDataId {get; set;}

        [FirestoreProperty]
        [Required(ErrorMessage = "Please enter a Job Number")]
        public required string JobNum {get; set;}
        
        [FirestoreProperty]
        [Required(ErrorMessage = "Please select a Start Time")]
        public required string StartTime {get; set;}

        [FirestoreProperty]
        [Required(ErrorMessage = "Please select an End Time")]
        public required string EndTime {get; set;}

        [FirestoreProperty]
        [Required(ErrorMessage = "Please enter the Quantity")]
        public required int Quantity{get; set;}

        [FirestoreProperty]
        [Required(ErrorMessage = "Please select a Date")]
        public required DateTime DateToday {get; set;}= DateTime.Now; // default to today's date

        [FirestoreProperty]
        public double? Minutes {get; set;}

        [BindProperty]
        [FirestoreProperty]
        [Required(ErrorMessage = "Please select an Operation")]
        public required int JobOpId {get; set;}

        public required virtual JobOp JobOp {get; set;}

        [BindProperty]
        [FirestoreProperty]
        [Required(ErrorMessage = "Please select your initials")]
        public required string InitialsAssignedId {get; set;}

        public required virtual InitialsAssigned InitialsAssigned {get; set;}    
    

    }
}