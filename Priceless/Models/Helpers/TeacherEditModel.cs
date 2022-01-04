using System;
using Microsoft.AspNetCore.Http;

namespace Priceless.Models.Helpers
{
    public class TeacherEditModel
    {
        public int Id { get; set; }
        public IFormFile Image { get; set; }
        public int UserId { get; set; }
        public bool EditNotValid { get; set; }
    }
}
