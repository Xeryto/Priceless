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
    public class ExercisesController : Controller
    {
        private readonly PricelessContext _context;

        public ExercisesController(PricelessContext context)
        {
            _context = context;
        }

        // GET: Exercises
        public async Task<IActionResult> Index()
        {
            var pricelessContext = _context.Exercises.Include(e => e.Course);
            return View(await pricelessContext.ToListAsync());
        }

        // GET: Exercises/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var exercise = await _context.Exercises
                .Include(e => e.Course)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (exercise == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(i => i.CourseAssignments).ThenInclude(i => i.Teacher)
                .Include(i => i.Enrollments).ThenInclude(i => i.Student)
                .Include(i => i.Exercises)
                .Include(i => i.Pages)
                .AsNoTracking().FirstOrDefaultAsync(m => m.Id == exercise.CourseId);

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
                        return View(exercise);
                    }
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
                return StatusCode(StatusCodes.Status403Forbidden);
            }
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        // GET: Exercises/Create
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

        // POST: Exercises/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Deadline,Json,CourseId,Title")] Exercise exercise)
        {
            if (ModelState.IsValid)
            {
                var course = await _context.Courses
                .Include(i => i.CourseAssignments).ThenInclude(i => i.Teacher)
                .Include(i => i.Enrollments).ThenInclude(i => i.Student)
                .Include(i => i.Exercises)
                .Include(i => i.Pages)
                .AsNoTracking().FirstOrDefaultAsync(m => m.Id == exercise.CourseId);

                var teachers = course.CourseAssignments.Select(i => i.TeacherId);

                if (HttpContext.Request.Cookies.TryGetValue("Id", out string ids) && ids != null)
                {
                    PersonCacheModel personCache = WebCache.Get("LoggedIn" + ids);
                    if (personCache != null)
                    {
                        if (personCache.Status == "Admin" || personCache.Status == "Curator" || teachers.Contains(personCache.Id))
                        {
                            _context.Add(exercise);
                            await _context.SaveChangesAsync();
                            return RedirectToAction("Exercises", "Courses", new { id = exercise.CourseId });
                        }
                        return StatusCode(StatusCodes.Status403Forbidden);
                    }
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
                return StatusCode(StatusCodes.Status403Forbidden);
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Id", exercise.CourseId);
            return View(exercise);
        }

        // GET: Exercises/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var exercise = await _context.Exercises.FindAsync(id);
            if (exercise == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(i => i.CourseAssignments).ThenInclude(i => i.Teacher)
                .Include(i => i.Enrollments).ThenInclude(i => i.Student)
                .Include(i => i.Exercises)
                .Include(i => i.Pages)
                .AsNoTracking().FirstOrDefaultAsync(m => m.Id == exercise.CourseId);

            var teachers = course.CourseAssignments.Select(i => i.TeacherId);

            if (HttpContext.Request.Cookies.TryGetValue("Id", out string ids) && ids != null)
            {
                PersonCacheModel personCache = WebCache.Get("LoggedIn" + ids);
                if (personCache != null)
                {
                    if (personCache.Status == "Admin" || personCache.Status == "Curator" || teachers.Contains(personCache.Id))
                    {
                        ViewData["courseId"] = exercise.CourseId;
                        return View(exercise);
                    }
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
                return StatusCode(StatusCodes.Status403Forbidden);
            }
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        // POST: Exercises/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Deadline,Json,CourseId,Title")] Exercise exercise)
        {
            if (id != exercise.Id)
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
                
                .AsNoTracking().FirstOrDefaultAsync(m => m.Id == exercise.CourseId);

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
                                _context.Update(exercise);
                                await _context.SaveChangesAsync();
                            }
                            catch (DbUpdateConcurrencyException)
                            {
                                if (!ExerciseExists(exercise.Id))
                                {
                                    return NotFound();
                                }
                                else
                                {
                                    throw;
                                }
                            }
                            return RedirectToAction("Exercises", "Courses", new { id = exercise.CourseId });
                        }
                        return StatusCode(StatusCodes.Status403Forbidden);
                    }
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
                return StatusCode(StatusCodes.Status403Forbidden);
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Id", exercise.CourseId);
            return View(exercise);
        }

        // GET: Exercises/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var exercise = await _context.Exercises
                .Include(e => e.Course)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (exercise == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(i => i.CourseAssignments).ThenInclude(i => i.Teacher)
                .Include(i => i.Enrollments).ThenInclude(i => i.Student)
                .Include(i => i.Exercises)
                .Include(i => i.Pages)
                
                .AsNoTracking().FirstOrDefaultAsync(m => m.Id == exercise.CourseId);

            var teachers = course.CourseAssignments.Select(i => i.TeacherId);

            if (HttpContext.Request.Cookies.TryGetValue("Id", out string ids) && ids != null)
            {
                PersonCacheModel personCache = WebCache.Get("LoggedIn" + ids);
                if (personCache != null)
                {
                    if (personCache.Status == "Admin" || personCache.Status == "Curator" || teachers.Contains(personCache.Id))
                    {
                        return View(exercise);
                    }
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
                return StatusCode(StatusCodes.Status403Forbidden);
            }
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        // POST: Exercises/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var exercise = await _context.Exercises.FindAsync(id);
            var course = await _context.Courses
                .Include(i => i.CourseAssignments).ThenInclude(i => i.Teacher)
                .Include(i => i.Enrollments).ThenInclude(i => i.Student)
                .Include(i => i.Exercises)
                .Include(i => i.Pages)
                
                .AsNoTracking().FirstOrDefaultAsync(m => m.Id == exercise.CourseId);

            var teachers = course.CourseAssignments.Select(i => i.TeacherId);

            if (HttpContext.Request.Cookies.TryGetValue("Id", out string ids) && ids != null)
            {
                PersonCacheModel personCache = WebCache.Get("LoggedIn" + ids);
                if (personCache != null)
                {
                    if (personCache.Status == "Admin" || personCache.Status == "Curator" || teachers.Contains(personCache.Id))
                    {
                        _context.Exercises.Remove(exercise);
                        await _context.SaveChangesAsync();
                        return RedirectToAction("Exercises", "Courses", new { id = exercise.CourseId });
                    }
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
                return StatusCode(StatusCodes.Status403Forbidden);
            }
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        private bool ExerciseExists(int id)
        {
            return _context.Exercises.Any(e => e.Id == id);
        }
    }
}
