using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using Priceless;
using Priceless.Models;

namespace Priceless.Controllers
{
    public class StreamsController : Controller
    {
        private readonly PricelessContext _context;

        public StreamsController(PricelessContext context)
        {
            _context = context;
        }

        // GET: Streams
        public async Task<IActionResult> Index()
        {
            return View(await _context.Streams.ToListAsync());
        }

        public async Task<IActionResult> Manage(int userId, bool open)
        {
            var curStream = await _context.Streams.Where(i => i.Start <= DateTime.Now && i.End >= DateTime.Now).FirstOrDefaultAsync();
            var person = await _context.Teachers.FirstOrDefaultAsync(i => i.Id == userId);
            if (person != null)
            {
                if (person.Status == "Admin" || person.Status == "Curator")
                {
                    curStream.RegAllowed = open;
                    curStream.Notified = open;
                    _context.Update(curStream);
                    await _context.SaveChangesAsync();

                    if (open)
                    {
                        var emailMessage = new MimeMessage();

                        emailMessage.From.Add(new MailboxAddress("Администрация сайта", "priceless.edu@mail.ru"));
                        var students = await _context.Students.ToListAsync();
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
                            await client.AuthenticateAsync("priceless.edu@mail.ru", "*");
                            await client.SendAsync(emailMessage);

                            await client.DisconnectAsync(true);
                        }
                    }
                }
            }
            return RedirectToAction(nameof(Index), "Home");
        }

        public async Task<IActionResult> Close(int userId)
        {
            var person = await _context.Teachers.FirstOrDefaultAsync(i => i.Id == userId);
            if (person != null)
            {
                if (person.Status == "Admin" || person.Status == "Curator")
                {
                    var students = await _context.Students.ToListAsync();
                    foreach (var student in students)
                    {
                        student.Admissions.Clear();
                        student.Status = "Inactive";
                    }
                }
            }
            return RedirectToAction(nameof(Index), "Home");
        }

        // GET: Streams/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stream = await _context.Streams
                .FirstOrDefaultAsync(m => m.Id == id);
            if (stream == null)
            {
                return NotFound();
            }

            return View(stream);
        }

        // GET: Streams/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Streams/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,RegStart,RegEnd,Start,End,RegAllowed")] Stream stream)
        {
            if (ModelState.IsValid)
            {
                stream.Notified = false;
                _context.Add(stream);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(stream);
        }

        // GET: Streams/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stream = await _context.Streams.FindAsync(id);
            if (stream == null)
            {
                return NotFound();
            }
            return View(stream);
        }

        // POST: Streams/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,RegStart,RegEnd,Start,End,RegAllowed")] Stream stream)
        {
            if (id != stream.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(stream);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StreamExists(stream.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(stream);
        }

        // GET: Streams/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stream = await _context.Streams
                .FirstOrDefaultAsync(m => m.Id == id);
            if (stream == null)
            {
                return NotFound();
            }

            return View(stream);
        }

        // POST: Streams/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var stream = await _context.Streams.FindAsync(id);
            _context.Streams.Remove(stream);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StreamExists(int id)
        {
            return _context.Streams.Any(e => e.Id == id);
        }
    }
}
