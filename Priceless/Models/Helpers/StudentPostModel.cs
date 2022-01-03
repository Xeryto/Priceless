using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Priceless.Models.Helpers
{
    public class StudentPostModel
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public IFormFile Image { get; set; }
        public int Grade { get; set; }
        public ICollection<Enrollment> Enrollments { get; set; }
    }
}
