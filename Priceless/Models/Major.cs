﻿using System;
using System.Collections.Generic;

namespace Priceless.Models
{
    public class Major
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public ICollection<Admission> Admissions { get; set; }
    }
}