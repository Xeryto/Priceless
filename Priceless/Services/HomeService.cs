using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Priceless.Models;

namespace Priceless.Services
{
    public class HomeService
    {
        private readonly PricelessContext _context;

        public HomeService(PricelessContext context)
        {
            _context = context;
        }

        public async Task<Stream> GetCurrentStream()
        {
            return await _context.Streams.Where(i => i.Start <= DateTime.Now && i.End >= DateTime.Now).FirstOrDefaultAsync();
        }

        public async Task<Stream> UpdateStream(Stream stream)
        {
            _context.Update(stream);
            await _context.SaveChangesAsync();
            return stream;
        }

        public async Task<List<Student>> GetAllStudents()
        {
            return await _context.Students.ToListAsync();
        }

        public async Task<List<Major>> GetAllMajors()
        {
            return await _context.Majors.ToListAsync();
        }

        public async Task<List<Teacher>> GetAllAdmins()
        {
            return await _context.Teachers.Where(i => i.Status == "Admin" && i.Id != 1).ToListAsync();
        }

        public async Task<List<Teacher>> GetAllCurators()
        {
            return await _context.Teachers.Where(i => i.Status == "Curator").Include(i => i.CourseAssignments).ThenInclude(i => i.Course).Include(i => i.MajorAssignments).ThenInclude(i => i.Major).AsNoTracking().ToListAsync();
        }

        public async Task<Person> GetByLogin(string login)
        {
            return await _context.People.FirstOrDefaultAsync(x => x.Login == login);
        }

        public async Task<Person> GetPersonById(int id)
        {
            return await _context.People.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Teacher> GetTeacherById(int id)
        {
            return await _context.Teachers.Include(i => i.CourseAssignments).ThenInclude(i => i.Course).Include(i => i.MajorAssignments).ThenInclude(i => i.Major).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Student> GetStudentById(int id)
        {
            return await _context.Students.Include(i => i.Admissions).ThenInclude(i => i.Major).Include(i => i.Enrollments).ThenInclude(i => i.Course).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Person> GetById(string id)
        {
            var people = await _context.People.ToListAsync();
            return people.Where(i => VerifyHashed(id, i.Id.ToString())).FirstOrDefault();
        }

        public async Task<Person> UpdatePerson(Person person)
        {
            _context.Update(person);
            await _context.SaveChangesAsync();
            return person;
        }

        public async Task<bool> Login(string login, string password)
        {
            var person = await GetByLogin(login);
            if (person != null)
            {
                return VerifyHashed(person.Password, password);
            }
            else
            {
                return false;
            }
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
