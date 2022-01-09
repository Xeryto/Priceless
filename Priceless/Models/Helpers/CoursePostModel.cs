using System;
using Microsoft.AspNetCore.Http;

namespace Priceless.Models.Helpers
{
    public class CoursePostModel : Course
    {
        public IFormFile Image { get; set; }
    }
}
