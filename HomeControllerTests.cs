using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using VacationManager.Controllers;
using VacationManager.Data;
using VacationManager.Models;

namespace VacationManager.Tests.Controllers
{
    [TestClass]
    public class HomeControllerTests
    {
        private VacationManagerDbContext _context;
        private HomeController _controller;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<VacationManagerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new VacationManagerDbContext(options);
            _controller = new HomeController(Mock.Of<ILogger<HomeController>>(), _context);
        }

        [TestCleanup]
        public void Cleanup() => _context.Dispose();

        [TestMethod]
        public async Task Index_ReturnsView_WithViewBagCounts()
        {
            _context.VacationRequests.AddRange(
                new VacationRequest { Status = "Approved" },
                new VacationRequest { Status = "Pending" },
                new VacationRequest { Status = "Pending" }
            );
            await _context.SaveChangesAsync();

            var result = await _controller.Index() as ViewResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(3, _controller.ViewBag.Total);
            Assert.AreEqual(1, _controller.ViewBag.Approved);
            Assert.AreEqual(2, _controller.ViewBag.Pending);
        }

        [TestMethod]
        public async Task Dashboard_ReturnsView_WithAllCounts()
        {
            _context.VacationRequests.AddRange(
                new VacationRequest { Status = "Approved" },
                new VacationRequest { Status = "Pending" },
                new VacationRequest { Status = "Rejected" }
            );
            await _context.SaveChangesAsync();

            var result = await _controller.Dashboard() as ViewResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(3, _controller.ViewBag.Total);
            Assert.AreEqual(1, _controller.ViewBag.Approved);
            Assert.AreEqual(1, _controller.ViewBag.Pending);
            Assert.AreEqual(1, _controller.ViewBag.Rejected);
        }
    }
}