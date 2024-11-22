using DemocraticTapON.Models.DemocraticTapON.Models;

namespace DemocraticTapON.Models
{
    public class Vote
    {
        public int VoteId { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } // Reference to User class
        public int BillId { get; set; }
        public Bill Bill { get; set; } // Reference to Bill class
        public string VoteType { get; set; } // e.g., "Yes" or "No"
        public DateTime VoteDate { get; set; }

      
    }

}
