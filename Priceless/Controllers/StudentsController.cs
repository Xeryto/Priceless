using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Priceless.Models.Helpers;
using Priceless.Models;

namespace Priceless.Controllers
{
    public class StudentsController : Controller
    {
        private readonly PricelessContext _context;
        private readonly MapperConfiguration config = new(cfg => cfg
        .CreateMap<StudentPostModel, Student>().ForMember("Image", opt => opt.Ignore()));

        public StudentsController(PricelessContext context)
        {
            _context = context;
        }

        // GET: Students
        public async Task<IActionResult> Index()
        {
            return View(await _context.Students.ToListAsync());
        }

        // GET: Students/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(m => m.Id == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // GET: Students/Create
        public IActionResult Create()
        {
            var student = new Student();
            student.Admissions = new List<Admission>();
            PopulateAssignedMajorData(student);
            return View();
        }

        // POST: Students/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Grade,Id,Login,Password,Name,Image")] Student student)
        {
            //var mapper = new Mapper(config);
            //var student = mapper.Map<StudentPostModel, Student>(studentPost);
            if (ModelState.IsValid)
            {
                /*if (studentPost.Image != null)
                {
                    var stream = new MemoryStream();
                    await studentPost.Image.CopyToAsync(stream);
                    student.Image = stream.ToArray();
                }

                PersonCacheModel userCache = new()
                {
                    Id = student.Id,
                    Image = student.Image
                };
                WebCache.Set("LoggedIn", userCache, 60, true);*/
                _context.Add(student);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            PopulateAssignedMajorData(student);
            return View(student);
        }

        // GET: Students/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(i => i.Enrollments).ThenInclude(i => i.Course)
                .AsNoTracking()
                .Include(i => i.Admissions).ThenInclude(i => i.Major)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (student == null)
            {
                return NotFound();
            }
            PopulateAssignedMajorData(student);
            return View(student);
        }

        private void PopulateAssignedMajorData(Student student)
        {
            var allMajors = _context.Majors;
            var studentMajors = new HashSet<int>(student.Admissions.Select(c => c.MajorId));
            var viewModel = new List<AssignedMajorData>();
            foreach (var major in allMajors)
            {
                viewModel.Add(new AssignedMajorData
                {
                    MajorId = major.Id,
                    Title = major.Title,
                    Assigned = studentMajors.Contains(major.Id)
                });
            }
            ViewData["Majors"] = viewModel;
        }

        // POST: Students/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, int[] selectedMajors)
        {
            var studentToUpdate = await _context.Students
                .Include(i => i.Enrollments)
                    .ThenInclude(i => i.Course)
                .Include(i => i.Admissions)
                    .ThenInclude(i => i.Major)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (await TryUpdateModelAsync<Student>(
                studentToUpdate,
                "",
                i => i.Name, i => i.Login, i => i.Password, i => i.Grade, i => i.City, i => i.ParentName, i => i.Phone, i => i.ParentPhone))
            {
                UpdateStudentMajors(selectedMajors, studentToUpdate);
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
            UpdateStudentMajors(selectedMajors, studentToUpdate);
            PopulateAssignedMajorData(studentToUpdate);
            return View(studentToUpdate);
        }

        private void UpdateStudentMajors(int[] selectedMajors, Student studentToUpdate)
        {
            if (selectedMajors == null)
            {
                studentToUpdate.Admissions = new List<Admission>();
                return;
            }

            var selectedCoursesHS = new HashSet<int>(selectedMajors);
            var instructorCourses = new HashSet<int>
                (studentToUpdate.Admissions.Select(c => c.Major.Id));
            foreach (var course in _context.Courses)
            {
                if (selectedCoursesHS.Contains(course.Id))
                {
                    if (!instructorCourses.Contains(course.Id))
                    {
                        studentToUpdate.Admissions.Add(new Admission { StudentId = studentToUpdate.Id, MajorId = course.Id });
                    }
                }
                else
                {
                    if (instructorCourses.Contains(course.Id))
                    {
                        Admission AdmissionToRemove = studentToUpdate.Admissions.FirstOrDefault(i => i.MajorId == course.Id);
                        _context.Remove(AdmissionToRemove);
                    }
                }
            }
        }

        // GET: Students/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(m => m.Id == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students
                 .Include(i => i.Enrollments)
                 .Include(i => i.Admissions)
                 .SingleAsync(i => i.Id == id);

            _context.Students.Remove(student);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.Id == id);
        }
    }
}
