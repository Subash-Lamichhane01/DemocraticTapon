﻿using DemocraticTapON.Models;
using System.ComponentModel.DataAnnotations;

namespace DemocraticTapON.Models
{
    public class AccountModel
    {
        // Primary Key for database
        public int Id { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email Address")]
        public string Email { get; set; }
        
        [Display(Name = "Email Verified")]
        public bool IsEmailVerified { get; set; } = false;

        // Optional: Store the last verification date
        [Display(Name = "Last Verification Date")]
        public DateTime? LastVerificationDate { get; set; }

        public User User { get; set; }

        [Required]
        public UserRole Role { get; set; }
    }
}