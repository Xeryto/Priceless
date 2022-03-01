﻿using System;
using System.Collections.Generic;

namespace Priceless.Models
{
    public class Page
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Json { get; set; }
        public int CourseId { get; set; }
        public Course Course { get; set; }
    }
}
