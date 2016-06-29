using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;

namespace FastBar.Models
{

    public class EditProfileViewModel
    {

        [Required]
        [StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [Display(Name = "Credit Card Number")]
        [RegularExpression(@"^[0-9]{16,16}$", ErrorMessage = "Only numbers are allowed")]
        public string CCNumber { get; set; }

        [Required]
        [Display(Name = "Expiration Month")]
        [RegularExpression(@"^[0-9]{2,2}$", ErrorMessage = "Only numbers are allowed")]
        public string ExpirationMonth { get; set; }

        [Required]
        [RegularExpression(@"^[0-9]{4,4}$", ErrorMessage = "Only numbers are allowed")]
        [Display(Name = "Expiration Year")]
        public string ExpirationYear { get; set; }

        [Required]
        [RegularExpression(@"^[0-9]{3,3}$", ErrorMessage = "Only numbers are allowed")]
        [Display(Name = "CVC")]
        public string CVC { get; set; }

        public bool Success { get; set; }

    }
}