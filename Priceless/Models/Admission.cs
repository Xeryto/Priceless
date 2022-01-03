using System;
namespace Priceless.Models
{
    public class Admission
    {
        public int StudentId { get; set; }
        public int MajorId { get; set; }
        public Student Student { get; set; }
        public Major Major { get; set; }
    }
}
