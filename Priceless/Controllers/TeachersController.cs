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
            teacher.MajorAssignments = new List<MajorAssignment>();
            PopulateAssignedMajorData(teacher);
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
                if (! _context.People.Any(p => p.Login == teacher.Login))
                {
                    teacher.Password = Hash(teacher.Password);
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
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (teacher == null)
            {
                return NotFound();
            }
            PopulateAssignedMajorData(teacher);
            return View(teacher);
        }

        private void PopulateAssignedMajorData(Teacher teacher)
        {
            var allMajors = _context.Majors;
            var instructorMajors = new HashSet<int>(teacher.MajorAssignments.Select(c => c.MajorId));
            var viewModel = new List<AssignedMajorData>();
            foreach (var Major in allMajors)
            {
                viewModel.Add(new AssignedMajorData
                {
                    MajorId = Major.Id,
                    Title = Major.Title,
                    Assigned = instructorMajors.Contains(Major.Id)
                });
            }
            ViewData["Majors"] = viewModel;
        }

        // POST: Teachers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, int[] selectedMajors)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacherToUpdate = await _context.Teachers
                .Include(i => i.MajorAssignments)
                    .ThenInclude(i => i.Major)
                .FirstOrDefaultAsync(m => m.Id == id);
            var pass = teacherToUpdate.Password;

            if (await TryUpdateModelAsync<Teacher>(
                teacherToUpdate,
                "",
                i => i.Name, i => i.Password))
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
            PopulateAssignedMajorData(teacherToUpdate);
            return View(teacherToUpdate);
        }

        private void UpdateTeacherMajors(int[] selectedMajors, Teacher teacherToUpdate)
        {
            if (selectedMajors == null)
            {
                teacherToUpdate.MajorAssignments = new List<MajorAssignment>();
                return;
            }

            var selectedMajorsHS = new HashSet<int>(selectedMajors);
            var instructorMajors = new HashSet<int>
                (teacherToUpdate.MajorAssignments.Select(c => c.Major.Id));
            foreach (var Major in _context.Majors)
            {
                if (selectedMajorsHS.Contains(Major.Id))
                {
                    if (!instructorMajors.Contains(Major.Id))
                    {
                        teacherToUpdate.MajorAssignments.Add(new MajorAssignment { TeacherId = teacherToUpdate.Id, MajorId = Major.Id });
                    }
                }
                else
                {

                    if (instructorMajors.Contains(Major.Id))
                    {
                        MajorAssignment MajorToRemove = teacherToUpdate.MajorAssignments.FirstOrDefault(i => i.MajorId == Major.Id);
                        _context.Remove(MajorToRemove);
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
                .Include(i => i.MajorAssignments)
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
