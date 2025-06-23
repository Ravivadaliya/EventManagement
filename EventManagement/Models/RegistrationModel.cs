using System;
using System.ComponentModel.DataAnnotations;

namespace EventManagement.Models
{
    public class RegistrationModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "First Name is required")]
        [StringLength(50, ErrorMessage = "First Name cannot exceed 50 characters")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last Name is required")]
        [StringLength(50, ErrorMessage = "Last Name cannot exceed 50 characters")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Guest count is required")]
        [Range(1, 100, ErrorMessage = "Guest count must be between 1 and 100")]
        public int GuestCount { get; set; }
        
        [Required(ErrorMessage = "Contact number is required")]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit Indian mobile number")]
        public string ContactNo { get; set; }


        //[Required(ErrorMessage = "Event date is required")]
        //[DataType(DataType.Date)]
        //[Display(Name = "Event Date")]
        //public DateTime EventDate { get; set; }

        //[Required(ErrorMessage = "People Names is required")]
        //[StringLength(50, ErrorMessage = "People Names is Required")]
        //public string PeopleNames { get; set; }

        public string UniqueCode { get; set; } // Generated on the backend, no need to validate
    }
}
