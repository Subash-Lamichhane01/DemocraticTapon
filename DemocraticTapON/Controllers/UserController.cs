using DemocraticTapON.Data;
using DemocraticTapON.Models;
using DemocraticTapON.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System.Security.Claims;
using DemocraticTapON.Models.ViewModels;

namespace DemocraticTapON.Controllers
{
    [Authorize(Roles = "User")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBillStatisticsService _statisticsService;

        public UserController(ApplicationDbContext context, IBillStatisticsService statisticsService)
        {
            _context = context;
            _statisticsService = statisticsService;
        }

        public async Task<IActionResult> Bills(int page = 1)
        {
            int pageSize = 10;
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var user = await _context.Users
            .FirstOrDefaultAsync(u => u.AccountId == accountId);

            if (user == null)
            {
                return NotFound("User not found");
            }

            var bills = await _context.Bills
                .Include(b => b.ProposedByRep)
                .ThenInclude(r => r.User)
                .Include(b => b.UserBills)
                .Where(b => b.Status == BillStatus.Active)
                .OrderByDescending(b => b.ProposedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userVotes = await _context.UserBills
                .Where(ub => ub.UserId == user.UserId)
                .ToDictionaryAsync(ub => ub.BillId, ub => ub.Vote);

            var viewModel = new UserBillsViewModel
            {
                Bills = bills,
                UserVotes = userVotes,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(await _context.Bills.CountAsync() / (double)pageSize)
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Vote(int billId, Models.Vote vote)
        {
            var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var user = await _context.Users
          .FirstOrDefaultAsync(u => u.AccountId == accountId);

            if (user == null)
            {
                return BadRequest("User not found");
            }

            var bill = await _context.Bills
            .Include(b => b.UserBills)
            .FirstOrDefaultAsync(b => b.BillId == billId);


            if (bill == null || bill.Status != BillStatus.Active)
            {
                return BadRequest("Invalid bill or voting is closed");
            }

            var existingVote = await _context.UserBills
                .FirstOrDefaultAsync(ub => ub.UserId == user.UserId && ub.BillId == billId);

            if (existingVote != null)
            {
                // Update existing vote
                if (existingVote.Vote != vote)
                {
                    // Remove old vote count
                    if (existingVote.Vote == Models.Vote.Yes)
                        bill.YesVotes--;
                    else
                        bill.NoVotes--;

                    // Add new vote count
                    if (vote == Models.Vote.Yes)
                        bill.YesVotes++;
                    else
                        bill.NoVotes++;

                    existingVote.Vote = vote;
                    existingVote.VoteDate = DateTime.UtcNow;
                }
            }
            else
            {
                // Create new vote
                var userBill = new UserBill
                {
                    UserId = user.UserId,
                    BillId = billId,
                    Vote = vote,
                    VoteDate = DateTime.UtcNow
                };

                if (vote == Models.Vote.Yes)
                    bill.YesVotes++;
                else
                    bill.NoVotes++;

                _context.UserBills.Add(userBill);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Bills));
        }
    }
}