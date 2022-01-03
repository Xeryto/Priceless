using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Priceless.Models.Helpers;
using Priceless.Models;

namespace Priceless.Controllers
{
    public class TeachersController : Controller
    {
        private readonly PricelessContext _context;

        public TeachersController(PricelessContext context)
        {
            _context = context;
        }

        // GET: Teachers
        public async Task<IActionResult> Index()
        {
            return View(await _context.Teachers.ToListAsync());
        }

        // GET: Teachers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (teacher == null)
            {
                return NotFound();
            }

            return View(teacher);
        }

        // GET: Teachers/Create
        public IActionResult Create()
        {
            var teacher = new Teacher();
            teacher.CourseAssignments = new List<CourseAssignment>();
            PopulateAssignedCourseData(teacher);
            return View();
        }

        // POST: Teachers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Login,Password,Name,Image")] Teacher teacher)
        {
            if (ModelState.IsValid)
            {
                _context.Add(teacher);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            PopulateAssignedCourseData(teacher);
            return View(teacher);
        }

        // GET: Teachers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacher = await _context.Teachers
                .Include(i => i.CourseAssignments).ThenInclude(i => i.Course)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (teacher == null)
            {
                return NotFound();
            }
            PopulateAssignedCourseData(teacher);
            return View(teacher);
        }

        private void PopulateAssignedCourseData(Teacher teacher)
        {
            var allCourses = _context.Courses;
            var instructorCourses = new HashSet<int>(teacher.CourseAssignments.Select(c => c.CourseId));
            var viewModel = new List<AssignedCourseData>();
            foreach (var course in allCourses)
            {
                viewModel.Add(new AssignedCourseData
                {
                    CourseId = course.Id,
                    Title = course.Title,
                    Assigned = instructorCourses.Contains(course.Id)
                });
            }
            ViewData["Courses"] = viewModel;
        }

        // POST: Teachers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, int[] selectedCourses)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacherToUpdate = await _context.Teachers
                .Include(i => i.CourseAssignments)
                    .ThenInclude(i => i.Course)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (await TryUpdateModelAsync<Teacher>(
                teacherToUpdate,
                "",
                i => i.Name, i => i.Login, i => i.Password))
            {
                UpdateTeacherCourses(selectedCourses, teacherToUpdate);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException /* ex */)
                {
                    //Log the error (uncomment ex variable name and write a log.)
                    ModelState.AddModelError("", "Unable to save changes. " +
                        "Try again, and if the problem persists, " +
                        "see your system administrator.");
                }
                return RedirectToAction(nameof(Index));
            }
            UpdateTeacherCourses(selectedCourses, teacherToUpdate);
            PopulateAssignedCourseData(teacherToUpdate);
            return View(teacherToUpdate);
        }

        private void UpdateTeacherCourses(int[] selectedCourses, Teacher teacherToUpdate)
        {
            if (selectedCourses == null)
            {
                teacherToUpdate.CourseAssignments = new List<CourseAssignment>();
                return;
            }

            var selectedCoursesHS = new HashSet<int>(selectedCourses);
            var instructorCourses = new HashSet<int>
                (teacherToUpdate.CourseAssignments.Select(c => c.Course.Id));
            foreach (var course in _context.Courses)
            {
                if (selectedCoursesHS.Contains(course.Id))
                {
                    if (!instructorCourses.Contains(course.Id))
                    {
                        teacherToUpdate.CourseAssignments.Add(new CourseAssignment { TeacherId = teacherToUpdate.Id, CourseId = course.Id });
                    }
                }
                else
                {

                    if (instructorCourses.Contains(course.Id))
                    {
                        CourseAssignment courseToRemove = teacherToUpdate.CourseAssignments.FirstOrDefault(i => i.CourseId == course.Id);
                        _context.Remove(courseToRemove);
                    }
                }
            }
        }

        // GET: Teachers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (teacher == null)
            {
                return NotFound();
            }

            return View(teacher);
        }

        // POST: Teachers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var teacher = await _context.Teachers
                .Include(i => i.CourseAssignments)
                .SingleAsync(i => i.Id == id);

            _context.Teachers.Remove(teacher);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TeacherExists(int id)
        {
            return _context.Teachers.Any(e => e.Id == id);
        }
    }
}
