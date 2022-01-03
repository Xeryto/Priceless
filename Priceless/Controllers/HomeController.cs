using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Priceless.Models;

using Excel = Microsoft.Office.Interop.Excel;
using ExcelAutoFormat = Microsoft.Office.Interop.Excel.XlRangeAutoFormat;
using Priceless.Models.Helpers;
using Priceless.Services;
using System.Web.Helpers;

namespace Priceless.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly PersonService _service;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment webHostEnvironment, PersonService service)
        {
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _service = service;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Logout()
        {
            WebCache.Remove("LoggedIn");
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(PersonLoginModel person)
        {
            if (await _service.Login(person.Login, person.Password))
            {
                var commonPerson = await _service.GetByLogin(person.Login);
                SqlConnection conn = new("*");
                conn.Open();
                SqlCommand command = new("SELECT Discriminator FROM People WHERE Login = @login");
                command.Connection = conn;
                command.Parameters.AddWithValue("@login", person.Login);
                string role = command.ExecuteScalar().ToString();
                conn.Close();
                PersonCacheModel personCache = new()
                {
                    Id = commonPerson.Id,
                    Image = commonPerson.Image,
                    Role = role
                };
                WebCache.Set("LoggedIn", personCache, 60, true);
                return RedirectToAction("Index", "Home");
            }
            else return View(new PersonLoginModel()
            {
                InfoNotValid = true
            });
        }

        public IActionResult Register()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
