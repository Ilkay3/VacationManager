using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace VacationManager.Controllers
{
    [Authorize(Roles = "CEO")]
    public class RolesController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public RolesController(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        // LIST
        public IActionResult Index()
        {
            return View(_roleManager.Roles);
        }

        // CREATE GET
        public IActionResult Create()
        {
            return View();
        }

        // CREATE POST
        [HttpPost]
        public async Task<IActionResult> Create(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                ModelState.AddModelError("", "Role name is required");
                return View();
            }

            var role = new IdentityRole(name);
            await _roleManager.CreateAsync(role);

            return RedirectToAction(nameof(Index));
        }

        // DELETE
        public async Task<IActionResult> Delete(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);

            if (role == null) return NotFound();

            await _roleManager.DeleteAsync(role);

            return RedirectToAction(nameof(Index));
        }
    }
}