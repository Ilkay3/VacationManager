using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacationManager.Data;
using VacationManager.Models;

public class ProjectsController : Controller
{
    private readonly VacationManagerDbContext _context;

    public ProjectsController(VacationManagerDbContext context)
    {
        _context = context;
    }

    // ProjectList
    [Authorize(Roles = "CEO,Team Lead,Developer")]
    public async Task<IActionResult> Index(string search, int page = 1, int pageSize = 10)
    {
        IQueryable<Project> query = _context.Projects;
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
        }
        var total = await query.CountAsync();
        var projects = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.Total = total;

        return View(projects);
    }

    // DETAILS
    [Authorize(Roles = "CEO,Team Lead,Developer")]
    public async Task<IActionResult> Details(int id)
    {
        var project = await _context.Projects
            .Include(p => p.Teams)
            .ThenInclude(t => t.TeamLead)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (project == null) return NotFound();
        return View(project);
    }

    // CREATE GET
    [Authorize(Roles = "CEO")]
    public IActionResult Create() => View();

    // CREATE POST
    [Authorize(Roles = "CEO")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Project project)
    {
        if (!ModelState.IsValid)
            return View(project);
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // EDIT GET
    [Authorize(Roles = "CEO")]
    public async Task<IActionResult> Edit(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null) return NotFound();
        return View(project);
    }

    // EDIT POST
    [Authorize(Roles = "CEO")]
    [HttpPost]
    public async Task<IActionResult> Edit(int id, Project project)
    {
        if (id != project.Id) return NotFound();
        _context.Update(project);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // DELETE
    [Authorize(Roles = "CEO")]
    public async Task<IActionResult> Delete(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null) return NotFound();
        return View(project);
    }

    [Authorize(Roles = "CEO")]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project != null) _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}