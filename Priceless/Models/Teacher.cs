using System;
using System.Collections.Generic;

namespace Priceless.Models
{
    public class Teacher : Person
    {
        public ICollection<CourseAssignment> CourseAssignments { get; set; }
    }
}
