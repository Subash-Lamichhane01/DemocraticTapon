using System.ComponentModel.DataAnnotations;

namespace DemocraticTapON.Models
{
    public class Bill
    {
        [Key]
        public int BillId { get; set; }
        public string Title { get; set; }
        public DateTime DateIntroduced { get; set; }
        public string Status { get; set; }

        // Navigation property
        public ICollection<UserBill> UserBill { get; set; }
    }
}
