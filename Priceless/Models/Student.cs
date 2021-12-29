using System;
using System.Collections.Generic;

namespace Priceless.Models
{
    public class Student : Person
    {
        public int Grade { get; set; }
        public ICollection<Enrollment> Enrollments { get; set; }
    }
}
