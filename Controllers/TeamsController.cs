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
            var teams = await _context.Teams
                .Include(t => t.Project)
                .Include(t => t.TeamLead)
                .ToListAsync();

            return View(teams);
        }

        // GET: Teams/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var team = await _context.Teams
                .Include(t => t.Project)
                .Include(t => t.TeamLead)
                .Include(t => t.Members)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (team == null) return NotFound();

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
            if (id == null) return NotFound();

            var team = await _context.Teams.FindAsync(id);
            if (team == null) return NotFound();

            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", team.ProjectId);

            var teamLeads = await _userManager.GetUsersInRoleAsync("Team Lead");
            ViewData["TeamLeadId"] = new SelectList(teamLeads, "Id", "Email", team.TeamLeadId);

            return View(team);
        }

        // POST: Teams/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Team team)
        {
            if (id != team.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", team.ProjectId);

                var teamLeads = await _userManager.GetUsersInRoleAsync("Team Lead");
                ViewData["TeamLeadId"] = new SelectList(teamLeads, "Id", "Email", team.TeamLeadId);

                return View(team);
            }

            try
            {
                _context.Update(team);
                await _context.SaveChangesAsync();
            }
            catch
            {
                if (!_context.Teams.Any(e => e.Id == team.Id))
                    return NotFound();
                else
                    throw;
            }

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

            return RedirectToAction("Index");
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
