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
    public class TeamsControllerTests
    {
        private VacationManagerDbContext _context;
        private Mock<UserManager<ApplicationUser>> _userManagerMock;
        private TeamsController _controller;
        private ClaimsPrincipal _ceoUser;
        private ClaimsPrincipal _teamLeadUser;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<VacationManagerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new VacationManagerDbContext(options);
            _userManagerMock = MockUserManager<ApplicationUser>();
            _controller = new TeamsController(_context, _userManagerMock.Object);

            _ceoUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "CEO") }));
            _teamLeadUser = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Team Lead") }));
        }

        private Mock<UserManager<T>> MockUserManager<T>() where T : class
        {
            var store = new Mock<IUserStore<T>>();
            return new Mock<UserManager<T>>(store.Object, null, null, null, null, null, null, null, null);
        }

        [TestCleanup]
        public void Cleanup() => _context.Dispose();

        [TestMethod]
        public async Task Index_AsCEO_ReturnsAllTeams()
        {
            var ceoUserObj = new ApplicationUser { Id = "ceo1", FirstName = "CEO", LastName = "User" };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(ceoUserObj);
            _userManagerMock.Setup(u => u.IsInRoleAsync(ceoUserObj, "CEO")).ReturnsAsync(true);

            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = _ceoUser } };
            var team = new Team { Id = 1, Name = "Team A" };
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            var result = await _controller.Index(null, null) as ViewResult;

            Assert.IsNotNull(result);
            var teams = result.Model as List<Team>;
            Assert.AreEqual(1, teams.Count);
        }

        [TestMethod]
        public async Task Index_AsTeamLead_ReturnsOnlyHisTeam()
        {
            var leadUser = new ApplicationUser { Id = "lead1", UserName = "lead@test.com" };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(leadUser);
            _userManagerMock.Setup(u => u.IsInRoleAsync(leadUser, "CEO")).ReturnsAsync(false);
            _userManagerMock.Setup(u => u.IsInRoleAsync(leadUser, "Team Lead")).ReturnsAsync(true);
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = _teamLeadUser } };

            var team1 = new Team { Id = 1, Name = "His Team", TeamLeadId = "lead1" };
            var team2 = new Team { Id = 2, Name = "Other Team", TeamLeadId = "other" };
            _context.Teams.AddRange(team1, team2);
            await _context.SaveChangesAsync();

            var result = await _controller.Index(null, null) as ViewResult;
            var teams = result.Model as List<Team>;
            Assert.AreEqual(1, teams.Count);
            Assert.AreEqual("His Team", teams[0].Name);
        }

        [TestMethod]
        public async Task Details_TeamLeadAccessingOwnTeam_ReturnsView()
        {
            var leadUser = new ApplicationUser { Id = "lead1" };
            var team = new Team { Id = 1, Name = "Team", TeamLeadId = "lead1" };
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(leadUser);
            _userManagerMock.Setup(u => u.IsInRoleAsync(leadUser, "CEO")).ReturnsAsync(false);
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = _teamLeadUser } };

            var result = await _controller.Details(1) as ViewResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(team, result.Model);
        }

        [TestMethod]
        public async Task Create_ValidTeam_RedirectsAndAssignsTeamLead()
        {
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = _ceoUser } };
            var leadUser = new ApplicationUser { Id = "lead1", FirstName = "Lead", LastName = "User" };
            _context.Users.Add(leadUser);
            var team = new Team { Name = "NewTeam", ProjectId = 1, TeamLeadId = "lead1" };
            _context.Projects.Add(new Project { Id = 1, Name = "Proj", Description = "ProjDesc" });
            await _context.SaveChangesAsync();

            var result = await _controller.Create(team) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            var savedTeam = await _context.Teams.FirstOrDefaultAsync();
            Assert.IsNotNull(savedTeam);
            Assert.AreEqual("lead1", savedTeam.TeamLeadId);
            var updatedLead = await _context.Users.FindAsync("lead1");
            Assert.AreEqual(savedTeam.Id, updatedLead.TeamId);
        }

        [TestMethod]
        public async Task AssignUser_Valid_UpdatesUserTeam()
        {
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = _ceoUser } };
            var user = new ApplicationUser { Id = "user1", FirstName = "Test", LastName = "User" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = await _controller.AssignUser("user1", 42) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            var updatedUser = await _context.Users.FindAsync("user1");
            Assert.AreEqual(42, updatedUser.TeamId);
        }
    }
}