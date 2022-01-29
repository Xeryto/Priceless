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
using AutoMapper;
using System.IO;
using System.Web.Helpers;
using Microsoft.AspNetCore.Http;

namespace Priceless.Controllers
{
    public class TeachersController : Controller
    {
        private readonly PricelessContext _context;
        private readonly MapperConfiguration config = new(cfg => cfg
            .CreateMap<TeacherPostModel, Teacher>().ForMember("Image", opt => opt.Ignore()));
        private readonly string code = Hash("*");

        public TeachersController(PricelessContext context)
        {
            _context = context;
        }


        // GET: Teachers
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
            return View(await _context.Teachers.Where(i => i.Status == "In process").ToListAsync());
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
            var command = _context.Teachers.Include(i => i.MajorAssignments).ThenInclude(i => i.Major).AsNoTracking();
            if (admitted)
            {
                command = command.Where(i => i.Status == "In process");
            }
            foreach (var majorId in selectedMajors)
            {
                command = command.Where(i => i.MajorAssignments.Where(a => a.MajorId == majorId).Any());
            }
            return View(await command.ToListAsync());
        }

        // GET: Teachers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacher = await _context.Teachers
                .Include(i => i.CourseAssignments).ThenInclude(i => i.Course)
                .Include(i => i.MajorAssignments).ThenInclude(i => i.Major)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (teacher == null)
            {
                return NotFound();
            }

            return View(teacher);
        }

        public IActionResult CreateAdmin()
        {
            var teacher = new Teacher();
            teacher.MajorAssignments = new List<MajorAssignment>();
            PopulateAssignedMajorData(teacher);
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdmin([Bind("Id,Login,Password,Name,Phone,VK,Grade,School,Image")] TeacherPostModel teacherPost, int[] selectedMajors, string Code, bool Curator)
        {
            var mapper = new Mapper(config);
            var teacher = mapper.Map<TeacherPostModel, Teacher>(teacherPost);
            teacher.MajorAssignments = new List<MajorAssignment>();
            if (ModelState.IsValid)
            {
                if (!_context.People.Any(p => p.Login == teacher.Login) && VerifyHashed(code, Code))
                {
                    if (teacherPost.Image != null)
                    {
                        var stream = new MemoryStream();
                        await teacherPost.Image.CopyToAsync(stream);
                        teacher.Image = stream.ToArray();
                    }

                    teacher.Password = Hash(teacher.Password);
                    if (Curator)
                    {
                        teacher.Status = "Curator";
                    }
                    else
                    {
                        teacher.Status = "Admin";
                    }
                    AddTeacherMajors(selectedMajors, teacher);
                    _context.Add(teacher);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    return View(teacherPost);
                }
            }
            PopulateAssignedMajorData(teacher);
            return View(teacherPost);
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
        public async Task<IActionResult> Create([Bind("Id,Login,Password,Name,Phone,VK,Grade,School,FirstQA,SecondQA,ThirdQA,Image")] TeacherPostModel teacherPost, int[] selectedMajors)
        {
            var mapper = new Mapper(config);
            var teacher = mapper.Map<TeacherPostModel, Teacher>(teacherPost);
            teacher.MajorAssignments = new List<MajorAssignment>();
            if (ModelState.IsValid)
            {
                if (! _context.People.Any(p => p.Login == teacher.Login))
                {
                    if (teacherPost.Image != null)
                    {
                        var stream = new MemoryStream();
                        await teacherPost.Image.CopyToAsync(stream);
                        teacher.Image = stream.ToArray();
                    }

                    teacher.Password = Hash(teacher.Password);
                    teacher.Status = "In process";
                    AddTeacherMajors(selectedMajors, teacher);
                    _context.Add(teacher);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    return View(teacherPost);
                }
            }
            PopulateAssignedMajorData(teacher);
            return View(teacherPost);
        }

        public async Task<IActionResult> EditImage(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacher = await _context.Teachers
                .Include(i => i.MajorAssignments).ThenInclude(i => i.Major)
                .Include(i => i.CourseAssignments).ThenInclude(i => i.Course)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (teacher == null)
            {
                return NotFound();
            }
            var teacherEdit = new ImageEditModel()
            {
                Id = teacher.Id
            };
            return View(teacherEdit);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditImage(int id, [Bind("Image")] ImageEditModel teacherEdit)
        {

            if (ModelState.IsValid)
            {
                var teacher = await _context.Teachers
                .Include(i => i.MajorAssignments).ThenInclude(i => i.Major)
                .Include(i => i.CourseAssignments).ThenInclude(i => i.Course)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

                if (teacherEdit.Image != null)
                {
                    var stream = new MemoryStream();
                    await teacherEdit.Image.CopyToAsync(stream);
                    teacher.Image = stream.ToArray();
                }

                PersonCacheModel editor = WebCache.Get("LoggedIn" + id.ToString());
                WebCache.Remove("LoggedIn"+id.ToString());
                editor.Image = teacher.Image;
                WebCache.Set("LoggedIn"+id.ToString(), editor, 60, true);
                _context.Update(teacher);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(teacherEdit);
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
            PopulateCurrentCourseData(teacher);
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

        private void PopulateCurrentCourseData(Teacher teacher)
        {
            var allCourse = teacher.CourseAssignments.Select(c => c.Course);
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
        public async Task<IActionResult> Edit(int? id, int[] selectedMajors, int[] selectedCourses, bool reconsider, string oldPas, string newPas)
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

            if (await TryUpdateModelAsync<Teacher>(
                teacherToUpdate,
                "",
                i => i.Name, i => i.Password, i => i.Phone, i => i.VK, i => i.Grade, i => i.School))
            {
                if (oldPas != null && VerifyHashed(teacherToUpdate.Password, oldPas))
                {
                    teacherToUpdate.Password = Hash(newPas);
                }
                if (reconsider)
                {
                    teacherToUpdate.Status = "In process";
                    PersonCacheModel personCache = WebCache.Get("LoggedIn" + teacherToUpdate.Id.ToString());
                    if (personCache != null)
                    {
                        personCache.Status = teacherToUpdate.Status;
                        WebCache.Remove("LoggedIn" + teacherToUpdate.Id.ToString());
                    }
                    else
                    {
                        personCache = new PersonCacheModel()
                        {
                            Id = teacherToUpdate.Id,
                            Role = "Teacher",
                            Status = teacherToUpdate.Status
                        };
                    }
                    WebCache.Set("LoggedIn" + teacherToUpdate.Id.ToString(), personCache, 60, true);
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
            PopulateCurrentCourseData(teacherToUpdate);
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

            WebCache.Remove("LoggedIn"+id.ToString());

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
