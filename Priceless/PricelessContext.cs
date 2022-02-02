using System;
using Microsoft.EntityFrameworkCore;
using Priceless.Models;

namespace Priceless
{
    public class PricelessContext : DbContext
    {
        public PricelessContext(DbContextOptions<PricelessContext> options) : base(options)
        {
        }

        public DbSet<Course> Courses { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<CourseAssignment> CourseAssignments { get; set; }
        public DbSet<Person> People { get; set; }
        public DbSet<Major> Majors { get; set; }
        public DbSet<Admission> Admissions { get; set; }
        public DbSet<MajorAssignment> MajorAssignments { get; set; }
        public DbSet<Stream> Streams { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CourseAssignment>()
                .HasKey(c => new { c.CourseId, c.TeacherId });
            modelBuilder.Entity<Enrollment>()
                .HasKey(c => new { c.CourseId, c.StudentId });
            modelBuilder.Entity<Admission>()
                .HasKey(c => new { c.MajorId, c.StudentId });
            modelBuilder.Entity<MajorAssignment>()
                .HasKey(c => new { c.MajorId, c.TeacherId });
        }
    }
}