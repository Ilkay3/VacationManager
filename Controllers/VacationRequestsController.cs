using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

        public VacationRequestsController(VacationManagerDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> MyRequests()
        {
            var user = await _userManager.GetUserAsync(User);

            var requests = await _context.VacationRequests
                .Include(v => v.VacationType)
                .Where(v => v.UserId == user.Id)
                .ToListAsync();

            return View(requests);
        }

        public IActionResult Create()
        {
            ViewBag.Types = _context.VacationTypes.ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(VacationRequest model, IFormFile file)
        {
            var user = await _userManager.GetUserAsync(User);

            if (model.StartDate > model.EndDate)
            {
                ModelState.AddModelError("", "Start date cannot be after end date.");
            }

            if (model.StartDate < DateTime.Today)
            {
                ModelState.AddModelError("", "Start date cannot be in the past.");
            }

            if (model.EndDate < DateTime.Today)
            {
                ModelState.AddModelError("", "End date cannot be in the past.");
            }

            var vacationType = await _context.VacationTypes
                .FirstOrDefaultAsync(v => v.Id == model.VacationTypeId);

            if (vacationType == null)
            {
                ModelState.AddModelError("", "Invalid vacation type.");
            }

            if (vacationType != null && vacationType.Name == "Sick")
            {
                if (model.IsHalfDay)
                {
                    ModelState.AddModelError("", "Sick leave cannot be half-day.");
                }

                if (file == null)
                {
                    ModelState.AddModelError("", "Sick leave requires a medical document.");
                }
            }

            if (model.IsHalfDay && model.StartDate != model.EndDate)
            {
                ModelState.AddModelError("", "Half day is allowed only for one day.");
            }

            if (file != null)
            {
                var allowedExtensions = new[] { ".jpg", ".png", ".pdf" };
                var extension = Path.GetExtension(file.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("", "Invalid file type.");
                }

                if (file.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("", "File too large (max 5MB).");
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Types = _context.VacationTypes.ToList();
                return View(model);
            }

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

        public async Task<IActionResult> Edit(int id)
        {
            var request = await _context.VacationRequests.FindAsync(id);
            ViewBag.Types = _context.VacationTypes.ToList();
            return View(request);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(VacationRequest model, IFormFile file)
        {
            var request = await _context.VacationRequests.FindAsync(model.Id);

            if (request == null)
                return NotFound();

            if (model.StartDate > model.EndDate)
            {
                ModelState.AddModelError("", "Start date cannot be after end date.");
            }

            if (model.StartDate < DateTime.Today)
            {
                ModelState.AddModelError("", "Start date cannot be in the past.");
            }

            if (model.EndDate < DateTime.Today)
            {
                ModelState.AddModelError("", "End date cannot be in the past.");
            }

            var vacationType = await _context.VacationTypes
                .FirstOrDefaultAsync(v => v.Id == model.VacationTypeId);

            if (vacationType == null)
            {
                ModelState.AddModelError("", "Invalid vacation type.");
            }

            if (vacationType != null && vacationType.Name == "Sick")
            {
                if (model.IsHalfDay)
                {
                    ModelState.AddModelError("", "Sick leave cannot be half-day.");
                }

                if (file == null && request.FilePath == null)
                {
                    ModelState.AddModelError("", "Sick leave requires a medical document.");
                }
            }

            if (model.IsHalfDay && model.StartDate != model.EndDate)
            {
                ModelState.AddModelError("", "Half day only for one day.");
            }

            if (file != null)
            {
                var allowedExtensions = new[] { ".jpg", ".png", ".pdf" };
                var extension = Path.GetExtension(file.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("", "Invalid file type.");
                }

                if (file.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("", "File too large.");
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Types = _context.VacationTypes.ToList();
                return View(model);
            }

            request.StartDate = model.StartDate;
            request.EndDate = model.EndDate;
            request.VacationTypeId = model.VacationTypeId;
            request.IsHalfDay = model.IsHalfDay;

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
    }
}
