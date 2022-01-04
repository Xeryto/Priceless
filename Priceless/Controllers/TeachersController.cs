using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Priceless.Models.Helpers;
using Priceless.Models;
using System.Security.Cryptography;

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
            return View(await _context.Teachers.Where(t => t.Status != "Admitted").ToListAsync());
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
            teacher.MajorAssignments = new List<MajorAssignment>();
            PopulateAssignedMajorData(teacher);
            return View();
        }

        // POST: Teachers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Login,Password,Name,Phone,VK,Grade,School,FirstQA,SecondQA,ThirdQA,Image")] Teacher teacher, int[] selectedMajors)
        {
            teacher.MajorAssignments = new List<MajorAssignment>();
            if (ModelState.IsValid)
            {
                if (! _context.People.Any(p => p.Login == teacher.Login))
                {
                    teacher.Password = Hash(teacher.Password);
                    teacher.Status = "In process";
                    AddTeacherMajors(selectedMajors, teacher);
                    _context.Add(teacher);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    return View(teacher);
                }
            }
            PopulateAssignedMajorData(teacher);
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
                .Include(i => i.MajorAssignments).ThenInclude(i => i.Major)
                //.AsNoTracking()
                .Include(i => i.CourseAssignments).ThenInclude(i => i.Course)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (teacher == null)
            {
                return NotFound();
            }
            PopulateAssignedMajorData(teacher);
            PopulateAssignedCourseData(teacher);
            return View(teacher);
        }

        private void PopulateAssignedMajorData(Teacher teacher)
        {
            var allMajors = _context.Majors;
            var teacherMajors = new HashSet<int>(teacher.MajorAssignments.Select(c => c.MajorId));
            var viewModel = new List<AssignedMajorData>();
            foreach (var Major in allMajors)
            {
                viewModel.Add(new AssignedMajorData
                {
                    MajorId = Major.Id,
                    Title = Major.Title,
                    Assigned = teacherMajors.Contains(Major.Id)
                });
            }
            ViewData["Majors"] = viewModel;
        }

        private void PopulateAssignedCourseData(Teacher teacher)
        {
            var allCourse = _context.Courses;
            var teacherCourses = new HashSet<int>(teacher.CourseAssignments.Select(c => c.CourseId));
            var viewModel = new List<AssignedCourseData>();
            foreach (var course in allCourse)
            {
                viewModel.Add(new AssignedCourseData
                {
                    CourseId = course.Id,
                    Title = course.Title,
                    Assigned = teacherCourses.Contains(course.Id)
                });
            }
            ViewData["Courses"] = viewModel;
        }

        // POST: Teachers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, int[] selectedMajors, int[] selectedCourses)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacherToUpdate = await _context.Teachers
                .Include(i => i.MajorAssignments)
                    .ThenInclude(i => i.Major)
                .Include(i => i.CourseAssignments)
                    .ThenInclude(i => i.Course)
                .FirstOrDefaultAsync(m => m.Id == id);
            var pass = teacherToUpdate.Password;

            if (await TryUpdateModelAsync<Teacher>(
                teacherToUpdate,
                "",
                i => i.Name, i => i.Password, i => i.Phone, i => i.VK, i => i.Grade, i => i.School))
            {
                if (teacherToUpdate.Password != null)
                {
                    teacherToUpdate.Password = Hash(teacherToUpdate.Password);
                }
                else
                {
                    teacherToUpdate.Password = pass;
                }
                UpdateTeacherMajors(selectedMajors, teacherToUpdate);
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
            UpdateTeacherMajors(selectedMajors, teacherToUpdate);
            UpdateTeacherCourses(selectedCourses, teacherToUpdate);
            PopulateAssignedMajorData(teacherToUpdate);
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
            var teacherCourses = new HashSet<int>
                (teacherToUpdate.CourseAssignments.Select(c => c.Course.Id));
            foreach (var course in _context.Courses)
            {
                if (selectedCoursesHS.Contains(course.Id))
                {
                    if (!teacherCourses.Contains(course.Id))
                    {
                        teacherToUpdate.CourseAssignments.Add(new CourseAssignment { TeacherId = teacherToUpdate.Id, CourseId = course.Id });
                    }
                }
                else
                {

                    if (teacherCourses.Contains(course.Id))
                    {
                        CourseAssignment CourseToRemove = teacherToUpdate.CourseAssignments.FirstOrDefault(i => i.CourseId == course.Id);
                        _context.Remove(CourseToRemove);
                    }
                }
            }
        }

        private void UpdateTeacherMajors(int[] selectedMajors, Teacher teacherToUpdate)
        {
            if (selectedMajors == null)
            {
                teacherToUpdate.MajorAssignments = new List<MajorAssignment>();
                return;
            }

            var selectedMajorsHS = new HashSet<int>(selectedMajors);
            var teacherMajors = new HashSet<int>
                (teacherToUpdate.MajorAssignments.Select(c => c.Major.Id));
            foreach (var Major in _context.Majors)
            {
                if (selectedMajorsHS.Contains(Major.Id))
                {
                    if (!teacherMajors.Contains(Major.Id))
                    {
                        teacherToUpdate.MajorAssignments.Add(new MajorAssignment { TeacherId = teacherToUpdate.Id, MajorId = Major.Id });
                    }
                }
                else
                {

                    if (teacherMajors.Contains(Major.Id))
                    {
                        MajorAssignment MajorToRemove = teacherToUpdate.MajorAssignments.FirstOrDefault(i => i.MajorId == Major.Id);
                        _context.Remove(MajorToRemove);
                    }
                }
            }
        }

        private void AddTeacherMajors(int[] selectedMajors, Teacher teacherToAdd)
        {
            if (selectedMajors == null)
            {
                teacherToAdd.MajorAssignments = new List<MajorAssignment>();
                return;
            }

            var selectedMajorsHS = new HashSet<int>(selectedMajors);
            foreach (var Major in _context.Majors)
            {
                if (selectedMajorsHS.Contains(Major.Id))
                {
                    teacherToAdd.MajorAssignments.Add(new MajorAssignment { TeacherId = teacherToAdd.Id, MajorId = Major.Id });
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
                .Include(i => i.MajorAssignments)
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

        private static string Hash(string password)
        {
            byte[] salt;
            byte[] buffer2;
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }
            using (Rfc2898DeriveBytes bytes = new(password, 0x10, 0x3e8))
            {
                salt = bytes.Salt;
                buffer2 = bytes.GetBytes(0x20);
            }
            byte[] dst = new byte[0x31];
            Buffer.BlockCopy(salt, 0, dst, 1, 0x10);
            Buffer.BlockCopy(buffer2, 0, dst, 0x11, 0x20);
            return Convert.ToBase64String(dst);
        }
    }
}
