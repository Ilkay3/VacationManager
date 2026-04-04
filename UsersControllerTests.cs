using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Moq;
using VacationManager.Controllers;
using VacationManager.Data;
using VacationManager.Models;

namespace VacationManager.Tests.Controllers
{
    [TestClass]
    public class UsersControllerTests
    {
        private VacationManagerDbContext _context;
        private Mock<UserManager<ApplicationUser>> _userManagerMock;
        private Mock<RoleManager<IdentityRole>> _roleManagerMock;
        private UsersController _controller;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<VacationManagerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new VacationManagerDbContext(options);
            _userManagerMock = MockUserManager<ApplicationUser>();
            _roleManagerMock = MockRoleManager();
            _controller = new UsersController(_userManagerMock.Object, _roleManagerMock.Object, _context);
        }

        private Mock<UserManager<T>> MockUserManager<T>() where T : class
        {
            var store = new Mock<IUserStore<T>>();
            return new Mock<UserManager<T>>(store.Object, null, null, null, null, null, null, null, null);
        }

        private Mock<RoleManager<IdentityRole>> MockRoleManager()
        {
            var store = new Mock<IRoleStore<IdentityRole>>();
            return new Mock<RoleManager<IdentityRole>>(store.Object, null, null, null, null);
        }

        [TestCleanup]
        public void Cleanup() => _context.Dispose();

        [TestMethod]
        public async Task Index_FiltersBySearchAndRole()
        {
            var users = new List<ApplicationUser>
    {
        new ApplicationUser { Id = "1", UserName = "john", FirstName = "John", LastName = "Doe", Email = "john@test.com" },
        new ApplicationUser { Id = "2", UserName = "jane", FirstName = "Jane", LastName = "Smith", Email = "jane@test.com" }
    };
            var mockUsers = users.AsQueryable().BuildMockDbSet();
            _userManagerMock.Setup(u => u.Users).Returns(mockUsers.Object);

            _userManagerMock.Setup(u => u.GetRolesAsync(It.Is<ApplicationUser>(x => x.Id == "1")))
                .ReturnsAsync(new List<string> { "Developer" });
            _userManagerMock.Setup(u => u.GetRolesAsync(It.Is<ApplicationUser>(x => x.Id == "2")))
                .ReturnsAsync(new List<string> { "Developer" });

            var roles = new List<IdentityRole> { new IdentityRole { Name = "Developer" } };
            var mockRoles = roles.AsQueryable().BuildMockDbSet();
            _roleManagerMock.Setup(r => r.Roles).Returns(mockRoles.Object);

            var result = await _controller.Index(search: "john", role: "Developer") as ViewResult;

            Assert.IsNotNull(result);
            var model = result.Model as List<UserViewModel>;
            Assert.AreEqual(1, model.Count);
            Assert.AreEqual("john@test.com", model[0].Email);
        }

        [TestMethod]
        public async Task Edit_Get_ReturnsViewWithUser()
        {
            var user = new ApplicationUser { Id = "1", Email = "test@test.com", FirstName = "Test", LastName = "User" };
            _userManagerMock.Setup(u => u.FindByIdAsync("1")).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Developer" });
            _roleManagerMock.Setup(r => r.Roles).Returns(new List<IdentityRole>().AsQueryable().BuildMockDbSet().Object);

            var result = await _controller.Edit("1") as ViewResult;
            Assert.IsNotNull(result);
            var model = result.Model as UserViewModel;
            Assert.AreEqual("test@test.com", model.Email);
        }

        [TestMethod]
        public async Task Edit_Post_Valid_UpdatesUserAndRole()
        {
            var user = new ApplicationUser { Id = "1", FirstName = "Old", LastName = "User", TeamId = 1 };
            _userManagerMock.Setup(u => u.FindByIdAsync("1")).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Unassigned" });
            _userManagerMock.Setup(u => u.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(u => u.AddToRoleAsync(user, "CEO")).ReturnsAsync(IdentityResult.Success);

            var model = new UserViewModel { Id = "1", FirstName = "New", LastName = "Name", TeamId = 2, Role = "CEO" };
            var result = await _controller.Edit("1", model) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual("New", user.FirstName);
            Assert.AreEqual("Name", user.LastName);
            Assert.AreEqual(2, user.TeamId);
        }

        [TestMethod]
        public async Task DeleteConfirmed_RemovesUser()
        {
            var user = new ApplicationUser { Id = "1" };
            _userManagerMock.Setup(u => u.FindByIdAsync("1")).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);
            var result = await _controller.DeleteConfirmed("1") as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
        }

        [TestMethod]
        public async Task Details_ReturnsViewWithTeamName()
        {
            var team = new Team { Id = 1, Name = "Awesome Team" };
            _context.Teams.Add(team);
            var user = new ApplicationUser
            {
                Id = "1",
                TeamId = 1,
                Team = team,
                FirstName = "Test",
                LastName = "User",
                Email = "test@test.com"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Developer" });

            var result = await _controller.Details("1") as ViewResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Awesome Team", _controller.ViewBag.TeamName);
        }
    }
}