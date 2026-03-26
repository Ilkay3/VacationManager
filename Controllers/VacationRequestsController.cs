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
        public async Task<IActionResult> Create(VacationRequest model)
        {
            var user = await _userManager.GetUserAsync(User);

            model.UserId = user.Id;
            model.CreatedOn = DateTime.UtcNow;

            _context.VacationRequests.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyRequests));
        }
    }
}
