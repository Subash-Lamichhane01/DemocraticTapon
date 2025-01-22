
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

namespace DemocraticTapON.Models
{
    public class User
        {

            public int UserId { get; set; }

            [StringLength(50, ErrorMessage = "First name cannot be longer than 50 characters")]
            [Display(Name = "First Name")]
            public string FirstName { get; set; }

            [StringLength(50, ErrorMessage = "Last name cannot be longer than 50 characters")]
            [Display(Name = "Last Name")]
            public string LastName { get; set; }


            [Phone(ErrorMessage = "Invalid phone number format")]
            [Display(Name = "Phone Number")]
            public string PhoneNumber { get; set; }

            public int AccountId { get; set; }  // Foreign Key to Account
            public AccountModel Account { get; set; }

            // Navigation property
            public ICollection<UserBill> UserBill { get; set; }
        }
    }



