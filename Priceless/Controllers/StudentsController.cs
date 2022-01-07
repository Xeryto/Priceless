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
using System.Security.Cryptography;
using System.IO;
using System.Web.Helpers;
using Microsoft.AspNetCore.Http;

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
            return View(await _context.Students.Where(s => s.Status == "In process").ToListAsync());
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
        public async Task<IActionResult> Create([Bind("Grade,Id,Login,Password,Name,ParentName,Phone,ParentPhone,City,FirstQA,SecondQA,Image")] StudentPostModel studentPost, int[] selectedMajors)
        {
            var mapper = new Mapper(config);
            var student = mapper.Map<StudentPostModel, Student>(studentPost);
            student.Admissions = new List<Admission>();
            if (ModelState.IsValid)
            {
                if (!_context.People.Any(p => p.Login == student.Login))
                {
                    if (studentPost.Image != null)
                    {
                        var stream = new MemoryStream();
                        await studentPost.Image.CopyToAsync(stream);
                        student.Image = stream.ToArray();
                    }

                    student.Password = Hash(student.Password);
                    student.Status = "In process";
                    AddStudentMajors(selectedMajors, student);
                    _context.Add(student);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    return View(studentPost);
                }
            }
            PopulateAssignedMajorData(student);
            return View(studentPost);
        }

        public async Task<IActionResult> EditImage(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(i => i.Admissions).ThenInclude(i => i.Major)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (student == null)
            {
                return NotFound();
            }
            var studentEdit = new StudentEditModel()
            {
                Id = (int)id
            };
            return View(studentEdit);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditImage(int id, [Bind("Image")] StudentEditModel studentEdit)
        {

            if (ModelState.IsValid)
            {
                var student = await _context.Students
                .Include(i => i.Admissions).ThenInclude(i => i.Major)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

                if (studentEdit.Image != null)
                {
                    var stream = new MemoryStream();
                    await studentEdit.Image.CopyToAsync(stream);
                    student.Image = stream.ToArray();
                }

                PersonCacheModel editor = WebCache.Get("LoggedIn"+id.ToString());
                WebCache.Remove("LoggedIn"+id.ToString());
                editor.Image = student.Image;
                WebCache.Set("LoggedIn"+id.ToString(), editor, 60, true);
                _context.Update(student);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(studentEdit);
        }

        // GET: Students/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
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
                .Include(i => i.Admissions)
                    .ThenInclude(i => i.Major)
                .FirstOrDefaultAsync(m => m.Id == id);

            var pass = studentToUpdate.Password;

            if (await TryUpdateModelAsync<Student>(
                studentToUpdate,
                "",
                i => i.Name, i => i.Password))
            {
                if (studentToUpdate.Password != null)
                {
                    studentToUpdate.Password = Hash(studentToUpdate.Password);
                }
                else
                {
                    studentToUpdate.Password = pass;
                }
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

            var selectedMajorsHS = new HashSet<int>(selectedMajors);
            var studentCourses = new HashSet<int>
                (studentToUpdate.Admissions.Select(c => c.Major.Id));
            foreach (var major in _context.Majors)
            {
                if (selectedMajorsHS.Contains(major.Id))
                {
                    if (!studentCourses.Contains(major.Id))
                    {
                        studentToUpdate.Admissions.Add(new Admission { StudentId = studentToUpdate.Id, MajorId = major.Id });
                    }
                }
                else
                {
                    if (studentCourses.Contains(major.Id))
                    {
                        Admission AdmissionToRemove = studentToUpdate.Admissions.FirstOrDefault(i => i.MajorId == major.Id);
                        _context.Remove(AdmissionToRemove);
                    }
                }
            }
        }

        private void AddStudentMajors(int[] selectedMajors, Student studentToUpdate)
        {
            if (selectedMajors == null)
            {
                studentToUpdate.Admissions = new List<Admission>();
                return;
            }

            var selectedMajorsHS = new HashSet<int>(selectedMajors);
            foreach (var major in _context.Majors)
            {
                if (selectedMajorsHS.Contains(major.Id))
                {
                    studentToUpdate.Admissions.Add(new Admission { StudentId = studentToUpdate.Id, MajorId = major.Id });
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

            
            WebCache.Remove("LoggedIn"+id.ToString());

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Admit(int id, int userId)
        {
            var admittingPerson = _context.Teachers.FirstOrDefault(i => i.Id == userId);
            var admittedPerson = _context.Students.FirstOrDefault(i => i.Id == id);
            if (admittingPerson != null && admittedPerson != null && admittingPerson.Status == "Admin")
            {
                admittedPerson.Status = "Admitted";
                PersonCacheModel personCache = WebCache.Get("LoggedIn" + id.ToString());
                if (personCache != null)
                {
                    personCache.Status = admittedPerson.Status;
                    WebCache.Remove("LoggedIn" + id.ToString());
                }
                else
                {
                    personCache = new PersonCacheModel()
                    {
                        Id = id,
                        Role = "Student",
                        Status = admittedPerson.Status
                    };
                }
                WebCache.Set("LoggedIn" + id.ToString(), personCache, 60, true);
                _context.Update(admittedPerson);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        public async Task<IActionResult> Reject(int id, int userId)
        {
            var admittingPerson = _context.Teachers.FirstOrDefault(i => i.Id == userId);
            var admittedPerson = _context.Students.FirstOrDefault(i => i.Id == id);
            if (admittingPerson != null && admittedPerson != null && admittingPerson.Status == "Admin")
            {
                admittedPerson.Status = "Rejected";
                PersonCacheModel personCache = WebCache.Get("LoggedIn" + id.ToString());
                if (personCache != null)
                {
                    personCache.Status = admittedPerson.Status;
                    WebCache.Remove("LoggedIn" + id.ToString());
                }
                else
                {
                    personCache = new PersonCacheModel()
                    {
                        Id = id,
                        Role = "Student",
                        Status = admittedPerson.Status
                    };
                }
                WebCache.Set("LoggedIn" + id.ToString(), personCache, 60, true);
                _context.Update(admittedPerson);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.Id == id);
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
