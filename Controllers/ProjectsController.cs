using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacationManager.Data;
using VacationManager.Models;

namespace VacationManager.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly VacationManagerDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProjectsController(
            VacationManagerDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 📄 MY REQUESTS (вместо Index)
        public async Task<IActionResult> MyRequests()
        {
            var user = await _userManager.GetUserAsync(User);

            var requests = await _context.VacationRequests
                .Where(r => r.UserId == user.Id)
                .Include(r => r.VacationType)
                .ToListAsync();

            return View(requests);
        }

        // ➕ CREATE (GET)
        public IActionResult Create()
        {
            ViewBag.Types = _context.VacationTypes.ToList();
            return View();
        }

        // ➕ CREATE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VacationRequest model, IFormFile file)
        {
            var user = await _userManager.GetUserAsync(User);

            // DATE VALIDATION
            if (model.StartDate > model.EndDate)
                ModelState.AddModelError("", "Start date cannot be after end date.");

            if (model.StartDate < DateTime.Today)
                ModelState.AddModelError("", "Start date cannot be in the past.");

            if (model.EndDate < DateTime.Today)
                ModelState.AddModelError("", "End date cannot be in the past.");

            var vacationType = await _context.VacationTypes
                .FirstOrDefaultAsync(v => v.Id == model.VacationTypeId);

            if (vacationType == null)
                ModelState.AddModelError("", "Invalid vacation type.");

            // SICK RULES
            if (vacationType != null && vacationType.Name == "Sick")
            {
                if (model.IsHalfDay)
                    ModelState.AddModelError("", "Sick leave cannot be half-day.");

                if (file == null)
                    ModelState.AddModelError("", "Medical document is required.");
            }

            // HALF DAY
            if (model.IsHalfDay && model.StartDate != model.EndDate)
                ModelState.AddModelError("", "Half day only for one day.");

            // FILE VALIDATION
            if (file != null)
            {
                var allowed = new[] { ".jpg", ".png", ".pdf" };
                var ext = Path.GetExtension(file.FileName).ToLower();

                if (!allowed.Contains(ext))
                    ModelState.AddModelError("", "Invalid file type.");

                if (file.Length > 5 * 1024 * 1024)
                    ModelState.AddModelError("", "File too large.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Types = _context.VacationTypes.ToList();
                return View(model);
            }

            // SAVE FILE
            if (file != null)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                var path = Path.Combine("wwwroot/files", fileName);

                using var stream = new FileStream(path, FileMode.Create);
                await file.CopyToAsync(stream);

                model.FilePath = "/files/" + fileName;
            }

            model.UserId = user.Id;
            model.CreatedOn = DateTime.UtcNow;
            model.IsApproved = false;

            _context.VacationRequests.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyRequests));
        }

        // ✏️ EDIT (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var request = await _context.VacationRequests.FindAsync(id);

            if (request == null)
                return NotFound();

            ViewBag.Types = _context.VacationTypes.ToList();
            return View(request);
        }

        // ✏️ EDIT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(VacationRequest model, IFormFile file)
        {
            var request = await _context.VacationRequests.FindAsync(model.Id);

            if (request == null)
                return NotFound();

            // VALIDATION (същото като Create)
            if (model.StartDate > model.EndDate)
                ModelState.AddModelError("", "Start date cannot be after end date.");

            var vacationType = await _context.VacationTypes
                .FirstOrDefaultAsync(v => v.Id == model.VacationTypeId);

            if (vacationType != null && vacationType.Name == "Sick")
            {
                if (model.IsHalfDay)
                    ModelState.AddModelError("", "Sick leave cannot be half-day.");

                if (file == null && request.FilePath == null)
                    ModelState.AddModelError("", "Medical document required.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Types = _context.VacationTypes.ToList();
                return View(model);
            }

            // UPDATE
            request.StartDate = model.StartDate;
            request.EndDate = model.EndDate;
            request.VacationTypeId = model.VacationTypeId;
            request.IsHalfDay = model.IsHalfDay;

            // FILE
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

        // 🗑️ DELETE
        public async Task<IActionResult> Delete(int id)
        {
            var request = await _context.VacationRequests.FindAsync(id);
            return View(request);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var request = await _context.VacationRequests.FindAsync(id);

            if (request != null)
                _context.VacationRequests.Remove(request);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyRequests));
        }
    }
}