using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using VacationManager.Controllers;
using VacationManager.Data;
using VacationManager.Models;

namespace VacationManager.Tests.Controllers
{
    [TestClass]
    public class ProjectsControllerTests
    {
        private VacationManagerDbContext _context;
        private ProjectsController _controller;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<VacationManagerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new VacationManagerDbContext(options);
            _controller = new ProjectsController(_context);
        }

        [TestCleanup]
        public void Cleanup() => _context.Dispose();

        [TestMethod]
        public async Task Index_FiltersProjectsByName()
        {
            _context.Projects.Add(new Project { Name = "UniqueProject", Description = "First" });
            _context.Projects.Add(new Project { Name = "OtherProject", Description = "Other" });
            _context.Projects.Add(new Project { Name = "Another", Description = "Another" });
            await _context.SaveChangesAsync();

            var result = await _controller.Index(search: "Unique", page: 1, pageSize: 10) as ViewResult;
            var projects = result.Model as List<Project>;

            Assert.AreEqual(1, projects.Count);
            Assert.AreEqual("UniqueProject", projects[0].Name);
        }

        [TestMethod]
        public async Task Details_ExistingId_ReturnsViewWithProject()
        {
            var project = new Project { Id = 1, Name = "Test", Description = "Test description" };
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            var result = await _controller.Details(1) as ViewResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(project, result.Model);
        }

        [TestMethod]
        public async Task Details_NonExistingId_ReturnsNotFound()
        {
            var result = await _controller.Details(999);
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Create_ValidModel_RedirectsToIndex()
        {
            var project = new Project { Name = "New", Description = "Desc" };
            var result = await _controller.Create(project) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual(1, await _context.Projects.CountAsync());
        }

        [TestMethod]
        public async Task Create_InvalidModel_ReturnsViewWithProject()
        {
            _controller.ModelState.AddModelError("Name", "Required");
            var project = new Project();
            var result = await _controller.Create(project) as ViewResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(project, result.Model);
        }

        [TestMethod]
        public async Task Edit_ValidModel_RedirectsToIndex()
        {
            var project = new Project { Id = 1, Name = "Old", Description = "OldDesc" };
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            _context.Entry(project).State = EntityState.Detached;

            var updated = new Project { Id = 1, Name = "New", Description = "NewDesc" };
            var result = await _controller.Edit(1, updated) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            var changed = await _context.Projects.FindAsync(1);
            Assert.AreEqual("New", changed.Name);
        }

        [TestMethod]
        public async Task DeleteConfirmed_RemovesProject()
        {
            var project = new Project { Id = 1, Name = "ToDelete", Description = "Some description" };
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteConfirmed(1) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual(0, await _context.Projects.CountAsync());
        }
    }
}