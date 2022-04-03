using System;
namespace Priceless.Models
{
    public class MajorAssignment
    {
        public int MajorId { get; set; }
        public int TeacherId { get; set; }
        public Major Major { get; set; }
        public Teacher Teacher { get; set; }
        public string Status { get; set; }
        public string StatusComment { get; set; }
    }
}
