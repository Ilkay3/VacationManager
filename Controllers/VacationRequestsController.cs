using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VacationManager.Data;
using VacationManager.Models;

namespace VacationManager.Controllers
{
    [Authorize]
    public class VacationRequestsController : Controller
    {
        private readonly VacationManagerDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public VacationRequestsController(
            VacationManagerDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // MY REQUESTS (вместо Index)
        public async Task<IActionResult> MyRequests()
        {
            var user = await _userManager.GetUserAsync(User);

            var requests = await _context.VacationRequests
                .Where(r => r.UserId == user.Id)
                .Include(r => r.VacationType)
                .ToListAsync();

            return View(requests);
        }

        // DETAILS
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.VacationRequests
                .Include(v => v.User)
                .Include(v => v.VacationType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (request == null) return NotFound();

            return View(request);
        }

        // CREATE (GET)
        public IActionResult Create()
        {
            ViewData["VacationTypeId"] =
                new SelectList(_context.VacationTypes, "Id", "Name");

            return View();
        }

        // CREATE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VacationRequest model, IFormFile? file)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Account");

            // ❗ FIX
            ModelState.Remove("file");

            // VALIDATION
            if (model.StartDate > model.EndDate)
                ModelState.AddModelError("", "Start date cannot be after end date.");

            if (model.StartDate < DateTime.Today)
                ModelState.AddModelError("", "Start date cannot be in the past.");

            var type = await _context.VacationTypes
                .FirstOrDefaultAsync(t => t.Id == model.VacationTypeId);

            if (type != null && type.Name == "Sick")
            {
                if (model.IsHalfDay)
                    ModelState.AddModelError("", "Sick leave cannot be half-day.");

                if (file == null)
                    ModelState.AddModelError("", "Medical document required.");
            }

            if (model.IsHalfDay && model.StartDate != model.EndDate)
                ModelState.AddModelError("", "Half day must be same day.");

            if (!ModelState.IsValid)
            {
                ViewData["VacationTypeId"] =
                    new SelectList(_context.VacationTypes, "Id", "Name", model.VacationTypeId);

                return View(model);
            }

            // FILE
            if (file != null)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                var directory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/files");

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var path = Path.Combine(directory, fileName);

                using var stream = new FileStream(path, FileMode.Create);
                await file.CopyToAsync(stream);

                model.FilePath = "/files/" + fileName;
            }

            model.UserId = user.Id;
            model.CreatedOn = DateTime.UtcNow;
            model.Status = "Rejected";

            _context.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyRequests));
        }
        //Approve
        [Authorize]
        public async Task<IActionResult> Approve(int id)
        {
            var request = await _context.VacationRequests
                .Include(r => r.User)
                .ThenInclude(u => u.Team)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Unauthorized();

            var isCEO = await _userManager.IsInRoleAsync(currentUser, "CEO");

            if (isCEO)
            {
                request.Status = "Approved";
            }
            else
            {
                var isTeamLead = await _userManager.IsInRoleAsync(currentUser, "Team Lead");

                if (!isTeamLead)
                    return Forbid();

                // 🔥 ключовата проверка
                if (request.User?.TeamId != currentUser.TeamId)
                    return Forbid();

                // ако Team Lead е в отпуск
                var leadOnLeave = await _context.VacationRequests
                    .AnyAsync(r => r.UserId == currentUser.Id
                                && r.Status == "Approved"
                                && r.StartDate <= DateTime.Today
                                && r.EndDate >= DateTime.Today);

                if (leadOnLeave)
                {
                    request.Status = "Pending CEO";
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(AllRequests));
                }
                request.Status = "Approved";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(AllRequests));
        }

		//Reject
		public async Task<IActionResult> Reject(int id)
		{
			var request = await _context.VacationRequests.FindAsync(id);

			if (request == null) return NotFound();

			request.Status = "Rejected";

			await _context.SaveChangesAsync();

			return RedirectToAction(nameof(AllRequests));
		}

		[Authorize]
        public async Task<IActionResult> AllRequests()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var isCEO = await _userManager.IsInRoleAsync(user, "CEO");
            var isTeamLead = await _userManager.IsInRoleAsync(user, "Team Lead");

            if (!isCEO && !isTeamLead)
                return Forbid();

            IQueryable<VacationRequest> query = _context.VacationRequests
                .Include(r => r.User)
                .ThenInclude(u => u.Team)
                .Include(r => r.VacationType);

            if (!isCEO)
            {
                // Team Lead вижда само своя team
                query = query.Where(r => r.User.TeamId == user.TeamId);
            }

            var requests = await query.ToListAsync();

            return View(requests);
        }
        //On leave
        public async Task<IActionResult> OnLeave()
        {
            var today = DateTime.Today;

            var usersOnLeave = await _context.VacationRequests
                .Where(r => r.Status == "Approved" &&
                            r.StartDate <= today &&
                            r.EndDate >= today)
                .Include(r => r.User)
                .Include(r => r.VacationType)
                .ToListAsync();

            return View(usersOnLeave);
        }

        // EDIT (GET)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.VacationRequests.FindAsync(id);
            if (request == null) return NotFound();

            ViewData["VacationTypeId"] =
                new SelectList(_context.VacationTypes, "Id", "Name", request.VacationTypeId);

            return View(request);
        }

        // EDIT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, VacationRequest model, IFormFile file)
        {
            if (id != model.Id) return NotFound();

            var request = await _context.VacationRequests.FindAsync(id);
            if (request == null) return NotFound();

            if (request.Status == "Approved")
            {
                ModelState.AddModelError("", "Cannot edit approved request.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["VacationTypeId"] =
                    new SelectList(_context.VacationTypes, "Id", "Name", model.VacationTypeId);
                return View(model);
            }

            request.StartDate = model.StartDate;
            request.EndDate = model.EndDate;
            request.IsHalfDay = model.IsHalfDay;
            request.VacationTypeId = model.VacationTypeId;

            if (file != null)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                var path = Path.Combine("wwwroot/files", fileName);

                using var stream = new FileStream(path, FileMode.Create);
                await file.CopyToAsync(stream);

                request.FilePath = "/files/" + fileName;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyRequests));
        }

        // DELETE
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.VacationRequests
                .Include(v => v.VacationType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (request == null) return NotFound();

            return View(request);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var request = await _context.VacationRequests.FindAsync(id);

            if (request == null) return NotFound();

            if (request.Status == "Approved")
                return BadRequest("Cannot delete approved request.");

            _context.VacationRequests.Remove(request);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyRequests));
        }
    }
}
