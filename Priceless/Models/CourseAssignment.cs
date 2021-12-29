using System;
namespace Priceless2.Models
{
    public class CourseAssignment
    {
        public int CourseId { get; set; }
        public int TeacherId { get; set; }
        public Course Course { get; set; }
        public Teacher Teacher { get; set; }
    }
}
