using System;
using Microsoft.AspNetCore.Http;

namespace Priceless.Models.Helpers
{
    public class TeacherPostModel : Teacher
    {
        public IFormFile Image { get; set; }
    }
}
