﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemocraticTapON.Models
{
    public class Representative
    {
        [Key]
        public int RepresentativeId { get; set; }

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

        // Office contact details
        [Phone]
        [Display(Name = "Office Phone")]
        public string? OfficePhone { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Office Address")]
        public string OfficeAddress { get; set; }

        // Link to User model
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        // Navigation property for Bills proposed by this representative
        public virtual ICollection<Bill> ProposedBills { get; set; }

        // Calculated property for active status
        [NotMapped]
        public bool IsActive => DateTime.Now >= TermStart && DateTime.Now <= TermEnd;

        public Representative()
        {
            ProposedBills = new List<Bill>();
        }
    }
}