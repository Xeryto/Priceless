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
using System.IO;
using System.Web.Helpers;
using Microsoft.AspNetCore.Http;

namespace Priceless.Controllers
{
    public class StudentsController : Controller
    {
        private readonly PricelessContext _context;

        public StudentsController(PricelessContext context)
        {
            _context = context;
        }

        // GET: Students
        public async Task<IActionResult> Index()
        {
            var majors = await _context.Majors.ToListAsync();
            ViewData["Majors"] = majors;
            ViewData["Process"] = true;
            Dictionary<int, bool> selectedMajors = new();
            foreach (var major in majors)
            {
                selectedMajors.Add(major.Id, false);
            }
            ViewData["SelectedMajors"] = selectedMajors;
            return View(await _context.Students.Include(i => i.Admissions).ThenInclude(i => i.Major).AsNoTracking().Where(i => i.Admissions.Where(i => i.Status == "In process").Any() || i.Status == "In process").ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(int[] selectedMajors, bool admitted)
        {
            var majors = await _context.Majors.ToListAsync();
            ViewData["Majors"] = majors;
            Dictionary<int, bool> selectedMajorsDict = new();
            foreach (var major in majors)
            {
                selectedMajorsDict.Add(major.Id, selectedMajors.Contains(major.Id));
            }
            ViewData["SelectedMajors"] = selectedMajorsDict;
            ViewData["Process"] = admitted;
            var command = _context.Students.Include(i => i.Admissions).ThenInclude(i => i.Major).AsNoTracking();
            if (selectedMajors.Length != 0)
            {
                if (admitted)
                {
                    command = command.Where(i => i.Admissions.Where(a => selectedMajors.Contains(a.MajorId) && a.Status == "In process").Any());
                }
                else
                {
                    command = command.Where(i => i.Admissions.Where(a => selectedMajors.Contains(a.MajorId)).Any());
                }
            }
            else if (admitted)
            {
                command = command.Where(i => i.Admissions.Where(i => i.Status == "In process").Any() || i.Status == "In process");
            }
            
            return View(await command.ToListAsync());
        }

        // GET: Students/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(i => i.Admissions).ThenInclude(i => i.Major)
                .Include(i => i.Enrollments).ThenInclude(i => i.Course)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (student == null)
            {
                return NotFound();
            }
            var curStream = await _context.Streams.Where(i => i.RegAllowed == true || (i.RegStart <= DateTime.Now && i.RegEnd >= DateTime.Now)).FirstOrDefaultAsync();
            if (curStream == null)
            {
                ViewData["Open"] = false;
                var nextStream = await _context.Streams.Where(i => i.RegStart >= DateTime.Now).FirstOrDefaultAsync();
                ViewData["Stream"] = nextStream;
            }
            else
            {
                ViewData["Open"] = true;
                ViewData["Stream"] = curStream;
            }
            
            return View(student);
        }

        // GET: Students/Create
        public IActionResult Create()
        {
            var student = new Student();
            student.Admissions = new List<Admission>();
            PopulateAssignedMajorData(student);
            var curStream = _context.Streams.Where(i => i.RegAllowed == true || (i.RegStart <= DateTime.Now && i.RegEnd >= DateTime.Now)).FirstOrDefault();
            ViewData["Open"] = curStream != null;
            return View();
        }

        // POST: Students/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Grade,Id,Login,Password,Name,ParentName,Phone,ParentPhone,City,FirstQA,SecondQA,ThirdQA,Image")] Student student, int[] selectedMajors)
        {
            var curStream = _context.Streams.Where(i => i.RegAllowed == true || (i.RegStart <= DateTime.Now && i.RegEnd >= DateTime.Now)).FirstOrDefault();
            student.Admissions = new List<Admission>();
            if (ModelState.IsValid)
            {
                if (!_context.People.Any(p  => p.Login == student.Login))
                {
                    student.Password = Hash(student.Password);
                    if (curStream != null)
                    {
                        student.Status = "In process";
                    }
                    else
                    {
                        student.Status = "Inactive";
                    }
                    AddStudentMajors(selectedMajors, student);
                    _context.Add(student);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index), "Home");
                }
                else
                {
                    return View(student);
                }
            }
            PopulateAssignedMajorData(student);
            ViewData["Open"] = curStream != null;
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
                .Include(i => i.Admissions).ThenInclude(i => i.Major)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (student == null)
            {
                return NotFound();
            }
            PopulateAssignedMajorData(student);
            var curStream = _context.Streams.Where(i => i.RegAllowed == true || (i.RegStart <= DateTime.Now && i.RegEnd >= DateTime.Now)).FirstOrDefault();
            ViewData["Open"] = curStream != null;
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
        public async Task<IActionResult> Edit(int? id, int[] selectedMajors, string oldPas, string newPas, bool deleteImage)
        {
            var studentToUpdate = await _context.Students
                .Include(i => i.Admissions)
                    .ThenInclude(i => i.Major)
                .FirstOrDefaultAsync(m => m.Id == id);

            var pass = studentToUpdate.Password;

            if (await TryUpdateModelAsync<Student>(
                studentToUpdate,
                "",
                i => i.Name, i => i.Password, i => i.Grade, i => i.Phone, i => i.ParentName, i => i.ParentPhone, i => i.City, i => i.Image))
            {
                if (deleteImage)
                {
                    studentToUpdate.Image = null;
                }
                if (oldPas != null && VerifyHashed(studentToUpdate.Password, oldPas))
                {
                    studentToUpdate.Password = Hash(newPas);
                }
                UpdateStudentMajors(selectedMajors, studentToUpdate);
                if (studentToUpdate.Admissions.Where(i => i.Status == "In process").Any() && studentToUpdate.Status != "Admitted")
                {
                    studentToUpdate.Status = "In process";
                }
                string ids;
                HttpContext.Request.Cookies.TryGetValue("Id", out ids);
                PersonCacheModel personCache = WebCache.Get("LoggedIn" + studentToUpdate.Id.ToString());
                if (personCache != null)
                {
                    personCache.Status = studentToUpdate.Status;
                    personCache.Image = studentToUpdate.Image;
                    WebCache.Remove("LoggedIn" + studentToUpdate.Id.ToString());
                }
                else
                {
                    personCache = new PersonCacheModel()
                    {
                        Id = studentToUpdate.Id,
                        Role = "Student",
                        Status = studentToUpdate.Status,
                        Image = studentToUpdate.Image
                    };
                }
                WebCache.Set("LoggedIn" + studentToUpdate.Id.ToString(), personCache, 60, true);
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
                return RedirectToAction(nameof(Index), "Home");
            }
            UpdateStudentMajors(selectedMajors, studentToUpdate);
            PopulateAssignedMajorData(studentToUpdate);
            var curStream = _context.Streams.Where(i => i.RegAllowed == true || (i.RegStart <= DateTime.Now && i.RegEnd >= DateTime.Now)).FirstOrDefault();
            ViewData["Open"] = curStream != null;
            return View(studentToUpdate);
        }

