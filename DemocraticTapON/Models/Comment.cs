using System;
using DemocraticTapON.Models;
using DemocraticTapON.Models.DemocraticTapON.Models;

namespace DemocraticTapON.Models
{
    public class Comment
    {
        public int CommentId { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } // Reference to the User class

        public int BillId { get; set; }
        public Bill Bill { get; set; } // Reference to the Bill class

        public string Content { get; set; }
        public DateTime CommentDate { get; set; }
    }
}
