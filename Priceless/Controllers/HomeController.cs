﻿using System;
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
using Microsoft.AspNetCore.Http;
using MimeKit;
using MailKit.Net.Smtp;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Net;
using System.Text.Json;

namespace Priceless.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly HomeService _service;
        private readonly string domain = "http://pricelessedu.azurewebsites.net/";

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment webHostEnvironment, HomeService service)
        {
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _service = service;
        }

        [HttpPost]
        public async Task<string> SaveFile(IFormFile file)
        {
            if (file != null)
            {

            }
            if (file != null && file.Length > 0)
            {
                var fileName = Path.GetFileName(file.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot/files", fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
                FileResponse rfile = new()
                {
                    url = domain + "files/" + fileName,
                    name = fileName
                };
                JsonResponse response = new()
                {
                    success = 1,
                    file = rfile
                };
                return JsonSerializer.Serialize(response);
            }
            JsonResponse response1 = new()
            {
                success = 0
            };
            return JsonSerializer.Serialize(response1);
        }

        public string SaveLink(string url)
        {
            JsonResponse response = new();
            if (url != null)
            {
                if (!url.Contains("http"))
                {
                    response.link = "https://" + url;
                }
                else
                {
                    response.link = url;
                }
            }
            else
            {
                response.link = url;
            }
            LinkResponse rlink = new()
            {
                title = url
            };
            response.success = 1;
            response.meta = rlink;
            return JsonSerializer.Serialize(response);
        }

        public async Task<IActionResult> Index()
        {
            var curStream = await _service.GetCurrentStream();

            if (curStream != null)
            {
                if (!curStream.Notified)
                {
                    var emailMessage = new MimeMessage();

                    emailMessage.From.Add(new MailboxAddress("Администрация сайта", "priceless.edu@mail.ru"));
                    var students = await _service.GetAllStudents();
                    foreach (var student in students)
                    {
                        emailMessage.To.Add(new MailboxAddress("", student.Login));
                    }
                    emailMessage.Subject = "Открыта регистрация в Priceless";
                    emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                    {
                        Text = "<p>Здравствуйте! Рады сообщить, что мы открываем запись на новый поток проекта Priceless education. Следить за изменением статуса заявки вы сможете в личном кабинете на нашем <a href='https://pricelessedu.azurewebsites.net'>сайте</a>.</p>"
                    };

                    using (var client = new SmtpClient())
                    {
                        await client.ConnectAsync("smtp.mail.ru", 465, true);
                        await client.AuthenticateAsync("priceless.edu@mail.ru", "Jja6jrrANxQMrDXt4pYb");
                        await client.SendAsync(emailMessage);

                        await client.DisconnectAsync(true);
                    }

                    curStream.Notified = true;
                    await _service.UpdateStream(curStream);
                }
            }
            

            ViewData["Majors"] = await _service.GetAllMajors();
            ViewData["Admins"] = await _service.GetAllAdmins();
            ViewData["Curators"] = await _service.GetAllCurators();
            return View();
        }

        public IActionResult Restore(bool exists = true)
        {
            ViewData["Exists"] = exists;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(string login)
        {
            var person = await _service.GetByLogin(login);

            if (person == null)
            {
                Restore(false);
            }

            var emailMessage = new MimeMessage();

            emailMessage.From.Add(new MailboxAddress("Администрация сайта", "priceless.edu@mail.ru"));
            emailMessage.To.Add(new MailboxAddress("", login));
            emailMessage.Subject = "Восстановление пароля";
            var hash = Hash(person.Id.ToString()).Replace("/", "slash");
            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = "<p>Здравствуйте! Вы запросили восстановление пароля. Если это были вы, перейдите по <a href='https://pricelessedu.azurewebsites.net/Home/RestoreConfirm/"+hash+"'>ссылке</a> для смены пароля. Иначе, проигнорируйте это письмо.</p>"
            };

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync("smtp.mail.ru", 465, true);
                await client.AuthenticateAsync("priceless.edu@mail.ru", "Jja6jrrANxQMrDXt4pYb");
                await client.SendAsync(emailMessage);

                await client.DisconnectAsync(true);
            }

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> RestoreConfirm(string id)
        {
            id = id.Replace("slash", "/");
            var person = await _service.GetById(id);
            ViewData["Exists"] = person != null;
            ViewData["Id"] = id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreConfirm(string password, string id)
        {
            var person = await _service.GetById(id);

            if (person != null)
            {
                person.Password = Hash(password);
                await _service.UpdatePerson(person);
                return RedirectToAction("Index", "Home");
            }
            ViewData["Exists"] = person != null;
            ViewData["Id"] = id;
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
                SqlConnection conn = new("Persist Security Info=False;User ID=igoshin;Password=-!Игошин!-;Initial Catalog=igoshin2;Server=78.29.44.70\\SQLSERVER");
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

        public async Task<IActionResult> Manage(int id, int userId, bool admit)
        {
            var admittingPerson = await _service.GetPersonById(userId);
            var admittedPerson = await _service.GetPersonById(id);
            if (admittingPerson != null && admittedPerson != null && (admittingPerson.Status == "Admin" || admittingPerson.Status == "Curator"))
            {
                ViewData["id"] = id;
                ViewData["userId"] = userId;
                ViewData["admit"] = admit;
                return View();
            }
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(int id, int userId, string comment, bool admit)
        {
            var admittingPerson = await _service.GetPersonById(userId);
            var admittedPerson = await _service.GetPersonById(id);
            if (admittingPerson != null && admittedPerson != null && (admittingPerson.Status == "Admin" || admittingPerson.Status == "Curator"))
            {
                if (admit)
                {
                    admittedPerson.Status = "Admitted";
                }
                else
                {
                    admittedPerson.Status = "Rejected";
                }
                admittedPerson.StatusComment = comment;
                PersonCacheModel personCache = WebCache.Get("LoggedIn" + id.ToString());
                if (personCache != null)
                {
                    personCache.Status = admittedPerson.Status;
                    WebCache.Remove("LoggedIn" + id.ToString());
                }
                else
                {
                    personCache = new PersonCacheModel()
                    {
                        Id = id,
                        Role = "Teacher",
                        Status = admittedPerson.Status
                    };
                }
                WebCache.Set("LoggedIn" + id.ToString(), personCache, 60, true);
                await _service.UpdatePerson(admittedPerson);

                var emailMessage = new MimeMessage();

                emailMessage.From.Add(new MailboxAddress("Администрация сайта", "priceless.edu@mail.ru"));
                emailMessage.To.Add(new MailboxAddress("", admittedPerson.Login));
                emailMessage.Subject = "Заявка в Priceless";
                emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                {
                    Text = "<p>Здравствуйте! Вы подавали заявку на занятия в проекте Priceless Education. Сообщаем, что ваша заявка рассмотрена, с нашим решением вы можете ознакомиться на нашем <a href='https://pricelessedu.azurewebsites.net'>сайте,</a> зайдя в личный кабинет.</p>"
                };

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync("smtp.mail.ru", 465, true);
                    await client.AuthenticateAsync("priceless.edu@mail.ru", "Jja6jrrANxQMrDXt4pYb");
                    await client.SendAsync(emailMessage);

                    await client.DisconnectAsync(true);
                }

                return RedirectToAction("Index");
            }
            return StatusCode(StatusCodes.Status403Forbidden);
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

        private static string Hash(string password)
        {
            byte[] salt;
            byte[] buffer2;
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }
            using (Rfc2898DeriveBytes bytes = new(password, 0x10, 0x3e8))
            {
                salt = bytes.Salt;
                buffer2 = bytes.GetBytes(0x20);
            }
            byte[] dst = new byte[0x31];
            Buffer.BlockCopy(salt, 0, dst, 1, 0x10);
            Buffer.BlockCopy(buffer2, 0, dst, 0x11, 0x20);
            return Convert.ToBase64String(dst);
        }
    }
}
