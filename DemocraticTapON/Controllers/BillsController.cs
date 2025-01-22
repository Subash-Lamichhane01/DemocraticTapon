using DemocraticTapON.Data;
using DemocraticTapON.Models;
using DemocraticTapON.Models.ViewModels;
using DemocraticTapON.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DemocraticTapON.Controllers
{
    [Authorize] // Allows both Users and Representatives
    public class BillsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBillStatisticsService _statisticsService;

        public BillsController(
            ApplicationDbContext context,
            IBillStatisticsService statisticsService)
        {
            _context = context;
            _statisticsService = statisticsService;
        }

        // GET: Bills/Index - Shows all bills
        public async Task<IActionResult> Index(int page = 1, string? searchTerm = null, string? status = null)
        {
            int pageSize = 10;
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.AccountId == accountId);

            if (user == null)
                return NotFound("User not found");

            // Start with all bills
            var query = _context.Bills
                .Include(b => b.ProposedByRep)
                .ThenInclude(r => r.User)
                .AsQueryable();

            // Apply filters if provided
            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(b => b.Title.Contains(searchTerm) || b.Description.Contains(searchTerm));

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<BillStatus>(status, out var billStatus))
                query = query.Where(b => b.Status == billStatus);

            // Order by most recent first
            query = query.OrderByDescending(b => b.ProposedDate);

            var totalBills = await query.CountAsync();
            var bills = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get user votes if the user is a regular user
            var userVotes = new Dictionary<int, Vote?>();
            if (User.IsInRole("User"))
            {
                userVotes = await _context.UserBills
                    .Where(ub => ub.UserId == user.UserId)
                    .ToDictionaryAsync(ub => ub.BillId, ub => ub.Vote);
            }

            var viewModel = new UserBillsViewModel
            {
                Bills = bills,
                UserVotes = userVotes,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalBills / (double)pageSize),
                SearchTerm = searchTerm,
                Status = status
            };

            return View(viewModel);
        }

        // GET: Bills/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.AccountId == accountId);

            if (user == null)
                return NotFound("User not found");

            var bill = await _context.Bills
                .Include(b => b.ProposedByRep)
                .ThenInclude(r => r.User)
                .Include(b => b.Documents)
                .Include(b => b.Comments)
                .ThenInclude(c => c.User) // Include comment author details
                .FirstOrDefaultAsync(b => b.BillId == id);

            if (bill == null)
                return NotFound();

            // Get user's vote if they're a regular user
            Vote? userVote = null;
            if (User.IsInRole("User"))
            {
                var vote = await _context.UserBills
                    .FirstOrDefaultAsync(ub => ub.UserId == user.UserId && ub.BillId == bill.BillId);
                userVote = vote?.Vote;
            }

            var viewModel = new BillDetailsViewModel
            {
                Bill = bill,
                Documents = bill.Documents.ToList(),
                VotingStats = await _statisticsService.CalculateBillVotingStatistics(bill),
                Comments = await _statisticsService.GetBillComments(bill.BillId),
                UserVote = userVote,
                IsUserRepresentative = User.IsInRole("Representative")
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddComment(int billId, string content, int? parentCommentId)
        {
            if (string.IsNullOrEmpty(content))
                return BadRequest("Comment content cannot be empty");

            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.AccountId == accountId);

            if (user == null)
                return NotFound("User not found");

            var comment = new BillComment
            {
                Content = content,
                BillId = billId,
                UserId = user.UserId,
                PostedDate = DateTime.UtcNow,
                ParentCommentId = parentCommentId
            };

            _context.BillComments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = billId });
        }
    }
}