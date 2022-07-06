using System;
using System.Collections.Generic;
using System.IO;
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

        public async Task<IActionResult> Pages(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(i => i.CourseAssignments).ThenInclude(i => i.Teacher)
                .Include(i => i.Enrollments).ThenInclude(i => i.Student)
                .Include(i => i.Exercises)
                .Include(i => i.Pages)
                .AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);

            var teachers = course.CourseAssignments.Select(i => i.TeacherId);
            var students = course.Enrollments.Select(i => i.StudentId);
            if (course == null)
            {
                return NotFound();
            }

            string ids;
            if (HttpContext.Request.Cookies.TryGetValue("Id", out ids) && ids != null)
            {
                PersonCacheModel personCache = WebCache.Get("LoggedIn" + ids);
                if (personCache != null)
                {
                    if (personCache.Status == "Admin" || personCache.Status == "Curator" || teachers.Contains(personCache.Id)
                        || students.Contains(personCache.Id))
                    {
                        return View(course);
                    }
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
                return StatusCode(StatusCodes.Status403Forbidden);
            }
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        public async Task<IActionResult> Files(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(i => i.CourseAssignments).ThenInclude(i => i.Teacher)
                .Include(i => i.Enrollments).ThenInclude(i => i.Student)
                .Include(i => i.Exercises)
                .Include(i => i.Pages)
                .AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);

            var teachers = course.CourseAssignments.Select(i => i.TeacherId);
            var students = course.Enrollments.Select(i => i.StudentId);
            if (course == null)
            {
                return NotFound();
            }

            string ids;
            if (HttpContext.Request.Cookies.TryGetValue("Id", out ids) && ids != null)
            {
                PersonCacheModel personCache = WebCache.Get("LoggedIn" + ids);
                if (personCache != null)
                {
                    if (personCache.Status == "Admin" || personCache.Status == "Curator" || teachers.Contains(personCache.Id) || students.Contains(personCache.Id))
                    {
                        return View(course);
                    }
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
                return StatusCode(StatusCodes.Status403Forbidden);
            }
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFiles(int id, string formFile)
        {
            var course = await _context.Courses
                    .FirstOrDefaultAsync(m => m.Id == id);

            if (ModelState.IsValid)
            {
                string ids;
                PersonCacheModel personCache = null;
                if (HttpContext.Request.Cookies.TryGetValue("Id", out ids))
                {
                    personCache = WebCache.Get("LoggedIn" + ids);
                }

                var teachers = new HashSet<int>
                (_context.Teachers.Where(i => i.CourseAssignments.Where(c => c.CourseId == course.Id).Any()).Select(i => i.Id));
                if (personCache != null)
                {
                    var editor = _context.Teachers.FirstOrDefault(i => i.Id == personCache.Id);
                    if (editor != null)
                    {
                        if (teachers.Contains(editor.Id) || editor.Status == "Admin" || editor.Status == "Curator")
                        {
                            course.Uploads = formFile;

                            _context.Update(course);
                            await _context.SaveChangesAsync();

                            return RedirectToAction("Details", new { id = course.Id });
                        }
                        return StatusCode(StatusCodes.Status403Forbidden);
                    }
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
                return StatusCode(StatusCodes.Status403Forbidden);
            }
            return RedirectToAction("Details", new { id = course.Id });
        }

        public async Task<IActionResult> Exercises(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(i => i.CourseAssignments).ThenInclude(i => i.Teacher)
                .Include(i => i.Enrollments).ThenInclude(i => i.Student)
                .Include(i => i.Exercises)
                .Include(i => i.Pages)
                .AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);

            var teachers = course.CourseAssignments.Select(i => i.TeacherId);
            var students = course.Enrollments.Select(i => i.StudentId);
            if (course == null)
            {
                return NotFound();
            }

            string ids;
            if (HttpContext.Request.Cookies.TryGetValue("Id", out ids) && ids != null)
            {
                PersonCacheModel personCache = WebCache.Get("LoggedIn" + ids);
                if (personCache != null)
                {
                    if (personCache.Status == "Admin" || personCache.Status == "Curator" || teachers.Contains(personCache.Id) || students.Contains(personCache.Id))
                    {
                        return View(course);
                    }
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
                return StatusCode(StatusCodes.Status403Forbidden);
            }
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        // GET: Courses
        public async Task<IActionResult> Index()
        {
            string ids;
            if (HttpContext.Request.Cookies.TryGetValue("Id", out ids) && ids != null)
            {
                PersonCacheModel personCache = WebCache.Get("LoggedIn" + ids);
                if (personCache != null)
                {
                    if (personCache.Status == "Admin" || personCache.Status == "Curator")
                    {
                        return View(await _context.Courses.ToListAsync());
                    }
                    return View(await _context.Courses.Where(i => i.CourseAssignments.Where(c => c.TeacherId == int.Parse(ids)).Any() || i.Enrollments.Where(c => c.StudentId == int.Parse(ids)).Any()).ToListAsync());
                }
                return StatusCode(StatusCodes.Status403Forbidden);
            }
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        // GET: Courses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(i => i.CourseAssignments).ThenInclude(i => i.Teacher)
                .Include(i => i.Enrollments).ThenInclude(i => i.Student)
                .Include(i => i.Exercises)
                .Include(i => i.Pages)
                .AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);

            var teachers = course.CourseAssignments.Select(i => i.TeacherId);
            var students = course.Enrollments.Select(i => i.StudentId);
            if (course == null)
            {
                return NotFound();
            }

            string ids;
            if (HttpContext.Request.Cookies.TryGetValue("Id", out ids) && ids != null)
            {
                PersonCacheModel personCache = WebCache.Get("LoggedIn" + ids);
                if (personCache != null)
                {
                    if (personCache.Status == "Admin" || personCache.Status == "Curator" || teachers.Contains(personCache.Id) || students.Contains(personCache.Id))
                    {
                        return View(course);
                    }
                    return StatusCode(StatusCodes.Status403Forbidden);
                }
                return StatusCode(StatusCodes.Status403Forbidden);
            }
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        // GET: Courses/Create
        public IActionResult Create()
        {
            var course = new Course();
            course.CourseAssignments = new List<CourseAssignment>();
            string ids;
            HttpContext.Request.Cookies.TryGetValue("Id", out ids);
            course.Enrollments = new List<Enrollment>();
            PopulateAssignedStudentData(course);
            PopulateAssignedTeacherData(course, int.Parse(ids));
            return View();
        }

        // POST: Courses/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Link,Image")] Course course, int[] selectedStudents, int[] selectedTeachers)
        {
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
                    Login = student.Login,
                    Name = student.Name,
                    Assigned = courseStudents.Contains(student.Id)
                });
            }
            ViewData["Students"] = viewModel;
        }

        private void PopulateAssignedTeacherData(Course course, int? include = null)
        {
            var allTeachers = _context.Teachers.Where(s => s.Status == "Admitted" || s.Status == "Admin" || s.Status == "Curator");
            var courseTeachers = new HashSet<int>(course.CourseAssignments.Select(c => c.TeacherId));
            var viewModel = new List<AssignedTeacherData>();
            foreach (var teacher in allTeachers)
            {
                if (include != null && teacher.Id == include)
                {
                    viewModel.Add(new AssignedTeacherData
                    {
                        TeacherId = teacher.Id,
                        Login = teacher.Login,
                        Name = teacher.Name,
                        Assigned = true
                    });
                }
                else
                {
                    viewModel.Add(new AssignedTeacherData
                    {
                        TeacherId = teacher.Id,
                        Login = teacher.Login,
                        Name = teacher.Name,
                        Assigned = courseTeachers.Contains(teacher.Id)
                    });
                }
            }
            ViewData["Teachers"] = viewModel;
        }

        // GET: Courses/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            string ids;
            PersonCacheModel personCache = null;
            if (HttpContext.Request.Cookies.TryGetValue("Id", out ids))
            {
                personCache = WebCache.Get("LoggedIn" + ids);
            }

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
                    if (teachers.Contains(editor.Id) || editor.Status == "Admin" || editor.Status == "Curator")
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
        public async Task<IActionResult> Edit(int? id, int[] selectedStudents, int[] selectedTeachers, bool deleteImage)
        {

            if (id == null)
            {
                return NotFound();
            }

            string ids;
            PersonCacheModel personCache = null;
            if (HttpContext.Request.Cookies.TryGetValue("Id", out ids))
            {
                personCache = WebCache.Get("LoggedIn" + ids);
            }


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
                    if (teachers.Contains(editor.Id) || editor.Status == "Admin" || editor.Status == "Curator")
                    {
                        if (await TryUpdateModelAsync<Course>(
                        courseToUpdate,
                        "",
                        i => i.Title, i => i.Link, i => i.Image))
                        {
                            if (deleteImage)
                            {
                                courseToUpdate.Image = null;
                            }
                            UpdateCourseStudents(selectedStudents, courseToUpdate);
                            UpdateCourseTeachersAsync(selectedTeachers, courseToUpdate);
                            try
                            {
                                _context.Entry(courseToUpdate).State = EntityState.Modified;
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
                            UpdateCourseTeachersAsync(selectedTeachers, courseToUpdate);
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
            var allStudents = _context.Students.ToList();
            foreach (var student in allStudents)
            {
                if (selectedStudentsHS.Contains(student.Id))
                {
                    if (!courseStudents.Contains(student.Id))
                    {
                        Enrollment enrollment = new Enrollment { CourseId = courseToUpdate.Id, StudentId = student.Id };
                        courseToUpdate.Enrollments.Add(enrollment);
                        _context.Add(enrollment);
                    }
                }
                else
                {

                    if (courseStudents.Contains(student.Id))
                    {
                        Enrollment studentToRemove = _context.Enrollments.FirstOrDefault(i => i.StudentId == student.Id && i.CourseId == courseToUpdate.Id);
                        _context.Remove(studentToRemove);
                    }
                }
            }
        }

        private void UpdateCourseTeachersAsync(int[] selectedTeachers, Course courseToUpdate)
        {
            if (selectedTeachers == null)
            {
                courseToUpdate.CourseAssignments = new List<CourseAssignment>();
                return;
            }

            var selectedTeachersHS = new HashSet<int>(selectedTeachers);
            var courseTeachers = new HashSet<int>
                (courseToUpdate.CourseAssignments.Select(c => c.Teacher.Id));
            var allTeachers = _context.Teachers.ToList();
            foreach (var teacher in allTeachers)
            {
                if (selectedTeachersHS.Contains(teacher.Id))
                {
                    if (!courseTeachers.Contains(teacher.Id))
                    {
                        CourseAssignment courseAssignment = new CourseAssignment { CourseId = courseToUpdate.Id, TeacherId = teacher.Id };
                        courseToUpdate.CourseAssignments.Add(courseAssignment);
                        _context.Add(courseAssignment);
                    }
                }
                else
                {

                    if (courseTeachers.Contains(teacher.Id))
                    {
                        CourseAssignment teacherToRemove = _context.CourseAssignments.FirstOrDefault(i => i.CourseId == courseToUpdate.Id && i.TeacherId == teacher.Id);
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

            var course = await _context.Courses.FirstOrDefaultAsync(m => m.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            string ids;
            PersonCacheModel personCache = null;
            if (HttpContext.Request.Cookies.TryGetValue("Id", out ids))
            {
                personCache = WebCache.Get("LoggedIn" + ids);
            }
            var teachers = new HashSet<int>
                (_context.Teachers.Where(i => i.CourseAssignments.Where(i => i.CourseId == course.Id).Any()).Select(i => i.Id));

            if (personCache != null)
            {
                var editor = _context.Teachers.FirstOrDefault(i => i.Id == personCache.Id);
                if (editor != null)
                {
                    if (teachers.Contains(editor.Id) || editor.Status == "Admin" || editor.Status == "Curator")
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
                .Include(i => i.CourseAssignments)
                .AsNoTracking().SingleAsync(i => i.Id == id);
            string ids;
            PersonCacheModel personCache = null;
            if (HttpContext.Request.Cookies.TryGetValue("Id", out ids))
            {
                personCache = WebCache.Get("LoggedIn" + ids);
            }
            var teachers = new HashSet<int>
                (_context.Teachers.Where(i => i.CourseAssignments.Where(i => i.CourseId == course.Id).Any()).Select(i => i.Id));

            if (personCache != null)
            {
                var editor = _context.Teachers.FirstOrDefault(i => i.Id == personCache.Id);
                if (editor != null)
                {
                    if (teachers.Contains(editor.Id) || editor.Status == "Admin" || editor.Status == "Curator")
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