        private void UpdateStudentMajors(int[] selectedMajors, Student studentToUpdate, bool? banChange = null)
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
                        studentToUpdate.Admissions.Add(new Admission { StudentId = studentToUpdate.Id, MajorId = major.Id, Status = "In process" });
                    }
                }
                else
                {
                    if (studentCourses.Contains(major.Id))
                    {                        Admission AdmissionToRemove = studentToUpdate.Admissions.FirstOrDefault(i => i.MajorId == major.Id);
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
                    studentToUpdate.Admissions.Add(new Admission { StudentId = studentToUpdate.Id, MajorId = major.Id, Status = "In process" });
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

        private static bool VerifyHashed(string hashedPassword, string password)
        {
            byte[] buffer4;
            if (hashedPassword == null)
            {
                return false;
            }
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }
            byte[] src = Convert.FromBase64String(hashedPassword);
            if ((src.Length != 0x31) || (src[0] != 0))
            {
                return false;
            }
            byte[] dst = new byte[0x10];
            Buffer.BlockCopy(src, 1, dst, 0, 0x10);
            byte[] buffer3 = new byte[0x20];
            Buffer.BlockCopy(src, 0x11, buffer3, 0, 0x20);
            using (Rfc2898DeriveBytes bytes = new(password, dst, 0x3e8))
            {
                buffer4 = bytes.GetBytes(0x20);
            }
            return ByteArraysEqual(buffer3, buffer4);
        }

        private static bool ByteArraysEqual(byte[] b1, byte[] b2)
        {
            if (b1 == b2) return true;
            if (b1 == null || b2 == null) return false;
            if (b1.Length != b2.Length) return false;
            for (int i = 0; i < b1.Length; i++)
            {
                if (b1[i] != b2[i]) return false;
            }
            return true;
        }
    }
}
