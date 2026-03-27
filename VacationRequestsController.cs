using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VacationManager.Data;
using VacationManager.Models;

namespace VacationManager
{
    public class VacationRequestsController : Controller
    {
        private readonly VacationManagerDbContext _context;

        public VacationRequestsController(VacationManagerDbContext context)
        {
            _context = context;
        }

        // GET: VacationRequests
        public async Task<IActionResult> Index()
        {
            var vacationManagerDbContext = _context.VacationRequests.Include(v => v.User).Include(v => v.VacationType);
            return View(await vacationManagerDbContext.ToListAsync());
        }

        // GET: VacationRequests/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vacationRequest = await _context.VacationRequests
                .Include(v => v.User)
                .Include(v => v.VacationType)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (vacationRequest == null)
            {
                return NotFound();
            }

            return View(vacationRequest);
        }

        // GET: VacationRequests/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id");
            ViewData["VacationTypeId"] = new SelectList(_context.VacationTypes, "Id", "Name");
            return View();
        }

        // POST: VacationRequests/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,StartDate,EndDate,CreatedOn,IsHalfDay,IsApproved,VacationTypeId,FilePath,UserId")] VacationRequest vacationRequest)
        {
            if (ModelState.IsValid)
            {
                _context.Add(vacationRequest);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", vacationRequest.UserId);
            ViewData["VacationTypeId"] = new SelectList(_context.VacationTypes, "Id", "Name", vacationRequest.VacationTypeId);
            return View(vacationRequest);
        }

        // GET: VacationRequests/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vacationRequest = await _context.VacationRequests.FindAsync(id);
            if (vacationRequest == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", vacationRequest.UserId);
            ViewData["VacationTypeId"] = new SelectList(_context.VacationTypes, "Id", "Name", vacationRequest.VacationTypeId);
            return View(vacationRequest);
        }

        // POST: VacationRequests/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StartDate,EndDate,CreatedOn,IsHalfDay,IsApproved,VacationTypeId,FilePath,UserId")] VacationRequest vacationRequest)
        {
            if (id != vacationRequest.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(vacationRequest);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VacationRequestExists(vacationRequest.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", vacationRequest.UserId);
            ViewData["VacationTypeId"] = new SelectList(_context.VacationTypes, "Id", "Name", vacationRequest.VacationTypeId);
            return View(vacationRequest);
        }

        // GET: VacationRequests/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vacationRequest = await _context.VacationRequests
                .Include(v => v.User)
                .Include(v => v.VacationType)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (vacationRequest == null)
            {
                return NotFound();
            }

            return View(vacationRequest);
        }

        // POST: VacationRequests/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var vacationRequest = await _context.VacationRequests.FindAsync(id);
            if (vacationRequest != null)
            {
                _context.VacationRequests.Remove(vacationRequest);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool VacationRequestExists(int id)
        {
            return _context.VacationRequests.Any(e => e.Id == id);
        }
    }
}
