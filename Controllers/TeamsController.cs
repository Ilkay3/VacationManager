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
    public class TeamsController : Controller
    {
        private readonly VacationManagerDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeamsController(VacationManagerDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Index
        [Authorize(Roles = "CEO,Team Lead")]
        public async Task<IActionResult> Index(string search, string project, int page = 1, int pageSize = 10)
        {
            var user = await _userManager.GetUserAsync(User);
            var isCEO = await _userManager.IsInRoleAsync(user, "CEO");

            IQueryable<Team> query = _context.Teams
                .Include(t => t.Project)
                .Include(t => t.TeamLead);

            if (!isCEO)
            {
                query = query.Where(t => t.TeamLeadId == user.Id);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t => t.Name.Contains(search));
            }
            if (!string.IsNullOrEmpty(project))
            {
                query = query.Where(t => t.Project.Name.Contains(project));
            }

            var total = await query.CountAsync();
            var teams = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.ProjectFilter = project;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;

            return View(teams);
        }

        // Details
        [Authorize(Roles = "CEO,Team Lead")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var team = await _context.Teams
                .Include(t => t.Project)
                .Include(t => t.TeamLead)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (team == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isCEO = await _userManager.IsInRoleAsync(user, "CEO");

            if (!isCEO && team.TeamLeadId != user.Id)
                return Forbid();

            var members = await _context.Users
                .Where(u => u.TeamId == team.Id)
                .ToListAsync();

            ViewBag.Members = members;

            return View(team);
        }

        // CREATE
        [Authorize(Roles = "CEO")]
        public async Task<IActionResult> Create()
        {
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name");

            var teamLeads = await _userManager.GetUsersInRoleAsync("Team Lead");
            ViewData["TeamLeadId"] = new SelectList(teamLeads, "Id", "Email");

            return View();
        }

        // CREATE POST
        [Authorize(Roles = "CEO")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Team team)
        {
            if (string.IsNullOrEmpty(team.Name))
                ModelState.AddModelError("", "Team name is required.");

            if (team.ProjectId == 0)
                ModelState.AddModelError("", "Project is required.");

            if (!ModelState.IsValid)
            {
                ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", team.ProjectId);
                var teamLeads = await _userManager.GetUsersInRoleAsync("Team Lead");
                ViewData["TeamLeadId"] = new SelectList(teamLeads, "Id", "Email", team.TeamLeadId);
                return View(team);
            }

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            var teamLead = await _context.Users.FindAsync(team.TeamLeadId);

            if (teamLead != null)
            {
                teamLead.TeamId = team.Id;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // EDIT
        [Authorize(Roles = "CEO")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var team = await _context.Teams
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null) return NotFound();

            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", team.ProjectId);

            var teamLeads = await _userManager.GetUsersInRoleAsync("Team Lead");
            ViewData["TeamLeadId"] = new SelectList(teamLeads, "Id", "Email", team.TeamLeadId);

            return View(team);
        }

        // EDIT POST
        [Authorize(Roles = "CEO")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Team model)
        {
            if (id != model.Id)
                return NotFound();

            var team = await _context.Teams.FindAsync(id);

            if (team == null)
                return NotFound();

            team.Name = model.Name;
            team.ProjectId = model.ProjectId;
            team.TeamLeadId = model.TeamLeadId;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ASSIGN USER
        [Authorize(Roles = "CEO")]
        public async Task<IActionResult> AssignUser()
        {
            ViewData["Users"] = new SelectList(_context.Users, "Id", "Email");
            ViewData["Teams"] = new SelectList(_context.Teams, "Id", "Name");

            return View();
        }

        // ASSIGN USER POST
        [Authorize(Roles = "CEO")]
        [HttpPost]
        public async Task<IActionResult> AssignUser(string userId, int teamId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound();

            user.TeamId = teamId;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // DELETE
        [Authorize(Roles = "CEO")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var team = await _context.Teams
                .Include(t => t.Project)
                .Include(t => t.TeamLead)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (team == null) return NotFound();

            return View(team);
        }

        // DELETE POST
        [Authorize(Roles = "CEO")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var team = await _context.Teams.FindAsync(id);

            if (team != null)
                _context.Teams.Remove(team);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}