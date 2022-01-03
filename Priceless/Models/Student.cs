using System;
using System.Collections.Generic;

namespace Priceless.Models
{
    public class Student : Person
    {
        public string ParentName { get; set; }
        public string ParentPhone { get; set; }
        public string City { get; set; }
        public ICollection<Admission> Admissions { get; set; }
        public ICollection<Enrollment> Enrollments { get; set; }
    }
}
