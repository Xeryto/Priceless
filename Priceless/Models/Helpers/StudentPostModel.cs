﻿using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Priceless.Models.Helpers
{
    public class StudentPostModel : Student
    {
        public IFormFile Image { get; set; }
    }
}
