using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Priceless;
using Priceless.Models;
using Priceless.Models.Helpers;

namespace Priceless.Controllers
{
    public class PagesController : Controller
    {
        private readonly PricelessContext _context;

        public PagesController(PricelessContext context)
        {
            _context = context;
        }

        // GET: Pages/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var page = await _context.Pages
                .Include(p => p.Course)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (page == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(i => i.CourseAssignments).ThenInclude(i => i.Teacher)
                .Include(i => i.Enrollments).ThenInclude(i => i.Student)
                .Include(i => i.Exercises)
                .Include(i => i.Pages)
                .AsNoTracking().FirstOrDefaultAsync(m => m.Id == page.CourseId);

            var teachers = course.CourseAssignments.Select(i => i.TeacherId);
            var students = course.Enrollments.Select(i => i.StudentId);

            if (HttpContext.Request.Cookies.TryGetValue("Id", out string ids) && ids != null)
            {
                PersonCacheModel personCache = WebCache.Get("LoggedIn" + ids);
                if (personCache != null)
                {
                    if (personCache.Status == "Admin" || personCache.Status == "Curator" || teachers.Contains(personCache.Id) || students.Contains(personCache.Id))
                    {
                        ViewData["isTeacher"] = !students.Contains(personCache.Id);
                        return View(page);
                    }
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
                return StatusCode(StatusCodes.Status403Forbidden);
            }
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        // GET: Pages/Create/1
        public async Task<IActionResult> Create(int courseId)
        {
            ViewData["CourseId"] = courseId;
            var course = await _context.Courses
                .Include(i => i.CourseAssignments).ThenInclude(i => i.Teacher)
                .Include(i => i.Enrollments).ThenInclude(i => i.Student)
                .Include(i => i.Exercises)
                .Include(i => i.Pages)
                .AsNoTracking().FirstOrDefaultAsync(m => m.Id == courseId);

            var teachers = course.CourseAssignments.Select(i => i.TeacherId);

            if (HttpContext.Request.Cookies.TryGetValue("Id", out string ids) && ids != null)
            {
                PersonCacheModel personCache = WebCache.Get("LoggedIn" + ids);
                if (personCache != null)
                {
                    if (personCache.Status == "Admin" || personCache.Status == "Curator" || teachers.Contains(personCache.Id))
                    {
                        return View();
                    }
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
                return StatusCode(StatusCodes.Status403Forbidden);
            }
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        // POST: Pages/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Json,CourseId,Title")] Page page)
        {
            if (ModelState.IsValid)
            {
                var course = await _context.Courses
                .Include(i => i.CourseAssignments).ThenInclude(i => i.Teacher)
                .Include(i => i.Enrollments).ThenInclude(i => i.Student)
                .Include(i => i.Exercises)
                .Include(i => i.Pages)
                .AsNoTracking().FirstOrDefaultAsync(m => m.Id == page.CourseId);

                var teachers = course.CourseAssignments.Select(i => i.TeacherId);

                if (HttpContext.Request.Cookies.TryGetValue("Id", out string ids) && ids != null)
                {
                    PersonCacheModel personCache = WebCache.Get("LoggedIn" + ids);
                    if (personCache != null)
                    {
                        if (personCache.Status == "Admin" || personCache.Status == "Curator" || teachers.Contains(personCache.Id))
                        {
                            _context.Add(page);
                            await _context.SaveChangesAsync();
                            return RedirectToAction("Pages", "Courses", new { id = page.CourseId});
                        }
                        return StatusCode(StatusCodes.Status403Forbidden);
                    }
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
                return StatusCode(StatusCodes.Status403Forbidden);
            }
            ViewData["CourseId"] = page.CourseId;
            return View(page);
        }

        // GET: Pages/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var page = await _context.Pages.FindAsync(id);
            if (page == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(i => i.CourseAssignments).ThenInclude(i => i.Teacher)
                .Include(i => i.Enrollments).ThenInclude(i => i.Student)
                .Include(i => i.Exercises)
                .Include(i => i.Pages)
                
                .AsNoTracking().FirstOrDefaultAsync(m => m.Id == page.CourseId);

            var teachers = course.CourseAssignments.Select(i => i.TeacherId);

            if (HttpContext.Request.Cookies.TryGetValue("Id", out string ids) && ids != null)
            {
                PersonCacheModel personCache = WebCache.Get("LoggedIn" + ids);
                if (personCache != null)
                {
                    if (personCache.Status == "Admin" || personCache.Status == "Curator" || teachers.Contains(personCache.Id))
                    {
                        ViewData["courseId"] = page.CourseId;
                        return View(page);
                    }
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
                return StatusCode(StatusCodes.Status403Forbidden);
            }
            return StatusCode(StatusCodes.Status403Forbidden);
            
        }

        // POST: Pages/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Json,Title,CourseId")] Page page)
        {
            if (id != page.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var course = await _context.Courses
                .Include(i => i.CourseAssignments).ThenInclude(i => i.Teacher)
                .Include(i => i.Enrollments).ThenInclude(i => i.Student)
                .Include(i => i.Exercises)
                .Include(i => i.Pages)
                
                .AsNoTracking().FirstOrDefaultAsync(m => m.Id == page.CourseId);

                var teachers = course.CourseAssignments.Select(i => i.TeacherId);

                if (HttpContext.Request.Cookies.TryGetValue("Id", out string ids) && ids != null)
                {
                    PersonCacheModel personCache = WebCache.Get("LoggedIn" + ids);
                    if (personCache != null)
                    {
                        if (personCache.Status == "Admin" || personCache.Status == "Curator" || teachers.Contains(personCache.Id))
                        {
                            try
                            {
                                _context.Update(page);
                                await _context.SaveChangesAsync();
                            }
                            catch (DbUpdateConcurrencyException)
                            {
                                if (!PageExists(page.Id))
                                {
                                    return NotFound();
                                }
                                else
                                {
                                    throw;
                                }
                            }
                            return RedirectToAction("Pages", "Courses", new { id = page.CourseId });
                        }
                        return StatusCode(StatusCodes.Status403Forbidden);
                    }
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
                return StatusCode(StatusCodes.Status403Forbidden);
            }
            return View(page);
        }

        // GET: Pages/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var page = await _context.Pages
                .Include(p => p.Course)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (page == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(i => i.CourseAssignments).ThenInclude(i => i.Teacher)
                .Include(i => i.Enrollments).ThenInclude(i => i.Student)
                .Include(i => i.Exercises)
                .Include(i => i.Pages)
                
                .AsNoTracking().FirstOrDefaultAsync(m => m.Id == page.CourseId);

            var teachers = course.CourseAssignments.Select(i => i.TeacherId);

            if (HttpContext.Request.Cookies.TryGetValue("Id", out string ids) && ids != null)
            {
                PersonCacheModel personCache = WebCache.Get("LoggedIn" + ids);
                if (personCache != null)
                {
                    if (personCache.Status == "Admin" || personCache.Status == "Curator" || teachers.Contains(personCache.Id))
                    {
                        return View(page);
                    }
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
                return StatusCode(StatusCodes.Status403Forbidden);
            }
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        // POST: Pages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var page = await _context.Pages.FindAsync(id);

            var course = await _context.Courses
                .Include(i => i.CourseAssignments).ThenInclude(i => i.Teacher)
                .Include(i => i.Enrollments).ThenInclude(i => i.Student)
                .Include(i => i.Exercises)
                .Include(i => i.Pages)
                
                .AsNoTracking().FirstOrDefaultAsync(m => m.Id == page.CourseId);

            var teachers = course.CourseAssignments.Select(i => i.TeacherId);

            if (HttpContext.Request.Cookies.TryGetValue("Id", out string ids) && ids != null)
            {
                PersonCacheModel personCache = WebCache.Get("LoggedIn" + ids);
                if (personCache != null)
                {
                    if (personCache.Status == "Admin" || personCache.Status == "Curator" || teachers.Contains(personCache.Id))
                    {
                        _context.Pages.Remove(page);
                        await _context.SaveChangesAsync();
                        return RedirectToAction("Pages", "Courses", new { id = page.CourseId });
                    }
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
                return StatusCode(StatusCodes.Status403Forbidden);
            }
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        private bool PageExists(int id)
        {
            return _context.Pages.Any(e => e.Id == id);
        }
    }
}
