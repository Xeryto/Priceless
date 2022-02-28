using System;
using System.Collections.Generic;

namespace Priceless.Models.Helpers
{
    public class JsonResponse
    {
        public int success { get; set; }
        public string link { get; set; }
        public FileResponse file { get; set; }
        public LinkResponse meta { get; set; }
    }
}
