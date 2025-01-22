using System.ComponentModel.DataAnnotations;
using DemocraticTapON.Models;
using DemocraticTapON.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
public class BillCreateViewModel
{
            [Required]
            [StringLength(200)]
            public string Title { get; set; }

            [Required]
            public string Description { get; set; }

            [Required]
            [Display(Name = "Voting End Date")]
            [DataType(DataType.Date)]
            public DateTime VotingEndDate { get; set; }

            [Display(Name = "Supporting Documents")]
            public IFormFileCollection Documents { get; set; }
        }

        public class BillDetailsViewModel
        {
            public Bill Bill { get; set; }
    public bool HasUserVoted => UserVote.HasValue;
            public Vote? UserVote { get; set; }
            public List<BillDocument> Documents { get; set; }
            public List<BillCommentViewModel> Comments { get; set; }
            public VotingStatistics VotingStats { get; set; }
    public bool IsUserRepresentative { get; set; }
    public List<SelectListItem> StatusList { get; set; } = new List<SelectListItem>();


}

        public class BillCommentViewModel
        {
            public int CommentId { get; set; }
            public string Content { get; set; }
            public string UserName { get; set; }
            public DateTime PostedDate { get; set; }
    public List<BillCommentViewModel> Replies { get; set; } = new List<BillCommentViewModel>();
        }

public class VotingStatistics
{
    public int TotalVotes { get; set; }
    public int YesVotes { get; set; }
    public int NoVotes { get; set; }
    public int TotalEligibleVoters { get; set; }
    public double YesPercentage => TotalVotes == 0 ? 0 : (YesVotes * 100.0 / TotalVotes);
    public double NoPercentage => TotalVotes == 0 ? 0 : (NoVotes * 100.0 / TotalVotes);
    public double VoterTurnout => TotalEligibleVoters == 0 ? 0 : (TotalVotes * 100.0 / TotalEligibleVoters);
}

public class MyBillsViewModel
    {
        public List<Bill> Bills { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        // Helper properties for pagination
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

