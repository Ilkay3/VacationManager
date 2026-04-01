using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacationManager.Data;
using VacationManager.Models;

[Authorize(Roles = "CEO")]
public class ProjectsController : Controller
{
    private readonly VacationManagerDbContext _context;

    public ProjectsController(VacationManagerDbContext context)
    {
        _context = context;
    }

    // LIST
    public async Task<IActionResult> Index()
    {
        return View(await _context.Projects.ToListAsync());
    }

    // CREATE GET
    public IActionResult Create()
    {
        return View();
    }

    // CREATE POST
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
    public async Task<IActionResult> Edit(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null) return NotFound();

        return View(project);
    }

    // EDIT POST
    [HttpPost]
    public async Task<IActionResult> Edit(int id, Project project)
    {
        if (id != project.Id) return NotFound();

        _context.Update(project);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // DELETE
    public async Task<IActionResult> Delete(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null) return NotFound();

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}