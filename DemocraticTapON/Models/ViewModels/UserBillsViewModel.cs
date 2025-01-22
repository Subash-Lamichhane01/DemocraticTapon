using DemocraticTapON.Models;

namespace DemocraticTapON.Models.ViewModels
{
    public class UserBillsViewModel
    {
        public IEnumerable<Bill> Bills { get; set; }
        public Dictionary<int, Vote?> UserVotes { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public IEnumerable<BillStatus> AvailableStatuses => Enum.GetValues<BillStatus>();
    }
}