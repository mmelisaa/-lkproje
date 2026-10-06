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
        public async Task<IActionResult> Index()
        {
            ViewBag.BlogSayisi = await _context.BlogPosts.CountAsync();
            ViewBag.ReferansSayisi = await _context.ReferansProjeler.CountAsync();
            ViewBag.MesajSayisi = await _context.SiteMesajlari.CountAsync();
            ViewBag.OkunmamisMesaj = await _context.SiteMesajlari.CountAsync(m => !m.Okundu);

            // Ziyaretçi istatistikleri
            var bugun = DateTime.Today;
            ViewBag.BugunkuZiyaret = await _context.ZiyaretciLoglari.CountAsync(z => z.ZiyaretTarihi.Date == bugun);
            ViewBag.HaftalikZiyaret = await _context.ZiyaretciLoglari.CountAsync(z => z.ZiyaretTarihi >= bugun.AddDays(-7));
            ViewBag.AylikZiyaret = await _context.ZiyaretciLoglari.CountAsync(z => z.ZiyaretTarihi >= bugun.AddDays(-30));
            ViewBag.ToplamZiyaret = await _context.ZiyaretciLoglari.CountAsync();

            // Son 5 mesaj
            ViewBag.SonMesajlar = await _context.SiteMesajlari
                .OrderByDescending(m => m.GonderimTarihi)
                .Take(5)
                .ToListAsync();

            return View();
        }

        // ============ ZİYARETÇİ İSTATİSTİKLERİ (JSON API) ============
        [HttpGet]
        public async Task<IActionResult> ZiyaretciVerileri(string periyot = "haftalik")
        {
            var bugun = DateTime.Today;
            object data;

            if (periyot == "gunluk")
            {
                // Son 24 saat, saatlik
                var saatler = Enumerable.Range(0, 24).Select(i => bugun.AddHours(i)).ToList();
                var counts = new List<int>();
                foreach (var saat in saatler)
                {
                    counts.Add(await _context.ZiyaretciLoglari.CountAsync(z => z.ZiyaretTarihi >= saat && z.ZiyaretTarihi < saat.AddHours(1)));
                }
                data = new { labels = saatler.Select(s => s.ToString("HH:00")), values = counts };
            }
            else if (periyot == "haftalik")
            {
                // Son 7 gün
                var gunler = Enumerable.Range(0, 7).Select(i => bugun.AddDays(-6 + i)).ToList();
                var counts = new List<int>();
                foreach (var gun in gunler)
                {
                    counts.Add(await _context.ZiyaretciLoglari.CountAsync(z => z.ZiyaretTarihi.Date == gun));
                }
                data = new { labels = gunler.Select(g => g.ToString("dd MMM")), values = counts };
            }
            else if (periyot == "aylik")
            {
                // Son 30 gün
                var gunler = Enumerable.Range(0, 30).Select(i => bugun.AddDays(-29 + i)).ToList();
                var counts = new List<int>();
                foreach (var gun in gunler)
                {
                    counts.Add(await _context.ZiyaretciLoglari.CountAsync(z => z.ZiyaretTarihi.Date == gun));
                }
                data = new { labels = gunler.Select(g => g.ToString("dd MMM")), values = counts };
            }
            else // yillik
            {
                // Son 12 ay
                var aylar = Enumerable.Range(0, 12).Select(i => bugun.AddMonths(-11 + i)).ToList();
                var counts = new List<int>();
                foreach (var ay in aylar)
                {
                    var ayBaslangic = new DateTime(ay.Year, ay.Month, 1);
                    var ayBitis = ayBaslangic.AddMonths(1);
                    counts.Add(await _context.ZiyaretciLoglari.CountAsync(z => z.ZiyaretTarihi >= ayBaslangic && z.ZiyaretTarihi < ayBitis));
                }
                data = new { labels = aylar.Select(a => a.ToString("MMM yyyy")), values = counts };
            }

            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> ZiyaretciKarsilastirma()
        {
            var bugun = DateTime.Today;

            // Bu hafta vs geçen hafta
            var buHaftaBas = bugun.AddDays(-(int)bugun.DayOfWeek + 1);
            var gecenHaftaBas = buHaftaBas.AddDays(-7);
            var buHafta = await _context.ZiyaretciLoglari.CountAsync(z => z.ZiyaretTarihi >= buHaftaBas);
            var gecenHafta = await _context.ZiyaretciLoglari.CountAsync(z => z.ZiyaretTarihi >= gecenHaftaBas && z.ZiyaretTarihi < buHaftaBas);

            // Bu ay vs geçen ay
            var buAyBas = new DateTime(bugun.Year, bugun.Month, 1);
            var gecenAyBas = buAyBas.AddMonths(-1);
            var buAy = await _context.ZiyaretciLoglari.CountAsync(z => z.ZiyaretTarihi >= buAyBas);
            var gecenAy = await _context.ZiyaretciLoglari.CountAsync(z => z.ZiyaretTarihi >= gecenAyBas && z.ZiyaretTarihi < buAyBas);

            // Bu yıl vs geçen yıl
            var buYilBas = new DateTime(bugun.Year, 1, 1);
            var gecenYilBas = buYilBas.AddYears(-1);
            var buYil = await _context.ZiyaretciLoglari.CountAsync(z => z.ZiyaretTarihi >= buYilBas);
            var gecenYil = await _context.ZiyaretciLoglari.CountAsync(z => z.ZiyaretTarihi >= gecenYilBas && z.ZiyaretTarihi < buYilBas);

            return Json(new
            {
                hafta = new { mevcut = buHafta, onceki = gecenHafta },
                ay = new { mevcut = buAy, onceki = gecenAy },
                yil = new { mevcut = buYil, onceki = gecenYil }
            });
        }

        // ============ MESAJ YÖNETİMİ ============
        public async Task<IActionResult> MesajListesi(string? filtre)
        {
            var query = _context.SiteMesajlari.AsQueryable();
            if (!string.IsNullOrEmpty(filtre) && filtre != "Tümü")
                query = query.Where(m => m.FormTipi == filtre);

            ViewBag.Filtre = filtre ?? "Tümü";
            var mesajlar = await query.OrderByDescending(m => m.GonderimTarihi).ToListAsync();
            return View(mesajlar);
        }

        public async Task<IActionResult> MesajDetay(int? id)
        {
            if (id == null) return NotFound();
            var mesaj = await _context.SiteMesajlari.FindAsync(id);
            if (mesaj == null) return NotFound();

            if (!mesaj.Okundu)
            {
                mesaj.Okundu = true;
                await _context.SaveChangesAsync();
            }
            return View(mesaj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MesajSil(int id)
        {
            var mesaj = await _context.SiteMesajlari.FindAsync(id);
            if (mesaj != null)
            {
                _context.SiteMesajlari.Remove(mesaj);
                await _context.SaveChangesAsync();
                TempData["Mesaj"] = "Mesaj silindi.";
            }
            return RedirectToAction(nameof(MesajListesi));
        }

        // ============ BLOG YÖNETİMİ ============
        public async Task<IActionResult> BlogListesi()
        {
            var bloglar = await _context.BlogPosts.ToListAsync();
            return View(bloglar);
        }

        public IActionResult BlogEkle() => View();

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
                TempData["Mesaj"] = "Blog yazısı güncellendi.";
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

        public IActionResult ReferansEkle() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReferansEkle(ReferansProje referans)
        {
            if (ModelState.IsValid)
            {
                _context.ReferansProjeler.Add(referans);
                await _context.SaveChangesAsync();
                TempData["Mesaj"] = "Referans proje eklendi.";
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
                TempData["Mesaj"] = "Referans proje güncellendi.";
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
                TempData["Mesaj"] = "Hakkımızda sayfası güncellendi.";
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
                TempData["Mesaj"] = "İnsan Kaynakları sayfası güncellendi.";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }
    }
}
