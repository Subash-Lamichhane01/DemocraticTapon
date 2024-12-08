using System.ComponentModel.DataAnnotations;

namespace DemocraticTapON.Models
{
    public class AccountModel
    {
        // Primary Key for database
        public int Id { get; set; }

        // Username - Required for both Login and Signup
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
        public string Username { get; set; }

        // Password - Required for both Login and Signup
        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        public string Password { get; set; }

        // Optional signup fields - not required for login
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [StringLength(50, ErrorMessage = "First name cannot be longer than 50 characters")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [StringLength(50, ErrorMessage = "Last name cannot be longer than 50 characters")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }
    }
}