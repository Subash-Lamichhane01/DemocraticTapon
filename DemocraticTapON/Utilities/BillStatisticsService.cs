using DemocraticTapON.Data;
using DemocraticTapON.Models;
using DemocraticTapON.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace DemocraticTapON.Utilities
{
    public interface IBillStatisticsService
    {
        Task<VotingStatistics> GetRepresentativeVotingStatistics(int representativeId);
        Task<VotingStatistics> CalculateBillVotingStatistics(Bill bill);
        Task<List<BillCommentViewModel>> GetBillComments(int billId);
    }

    public class BillStatisticsService : IBillStatisticsService
    {
        private readonly ApplicationDbContext _context;

        public BillStatisticsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<VotingStatistics> GetRepresentativeVotingStatistics(int representativeId)
        {
            var bills = await _context.Bills
                .Where(b => b.ProposedByRepId == representativeId)
                .Include(b => b.UserBills)
                .ToListAsync();

            var totalEligibleVoters = await _context.Users
                .CountAsync(u => u.Account.Role == UserRole.User);

            int totalVotes = 0;
            int yesVotes = 0;
            int noVotes = 0;

            foreach (var bill in bills)
            {
                totalVotes += bill.UserBills.Count(ub => ub.Vote.HasValue);
                yesVotes += bill.UserBills.Count(ub => ub.Vote == Vote.Yes);
                noVotes += bill.UserBills.Count(ub => ub.Vote == Vote.No);
            }

            return new VotingStatistics
            {
                TotalVotes = totalVotes,
                YesVotes = yesVotes,
                NoVotes = noVotes,
                TotalEligibleVoters = totalEligibleVoters
            };
        }

        public async Task<VotingStatistics> CalculateBillVotingStatistics(Bill bill)
        {
            // Ensure UserBills are loaded
            if (!_context.Entry(bill).Collection(b => b.UserBills).IsLoaded)
            {
                await _context.Entry(bill).Collection(b => b.UserBills).LoadAsync();
            }

            var totalEligibleVoters = await _context.Users
                .CountAsync(u => u.Account.Role == UserRole.User);

            var yesVotes = bill.UserBills.Count(ub => ub.Vote == Vote.Yes);
            var noVotes = bill.UserBills.Count(ub => ub.Vote == Vote.No);
            var totalVotes = yesVotes + noVotes;

            return new VotingStatistics
            {
                TotalVotes = totalVotes,
                YesVotes = yesVotes,
                NoVotes = noVotes,
                TotalEligibleVoters = totalEligibleVoters
            };
        }

        public async Task<List<BillCommentViewModel>> GetBillComments(int billId)
        {
            return await _context.BillComments
                .Where(c => c.BillId == billId && c.ParentCommentId == null)
                .Include(c => c.User)
                .Include(c => c.Replies)
                    .ThenInclude(r => r.User)
                .OrderByDescending(c => c.PostedDate)
                .Select(c => new BillCommentViewModel
                {
                    CommentId = c.CommentId,
                    Content = c.Content,
                    UserName = c.User.FirstName + " " + c.User.LastName,
                    PostedDate = c.PostedDate,
                    Replies = c.Replies.Select(r => new BillCommentViewModel
                    {
                        CommentId = r.CommentId,
                        Content = r.Content,
                        UserName = r.User.FirstName + " " + r.User.LastName,
                        PostedDate = r.PostedDate
                    }).OrderBy(r => r.PostedDate).ToList()
                })
                .ToListAsync();
        }
    }
}