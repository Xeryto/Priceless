using System;
using Microsoft.AspNetCore.Http;

namespace Priceless.Models.Helpers
{
    public class MajorPostModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public IFormFile Image { get; set; }
    }
}
