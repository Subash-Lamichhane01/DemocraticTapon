using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemocraticTapON.Models
{
    public class Bill
    {
        [Key]
        public int BillId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public DateTime ProposedDate { get; set; }

        [Required]
        public DateTime VotingEndDate { get; set; }

        public BillStatus Status { get; set; }

        [Required]
        public int ProposedByRepId { get; set; }  // FK to User (Representative)

        public Representative ProposedByRep { get; set; }  // Navigation property

        // Summary of votes
        public int YesVotes { get; set; }
        public int NoVotes { get; set; }

        // Navigation properties
        public ICollection<UserBill> UserBills { get; set; }
        public ICollection<BillDocument> Documents { get; set; }
        public ICollection<BillComment> Comments { get; set; }
    }

    public enum BillStatus
    {
        Pending,    // Just created, not yet open for voting
        Active,     // Currently open for voting
        Closed,     // Voting period ended
        Enacted,    // Bill passed and implemented
        Rejected    // Bill failed to pass
    }

    public class UserBill
    {
        [Key]
        public int UserId { get; set; }

        [Key]
        public int BillId { get; set; }

        public Vote? Vote { get; set; }
        public DateTime? VoteDate { get; set; }

        // Navigation properties
        public User User { get; set; }
        public Bill Bill { get; set; }
    }

    public enum Vote
    {
        Yes = 0,
        No = 1
    }

    public class BillDocument
    {
        [Key]
        public int DocumentId { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string FileUrl { get; set; }

        public string Description { get; set; }

        public DateTime UploadDate { get; set; }

        public int BillId { get; set; }
        public Bill Bill { get; set; }
    }

    public class BillComment
    {
        [Key]
        public int CommentId { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime PostedDate { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public int BillId { get; set; }
        public Bill Bill { get; set; }

        public int? ParentCommentId { get; set; }
        public BillComment ParentComment { get; set; }

        public ICollection<BillComment> Replies { get; set; }
    }
}