using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using VacationManager.Controllers;
using VacationManager.Data;
using VacationManager.Models;

namespace VacationManager.Tests.Controllers
{
    [TestClass]
    public class VacationRequestsControllerTests
    {
        private VacationManagerDbContext _context;
        private Mock<UserManager<ApplicationUser>> _userManagerMock;
        private VacationRequestsController _controller;
        private ClaimsPrincipal _userPrincipal;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<VacationManagerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new VacationManagerDbContext(options);
            _userManagerMock = MockUserManager<ApplicationUser>();
            _controller = new VacationRequestsController(_context, _userManagerMock.Object);
            _userPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user123") }));
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = _userPrincipal } };
        }

        private Mock<UserManager<T>> MockUserManager<T>() where T : class
        {
            var store = new Mock<IUserStore<T>>();
            return new Mock<UserManager<T>>(store.Object, null, null, null, null, null, null, null, null);
        }

        [TestCleanup]
        public void Cleanup() => _context.Dispose();

        [TestMethod]
        public async Task MyRequests_ReturnsPaginatedRequests()
        {
            _context.VacationTypes.AddRange(
                new VacationType { Id = 1, Name = "Paid" },
                new VacationType { Id = 2, Name = "Unpaid" },
                new VacationType { Id = 3, Name = "Sick" }
            );
            await _context.SaveChangesAsync();

            var user = new ApplicationUser
            {
                Id = "user123",
                FirstName = "Test",
                LastName = "User"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            for (int i = 1; i <= 12; i++)
            {
                _context.VacationRequests.Add(new VacationRequest
                {
                    UserId = "user123",
                    CreatedOn = DateTime.UtcNow.AddDays(-i),
                    VacationTypeId = 1,
                    Status = "Pending"
                });
            }
            await _context.SaveChangesAsync();

            var result = await _controller.MyRequests(page: 2, pageSize: 5) as ViewResult;

            Assert.IsNotNull(result);
            var requests = result.Model as List<VacationRequest>;
            Assert.AreEqual(5, requests.Count);
            Assert.AreEqual(2, _controller.ViewBag.Page);
            Assert.AreEqual(12, _controller.ViewBag.Total);
        }

        [TestMethod]
        public async Task Create_ValidRequest_SetsStatusPendingAndRedirects()
        {
            var user = new ApplicationUser { Id = "user123" };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _context.VacationTypes.Add(new VacationType { Id = 1, Name = "Paid" });
            await _context.SaveChangesAsync();

            var model = new VacationRequest { StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(2), VacationTypeId = 1 };
            var result = await _controller.Create(model, null) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("MyRequests", result.ActionName);
            var saved = await _context.VacationRequests.FirstAsync();
            Assert.AreEqual("Pending", saved.Status);
            Assert.AreEqual("user123", saved.UserId);
        }

        [TestMethod]
        public async Task Create_SickLeaveWithoutFile_ReturnsViewWithError()
        {
            var user = new ApplicationUser { Id = "user123" };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _context.VacationTypes.Add(new VacationType { Id = 3, Name = "Sick" });
            await _context.SaveChangesAsync();

            var model = new VacationRequest { StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(2), VacationTypeId = 3 };
            var result = await _controller.Create(model, null) as ViewResult;
            Assert.IsNotNull(result);
            Assert.IsFalse(_controller.ModelState.IsValid);
            Assert.IsTrue(_controller.ModelState[""].Errors.Any(e => e.ErrorMessage == "Medical document required."));
        }

        [TestMethod]
        public async Task Approve_AsCEO_SetsStatusApproved()
        {
            _context.VacationTypes.AddRange(
                new VacationType { Id = 1, Name = "Paid" },
                new VacationType { Id = 2, Name = "Unpaid" },
                new VacationType { Id = 3, Name = "Sick" }
            );
            await _context.SaveChangesAsync();

            var user = new ApplicationUser { Id = "user1", TeamId = 1, FirstName = "John", LastName = "Doe" };
            _context.Users.Add(user);
            var request = new VacationRequest
            {
                Id = 1,
                Status = "Pending",
                UserId = "user1",
                User = user,
                VacationTypeId = 1
            };
            _context.VacationRequests.Add(request);
            await _context.SaveChangesAsync();

            var ceoUser = new ApplicationUser { Id = "ceo", TeamId = 1, FirstName = "CEO", LastName = "User" };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(ceoUser);
            _userManagerMock.Setup(u => u.IsInRoleAsync(ceoUser, "CEO")).ReturnsAsync(true);
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "CEO") }));

            var result = await _controller.Approve(1) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("AllRequests", result.ActionName);
            var updated = await _context.VacationRequests.FindAsync(1);
            Assert.AreEqual("Approved", updated.Status);
        }

        [TestMethod]
        public async Task Approve_AsTeamLead_NotOnLeave_SetsApproved()
        {
            _context.VacationTypes.AddRange(
                new VacationType { Id = 1, Name = "Paid" },
                new VacationType { Id = 2, Name = "Unpaid" },
                new VacationType { Id = 3, Name = "Sick" }
            );
            await _context.SaveChangesAsync();

            var teamLead = new ApplicationUser
            {
                Id = "lead",
                TeamId = 1,
                FirstName = "Lead",
                LastName = "User"
            };
            _context.Users.Add(teamLead);

            var regularUser = new ApplicationUser
            {
                Id = "user1",
                TeamId = 1,
                FirstName = "John",
                LastName = "Doe"
            };
            _context.Users.Add(regularUser);

            var request = new VacationRequest
            {
                Id = 1,
                Status = "Pending",
                UserId = "user1",
                User = regularUser,
                VacationTypeId = 1
            };
            _context.VacationRequests.Add(request);
            await _context.SaveChangesAsync();

            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(teamLead);
            _userManagerMock.Setup(u => u.IsInRoleAsync(teamLead, "CEO")).ReturnsAsync(false);
            _userManagerMock.Setup(u => u.IsInRoleAsync(teamLead, "Team Lead")).ReturnsAsync(true);

            _context.VacationRequests.Add(new VacationRequest
            {
                UserId = "lead",
                Status = "Approved",
                StartDate = DateTime.Today.AddDays(-10),
                EndDate = DateTime.Today.AddDays(-5)
            });
            await _context.SaveChangesAsync();

            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Team Lead") }));

            var result = await _controller.Approve(1) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("AllRequests", result.ActionName);
            var updated = await _context.VacationRequests.FindAsync(1);
            Assert.AreEqual("Approved", updated.Status);
        }

        [TestMethod]
        public async Task Approve_AsTeamLead_OnLeave_SetsPendingCEO()
        {
            _context.VacationTypes.AddRange(
                new VacationType { Id = 1, Name = "Paid" },
                new VacationType { Id = 2, Name = "Unpaid" },
                new VacationType { Id = 3, Name = "Sick" }
            );
            await _context.SaveChangesAsync();

            var teamLead = new ApplicationUser
            {
                Id = "lead",
                TeamId = 1,
                FirstName = "Lead",
                LastName = "User"
            };
            _context.Users.Add(teamLead);

            var regularUser = new ApplicationUser
            {
                Id = "user1",
                TeamId = 1,
                FirstName = "John",
                LastName = "Doe"
            };
            _context.Users.Add(regularUser);
            await _context.SaveChangesAsync();

            var request = new VacationRequest
            {
                Id = 1,
                Status = "Pending",
                UserId = "user1",
                User = regularUser,
                VacationTypeId = 1
            };
            _context.VacationRequests.Add(request);
            await _context.SaveChangesAsync();

            var leadLeave = new VacationRequest
            {
                Id = 2,
                UserId = "lead",
                Status = "Approved",
                StartDate = DateTime.Today.AddDays(-1),
                EndDate = DateTime.Today.AddDays(1),
                VacationTypeId = 1
            };
            _context.VacationRequests.Add(leadLeave);
            await _context.SaveChangesAsync();

            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(teamLead);
            _userManagerMock.Setup(u => u.IsInRoleAsync(teamLead, "CEO")).ReturnsAsync(false);
            _userManagerMock.Setup(u => u.IsInRoleAsync(teamLead, "Team Lead")).ReturnsAsync(true);

            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Team Lead") }));

            var result = await _controller.Approve(1) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("AllRequests", result.ActionName);
            var updated = await _context.VacationRequests.FindAsync(1);
            Assert.AreEqual("Pending CEO", updated.Status);
        }

        [TestMethod]
        public async Task AllRequests_FiltersByFromDate()
        {
            _context.VacationTypes.AddRange(
                new VacationType { Id = 1, Name = "Paid" },
                new VacationType { Id = 2, Name = "Unpaid" },
                new VacationType { Id = 3, Name = "Sick" }
            );
            await _context.SaveChangesAsync();

            var currentUser = new ApplicationUser { Id = "user", TeamId = 1, FirstName = "Current", LastName = "User" };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(currentUser);
            _userManagerMock.Setup(u => u.IsInRoleAsync(currentUser, "CEO")).ReturnsAsync(true);

            var user1 = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe", TeamId = 1 };
            var user2 = new ApplicationUser { Id = "user2", FirstName = "Jane", LastName = "Smith", TeamId = 2 };
            _context.Users.AddRange(user1, user2);
            await _context.SaveChangesAsync();

            var request1 = new VacationRequest
            {
                UserId = "user1",
                CreatedOn = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                VacationTypeId = 1,
                Status = "Pending"
            };
            var request2 = new VacationRequest
            {
                UserId = "user2",
                CreatedOn = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                VacationTypeId = 1,
                Status = "Pending"
            };
            _context.VacationRequests.AddRange(request1, request2);
            await _context.SaveChangesAsync();

            var result = await _controller.AllRequests(fromDate: new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc)) as ViewResult;
            var requests = result.Model as List<VacationRequest>;

            Assert.AreEqual(2, _context.VacationRequests.Count(), "Sanity check: two requests in database");
            Assert.AreEqual(1, requests.Count, "Only request with CreatedOn >= 2025-01-15 should be returned");
            Assert.AreEqual(new DateTime(2025, 2, 1), requests[0].CreatedOn.Date);
        }

        [TestMethod]
        public async Task Delete_ApprovedRequest_ReturnsBadRequest()
        {
            var request = new VacationRequest { Id = 1, Status = "Approved" };
            _context.VacationRequests.Add(request);
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteConfirmed(1);
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public async Task Delete_PendingRequest_RemovesIt()
        {
            var request = new VacationRequest { Id = 1, Status = "Pending" };
            _context.VacationRequests.Add(request);
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteConfirmed(1) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("MyRequests", result.ActionName);
            Assert.AreEqual(0, await _context.VacationRequests.CountAsync());
        }
    }
}