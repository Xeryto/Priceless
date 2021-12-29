using System;
using System.Collections.Generic;

namespace Priceless2.Models
{
    public class Teacher : Person
    {
        public ICollection<CourseAssignment> CourseAssignments { get; set; }
    }
}
