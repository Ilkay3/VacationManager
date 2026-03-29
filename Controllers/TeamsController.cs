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
    [Authorize(Roles = "CEO")]
    public class TeamsController : Controller
    {
        private readonly VacationManagerDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeamsController(VacationManagerDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Teams
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var isCEO = await _userManager.IsInRoleAsync(user, "CEO");
            var isTeamLead = await _userManager.IsInRoleAsync(user, "Team Lead");

            IQueryable<Team> teamsQuery = _context.Teams
                .Include(t => t.Project)
                .Include(t => t.TeamLead);

            if (isTeamLead)
            {
                // Взима само отбора, където TeamLeadId съвпада с текущия потребител
                teamsQuery = teamsQuery.Where(t => t.TeamLeadId == user.Id);
            }

            var teams = await teamsQuery.ToListAsync();

            return View(teams);
        }

        // GET: Teams/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var team = await _context.Teams
                .Include(t => t.Project)
                .Include(t => t.TeamLead)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (team == null) return NotFound();

            // ВЗИМАМЕ MEMBERS ПРАВИЛНО
            var members = await _context.Users
                .Where(u => u.TeamId == team.Id)
                .ToListAsync();

            ViewBag.Members = members;

            return View(team);
        }

        // GET: Teams/Create
        public async Task<IActionResult> Create()
        {
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name");

            var teamLeads = await _userManager.GetUsersInRoleAsync("Team Lead");
            ViewData["TeamLeadId"] = new SelectList(teamLeads, "Id", "Email");

            return View();
        }

        // POST: Teams/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Team team)
        {
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            var teamLead = await _context.Users.FindAsync(team.TeamLeadId);

            if (teamLead != null)
            {
                teamLead.TeamId = team.Id;
                await _context.SaveChangesAsync();
            }

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

            return RedirectToAction(nameof(Index));
        }

        // GET: Teams/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var team = await _context.Teams
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null)
                return NotFound();

            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", team.ProjectId);

            var teamLeads = await _userManager.GetUsersInRoleAsync("Team Lead");
            ViewData["TeamLeadId"] = new SelectList(teamLeads, "Id", "Email", team.TeamLeadId);

            return View(team);
        }

        // POST: Teams/Edit
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

        //AssignUser
        public async Task<IActionResult> AssignUser()
        {
            ViewData["Users"] = new SelectList(_context.Users, "Id", "Email");
            ViewData["Teams"] = new SelectList(_context.Teams, "Id", "Name");

            return View();
        }

        //POST AssignUser
        [HttpPost]
        public async Task<IActionResult> AssignUser(string userId, int teamId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound();

            user.TeamId = teamId;

            await _context.SaveChangesAsync();

            return RedirectToAction("Teams");
        }

        // GET: Teams/Delete/5
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

        // POST: Teams/Delete
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
