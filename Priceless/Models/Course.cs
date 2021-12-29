using System;
using System.Collections.Generic;

namespace Priceless.Models
{
    public class Course
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        public ICollection<Enrollment> Enrollments { get; set; }
        public ICollection<CourseAssignment> CourseAssignments { get; set; }
    }
}
