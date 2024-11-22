namespace DemocraticTapON.Models
{
    using System.Collections.Generic;

    namespace DemocraticTapON.Models
    {
        public class User
        {
            public int UserId { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public string PasswordHash { get; set; }
            public bool EmailVerified { get; set; }
            public DateTime RegistrationDate { get; set; }
            public DateTime? LastLoginDate { get; set; }

            // Navigation property
            public ICollection<UserBill> UserBill { get; set; }
        }
    }


}
