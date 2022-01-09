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
using Npgsql;

namespace Priceless.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly HomeService _service;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment webHostEnvironment, HomeService service)
        {
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _service = service;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Majors"] = await _service.GetAllMajors();
            return View();
        }

        public IActionResult Logout()
        {
            string id;
            HttpContext.Request.Cookies.TryGetValue("Id", out id);
            WebCache.Remove("LoggedIn"+id);
            HttpContext.Response.Cookies.Delete("Id");
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
                //NpgsqlConnection conn = new("Server=localhost;Port=5432;Database=postgres;User Id=goldp1");
                SqlConnection conn = new("*");
                conn.Open();
                SqlCommand command = new("Select Discriminator from People where Login = @login");
                //NpgsqlCommand command = new("SELECT "+'"'+"Discriminator"+'"'+" FROM "+'"'+"People"+'"'+" WHERE "+'"'+"Login"+'"'+" = @login");
                command.Connection = conn;
                command.Parameters.AddWithValue("@login", person.Login);
                string role = command.ExecuteScalar().ToString();
                conn.Close();
                var cookieOptions = new Microsoft.AspNetCore.Http.CookieOptions()
                {
                    Path = "/",
                    HttpOnly = false,
                    IsEssential = true, //<- there
                    Secure = true,
                    Expires = DateTime.Now.AddMonths(1),
                };
                HttpContext.Response.Cookies.Append("Id", commonPerson.Id.ToString(), cookieOptions);
                PersonCacheModel personCache = new()
                {
                    Id = commonPerson.Id,
                    Image = commonPerson.Image,
                    Role = role,
                    Status = commonPerson.Status
                };
                WebCache.Set("LoggedIn"+commonPerson.Id, personCache, 60, true);
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

        public IActionResult FAQ()
        {
            return View();
        }

        public IActionResult Feedback()
        {
            return View();
        }

        public IActionResult TeacherPromote()
        {
            return View();
        }

        public async Task<IActionResult> StudentPromote()
        {
            ViewData["Majors"] = await _service.GetAllMajors();
            return View();
        }

        public IActionResult Contacts()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? statusCode = null)
        {
            if (statusCode.HasValue)
            {
                if (statusCode == 403)
                {
                    var viewName = statusCode.ToString();
                    return View(viewName);
                }
            }
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
