using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacationManager.Data;
using VacationManager.Models;

namespace VacationManager.Controllers
{
    [Authorize]
    public class TeamsController : Controller
    {
        private readonly VacationManagerDbContext _context;

        public TeamsController(VacationManagerDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string search)
        {
            var query = _context.Teams
                .Include(t => t.Project)
                .Include(t => t.TeamLead)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t =>
                    t.Name.Contains(search) ||
                    t.Project.Name.Contains(search));
            }

            return View(await query.ToListAsync());
        }

        public IActionResult Create()
        {
            ViewBag.Projects = _context.Projects.ToList();
            ViewBag.Users = _context.Users.ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Team model)
        {
            if (!ModelState.IsValid) return View(model);

            _context.Teams.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var team = await _context.Teams
                .Include(t => t.Project)
                .Include(t => t.TeamLead)
                .Include(t => t.Developers)
                .FirstOrDefaultAsync(t => t.Id == id);

            return View(team);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var team = await _context.Teams.FindAsync(id);

            if (team != null)
            {
                _context.Teams.Remove(team);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
