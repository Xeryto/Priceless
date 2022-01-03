using System;
using System.Collections.Generic;

namespace Priceless.Models
{
    public class Teacher : Person
    {
        public string VK { get; set; }
        public string School { get; set; }
        public ICollection<CourseAssignment> CourseAssignments { get; set; }
        public ICollection<MajorAssignment> MajorAssignments { get; set; }
    }
}
