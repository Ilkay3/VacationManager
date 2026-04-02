using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VacationManager.Data;
using VacationManager.Models;

[Authorize(Roles = "CEO")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly VacationManagerDbContext _context;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        VacationManagerDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    // GET: Users
    public async Task<IActionResult> Index(string search, string role, int page = 1, int pageSize = 10)
    {
        var users = await _userManager.Users.ToListAsync();

        var filtered = new List<ApplicationUser>();

        foreach (var user in users)
        {
            var userRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

            // Филтър по текст
            bool matchesSearch =
                string.IsNullOrEmpty(search) ||
                user.UserName.Contains(search) ||
                user.FirstName.Contains(search) ||
                user.LastName.Contains(search);

            // Филтър по роля
            bool matchesRole =
                string.IsNullOrEmpty(role) ||
                userRole == role;

            if (matchesSearch && matchesRole)
            {
                filtered.Add(user);
            }
        }

        var result = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserViewModel
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                TeamId = u.TeamId,
                Role = _userManager.GetRolesAsync(u).Result.FirstOrDefault()
            }).ToList();

        ViewBag.Roles = new SelectList(await _roleManager.Roles.ToListAsync(), "Name", "Name", role);

        ViewBag.Search = search;
        ViewBag.SelectedRole = role;

        ViewBag.PageSize = pageSize;
        ViewBag.Page = page;
        ViewBag.Total = filtered.Count;

        return View(result);
    }

    // GET: Users/Edit/5
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var model = new UserViewModel
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            TeamId = user.TeamId,
            Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault()
        };

        ViewBag.Teams = new SelectList(_context.Teams, "Id", "Name", user.TeamId);
        ViewBag.Roles = new SelectList(
            await _roleManager.Roles.ToListAsync(),
            "Name",
            "Name",
            model.Role
        );
        return View(model);
    }

    // POST: Users/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UserViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.TeamId = model.TeamId;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Смяна на ролята
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!string.IsNullOrEmpty(model.Role))
                    await _userManager.AddToRoleAsync(user, model.Role);

                return RedirectToAction(nameof(Index));
            }
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
        }

        ViewBag.Teams = new SelectList(_context.Teams, "Id", "Name", model.TeamId);
        ViewBag.Roles = new SelectList(
            await _roleManager.Roles.ToListAsync(),
            "Name",
            "Name",
            model.Role
        );
        return View(model);
    }

    // GET: Users/Delete/5
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var model = new UserViewModel
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName
        };
        return View(model);
    }

    // POST: Users/Delete
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user != null)
            await _userManager.DeleteAsync(user);

        return RedirectToAction(nameof(Index));
    }

    // GET: Users/Details/5
    public async Task<IActionResult> Details(string id)
    {
        // Зареждаме потребителя заедно с неговия екип
        var user = await _userManager.Users
            .Include(u => u.Team)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound();

        var model = new UserViewModel
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            TeamId = user.TeamId,
            Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault()
        };

        ViewBag.TeamName = user.Team?.Name ?? "No team";
        ViewBag.Teams = new SelectList(_context.Teams, "Id", "Name", model.TeamId);
        return View(model);
    }
}