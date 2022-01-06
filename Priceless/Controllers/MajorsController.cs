using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Helpers;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Priceless;
using Priceless.Models;
using Priceless.Models.Helpers;

namespace Priceless.Controllers
{
    public class MajorsController : Controller
    {
        private readonly PricelessContext _context;
        private readonly MapperConfiguration config = new(cfg => cfg
            .CreateMap<MajorPostModel, Major>().ForMember("Image", opt => opt.Ignore()));
        private readonly MapperConfiguration inverse = new(cfg => cfg
            .CreateMap<Major, MajorPostModel>().ForMember("Image", opt => opt.Ignore()));

        public MajorsController(PricelessContext context)
        {
            _context = context;
        }

        // GET: Majors
        public async Task<IActionResult> Index()
        {
            return View(await _context.Majors.ToListAsync());
        }

        // GET: Majors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var major = await _context.Majors
                .FirstOrDefaultAsync(m => m.Id == id);
            if (major == null)
            {
                return NotFound();
            }

            return View(major);
        }

        // GET: Majors/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Majors/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Image")] MajorPostModel majorPost)
        {
            var mapper = new Mapper(config);
            var major = mapper.Map<MajorPostModel, Major>(majorPost);
            if (ModelState.IsValid)
            {
                if (majorPost.Image != null)
                {
                    var stream = new MemoryStream();
                    await majorPost.Image.CopyToAsync(stream);
                    major.Image = stream.ToArray();
                }

                _context.Add(major);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(majorPost);
        }

        // GET: Majors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var major = await _context.Majors.FindAsync(id);
            if (major == null)
            {
                return NotFound();
            }

            var mapper = new Mapper(inverse);
            var majorUpdate = mapper.Map<Major, MajorPostModel>(major);

            return View(majorUpdate);
        }

        // POST: Majors/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Image")] MajorPostModel majorEdit)
        {
            if (id != majorEdit.Id)
            {
                return NotFound();
            }

            var mapper = new Mapper(config);
            var major = mapper.Map<MajorPostModel, Major>(majorEdit);

            if (ModelState.IsValid)
            {
                if (majorEdit.Image != null)
                {
                    var stream = new MemoryStream();
                    await majorEdit.Image.CopyToAsync(stream);
                    major.Image = stream.ToArray();
                }

                try
                {
                    _context.Update(major);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MajorExists(major.Id))
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
            return View(major);
        }

        // GET: Majors/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var major = await _context.Majors
                .FirstOrDefaultAsync(m => m.Id == id);
            if (major == null)
            {
                return NotFound();
            }

            return View(major);
        }

        // POST: Majors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var major = await _context.Majors
                .Include(i => i.Admissions)
                .Include(i => i.MajorAssignments)
                .SingleAsync(i => i.Id == id);

            _context.Majors.Remove(major);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MajorExists(int id)
        {
            return _context.Majors.Any(e => e.Id == id);
        }
    }
}
