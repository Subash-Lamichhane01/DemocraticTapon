using DemocraticTapON.Models.ViewModels;
using DemocraticTapON.Models;

public class RepresentativeDashboardViewModel
{
    public List<Bill> MyBills { get; set; }

    public Representative RepresentativeInfo { get; set; }
    public VotingStatistics VotingStatistics { get; set; }
}
