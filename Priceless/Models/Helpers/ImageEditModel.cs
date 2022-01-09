using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Priceless.Models.Helpers
{
    public class ImageEditModel
    {
        public int Id { get; set; }
        public IFormFile Image { get; set; }
    }
}
