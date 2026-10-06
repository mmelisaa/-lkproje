using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models.Siniflar;
using WebApplication1.Models.Siniflar.Kurumsal;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ============ DASHBOARD ============
        public IActionResult Index()
        {
            ViewBag.BlogSayisi = _context.BlogPosts.Count();
            ViewBag.ReferansSayisi = _context.ReferansProjeler.Count();
            return View();
        }

        // ============ BLOG YÖNETİMİ ============
        public async Task<IActionResult> BlogListesi()
        {
            var bloglar = await _context.BlogPosts.ToListAsync();
            return View(bloglar);
        }

        public IActionResult BlogEkle()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlogEkle(BlogPost blog)
        {
            if (ModelState.IsValid)
            {
                _context.BlogPosts.Add(blog);
                await _context.SaveChangesAsync();
                TempData["Mesaj"] = "Blog yazısı başarıyla eklendi.";
                return RedirectToAction(nameof(BlogListesi));
            }
            return View(blog);
        }

        public async Task<IActionResult> BlogDuzenle(int? id)
        {
            if (id == null) return NotFound();
            var blog = await _context.BlogPosts.FindAsync(id);
            if (blog == null) return NotFound();
            return View(blog);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlogDuzenle(int id, BlogPost blog)
        {
            if (id != blog.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(blog);
                await _context.SaveChangesAsync();
                TempData["Mesaj"] = "Blog yazısı başarıyla güncellendi.";
                return RedirectToAction(nameof(BlogListesi));
            }
            return View(blog);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlogSil(int id)
        {
            var blog = await _context.BlogPosts.FindAsync(id);
            if (blog != null)
            {
                _context.BlogPosts.Remove(blog);
                await _context.SaveChangesAsync();
                TempData["Mesaj"] = "Blog yazısı silindi.";
            }
            return RedirectToAction(nameof(BlogListesi));
        }

        // ============ REFERANS YÖNETİMİ ============
        public async Task<IActionResult> ReferansListesi()
        {
            var referanslar = await _context.ReferansProjeler.ToListAsync();
            return View(referanslar);
        }

        public IActionResult ReferansEkle()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReferansEkle(ReferansProje referans)
        {
            if (ModelState.IsValid)
            {
                _context.ReferansProjeler.Add(referans);
                await _context.SaveChangesAsync();
                TempData["Mesaj"] = "Referans proje başarıyla eklendi.";
                return RedirectToAction(nameof(ReferansListesi));
            }
            return View(referans);
        }

        public async Task<IActionResult> ReferansDuzenle(int? id)
        {
            if (id == null) return NotFound();
            var referans = await _context.ReferansProjeler.FindAsync(id);
            if (referans == null) return NotFound();
            return View(referans);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReferansDuzenle(int id, ReferansProje referans)
        {
            if (id != referans.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(referans);
                await _context.SaveChangesAsync();
                TempData["Mesaj"] = "Referans proje başarıyla güncellendi.";
                return RedirectToAction(nameof(ReferansListesi));
            }
            return View(referans);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReferansSil(int id)
        {
            var referans = await _context.ReferansProjeler.FindAsync(id);
            if (referans != null)
            {
                _context.ReferansProjeler.Remove(referans);
                await _context.SaveChangesAsync();
                TempData["Mesaj"] = "Referans proje silindi.";
            }
            return RedirectToAction(nameof(ReferansListesi));
        }

        // ============ HAKKIMIZDA YÖNETİMİ ============
        public async Task<IActionResult> HakkimizdaDuzenle()
        {
            var hakkimizda = await _context.Hakkimizda.FirstOrDefaultAsync();
            if (hakkimizda == null)
            {
                hakkimizda = new Hakkimizda();
                _context.Hakkimizda.Add(hakkimizda);
                await _context.SaveChangesAsync();
            }
            return View(hakkimizda);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HakkimizdaDuzenle(Hakkimizda model)
        {
            if (ModelState.IsValid)
            {
                _context.Update(model);
                await _context.SaveChangesAsync();
                TempData["Mesaj"] = "Hakkımızda sayfası başarıyla güncellendi.";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // ============ İNSAN KAYNAKLARI YÖNETİMİ ============
        public async Task<IActionResult> InsanKaynaklariDuzenle()
        {
            var ik = await _context.InsanKaynaklari.FirstOrDefaultAsync();
            if (ik == null)
            {
                ik = new InsanKaynaklari();
                _context.InsanKaynaklari.Add(ik);
                await _context.SaveChangesAsync();
            }
            return View(ik);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InsanKaynaklariDuzenle(InsanKaynaklari model)
        {
            if (ModelState.IsValid)
            {
                _context.Update(model);
                await _context.SaveChangesAsync();
                TempData["Mesaj"] = "İnsan Kaynakları sayfası başarıyla güncellendi.";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }
    }
}
