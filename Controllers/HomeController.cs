using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using VacationManager.Data;
using VacationManager.Models;

namespace VacationManager.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly VacationManagerDbContext _context;

		public HomeController(
			ILogger<HomeController> logger,
			VacationManagerDbContext context)
		{
			_logger = logger;
			_context = context;
		}

        public async Task<IActionResult> Index()
		{
			var totalRequests = await _context.VacationRequests.CountAsync();
			var approved = await _context.VacationRequests.CountAsync(r => r.Status == "Approved");
			var pending = await _context.VacationRequests.CountAsync(r => r.Status == "Pending");

			ViewBag.Total = totalRequests;
			ViewBag.Approved = approved;
			ViewBag.Pending = pending;

			return View();
		}

        [Authorize(Roles = "CEO,Team Lead")]
        public async Task<IActionResult> Dashboard()
        {
            var totalRequests = await _context.VacationRequests.CountAsync();
            var approved = await _context.VacationRequests.CountAsync(r => r.Status == "Approved");
            var pending = await _context.VacationRequests.CountAsync(r => r.Status == "Pending");
            var rejected = await _context.VacationRequests.CountAsync(r => r.Status == "Rejected");

            ViewBag.Total = totalRequests;
            ViewBag.Approved = approved;
            ViewBag.Pending = pending;
            ViewBag.Rejected = rejected;

            return View();
        }

        public IActionResult Privacy()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}