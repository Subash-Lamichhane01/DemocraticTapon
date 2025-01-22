using System.ComponentModel.DataAnnotations;

namespace DemocraticTapON.Models.ViewModels
{
    public class EditProfileViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [StringLength(100)]
        public string Constituency { get; set; }

        [Required]
        [StringLength(50)]
        public string State { get; set; }

        [Required]
        [Display(Name = "Political Party")]
        [StringLength(100)]
        public string PoliticalParty { get; set; }

        [Required]
        [Display(Name = "Term Start Date")]
        [DataType(DataType.Date)]
        public DateTime TermStart { get; set; }

        [Required]
        [Display(Name = "Term End Date")]
        [DataType(DataType.Date)]
        public DateTime TermEnd { get; set; }

        [Phone]
        [Display(Name = "Office Phone")]
        public string? OfficePhone { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Office Address")]
        public string OfficeAddress { get; set; }
    }
}
