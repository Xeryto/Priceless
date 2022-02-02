using System;
using System.ComponentModel.DataAnnotations;

namespace Priceless.Models
{
    public class Stream
    {
        public int Id { get; set; }
        public string Name { get; set; }
        [DataType(DataType.Date)]
        public DateTime RegStart { get; set; }
        [DataType(DataType.Date)]
        public DateTime RegEnd { get; set; }
        [DataType(DataType.Date)]
        public DateTime Start { get; set; }
        [DataType(DataType.Date)]
        public DateTime End { get; set; }
        public bool RegAllowed { get; set; }
    }
}
