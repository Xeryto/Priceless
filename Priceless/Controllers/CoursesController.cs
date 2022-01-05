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
    public class CoursesController : Controller
    {
        private readonly PricelessContext _context;

        public CoursesController(PricelessContext context)
        {
            _context = context;
        }

        // GET: Courses
        public async Task<IActionResult> Index()
        {
            return View(await _context.Courses.ToListAsync());
        }

        // GET: Courses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .FirstOrDefaultAsync(m => m.Id == id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // GET: Courses/Create
        public IActionResult Create()
        {
            var course = new Course();
            course.CourseAssignments = new List<CourseAssignment>();
            course.Enrollments = new List<Enrollment>();
            PopulateAssignedStudentData(course);
            PopulateAssignedTeacherData(course);
            return View();
        }

        // POST: Courses/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Link")] Course course, int[] selectedStudents, int[] selectedTeachers)
        {
            PersonCacheModel personCache = WebCache.Get("LoggedIn");
            course.CourseAssignments = new List<CourseAssignment>();
            course.Enrollments = new List<Enrollment>();
            if (ModelState.IsValid)
            {
                AddCourseTeachers(selectedTeachers, course);
                AddCourseStudents(selectedStudents, course);
                _context.Add(course);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            PopulateAssignedStudentData(course);
            PopulateAssignedTeacherData(course);
            return View(course);
        }

        private void PopulateAssignedStudentData(Course course)
        {
            var allStudents = _context.Students.Where(s => s.Status == "Admitted");
            var courseStudents = new HashSet<int>(course.Enrollments.Select(c => c.StudentId));
            var viewModel = new List<AssignedStudentData>();
            foreach (var student in allStudents)
            {
                viewModel.Add(new AssignedStudentData
                {
                    StudentId = student.Id,
                    Name = student.Name,
                    Assigned = courseStudents.Contains(student.Id)
                });
            }
            ViewData["Students"] = viewModel;
        }

        private void PopulateAssignedTeacherData(Course course)
        {
            var allTeachers = _context.Teachers.Where(s => s.Status == "Admitted" || s.Status == "Admin");
            var courseTeachers = new HashSet<int>(course.CourseAssignments.Select(c => c.TeacherId));
            var viewModel = new List<AssignedTeacherData>();
            foreach (var teacher in allTeachers)
            {
                viewModel.Add(new AssignedTeacherData
                {
                    TeacherId = teacher.Id,
                    Name = teacher.Name,
                    Assigned = courseTeachers.Contains(teacher.Id)
                });
            }
            ViewData["Teachers"] = viewModel;
        }

        // GET: Courses/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            PersonCacheModel personCache = WebCache.Get("LoggedIn");


            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.CourseAssignments)
                    .ThenInclude(c => c.Teacher)
                .Include(c => c.Enrollments)
                    .ThenInclude(c => c.Student)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == (int)id);
            if (course == null)
            {
                return NotFound();
            }
            var teachers = new HashSet<int>
                (course.CourseAssignments.Select(c => c.Teacher.Id));
            if (personCache != null)
            {
                var editor = _context.Teachers.FirstOrDefault(i => i.Id == personCache.Id);
                if (editor != null)
                {
                    if (teachers.Contains(editor.Id) || editor.Status == "Admin")
                    {
                        PopulateAssignedStudentData(course);
                        PopulateAssignedTeacherData(course);
                        return View(course);
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status403Forbidden);
                    }
                }
                else
                {
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
            }
            else
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }
        }

        // POST: Courses/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, int[] selectedStudents, int[] selectedTeachers)
        {
            if (id == null)
            {
                return NotFound();
            }

            PersonCacheModel personCache = WebCache.Get("LoggedIn");
            

            var courseToUpdate = await _context.Courses
                .Include(i => i.Enrollments)
                    .ThenInclude(i => i.Student)
                .Include(i => i.CourseAssignments)
                    .ThenInclude(i => i.Teacher)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            var teachers = new HashSet<int>
                (courseToUpdate.CourseAssignments.Select(c => c.Teacher.Id));
            if (personCache != null)
            {
                var editor = _context.Teachers.FirstOrDefault(i => i.Id == personCache.Id);
                if (editor != null)
                {
                    if (teachers.Contains(editor.Id) || editor.Status == "Admin")
                    {
                        if (await TryUpdateModelAsync<Course>(
                        courseToUpdate,
                        "",
                        i => i.Title, i => i.Link))
                        {
                            UpdateCourseStudents(selectedStudents, courseToUpdate);
                            UpdateCourseTeachers(selectedTeachers, courseToUpdate);
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
                        else
                        {
                            UpdateCourseStudents(selectedStudents, courseToUpdate);
                            UpdateCourseTeachers(selectedTeachers, courseToUpdate);
                            PopulateAssignedStudentData(courseToUpdate);
                            PopulateAssignedTeacherData(courseToUpdate);
                            return View(courseToUpdate);
                        }
                    }
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
                return StatusCode(StatusCodes.Status403Forbidden);
            }
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        private void AddCourseStudents(int[] selectedStudents, Course courseToUpdate)
        {
            if (selectedStudents == null)
            {
                courseToUpdate.Enrollments = new List<Enrollment>();
                return;
            }

            var selectedStudentsHS = new HashSet<int>(selectedStudents);
            foreach (var student in _context.Students)
            {
                if (selectedStudentsHS.Contains(student.Id))
                {
                    courseToUpdate.Enrollments.Add(new Enrollment { CourseId = courseToUpdate.Id, StudentId = student.Id });
                }
            }
        }

        private void UpdateCourseStudents(int[] selectedStudents, Course courseToUpdate)
        {
            if (selectedStudents == null)
            {
                courseToUpdate.Enrollments = new List<Enrollment>();
                return;
            }

            var selectedStudentsHS = new HashSet<int>(selectedStudents);
            var courseStudents = new HashSet<int>
                (courseToUpdate.Enrollments.Select(c => c.Student.Id));
            foreach (var student in _context.Students)
            {
                if (selectedStudentsHS.Contains(student.Id))
                {
                    if (!courseStudents.Contains(student.Id))
                    {
                        courseToUpdate.Enrollments.Add(new Enrollment { CourseId = courseToUpdate.Id, StudentId = student.Id });
                    }
                }
                else
                {

                    if (courseStudents.Contains(student.Id))
                    {
                        Enrollment studentToRemove = courseToUpdate.Enrollments.FirstOrDefault(i => i.StudentId == student.Id);
                        _context.Remove(studentToRemove);
                    }
                }
            }
        }

        private void UpdateCourseTeachers(int[] selectedTeachers, Course courseToUpdate)
        {
            if (selectedTeachers == null)
            {
                courseToUpdate.CourseAssignments = new List<CourseAssignment>();
                return;
            }

            var selectedTeachersHS = new HashSet<int>(selectedTeachers);
            var courseTeachers = new HashSet<int>
                (courseToUpdate.CourseAssignments.Select(c => c.Teacher.Id));
            foreach (var teacher in _context.Teachers)
            {
                if (selectedTeachersHS.Contains(teacher.Id))
                {
                    if (!courseTeachers.Contains(teacher.Id))
                    {
                        courseToUpdate.CourseAssignments.Add(new CourseAssignment { CourseId = courseToUpdate.Id, TeacherId = teacher.Id });
                    }
                }
                else
                {

                    if (courseTeachers.Contains(teacher.Id))
                    {
                        CourseAssignment teacherToRemove = courseToUpdate.CourseAssignments.FirstOrDefault(i => i.TeacherId == teacher.Id);
                        _context.Remove(teacherToRemove);
                    }
                }
            }
        }

        private void AddCourseTeachers(int[] selectedTeachers, Course courseToUpdate)
        {
            if (selectedTeachers == null)
            {
                courseToUpdate.CourseAssignments = new List<CourseAssignment>();
                return;
            }

            var selectedTeachersHS = new HashSet<int>(selectedTeachers);
            foreach (var teacher in _context.Teachers)
            {
                if (selectedTeachersHS.Contains(teacher.Id))
                {
                    courseToUpdate.CourseAssignments.Add(new CourseAssignment { CourseId = courseToUpdate.Id, TeacherId = teacher.Id });
                }
            }
        }


        // GET: Courses/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            PersonCacheModel personCache = WebCache.Get("LoggedIn");
            var teachers = new HashSet<int>
                (course.CourseAssignments.Select(c => c.Teacher.Id));

            if (personCache != null)
            {
                var editor = _context.Teachers.FirstOrDefault(i => i.Id == personCache.Id);
                if (editor != null)
                {
                    if (teachers.Contains(editor.Id) || editor.Status == "Admin")
                    {
                        return View(course);
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status403Forbidden);
                    }
                }
                return StatusCode(StatusCodes.Status403Forbidden);
            }
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        // POST: Courses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses
                .Include(i => i.Enrollments)
                .Include(i => i.CourseAssignments).SingleAsync(i => i.Id == id);
            PersonCacheModel personCache = WebCache.Get("LoggedIn");
            var teachers = new HashSet<int>
                (course.CourseAssignments.Select(c => c.Teacher.Id));

            if (personCache != null)
            {
                var editor = _context.Teachers.FirstOrDefault(i => i.Id == personCache.Id);
                if (editor != null)
                {
                    if (teachers.Contains(editor.Id) || editor.Status == "Admin")
                    {
                        _context.Courses.Remove(course);
                        await _context.SaveChangesAsync();
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status403Forbidden);
                    }
                }
                return StatusCode(StatusCodes.Status403Forbidden);
            }
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }
    }
}
