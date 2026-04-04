using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using VacationManager.Controllers;
using VacationManager.Models;

namespace VacationManager.Tests.Controllers
{
    [TestClass]
    public class RolesControllerTests
    {
        private Mock<UserManager<ApplicationUser>> _userManagerMock;
        private Mock<RoleManager<IdentityRole>> _roleManagerMock;
        private RolesController _controller;

        [TestInitialize]
        public void Setup()
        {
            _userManagerMock = MockUserManager<ApplicationUser>();
            _roleManagerMock = MockRoleManager();
            _controller = new RolesController(_roleManagerMock.Object, _userManagerMock.Object);
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

        [TestMethod]
        public async Task Index_ReturnsRoles_WithUserCounts()
        {
            var roles = new List<IdentityRole>
            {
                new IdentityRole { Id = "1", Name = "CEO" },
                new IdentityRole { Id = "2", Name = "Dev" }
            };
            var mockRoleSet = roles.AsQueryable().BuildMockDbSet();
            _roleManagerMock.Setup(r => r.Roles).Returns(mockRoleSet.Object);

            _userManagerMock.Setup(u => u.GetUsersInRoleAsync("CEO"))
                .ReturnsAsync(new List<ApplicationUser> { new ApplicationUser() });
            _userManagerMock.Setup(u => u.GetUsersInRoleAsync("Dev"))
                .ReturnsAsync(new List<ApplicationUser>());

            var result = await _controller.Index() as ViewResult;

            Assert.IsNotNull(result);
            var model = result.Model as List<IdentityRole>;
            Assert.AreEqual(2, model.Count);
            Assert.IsNotNull(_controller.ViewBag.UserCounts);
            Assert.AreEqual(1, _controller.ViewBag.UserCounts["CEO"]);
            Assert.AreEqual(0, _controller.ViewBag.UserCounts["Dev"]);
        }

        [TestMethod]
        public async Task Create_ValidName_RedirectsToIndex()
        {
            _roleManagerMock.Setup(r => r.CreateAsync(It.IsAny<IdentityRole>())).ReturnsAsync(IdentityResult.Success);
            var result = await _controller.Create("NewRole") as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
        }

        [TestMethod]
        public async Task Create_EmptyName_ReturnsViewWithError()
        {
            var result = await _controller.Create("") as ViewResult;
            Assert.IsNotNull(result);
            Assert.IsFalse(_controller.ModelState.IsValid);
        }

        [TestMethod]
        public async Task Delete_ExistingRole_RedirectsToIndex()
        {
            var role = new IdentityRole { Id = "1", Name = "ToDelete" };
            _roleManagerMock.Setup(r => r.FindByIdAsync("1")).ReturnsAsync(role);
            _roleManagerMock.Setup(r => r.DeleteAsync(role)).ReturnsAsync(IdentityResult.Success);
            var result = await _controller.Delete("1") as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
        }
    }
}