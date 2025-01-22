using DemocraticTapON.Data;
using DemocraticTapON.Models;
using DemocraticTapON.Models.ViewModels;
using DemocraticTapON.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DemocraticTapON.Controllers
{
    [Authorize(Roles = "Representative")]
    public class RepresentativeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;
        private readonly IBillStatisticsService _statisticsService;

        public RepresentativeController(
            ApplicationDbContext context,
            IFileService fileService,
            IBillStatisticsService statisticsService)
        {
            _context = context;
            _fileService = fileService;
            _statisticsService = statisticsService;
        }

        [Authorize(Roles ="Representative")]
        public async Task<IActionResult> Index()
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var representative = await _context.Representatives
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.UserId == currentUserId);

            var viewModel = new RepresentativeDashboardViewModel
            {
                RepresentativeInfo = representative,
                MyBills = await _context.Bills
                    .Where(b => b.ProposedByRepId == representative.RepresentativeId)
                    .OrderByDescending(b => b.ProposedDate)
                    .Take(5)
                    .ToListAsync(),
                VotingStatistics = await _statisticsService.GetRepresentativeVotingStatistics(representative.RepresentativeId)
            };

            return View(viewModel);
        }

        public IActionResult CreateBill()
        {
            return View(new BillCreateViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> CreateBill(BillCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                var bill = new Bill
                {
                    Title = model.Title,
                    Description = model.Description,
                    ProposedDate = DateTime.UtcNow,
                    VotingEndDate = model.VotingEndDate,
                    Status = BillStatus.Pending,
                    ProposedByRepId = currentUserId
                };

                _context.Bills.Add(bill);
                await _context.SaveChangesAsync();

                if (model.Documents != null && model.Documents.Count > 0)
                {
                    var uploadErrors = new List<string>();
                    foreach (var document in model.Documents)
                    {
                        try
                        {

                            Console.WriteLine($"Attempting to upload file: {document.FileName}");
                            var fileUrl = await _fileService.UploadFileAsync(document);

                            Console.WriteLine($"File uploaded successfully: {fileUrl}");
                            var billDocument = new BillDocument
                            {
                                Title = document.FileName,
                                FileUrl = fileUrl,
                                Description = $"Document for Bill: {bill.Title}",
                                UploadDate = DateTime.UtcNow,
                                BillId = bill.BillId
                            };
                            _context.BillDocuments.Add(billDocument);
                        }
                        catch (ArgumentException ex)
                        {
                            Console.WriteLine($"Error uploading file {document.FileName}: {ex}");
                            uploadErrors.Add($"Error uploading file {document.FileName}: {ex.Message}");
                        }
                    }

                    await _context.SaveChangesAsync();

                    if (uploadErrors.Any())
                    {
                        TempData["DocumentUploadErrors"] = uploadErrors;
                    }


                }

                TempData["SuccessMessage"] = "Bill created successfully!";
                return RedirectToAction(nameof(BillDetails), new { id = bill.BillId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating bill: {ex}");
                ModelState.AddModelError("", "An error occurred while creating the bill.");
                return View(model);
            }
        }

        public async Task<IActionResult> BillDetails(int id)
        {

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.AccountId == currentUserId);

            var bill = await _context.Bills
                .Include(b => b.Documents)
                .Include(b => b.UserBills)
                .Include(b => b.Comments)
                .FirstOrDefaultAsync(b => b.BillId == id);

            if (bill == null)
                return NotFound();

            var userVote = await _context.UserBills
        .FirstOrDefaultAsync(ub => ub.UserId == user.UserId && ub.BillId == bill.BillId);

            var viewModel = new BillDetailsViewModel
            {
                Bill = bill,
                Documents = bill.Documents.ToList(),
                VotingStats = await _statisticsService.CalculateBillVotingStatistics(bill),
                Comments = await _statisticsService.GetBillComments(bill.BillId),
                UserVote = userVote?.Vote
            };

            viewModel.StatusList = Enum.GetValues<BillStatus>()
         .Select(s => new SelectListItem
         {
             Value = s.ToString(),
             Text = s.ToString(),
             Selected = bill.Status == s
         })
         .ToList();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBillStatus(int billId, BillStatus newStatus)
        {
            var bill = await _context.Bills.FindAsync(billId);

            if (bill == null)
                return NotFound();

            // Verify the current user is the bill's author
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (bill.ProposedByRepId != currentUserId)
                return Forbid();

            bill.Status = newStatus;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Bill status updated successfully!";
            return RedirectToAction(nameof(BillDetails), new { id = billId });
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(int billId, string content, int? parentCommentId)
        {
            if (string.IsNullOrEmpty(content))
                return BadRequest("Comment content cannot be empty");

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var comment = new BillComment
            {
                Content = content,
                BillId = billId,
                UserId = currentUserId,
                PostedDate = DateTime.UtcNow,
                ParentCommentId = parentCommentId
            };

            _context.BillComments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(BillDetails), new { id = billId });
        }

        public async Task<IActionResult> MyBills(int page = 1)
        {
            int pageSize = 10;
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var bills = await _context.Bills
                .Where(b => b.ProposedByRepId == currentUserId)
                .OrderByDescending(b => b.ProposedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalBills = await _context.Bills
                .Where(b => b.ProposedByRepId == currentUserId)
                .CountAsync();

            var viewModel = new MyBillsViewModel
            {
                Bills = bills,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalBills / (double)pageSize)
            };

            return View(viewModel);
        }

        public async Task<IActionResult> EditProfile()
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var representative = await _context.Representatives
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.UserId == currentUserId);

            if (representative == null)
                return NotFound();

            var viewModel = new EditProfileViewModel
            {
                FirstName = representative.User.FirstName,
                LastName = representative.User.LastName,
                Constituency = representative.Constituency,
                State = representative.State,
                PoliticalParty = representative.PoliticalParty,
                TermStart = representative.TermStart,
                TermEnd = representative.TermEnd,
                OfficePhone = representative.OfficePhone,
                OfficeAddress = representative.OfficeAddress
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var representative = await _context.Representatives
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.UserId == currentUserId);

            if (representative == null)
                return NotFound();

            try
            {
                // Update user information
                representative.User.FirstName = model.FirstName;
                representative.User.LastName = model.LastName;

                // Update representative specific information
                representative.Constituency = model.Constituency;
                representative.State = model.State;
                representative.PoliticalParty = model.PoliticalParty;
                representative.TermStart = model.TermStart;
                representative.TermEnd = model.TermEnd;
                representative.OfficePhone = model.OfficePhone;
                representative.OfficeAddress = model.OfficeAddress;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating your profile.");
                return View(model);
            }
        }
    }
}