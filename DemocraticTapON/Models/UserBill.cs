using DemocraticTapON.Models;
using DemocraticTapON.Models.DemocraticTapON.Models;

namespace DemocraticTapON.Models
{
    public class UserBill
    {
        public int UserBillId { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } // Make sure User is properly recognized here
        public int BillId { get; set; }
        public Bill Bill { get; set; } // Make sure Bill is properly recognized here
    }
}
