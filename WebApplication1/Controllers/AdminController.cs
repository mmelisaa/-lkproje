using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Filters;
using WebApplication1.Models;
using WebApplication1.Models.Siniflar;
using WebApplication1.Models.Siniflar.Kurumsal;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = Roller.Personel)]
    [ServiceFilter(typeof(AdminGirisLogFilter))]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISeoService _seoService;
        private readonly IEmailService _emailService;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ISeoService seoService,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _seoService = seoService;
            _emailService = emailService;
        }

        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            var sessionOk = HttpContext.Session.GetString("AdminGirisYapildi") == "true";
            if (!sessionOk || User.Identity?.IsAuthenticated != true)
            {
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = HttpContext.Request.Path.Value });
            }
        }

        // ============ DASHBOARD ============
        public async Task<IActionResult> Index()
        {
            ViewBag.BlogSayisi = await _context.BlogPosts.CountAsync();
            ViewBag.ReferansSayisi = await _context.ReferansProjeler.CountAsync();
            ViewBag.MesajSayisi = await _context.SiteMesajlari.CountAsync();
            ViewBag.OkunmamisMesaj = await _context.SiteMesajlari.CountAsync(m => !m.Okundu || m.Durum == CrmDurumlari.Yeni);
            ViewBag.BekleyenBayiSayisi = await _userManager.Users.CountAsync(u => u.HesapDurumu == HesapDurumlari.Beklemede);
            ViewBag.AktifRmaSayisi = await _context.RmaKayitlari.CountAsync(r => r.Durum != "Müşteriye Teslim Edildi");

            // Ziyaretçi istatistikleri
            var bugun = DateTime.Today;
            var yarin = bugun.AddDays(1);
            ViewBag.BugunkuZiyaret = await _context.ZiyaretciLoglari.CountAsync(z => z.ZiyaretTarihi >= bugun && z.ZiyaretTarihi < yarin);
            ViewBag.HaftalikZiyaret = await _context.ZiyaretciLoglari.CountAsync(z => z.ZiyaretTarihi >= bugun.AddDays(-7));
            ViewBag.AylikZiyaret = await _context.ZiyaretciLoglari.CountAsync(z => z.ZiyaretTarihi >= bugun.AddDays(-30));
            ViewBag.YillikZiyaret = await _context.ZiyaretciLoglari.CountAsync(z => z.ZiyaretTarihi >= bugun.AddDays(-365));
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
                var simdi = DateTime.Now;
                var baslangicSaat = new DateTime(simdi.Year, simdi.Month, simdi.Day, simdi.Hour, 0, 0).AddHours(-23);
                var saatler = Enumerable.Range(0, 24).Select(i => baslangicSaat.AddHours(i)).ToList();
                var counts = new List<int>();
                foreach (var saat in saatler)
                {
                    var sonrakiSaat = saat.AddHours(1);
                    counts.Add(await _context.ZiyaretciLoglari.CountAsync(z => z.ZiyaretTarihi >= saat && z.ZiyaretTarihi < sonrakiSaat));
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
                    var sonrakiGun = gun.AddDays(1);
                    counts.Add(await _context.ZiyaretciLoglari.CountAsync(z => z.ZiyaretTarihi >= gun && z.ZiyaretTarihi < sonrakiGun));
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
                    var sonrakiGun = gun.AddDays(1);
                    counts.Add(await _context.ZiyaretciLoglari.CountAsync(z => z.ZiyaretTarihi >= gun && z.ZiyaretTarihi < sonrakiGun));
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
            var yarin = bugun.AddDays(1);
            var dun = bugun.AddDays(-1);

            // Bugün vs dün
            var buGunSayisi = await _context.ZiyaretciLoglari.CountAsync(z => z.ZiyaretTarihi >= bugun && z.ZiyaretTarihi < yarin);
            var dunSayisi = await _context.ZiyaretciLoglari.CountAsync(z => z.ZiyaretTarihi >= dun && z.ZiyaretTarihi < bugun);

            // Bu hafta vs geçen hafta (Pazartesi başlangıçlı)
            int diff = (7 + (bugun.DayOfWeek - DayOfWeek.Monday)) % 7;
            var buHaftaBas = bugun.AddDays(-1 * diff);
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
                gun = new { mevcut = buGunSayisi, onceki = dunSayisi },
                hafta = new { mevcut = buHafta, onceki = gecenHafta },
                ay = new { mevcut = buAy, onceki = gecenAy },
                yil = new { mevcut = buYil, onceki = gecenYil }
            });
        }

        // ============ MESAJ YÖNETİMİ & MİNİ-CRM ============
        public async Task<IActionResult> MesajListesi(string? filtre, string? durum)
        {
            var query = _context.SiteMesajlari.AsQueryable();
            if (!string.IsNullOrEmpty(filtre) && filtre != "Tümü")
                query = query.Where(m => m.FormTipi == filtre);
            if (!string.IsNullOrEmpty(durum) && durum != "Tümü")
                query = query.Where(m => m.Durum == durum);

            ViewBag.Filtre = filtre ?? "Tümü";
            ViewBag.DurumFiltre = durum ?? "Tümü";
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
                if (string.IsNullOrEmpty(mesaj.Durum) || mesaj.Durum == CrmDurumlari.Yeni)
                    mesaj.Durum = CrmDurumlari.Inceleniyor;
                mesaj.SonIslemTarihi = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            return View(mesaj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MesajCrmGuncelle(int id, string durum, string? atananKisi, string? dahiliNotlar, string? cevapMetni)
        {
            var mesaj = await _context.SiteMesajlari.FindAsync(id);
            if (mesaj == null) return NotFound();

            mesaj.Durum = string.IsNullOrWhiteSpace(durum) ? CrmDurumlari.Inceleniyor : durum;
            mesaj.AtananKisi = string.IsNullOrWhiteSpace(atananKisi) ? User.Identity?.Name : atananKisi.Trim();
            mesaj.DahiliNotlar = dahiliNotlar?.Trim();
            mesaj.SonIslemTarihi = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(cevapMetni))
            {
                mesaj.CevapMetni = cevapMetni.Trim();
                mesaj.CevapTarihi = DateTime.Now;
                mesaj.Durum = CrmDurumlari.Cevaplandi;
                await _emailService.SendAsync(
                    mesaj.Eposta,
                    $"Talebiniz Hakkında Yanıt - {mesaj.Konu ?? mesaj.FormTipi}",
                    $"<p>Sayın {WebUtility.HtmlEncode(mesaj.AdSoyad)},</p>" +
                    $"<p>{WebUtility.HtmlEncode(mesaj.CevapMetni).Replace("\n", "<br/>")}</p>" +
                    $"<hr/><p><small>Hayat İşleri Kurumsal Müşteri İlişkileri ({WebUtility.HtmlEncode(mesaj.AtananKisi)})</small></p>");
                TempData["Mesaj"] = "CRM durumu güncellendi ve yanıtınız müşteriye e-posta olarak iletildi.";
            }
            else
            {
                TempData["Mesaj"] = $"Talep durumu '{mesaj.Durum}' olarak güncellendi.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(MesajDetay), new { id });
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
        // ============ ADMİN GİRİŞ LOGLARI ============
        public async Task<IActionResult> GirisLoglari(int sayfa = 1)
        {
            int sayfaBasi = 25;
            var toplamKayit = await _context.AdminGirisLoglari.CountAsync();
            var loglar = await _context.AdminGirisLoglari
                .OrderByDescending(l => l.GirisTarihi)
                .Skip((sayfa - 1) * sayfaBasi)
                .Take(sayfaBasi)
                .ToListAsync();

            ViewBag.Sayfa = sayfa;
            ViewBag.ToplamSayfa = (int)Math.Ceiling((double)toplamKayit / sayfaBasi);
            ViewBag.ToplamKayit = toplamKayit;
            return View(loglar);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GirisLogTemizle()
        {
            var eskiKayitlar = _context.AdminGirisLoglari
                .Where(l => l.GirisTarihi < DateTime.Now.AddDays(-90));
            _context.AdminGirisLoglari.RemoveRange(eskiKayitlar);
            await _context.SaveChangesAsync();
            TempData["Mesaj"] = "90 günden eski giriş logları temizlendi.";
            return RedirectToAction(nameof(GirisLoglari));
        }

        // ============ KURUMSAL ERP & B2B / CPQ / RMA / SAAS / SLA MODÜLÜ ============
        public IActionResult KurumsalModuller(string modul = "TeklifCpq")
        {
            ViewBag.SeciliModul = modul;

            if (modul == "RmaServis" || modul == "SeriNoTakip" || modul == "CihazYasamDongusu" || modul == "GarantiBakim")
            {
                ViewBag.RmaKayitlari = _context.RmaKayitlari
                    .Include(r => r.Hareketler)
                    .OrderByDescending(r => r.SonGuncelleme)
                    .ToList();
            }

            if (modul == "KritikIslemLoglari" || modul == "ApiEntegrasyonLoglari")
            {
                ViewBag.DenetimKayitlari = _context.DenetimKayitlari
                    .OrderByDescending(d => d.Zaman)
                    .Take(100)
                    .ToList();
            }

            if (modul == "SiteAyarlari" || modul == "SeoYonetimi")
            {
                ViewBag.SeoAyarlari = _context.SeoAyarlari
                    .OrderBy(s => s.SayfaKodu)
                    .ToList();
            }

            if (modul == "RbacRoller" || modul == "Bayiler")
            {
                var kullanicilar = _userManager.Users.OrderByDescending(u => u.KayitTarihi).ToList();
                var rolMap = new Dictionary<string, string>();
                foreach (var u in kullanicilar)
                {
                    var roller = _userManager.GetRolesAsync(u).GetAwaiter().GetResult();
                    rolMap[u.Id] = roller.FirstOrDefault() ?? Roller.Bayi;
                }
                ViewBag.Kullanicilar = kullanicilar;
                ViewBag.KullaniciRolleri = rolMap;
            }

            return View("KurumsalErpModulu");
        }

        // ============ RMA (ARIZA, İADE, DEĞİŞİM, KARGO & GARANTİ) İŞLEMLERİ ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RmaEkle(
            string musteriAdi, string? eposta, string telefon, string cihazModeli, string seriNo,
            string arizaAciklamasi, bool garantiKapsaminda, int garantiKalanGun, string? garantiNotu,
            string cozum, string? degisenParca, decimal ucret, string durum, string konum,
            string kargoYonu, string? kargoFirmasi, string? kargoTakipNo, string? atananTeknisyen, string oncelik = "Normal")
        {
            var yil = DateTime.Now.Year;
            var sira = await _context.RmaKayitlari.CountAsync() + 1;
            var rmaNo = $"RMA-{yil}-{sira:D4}";

            var kayit = new RmaKaydi
            {
                RmaNo = rmaNo,
                MusteriAdi = musteriAdi.Trim(),
                Eposta = string.IsNullOrWhiteSpace(eposta) ? null : eposta.Trim(),
                Telefon = telefon.Trim(),
                CihazModeli = cihazModeli.Trim(),
                SeriNo = seriNo.Trim(),
                ArizaAciklamasi = arizaAciklamasi.Trim(),
                GarantiKapsaminda = garantiKapsaminda,
                GarantiBitisTarihi = DateTime.Now.Date.AddDays(garantiKalanGun),
                GarantiNotu = string.IsNullOrWhiteSpace(garantiNotu)
                    ? (garantiKapsaminda ? "Resmi distribütör garantisi kapsamında." : "Garanti süresi dolmuş veya kullanıcı hatası.")
                    : garantiNotu.Trim(),
                Cozum = string.IsNullOrWhiteSpace(cozum) ? "Belirlenmedi" : cozum,
                DegisenParca = string.IsNullOrWhiteSpace(degisenParca) ? null : degisenParca.Trim(),
                Ucret = garantiKapsaminda ? 0m : ucret,
                Durum = string.IsNullOrWhiteSpace(durum) ? "Teslim Alındı" : durum,
                Konum = string.IsNullOrWhiteSpace(konum) ? "Merkez Servis (Elazığ)" : konum,
                KargoYonu = string.IsNullOrWhiteSpace(kargoYonu) ? "Kargo Yok" : kargoYonu,
                KargoFirmasi = string.IsNullOrWhiteSpace(kargoFirmasi) ? null : kargoFirmasi.Trim(),
                KargoTakipNo = string.IsNullOrWhiteSpace(kargoTakipNo) ? null : kargoTakipNo.Trim(),
                KargoTarihi = !string.IsNullOrWhiteSpace(kargoTakipNo) ? DateTime.Now : null,
                AtananTeknisyen = string.IsNullOrWhiteSpace(atananTeknisyen) ? "Emre Can" : atananTeknisyen.Trim(),
                Oncelik = string.IsNullOrWhiteSpace(oncelik) ? "Normal" : oncelik,
                KabulTarihi = DateTime.Now,
                SonGuncelleme = DateTime.Now
            };
            kayit.Hareketler.Add(new RmaHareket
            {
                Zaman = DateTime.Now,
                Durum = kayit.Durum,
                Konum = kayit.Konum,
                Aciklama = $"RMA servis kaydı açıldı. İşlem: {kayit.Cozum}, Garanti: {(kayit.GarantiKapsaminda ? "Kapsamda" : "Garanti Dışı")}.",
                Kullanici = User.Identity?.Name ?? "admin"
            });

            _context.RmaKayitlari.Add(kayit);
            await _context.SaveChangesAsync();
            TempData["Mesaj"] = $"{rmaNo} numaralı RMA servis kaydı oluşturuldu.";
            return RedirectToAction(nameof(KurumsalModuller), new { modul = "RmaServis" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RmaGuncelle(
            int id, string durum, string konum, string cozum, string? degisenParca,
            bool garantiKapsaminda, string? garantiNotu, decimal ucret,
            string kargoYonu, string? kargoFirmasi, string? kargoTakipNo, string? hareketNotu)
        {
            var rma = await _context.RmaKayitlari.Include(r => r.Hareketler).FirstOrDefaultAsync(r => r.Id == id);
            if (rma == null) return NotFound();

            rma.Durum = durum;
            rma.Konum = konum;
            rma.Cozum = cozum;
            rma.DegisenParca = string.IsNullOrWhiteSpace(degisenParca) ? null : degisenParca.Trim();
            rma.GarantiKapsaminda = garantiKapsaminda;
            if (!string.IsNullOrWhiteSpace(garantiNotu)) rma.GarantiNotu = garantiNotu.Trim();
            rma.Ucret = garantiKapsaminda ? 0m : ucret;
            rma.KargoYonu = kargoYonu;
            rma.KargoFirmasi = string.IsNullOrWhiteSpace(kargoFirmasi) ? null : kargoFirmasi.Trim();
            rma.KargoTakipNo = string.IsNullOrWhiteSpace(kargoTakipNo) ? null : kargoTakipNo.Trim();
            if (!string.IsNullOrWhiteSpace(kargoTakipNo)) rma.KargoTarihi = DateTime.Now;
            if (durum == "Müşteriye Teslim Edildi" || durum == "Teslim Edildi") rma.KapanisTarihi = DateTime.Now;
            rma.SonGuncelleme = DateTime.Now;

            rma.Hareketler.Add(new RmaHareket
            {
                Zaman = DateTime.Now,
                Durum = durum,
                Konum = konum,
                Aciklama = string.IsNullOrWhiteSpace(hareketNotu)
                    ? $"Durum: {durum} | Konum: {konum} | İşlem: {cozum} {(rma.KargoTakipNo != null ? $"| Kargo: {rma.KargoFirmasi} ({rma.KargoTakipNo})" : "")}"
                    : hareketNotu.Trim(),
                Kullanici = User.Identity?.Name ?? "admin"
            });

            await _context.SaveChangesAsync();
            TempData["Mesaj"] = $"{rma.RmaNo} servis kaydı güncellendi (Konum: {konum}, Durum: {durum}).";
            return RedirectToAction(nameof(KurumsalModuller), new { modul = "RmaServis" });
        }

        // ============ DİNAMİK SEO & SLUG YÖNETİMİ ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeoGuncelle(int id, string? slug, string metaBaslik, string metaAciklama, string? anahtarKelimeler, bool indexle = true)
        {
            var seo = await _context.SeoAyarlari.FindAsync(id);
            if (seo == null) return NotFound();

            seo.Slug = string.IsNullOrWhiteSpace(slug) ? null : slug.Trim().Trim('/').ToLowerInvariant();
            seo.MetaBaslik = metaBaslik.Trim();
            seo.MetaAciklama = metaAciklama.Trim();
            seo.AnahtarKelimeler = string.IsNullOrWhiteSpace(anahtarKelimeler) ? null : anahtarKelimeler.Trim();
            seo.Aktif = indexle;
            seo.Robots = indexle ? "index, follow" : "noindex, nofollow";
            seo.GuncellemeTarihi = DateTime.Now;
            seo.GuncelleyenKullanici = User.Identity?.Name ?? "admin";

            await _context.SaveChangesAsync();
            _seoService.Temizle();
            TempData["Mesaj"] = $"'{seo.SayfaAdi}' sayfasının SEO meta etiketleri ve URL (slug) yapısı güncellendi.";
            return RedirectToAction(nameof(KurumsalModuller), new { modul = "SiteAyarlari" });
        }

        // ============ BAYİ HESAP ONAYI & ROL (RBAC) YÖNETİMİ ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BayiOnayla(string userId, string hesapDurumu, string? cariKodu, decimal? iskontoOrani, string? yeniRol, int? portalTipi = null, string geriDonModul = "Bayiler")
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var eskiDurum = user.HesapDurumu;
            user.HesapDurumu = hesapDurumu;
            if (!string.IsNullOrWhiteSpace(cariKodu)) user.CariKodu = cariKodu.Trim();
            else if (string.IsNullOrWhiteSpace(user.CariKodu) && hesapDurumu == HesapDurumlari.Aktif)
                user.CariKodu = $"HYT-{Random.Shared.Next(3500, 9900)}";

            if (iskontoOrani.HasValue) user.IskontoOrani = iskontoOrani.Value;
            if (portalTipi.HasValue && portalTipi.Value is 1 or 2 or 3) user.BayiPortalTipi = portalTipi.Value;

            if (hesapDurumu == HesapDurumlari.Aktif)
            {
                user.EmailConfirmed = true;
                user.OnayTarihi = DateTime.Now;
                user.OnaylayanKullanici = User.Identity?.Name ?? "admin";
            }

            await _userManager.UpdateAsync(user);

            if (!string.IsNullOrWhiteSpace(yeniRol) && Roller.Hepsi.Contains(yeniRol))
            {
                var mevcutRoller = await _userManager.GetRolesAsync(user);
                if (!mevcutRoller.Contains(yeniRol))
                {
                    await _userManager.RemoveFromRolesAsync(user, mevcutRoller);
                    await _userManager.AddToRoleAsync(user, yeniRol);
                }
            }

            _context.DenetimKayitlari.Add(new DenetimKaydi
            {
                Zaman = DateTime.Now,
                KullaniciAdi = User.Identity?.Name ?? "admin",
                Rol = "Admin",
                IpAdresi = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "bilinmiyor",
                Eylem = "HesapOnayVeRol",
                TabloAdi = "AspNetUsers",
                KayitId = user.Id,
                Ozet = $"{user.UserName} ({user.FirmaAdi}) durumu '{eskiDurum}' -> '{hesapDurumu}', Cari: {user.CariKodu}, İskonto: %{user.IskontoOrani} olarak güncellendi."
            });
            await _context.SaveChangesAsync();

            if (eskiDurum != HesapDurumlari.Aktif && hesapDurumu == HesapDurumlari.Aktif && !string.IsNullOrWhiteSpace(user.Email))
            {
                await _emailService.SendAsync(
                    user.Email,
                    "Bayilik Hesabınız Onaylandı - Hayat İşleri B2B Portalı",
                    $"<p>Sayın {WebUtility.HtmlEncode(user.AdSoyad)},</p>" +
                    $"<p><strong>{WebUtility.HtmlEncode(user.FirmaAdi)}</strong> için yaptığınız B2B bayilik başvurusu onaylanmıştır.</p>" +
                    $"<p><strong>Cari Kodunuz:</strong> {WebUtility.HtmlEncode(user.CariKodu)}<br/><strong>Bayi İskonto Oranınız:</strong> %{user.IskontoOrani:0.##}</p>");
            }

            TempData["Mesaj"] = $"{user.FirmaAdi} ({user.UserName}) hesabı '{hesapDurumu}' olarak güncellendi.";
            return RedirectToAction(nameof(KurumsalModuller), new { modul = geriDonModul });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PersonelEkle(string userName, string email, string adSoyad, string sifre, string rol, string? firmaAdi)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(sifre))
            {
                TempData["Mesaj"] = "Kullanıcı adı, e-posta ve şifre zorunludur.";
                return RedirectToAction(nameof(KurumsalModuller), new { modul = "RbacRoller" });
            }

            var seciliRol = Roller.Hepsi.Contains(rol) ? rol : Roller.Editor;
            var hasher = new PasswordHasher<ApplicationUser>();
            var user = new ApplicationUser
            {
                UserName = userName.Trim(),
                Email = email.Trim(),
                EmailConfirmed = true,
                AdSoyad = adSoyad?.Trim() ?? userName.Trim(),
                FirmaAdi = string.IsNullOrWhiteSpace(firmaAdi) ? "Hayat İşleri" : firmaAdi.Trim(),
                HesapDurumu = HesapDurumlari.Aktif,
                OnayTarihi = DateTime.Now,
                OnaylayanKullanici = User.Identity?.Name ?? "admin"
            };
            user.PasswordHash = hasher.HashPassword(user, sifre);

            var sonuc = await _userManager.CreateAsync(user);
            if (sonuc.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, seciliRol);
                TempData["Mesaj"] = $"'{user.UserName}' kullanıcısı '{seciliRol}' rolüyle oluşturuldu.";
            }
            else
            {
                TempData["Mesaj"] = "Hata: " + string.Join(", ", sonuc.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(KurumsalModuller), new { modul = "RbacRoller" });
        }

        // ============ SOL MENÜ ROTA ALIAS'LARI (TÜM BAĞLANTILAR KESİNTİSİZ ÇALIŞIR) ============
        [Route("Admin/Home/Index")]
        public IActionResult AdminHomeIndex() => RedirectToAction(nameof(Index));

        [Route("Admin/Slider/Index")]
        public IActionResult Slider() => KurumsalModuller("Slider");

        [Route("Admin/AnasayfaAksiyonKarti/Index")]
        public IActionResult AnasayfaAksiyonKarti() => KurumsalModuller("AnasayfaAksiyonKarti");

        [Route("Admin/CozumUrunu/Index")]
        public IActionResult CozumUrunu() => KurumsalModuller("CozumUrunu");

        [Route("Admin/SirketOlcegi/Index")]
        public IActionResult SirketOlcegi() => KurumsalModuller("SirketOlcegi");

        [Route("Admin/Sektor/Index")]
        public IActionResult Sektor() => KurumsalModuller("Sektor");

        [Route("Admin/YazilimUrunleri/Index")]
        public IActionResult YazilimUrunleri() => KurumsalModuller("YazilimUrunleri");

        [Route("Admin/DonanimUrunleri/Index")]
        public IActionResult DonanimUrunleri() => KurumsalModuller("DonanimUrunleri");

        [Route("Admin/Markalar/Index")]
        public IActionResult Markalar() => KurumsalModuller("Markalar");

        [Route("Admin/Kategoriler/Index")]
        public IActionResult Kategoriler() => KurumsalModuller("Kategoriler");

        [Route("Admin/Bayiler/Index")]
        public IActionResult Bayiler() => KurumsalModuller("Bayiler");

        [Route("Admin/BayiGruplari/Index")]
        public IActionResult BayiGruplari() => KurumsalModuller("BayiGruplari");

        [Route("Admin/BankaHesaplari/Index")]
        public IActionResult BankaHesaplari() => KurumsalModuller("BankaHesaplari");

        [Route("Admin/Lisanslar/Index")]
        public IActionResult Lisanslar() => KurumsalModuller("Lisanslar");

        [Route("Admin/Siparisler/Index")]
        public IActionResult Siparisler() => KurumsalModuller("Siparisler");

        [Route("Admin/KurumsalSayfalar/Index")]
        public IActionResult KurumsalSayfalar() => RedirectToAction(nameof(HakkimizdaDuzenle));

        [Route("Admin/Hizmetler/Index")]
        public IActionResult Hizmetler() => KurumsalModuller("Hizmetler");

        [Route("Admin/OzelProjeler/Index")]
        public IActionResult OzelProjeler() => KurumsalModuller("OzelProjeler");

        [Route("Admin/Referanslar/Index")]
        public IActionResult Referanslar() => RedirectToAction(nameof(ReferansListesi));

        [Route("Admin/BlogYazilari/Index")]
        public IActionResult BlogYazilari() => RedirectToAction(nameof(BlogListesi));

        [Route("Admin/Dokumanlar/Index")]
        public IActionResult Dokumanlar() => KurumsalModuller("Dokumanlar");

        [Route("Admin/Kampanyalar/Index")]
        public IActionResult Kampanyalar() => KurumsalModuller("Kampanyalar");

        [Route("Admin/KampanyaBasvurulari/Index")]
        public IActionResult KampanyaBasvurulari() => KurumsalModuller("KampanyaBasvurulari");

        [Route("Admin/DestekTalepleri/Index")]
        public IActionResult DestekTalepleri() => KurumsalModuller("DestekTalepleri");

        [Route("Admin/IletisimMesajlari/Index")]
        public IActionResult IletisimMesajlari() => RedirectToAction(nameof(MesajListesi));

        [Route("Admin/IsIlanlari/Index")]
        public IActionResult IsIlanlari() => KurumsalModuller("IsIlanlari");

        [Route("Admin/IsBasvurulari/Index")]
        public IActionResult IsBasvurulari() => RedirectToAction(nameof(MesajListesi), new { filtre = "İş Başvurusu" });

        [Route("Admin/DinamikFormlar/Index")]
        public IActionResult DinamikFormlar() => KurumsalModuller("DinamikFormlar");

        [Route("Admin/SiteAyarlari/Index")]
        public IActionResult SiteAyarlari() => KurumsalModuller("SiteAyarlari");

        [Route("Admin/Menuler/Index")]
        public IActionResult Menuler() => KurumsalModuller("Menuler");

        [Route("Admin/YasalMetinler/Index")]
        public IActionResult YasalMetinler() => KurumsalModuller("YasalMetinler");

        [Route("Admin/BayiSozlesmeOnaylari/Index")]
        public IActionResult BayiSozlesmeOnaylari() => KurumsalModuller("BayiSozlesmeOnaylari");

        [Route("Admin/CihazYasamDongusu/Index")]
        public IActionResult CihazYasamDongusu() => KurumsalModuller("CihazYasamDongusu");

        [Route("Admin/ApiEntegrasyonLoglari/Index")]
        public IActionResult ApiEntegrasyonLoglari() => KurumsalModuller("ApiEntegrasyonLoglari");

        [Route("Admin/KritikIslemLoglari/Index")]
        public IActionResult KritikIslemLoglari() => KurumsalModuller("KritikIslemLoglari");

        [Route("Admin/SatisPrimYonetimi/Index")]
        public IActionResult SatisPrimYonetimi() => KurumsalModuller("SatisPrimYonetimi");

        [Route("Admin/IadeMutabakatYonetimi/Index")]
        public IActionResult IadeMutabakatYonetimi() => KurumsalModuller("IadeMutabakatYonetimi");

        [Route("Admin/TopluIletisimMerkezi/Index")]
        public IActionResult TopluIletisimMerkezi() => KurumsalModuller("TopluIletisimMerkezi");

        [Route("Admin/DovizKurYonetimi/Index")]
        public IActionResult DovizKurYonetimi() => KurumsalModuller("DovizKurYonetimi");

        [Route("Admin/DepoLokasyonYonetimi/Index")]
        public IActionResult DepoLokasyonYonetimi() => KurumsalModuller("DepoLokasyonYonetimi");

        [Route("Admin/TeklifCpq/Index")]
        public IActionResult TeklifCpq() => KurumsalModuller("TeklifCpq");

        [Route("Admin/PaketlemeBundle/Index")]
        public IActionResult PaketlemeBundle() => KurumsalModuller("PaketlemeBundle");

        [Route("Admin/IskontoOnay/Index")]
        public IActionResult IskontoOnay() => KurumsalModuller("IskontoOnay");

        [Route("Admin/SeriNoTakip/Index")]
        public IActionResult SeriNoTakip() => KurumsalModuller("SeriNoTakip");

        [Route("Admin/GarantiBakim/Index")]
        public IActionResult GarantiBakim() => KurumsalModuller("GarantiBakim");

        [Route("Admin/RmaServis/Index")]
        public IActionResult RmaServis() => KurumsalModuller("RmaServis");

        [Route("Admin/KritikStok/Index")]
        public IActionResult KritikStok() => KurumsalModuller("KritikStok");

        [Route("Admin/SaasMrrArr/Index")]
        public IActionResult SaasMrrArr() => KurumsalModuller("SaasMrrArr");

        [Route("Admin/ModularLisans/Index")]
        public IActionResult ModularLisans() => KurumsalModuller("ModularLisans");

        [Route("Admin/SurumPatch/Index")]
        public IActionResult SurumPatch() => KurumsalModuller("SurumPatch");

        [Route("Admin/SahaKurulum/Index")]
        public IActionResult SahaKurulum() => KurumsalModuller("SahaKurulum");

        [Route("Admin/SlaTakip/Index")]
        public IActionResult SlaTakip() => KurumsalModuller("SlaTakip");

        [Route("Admin/CariRiskLimiti/Index")]
        public IActionResult CariRiskLimiti() => KurumsalModuller("CariRiskLimiti");

        [Route("Admin/VadeKontrol/Index")]
        public IActionResult VadeKontrol() => KurumsalModuller("VadeKontrol");

        [Route("Admin/RbacRoller/Index")]
        public IActionResult RbacRoller() => KurumsalModuller("RbacRoller");

        // ============ BAYİ PORTALLARI VE YETKİ YÖNETİMİ ============
        [HttpGet]
        [Route("Admin/BayiPortallari")]
        [Route("Admin/BayiPortallari/Index")]
        public async Task<IActionResult> BayiPortallari()
        {
            var bayiler = await _userManager.GetUsersInRoleAsync(Roller.Bayi);
            var sorted = bayiler.OrderByDescending(u => u.KayitTarihi).ToList();
            ViewBag.SeciliModul = "BayiPortallari";
            return View(sorted);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/BayiPortalTipiGuncelle")]
        public async Task<IActionResult> BayiPortalTipiGuncelle(string userId, int portalTipi)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var eskiTip = user.BayiPortalTipi;
            user.BayiPortalTipi = portalTipi is 1 or 2 or 3 ? portalTipi : 1;
            await _userManager.UpdateAsync(user);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "bilinmiyor";
            var ua = Request.Headers.UserAgent.ToString();
            var cihaz = ApplicationDbContext.CihazBilgisiCoz(ua, ip);

            _context.DenetimKayitlari.Add(new DenetimKaydi
            {
                Zaman = DateTime.Now,
                KullaniciAdi = User.Identity?.Name ?? "admin",
                Rol = "Admin",
                IpAdresi = ip,
                BilgisayarCihaz = cihaz,
                Eylem = "BayiPortalYetkiAtama",
                TabloAdi = "AspNetUsers",
                KayitId = user.Id,
                Ozet = $"{user.FirmaAdi} ({user.UserName}) portal görünüm yetkisi değiştirildi: '{BayiPortalTipleri.Tanim(eskiTip)}' -> '{BayiPortalTipleri.Tanim(user.BayiPortalTipi)}'",
                EskiDeger = $"PortalTipi: {eskiTip} ({BayiPortalTipleri.KisaAd(eskiTip)})",
                YeniDeger = $"PortalTipi: {user.BayiPortalTipi} ({BayiPortalTipleri.KisaAd(user.BayiPortalTipi)})"
            });
            await _context.SaveChangesAsync();

            TempData["AdminBasari"] = $"{user.FirmaAdi} bayisi için aktif portal görünümü '{BayiPortalTipleri.Tanim(user.BayiPortalTipi)}' olarak güncellendi.";
            return RedirectToAction(nameof(BayiPortallari));
        }

        // ============ BAYİ GİRİŞ & İŞLEM DENETİM İZİ ============
        [HttpGet]
        [Route("Admin/BayiIslemLoglari")]
        [Route("Admin/BayiIslemLoglari/Index")]
        public async Task<IActionResult> BayiIslemLoglari(string? kullanici, string? eylem, string? q)
        {
            ViewBag.SeciliModul = "BayiIslemLoglari";
            ViewBag.FiltreKullanici = kullanici;
            ViewBag.FiltreEylem = eylem;
            ViewBag.FiltreQ = q;

            var bayiler = (await _userManager.GetUsersInRoleAsync(Roller.Bayi)).Select(b => b.UserName).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var queryLog = _context.DenetimKayitlari.AsQueryable();
            if (!string.IsNullOrWhiteSpace(kullanici))
            {
                queryLog = queryLog.Where(x => x.KullaniciAdi == kullanici);
            }
            if (!string.IsNullOrWhiteSpace(eylem))
            {
                queryLog = queryLog.Where(x => x.Eylem == eylem);
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                queryLog = queryLog.Where(x => (x.Ozet != null && x.Ozet.Contains(q)) || x.KullaniciAdi.Contains(q) || (x.KayitId != null && x.KayitId.Contains(q)));
            }

            var denetimler = await queryLog.OrderByDescending(x => x.Zaman).Take(200).ToListAsync();

            var girisQuery = _context.AdminGirisLoglari.AsQueryable();
            if (!string.IsNullOrWhiteSpace(kullanici))
            {
                girisQuery = girisQuery.Where(g => g.KullaniciAdi == kullanici);
            }
            var girisler = await girisQuery.OrderByDescending(g => g.GirisTarihi).Take(100).ToListAsync();

            ViewBag.GirisLoglari = girisler;
            ViewBag.BayiKullanicilari = bayiler;

            return View(denetimler);
        }

        // ============ BAYİYE ÖZEL KAMPANYA VE İNDİRİM YÖNETİMİ ============
        [HttpGet]
        [Route("Admin/BayiKampanyalari")]
        [Route("Admin/BayiKampanyalari/Index")]
        public async Task<IActionResult> BayiKampanyalari()
        {
            var kampanyalar = await _context.BayiOzelKampanyalari.OrderByDescending(k => k.OlusturmaTarihi).ToListAsync();
            var bayiler = await _userManager.GetUsersInRoleAsync(Roller.Bayi);
            ViewBag.Bayiler = bayiler.Where(b => b.HesapDurumu == HesapDurumlari.Aktif).OrderBy(b => b.FirmaAdi).ToList();
            ViewBag.SeciliModul = "BayiKampanyalari";
            return View(kampanyalar);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/BayiKampanyaEkle")]
        public async Task<IActionResult> BayiKampanyaEkle(string? bayiId, string kategori, string baslik, string? aciklama, string? urunGrubu, decimal iskontoOrani, decimal? ozelFiyatTL, int gecerlilikGun)
        {
            string? bayiAdi = "Tüm Bayiler";
            if (!string.IsNullOrEmpty(bayiId))
            {
                var target = await _userManager.FindByIdAsync(bayiId);
                if (target != null)
                {
                    bayiAdi = target.FirmaAdi ?? target.AdSoyad ?? target.UserName;
                }
            }
            else
            {
                bayiId = null;
            }

            if (gecerlilikGun <= 0) gecerlilikGun = 30;

            var k = new BayiOzelKampanya
            {
                BayiId = bayiId,
                BayiAdi = bayiAdi,
                Kategori = kategori == "Yazılım" ? "Yazılım" : "Donanım",
                Baslik = baslik.Trim(),
                Aciklama = aciklama?.Trim(),
                UrunGrubu = urunGrubu?.Trim(),
                IskontoOrani = iskontoOrani,
                OzelFiyatTL = ozelFiyatTL,
                BaslangicTarihi = DateTime.Today,
                BitisTarihi = DateTime.Today.AddDays(gecerlilikGun),
                Aktif = true,
                OlusturmaTarihi = DateTime.Now,
                OlusturanAdmin = User.Identity?.Name ?? "admin"
            };

            _context.BayiOzelKampanyalari.Add(k);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "bilinmiyor";
            var ua = Request.Headers.UserAgent.ToString();
            var cihaz = ApplicationDbContext.CihazBilgisiCoz(ua, ip);

            _context.DenetimKayitlari.Add(new DenetimKaydi
            {
                Zaman = DateTime.Now,
                KullaniciAdi = User.Identity?.Name ?? "admin",
                Rol = "Admin",
                IpAdresi = ip,
                BilgisayarCihaz = cihaz,
                Eylem = "BayiKampanyaTanimi",
                TabloAdi = "BayiOzelKampanyalari",
                KayitId = bayiId,
                Ozet = $"'{k.Baslik}' kampanyası oluşturuldu. Hedef: {bayiAdi}, Kategori: {k.Kategori}, İskonto: %{k.IskontoOrani}, Süre: {gecerlilikGun} gün",
                YeniDeger = $"Kategori: {k.Kategori}, Iskonto: %{k.IskontoOrani}, SureGun: {gecerlilikGun}"
            });

            await _context.SaveChangesAsync();

            TempData["AdminBasari"] = $"'{k.Baslik}' kampanyası {bayiAdi} için başarıyla tanımlandı ve bayinin 'Size Özel Kampanyalar' paneline iletildi.";
            return RedirectToAction(nameof(BayiKampanyalari));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/BayiKampanyaDurumDegistir")]
        public async Task<IActionResult> BayiKampanyaDurumDegistir(int id)
        {
            var k = await _context.BayiOzelKampanyalari.FindAsync(id);
            if (k != null)
            {
                k.Aktif = !k.Aktif;
                await _context.SaveChangesAsync();
                TempData["AdminBasari"] = $"'{k.Baslik}' kampanyasının durumu '{(k.Aktif ? "Aktif" : "Pasif")}' olarak güncellendi.";
            }
            return RedirectToAction(nameof(BayiKampanyalari));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/BayiKampanyaSil")]
        public async Task<IActionResult> BayiKampanyaSil(int id)
        {
            var k = await _context.BayiOzelKampanyalari.FindAsync(id);
            if (k != null)
            {
                _context.BayiOzelKampanyalari.Remove(k);
                await _context.SaveChangesAsync();
                TempData["AdminBasari"] = $"'{k.Baslik}' kampanyası silindi.";
            }
            return RedirectToAction(nameof(BayiKampanyalari));
        }

        // =========================================================================
        // BAYİ YÖNETİM & İLETİŞİM MERKEZİ (DUYURU, SERVİS, TALEP, ÖDEME, SATIŞ, SWAP)
        // =========================================================================
        [HttpGet]
        [Route("Admin/BayiYonetimMerkezi")]
        public async Task<IActionResult> BayiYonetimMerkezi(string? tab = "hepsi", string? bayiId = null, string? q = null, string? eskiKategori = null)
        {
            tab = string.IsNullOrWhiteSpace(tab) ? "hepsi" : tab.ToLowerInvariant();
            ViewBag.AktifTab = tab;
            ViewBag.EskiKategori = eskiKategori ?? "tum";
            ViewBag.SeciliModul = "BayiYonetimMerkezi";

            var tumBayiler = await _userManager.GetUsersInRoleAsync(Roller.Bayi);
            var aktifBayiler = tumBayiler.Where(b => b.HesapDurumu == HesapDurumlari.Aktif).OrderBy(b => b.FirmaAdi).ToList();

            var model = new BayiYonetimMerkeziViewModel
            {
                AktifTab = tab,
                SeciliBayiId = bayiId,
                AramaKelimesi = q,
                Bayiler = aktifBayiler,
                Duyurular = BayiOperasyonVeriHavuzu.GetDuyurular(bayiId, q),
                ServisKayitlari = BayiOperasyonVeriHavuzu.GetServisKayitlari(bayiId, q),
                Talepler = BayiOperasyonVeriHavuzu.GetTalepler(bayiId, q),
                Odemeler = BayiOperasyonVeriHavuzu.GetOdemeler(bayiId, q),
                Satislar = BayiOperasyonVeriHavuzu.GetSatislar(bayiId, q),
                SwapKayitlari = BayiOperasyonVeriHavuzu.GetSwapKayitlari(bayiId, q),
                EskiMusteriler = BayiOperasyonVeriHavuzu.GetEskiMusteriler(eskiKategori, q),
                ToplamBayiSayisi = aktifBayiler.Count,
                ToplamDuyuruSayisi = BayiOperasyonVeriHavuzu.Duyurular.Count,
                ServistekiAracCihazSayisi = BayiOperasyonVeriHavuzu.ServisKayitlari.Count(s => !s.DurumAsamasi.Contains("Çıktı") && !s.DurumAsamasi.Contains("Teslim")),
                BekleyenTalepSayisi = BayiOperasyonVeriHavuzu.Talepler.Count(t => t.Durum != "Çözüldü & Kapatıldı"),
                ToplamTahsilatTL = BayiOperasyonVeriHavuzu.Odemeler.Where(o => o.Durum.Contains("Onaylandı") || o.Durum.Contains("Muhasebeleşti")).Sum(o => o.TutarTL),
                ToplamSatisCiroyuTL = BayiOperasyonVeriHavuzu.Satislar.Sum(s => s.ToplamTutarTL),
                AktifBozukCihazSwapSayisi = BayiOperasyonVeriHavuzu.SwapKayitlari.Count(sw => sw.Durum.Contains("İncelemede") || sw.Durum.Contains("Bekleniyor") || sw.Durum.Contains("Onaylandı")),
                ToplamEskiMusteriSayisi = BayiOperasyonVeriHavuzu.EskiMusteriler.Count,
                EskiMusteriYazilimSayisi = BayiOperasyonVeriHavuzu.EskiMusteriler.Count(e => e.Kategori == "Yazılım"),
                EskiMusteriDonanimSayisi = BayiOperasyonVeriHavuzu.EskiMusteriler.Count(e => e.Kategori == "Donanım"),
                EskiMusteriIkisiSayisi = BayiOperasyonVeriHavuzu.EskiMusteriler.Count(e => e.Kategori.Contains("İkisi")),
                EskiMusterilerdenToplamOdenenTL = BayiOperasyonVeriHavuzu.EskiMusteriler.Sum(e => e.OdenenToplamTutarTL)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/DuyuruBildirimGonder")]
        public async Task<IActionResult> DuyuruBildirimGonder(
            string baslik,
            string icerik,
            string kategori,
            string oncelik,
            string hedefTipi,
            List<string>? seciliBayiIdleri,
            bool portalBildirimi = true,
            bool epostaGonder = true,
            bool smsGonder = false)
        {
            if (string.IsNullOrWhiteSpace(baslik) || string.IsNullOrWhiteSpace(icerik))
            {
                TempData["AdminHata"] = "Duyuru başlığı ve bildirim içeriği zorunludur.";
                return RedirectToAction(nameof(BayiYonetimMerkezi), new { tab = "bildirim" });
            }

            var tumBayiler = await _userManager.GetUsersInRoleAsync(Roller.Bayi);
            var aktifBayiler = tumBayiler.Where(b => b.HesapDurumu == HesapDurumlari.Aktif).ToList();

            string hedefOzeti;
            var hedefListesi = new List<string>();

            if (hedefTipi == "SeciliBayiler" && seciliBayiIdleri != null && seciliBayiIdleri.Any())
            {
                var eslesenBayiler = aktifBayiler.Where(b => seciliBayiIdleri.Contains(b.Id)).ToList();
                hedefListesi = eslesenBayiler.Select(b => b.Id).ToList();
                hedefOzeti = string.Join(", ", eslesenBayiler.Select(b => b.FirmaAdi ?? b.AdSoyad));
            }
            else
            {
                hedefTipi = "TumBayiler";
                hedefOzeti = "Tüm Bayiler (Genel Dağıtım - " + aktifBayiler.Count + " Bayi)";
                hedefListesi = aktifBayiler.Select(b => b.Id).ToList();
            }

            var duyuru = new DuyuruBildirimKaydi
            {
                Baslik = baslik.Trim(),
                Icerik = icerik.Trim(),
                Kategori = string.IsNullOrWhiteSpace(kategori) ? "Genel Duyuru" : kategori,
                Oncelik = string.IsNullOrWhiteSpace(oncelik) ? "Normal" : oncelik,
                HedefTipi = hedefTipi,
                HedefBayilerOzeti = hedefOzeti,
                HedefBayiIdleri = hedefListesi,
                GonderimTarihi = DateTime.Now,
                GonderenAdmin = User.Identity?.Name ?? "Sistem Yöneticisi",
                PortalBildirimi = portalBildirimi,
                EpostaGonderildi = epostaGonder,
                SmsGonderildi = smsGonder,
                Durum = "Yayında & İletildi"
            };

            BayiOperasyonVeriHavuzu.DuyuruEkle(duyuru);

            // Audit Log
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "bilinmiyor";
            var ua = Request.Headers.UserAgent.ToString();
            var cihaz = ApplicationDbContext.CihazBilgisiCoz(ua, ip);

            _context.DenetimKayitlari.Add(new DenetimKaydi
            {
                Zaman = DateTime.Now,
                KullaniciAdi = User.Identity?.Name ?? "admin",
                Rol = "Admin",
                IpAdresi = ip,
                BilgisayarCihaz = cihaz,
                Eylem = "BayiBildirimDuyuruGonderimi",
                TabloAdi = "BayiDuyurular",
                KayitId = duyuru.Id,
                Ozet = $"'{duyuru.Baslik}' duyurusu gönderildi. Hedef: {hedefOzeti}, Kategori: {duyuru.Kategori}, Öncelik: {duyuru.Oncelik}",
                YeniDeger = $"Kanallar: Portal={portalBildirimi}, EPosta={epostaGonder}, SMS={smsGonder}"
            });

            await _context.SaveChangesAsync();

            TempData["AdminBasari"] = $"'{duyuru.Baslik}' duyurusu başarıyla oluşturuldu ve {hedefOzeti} hedefine anında iletildi.";
            return RedirectToAction(nameof(BayiYonetimMerkezi), new { tab = "bildirim" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/ServisDurumuGuncelleVeIlet")]
        public async Task<IActionResult> ServisDurumuGuncelleVeIlet(
            string? servisTakipNo,
            string bayiId,
            string aracCihazTipi,
            string plakaVeyaSeriNo,
            string modelTanim,
            string neredeKonum,
            string durumAsamasi,
            decimal tahminiTutarTL,
            decimal parcaTutarTL,
            decimal iscilikTutarTL,
            string garantiDurumu,
            string arizaVeYapilanIslemler,
            string atananTeknisyen,
            string? tahminiTeslimTarihi,
            bool bayiyeIlet = true)
        {
            var bayi = await _userManager.FindByIdAsync(bayiId);
            var bayiAdi = bayi?.FirmaAdi ?? bayi?.AdSoyad ?? "Genel Bayi";
            var cariKodu = bayi?.CariKodu ?? "CARİ-GENEL";

            DateTime? teslimTarihi = null;
            if (DateTime.TryParse(tahminiTeslimTarihi, out var dt)) teslimTarihi = dt;

            var kayit = new ServisAracCihazKaydi
            {
                ServisTakipNo = string.IsNullOrWhiteSpace(servisTakipNo) ? $"SRV-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(100, 999)}" : servisTakipNo.Trim(),
                BayiId = bayiId,
                BayiAdi = bayiAdi,
                CariKodu = cariKodu,
                AracCihazTipi = aracCihazTipi ?? "Saha Mobil Servis Aracı",
                PlakaVeyaSeriNo = plakaVeyaSeriNo?.Trim() ?? "PLAKA/SN-BELİRTİLMEDİ",
                ModelTanim = modelTanim?.Trim() ?? "Genel Model",
                NeredeKonum = neredeKonum?.Trim() ?? "Merkez Teknik Laboratuvar",
                DurumAsamasi = durumAsamasi?.Trim() ?? "İncelemede",
                TahminiTutarTL = tahminiTutarTL,
                ParcaTutarTL = parcaTutarTL,
                IscilikTutarTL = iscilikTutarTL,
                GarantiDurumu = garantiDurumu ?? "Garantili (0 TL)",
                ArizaVeYapilanIslemler = arizaVeYapilanIslemler?.Trim() ?? "Genel servis kontrol ve revizyonu yapıldı.",
                AtananTeknisyen = string.IsNullOrWhiteSpace(atananTeknisyen) ? "Teknik Servis Ekibi" : atananTeknisyen.Trim(),
                KabulTarihi = DateTime.Now.AddDays(-1),
                TahminiTeslimTarihi = teslimTarihi ?? DateTime.Now.AddDays(2),
                BayiyeIletildiMi = bayiyeIlet,
                SonIletimZamani = bayiyeIlet ? DateTime.Now : null
            };

            BayiOperasyonVeriHavuzu.ServisKaydiGuncelleVeyaEkle(kayit);

            // Audit Log
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "bilinmiyor";
            var ua = Request.Headers.UserAgent.ToString();
            var cihaz = ApplicationDbContext.CihazBilgisiCoz(ua, ip);

            _context.DenetimKayitlari.Add(new DenetimKaydi
            {
                Zaman = DateTime.Now,
                KullaniciAdi = User.Identity?.Name ?? "admin",
                Rol = "Admin",
                IpAdresi = ip,
                BilgisayarCihaz = cihaz,
                Eylem = "ServisDurumuGuncellemeVeIletim",
                TabloAdi = "ServisAracCihaz",
                KayitId = kayit.ServisTakipNo,
                Ozet = $"Servis No: {kayit.ServisTakipNo} ({kayit.PlakaVeyaSeriNo}) güncellendi. Konum: {kayit.NeredeKonum}, Aşama: {kayit.DurumAsamasi}, Tutar: ₺{kayit.ToplamTutarTL:N2}, İletilen Bayi: {bayiAdi}",
                YeniDeger = $"Tahmini={tahminiTutarTL}, Parca={parcaTutarTL}, Iscilik={iscilikTutarTL}, BayiyeIletildi={bayiyeIlet}"
            });

            await _context.SaveChangesAsync();

            TempData["AdminBasari"] = $"{kayit.ServisTakipNo} numaralı servis/araç durumu güncellendi{(bayiyeIlet ? " ve " + bayiAdi + " bayisine anlık olarak iletildi." : ".")}";
            return RedirectToAction(nameof(BayiYonetimMerkezi), new { tab = "servis", bayiId = bayiId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/TalepSikayetYanitla")]
        public async Task<IActionResult> TalepSikayetYanitla(string talepNo, string durum, string adminCevap)
        {
            if (string.IsNullOrWhiteSpace(talepNo) || string.IsNullOrWhiteSpace(adminCevap))
            {
                TempData["AdminHata"] = "Talep numarası ve yanıt mesajı zorunludur.";
                return RedirectToAction(nameof(BayiYonetimMerkezi), new { tab = "talepler" });
            }

            var t = BayiOperasyonVeriHavuzu.Talepler.FirstOrDefault(x => x.TalepNo == talepNo);
            if (t != null)
            {
                t.Durum = string.IsNullOrWhiteSpace(durum) ? "Çözüldü & Kapatıldı" : durum;
                t.AdminCevap = adminCevap.Trim();
                t.SonGuncellemeTarihi = DateTime.Now;
                t.AtananPersonel = User.Identity?.Name ?? "Müşteri Hizmetleri";

                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "bilinmiyor";
                var ua = Request.Headers.UserAgent.ToString();
                var cihaz = ApplicationDbContext.CihazBilgisiCoz(ua, ip);

                _context.DenetimKayitlari.Add(new DenetimKaydi
                {
                    Zaman = DateTime.Now,
                    KullaniciAdi = User.Identity?.Name ?? "admin",
                    Rol = "Admin",
                    IpAdresi = ip,
                    BilgisayarCihaz = cihaz,
                    Eylem = "BayiTalepCevaplama",
                    TabloAdi = "BayiTalepleri",
                    KayitId = talepNo,
                    Ozet = $"'{talepNo}' nolu bayilik talebine/şikayetine yanıt verildi. Yeni Durum: {t.Durum}, Bayi: {t.BayiAdi}",
                    YeniDeger = adminCevap.Trim()
                });

                await _context.SaveChangesAsync();
                TempData["AdminBasari"] = $"'{talepNo}' nolu talep/şikayet başarıyla yanıtlandı ve bayiye bildirildi.";
            }

            return RedirectToAction(nameof(BayiYonetimMerkezi), new { tab = "talepler" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/BozukCihazSwapOnayla")]
        public async Task<IActionResult> BozukCihazSwapOnayla(string swapNo, string verilenYeniCihazSeriNo, string durum, string? kargoTakipNo)
        {
            var sw = BayiOperasyonVeriHavuzu.SwapKayitlari.FirstOrDefault(x => x.SwapNo == swapNo);
            if (sw != null)
            {
                sw.Durum = string.IsNullOrWhiteSpace(durum) ? "Onaylandı & Yeni Cihaz Sevk Edildi" : durum;
                if (!string.IsNullOrWhiteSpace(verilenYeniCihazSeriNo)) sw.VerilenYeniCihazSeriNo = verilenYeniCihazSeriNo.Trim();
                if (!string.IsNullOrWhiteSpace(kargoTakipNo)) sw.KargoTakipNo = kargoTakipNo.Trim();
                sw.DegisimTarihi = DateTime.Now;
                sw.OnaylayanAdmin = User.Identity?.Name ?? "Teknik Servis Müdürü";

                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "bilinmiyor";
                var ua = Request.Headers.UserAgent.ToString();
                var cihaz = ApplicationDbContext.CihazBilgisiCoz(ua, ip);

                _context.DenetimKayitlari.Add(new DenetimKaydi
                {
                    Zaman = DateTime.Now,
                    KullaniciAdi = User.Identity?.Name ?? "admin",
                    Rol = "Admin",
                    IpAdresi = ip,
                    BilgisayarCihaz = cihaz,
                    Eylem = "BozukCihazSwapOnayi",
                    TabloAdi = "BozukCihazSwap",
                    KayitId = swapNo,
                    Ozet = $"'{swapNo}' nolu arızalı cihaz swap talebi onaylandı. Bozuk: {sw.BozukCihazSeriNo}, Yeni Verilen: {sw.VerilenYeniCihazSeriNo}, Bayi: {sw.BayiAdi}",
                    YeniDeger = $"Durum: {sw.Durum}, Kargo: {sw.KargoTakipNo}"
                });

                await _context.SaveChangesAsync();
                TempData["AdminBasari"] = $"'{swapNo}' nolu arızalı cihaz swap işlemi güncellendi ve yeni cihaz sevkiyat bilgisi bayiye iletildi.";
            }

            return RedirectToAction(nameof(BayiYonetimMerkezi), new { tab = "degisim" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/EskiMusteriGeriKazanimNotuKaydet")]
        public async Task<IActionResult> EskiMusteriGeriKazanimNotuKaydet(string musteriId, string geriKazanimDurumu, string sonIletisimNotu)
        {
            var m = BayiOperasyonVeriHavuzu.EskiMusteriler.FirstOrDefault(x => x.Id == musteriId);
            if (m != null)
            {
                m.GeriKazanimDurumu = string.IsNullOrWhiteSpace(geriKazanimDurumu) ? m.GeriKazanimDurumu : geriKazanimDurumu.Trim();
                m.SonIletisimNotu = sonIletisimNotu?.Trim();

                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "bilinmiyor";
                var ua = Request.Headers.UserAgent.ToString();
                var cihaz = ApplicationDbContext.CihazBilgisiCoz(ua, ip);

                _context.DenetimKayitlari.Add(new DenetimKaydi
                {
                    Zaman = DateTime.Now,
                    KullaniciAdi = User.Identity?.Name ?? "admin",
                    Rol = "Admin",
                    IpAdresi = ip,
                    BilgisayarCihaz = cihaz,
                    Eylem = "EskiMusteriGeriKazanimTakibi",
                    TabloAdi = "EskiMusteriler",
                    KayitId = m.Id,
                    Ozet = $"Eski müşteri/bayi '{m.FirmaVeyaKisiAdi}' geri kazanım kaydı güncellendi. Yeni Durum: {m.GeriKazanimDurumu}",
                    YeniDeger = sonIletisimNotu?.Trim()
                });

                await _context.SaveChangesAsync();
                TempData["AdminBasari"] = $"'{m.FirmaVeyaKisiAdi}' için geri kazanım aksiyon notu başarıyla kaydedildi.";
            }

            return RedirectToAction(nameof(BayiYonetimMerkezi), new { tab = "eskimusteriler" });
        }
    }

    // =========================================================================
    // STATİK VERİ HAVUZU (HAYAT İŞLERİ BAYİ OPERASYON & İLETİŞİM VERİLERİ)
    // =========================================================================
    public static class BayiOperasyonVeriHavuzu
    {
        private static readonly object _lock = new();

        public static List<DuyuruBildirimKaydi> Duyurular { get; } = new();
        public static List<ServisAracCihazKaydi> ServisKayitlari { get; } = new();
        public static List<BayiTalepSikayetKaydi> Talepler { get; } = new();
        public static List<BayiOdemeKaydi> Odemeler { get; } = new();
        public static List<BayiSatisKaydi> Satislar { get; } = new();
        public static List<BozukCihazSwapKaydi> SwapKayitlari { get; } = new();
        public static List<EskiBayiMusteriKaydi> EskiMusteriler { get; } = new();

        static BayiOperasyonVeriHavuzu()
        {
            SeedData();
        }

        private static void SeedData()
        {
            // 1. DUYURULAR
            Duyurular.AddRange(new[]
            {
                new DuyuruBildirimKaydi
                {
                    Baslik = "Yeni Nesil KodPOS T500 All-in-One Kasa Terminalleri Merkez Depomuzda",
                    Icerik = "Intel Core i5 işlemcili, alüminyum döküm gövdeli yeni nesil KodPOS T500 terminalleri merkez depomuza girmiştir. Yetkili bayilerimize özel %35 + %5 ek lansman iskontosu tanımlanmıştır. Siparişlerinizi B2B portalından oluşturabilirsiniz.",
                    Kategori = "Yeni Ürün Lansmanı",
                    Oncelik = "Önemli",
                    HedefTipi = "TumBayiler",
                    HedefBayilerOzeti = "Tüm Bayiler (Genel Dağıtım)",
                    GonderimTarihi = DateTime.Now.AddDays(-1),
                    GonderenAdmin = "Murat Çetin (Kanal Müdürü)",
                    PortalBildirimi = true,
                    EpostaGonderildi = true,
                    SmsGonderildi = true,
                    OkunmaSayisi = 28,
                    Durum = "Yayında & İletildi"
                },
                new DuyuruBildirimKaydi
                {
                    Baslik = "Resmi Tatil Nedeniyle Kargo ve Saha Kurulum Çıkış Takvimi",
                    Icerik = "Yaklaşan resmi tatil sebebiyle anlaşmalı Yurtiçi ve Aras Kargo son koli teslim saati Perşembe 16:00'dır. Bu saatten sonra onaylanan siparişler tatil sonrası ilk iş günü sevk edilecektir.",
                    Kategori = "Lojistik & Kargo",
                    Oncelik = "Acil & Kritik",
                    HedefTipi = "TumBayiler",
                    HedefBayilerOzeti = "Tüm Bayiler (Genel Dağıtım)",
                    GonderimTarihi = DateTime.Now.AddDays(-3),
                    GonderenAdmin = "Sistem Yöneticisi",
                    PortalBildirimi = true,
                    EpostaGonderildi = true,
                    SmsGonderildi = true,
                    OkunmaSayisi = 41,
                    Durum = "Yayında & İletildi"
                },
                new DuyuruBildirimKaydi
                {
                    Baslik = "Hafta Sonu Bulut ERP & GİB 2026 E-Fatura API Planlı Bakım",
                    Icerik = "Pazar gecesi 02:00 - 05:00 saatleri arasında GİB e-Belge ve e-Arşiv v2.6.2 servis entegrasyonu güncellenecektir. Portal ve API çağrılarında kısa süreli gecikmeler yaşanabilir.",
                    Kategori = "Sistem Bakımı",
                    Oncelik = "Normal",
                    HedefTipi = "TumBayiler",
                    HedefBayilerOzeti = "Tüm Bayiler (Genel Dağıtım)",
                    GonderimTarihi = DateTime.Now.AddDays(-5),
                    GonderenAdmin = "Teknik Altyapı Ekibi",
                    PortalBildirimi = true,
                    EpostaGonderildi = true,
                    SmsGonderildi = false,
                    OkunmaSayisi = 35,
                    Durum = "Tamamlandı"
                },
                new DuyuruBildirimKaydi
                {
                    Baslik = "Marmara Bölgesi Bayilerine Özel Sunmi Donanım Ciro Primi Programı",
                    Icerik = "Ekim-Kasım döneminde 200.000 TL ve üzeri Sunmi POS terminal alımı gerçekleştiren Marmara yetkili bayilerimize %5 ek hakediş çeki ve 1 yıl ücretsiz HiCariTakip ERP lisansı verilecektir.",
                    Kategori = "Fiyat & İskonto",
                    Oncelik = "Önemli",
                    HedefTipi = "SeciliBayiler",
                    HedefBayilerOzeti = "Marmara POS Sistemleri A.Ş., Ege Barkod & Otomasyon Ltd.",
                    GonderimTarihi = DateTime.Now.AddDays(-7),
                    GonderenAdmin = "Satış Direktörü",
                    PortalBildirimi = true,
                    EpostaGonderildi = true,
                    SmsGonderildi = true,
                    OkunmaSayisi = 12,
                    Durum = "Yayında & İletildi"
                }
            });

            // 2. SERVİS VE SAHA ARAÇ/CİHAZ TAKİBİ
            ServisKayitlari.AddRange(new[]
            {
                new ServisAracCihazKaydi
                {
                    ServisTakipNo = "SRV-2026-074",
                    BayiId = "bayi-1",
                    BayiAdi = "Marmara POS Sistemleri A.Ş.",
                    CariKodu = "CARİ-MARMARA-01",
                    AracCihazTipi = "Saha Mobil Servis Aracı",
                    PlakaVeyaSeriNo = "34 HYT 802 (Mobil Servis #01)",
                    ModelTanim = "Ford Transit Mobil Gezici Servis ve Saha Test Aracı",
                    NeredeKonum = "Saha Gezici Ekip - Yolda (Kadıköy / Ataşehir Güzergahı)",
                    DurumAsamasi = "Periyodik Revizyon Tamamlandı - Bayi Saha Ziyaretinde",
                    TahminiTutarTL = 4500m,
                    ParcaTutarTL = 2800m,
                    IscilikTutarTL = 1700m,
                    GarantiDurumu = "Şirket Filo Servis Kapsamında",
                    ArizaVeYapilanIslemler = "Mobil test kiti, invertör güç besleme ünitesi ve kalibrasyon ölçüm cihazlarının periyodik bakımı yapıldı. Saha yedek cihaz envanteri yüklendi.",
                    AtananTeknisyen = "Kemal Güner (Saha Operasyon Lideri)",
                    KabulTarihi = DateTime.Now.AddDays(-2),
                    TahminiTeslimTarihi = DateTime.Now.AddHours(4),
                    BayiyeIletildiMi = true,
                    SonIletimZamani = DateTime.Now.AddHours(-1)
                },
                new ServisAracCihazKaydi
                {
                    ServisTakipNo = "SRV-2026-068",
                    BayiId = "bayi-2",
                    BayiAdi = "Ege Barkod & Otomasyon Ltd.",
                    CariKodu = "CARİ-EGE-02",
                    AracCihazTipi = "Donanım / POS Terminali",
                    PlakaVeyaSeriNo = "SN: SUN-D2S-88412",
                    ModelTanim = "Sunmi D2s Android 15.6\" Dokunmatik POS Terminali",
                    NeredeKonum = "Merkez Teknik Laboratuvar (Masa 4 - Donanım Onarım)",
                    DurumAsamasi = "Yedek Parça Bekleniyor (Termal Baskı Motoru)",
                    TahminiTutarTL = 2850m,
                    ParcaTutarTL = 2100m,
                    IscilikTutarTL = 750m,
                    GarantiDurumu = "Garantili (0 TL Bayi)",
                    ArizaVeYapilanIslemler = "Fiş çıkışında kağıt besleme dişlisi sıkışması ve dokunmatik eksen kayması tespit edildi. Orijinal Seiko baskı mekanizması sipariş edildi, montaj bekleniyor.",
                    AtananTeknisyen = "Hakan Demir (Donanım Uzmanı)",
                    KabulTarihi = DateTime.Now.AddDays(-3),
                    TahminiTeslimTarihi = DateTime.Now.AddDays(1),
                    BayiyeIletildiMi = true,
                    SonIletimZamani = DateTime.Now.AddDays(-1)
                },
                new ServisAracCihazKaydi
                {
                    ServisTakipNo = "SRV-2026-055",
                    BayiId = "bayi-3",
                    BayiAdi = "İç Anadolu Teknoloji Dağıtım",
                    CariKodu = "CARİ-ANADOLU-03",
                    AracCihazTipi = "Barkod & Endüstriyel Yazıcı",
                    PlakaVeyaSeriNo = "SN: ZBR-411-99214",
                    ModelTanim = "Zebra ZT411 Endüstriyel Metal Kasa Barkod Yazıcı (203 DPI)",
                    NeredeKonum = "Test & Kalibrasyon Odası (İstasyon 2)",
                    DurumAsamasi = "Son Kalibrasyon & Isı Dayanım Testinde",
                    TahminiTutarTL = 1450m,
                    ParcaTutarTL = 900m,
                    IscilikTutarTL = 550m,
                    GarantiDurumu = "Ücretli Servis",
                    ArizaVeYapilanIslemler = "Termal baskı kafası yenisiyle değiştirildi. Merdane silindirleri temizlendi ve 10.000 etiketlik aralıksız baskı dayanım testi yürütülüyor.",
                    AtananTeknisyen = "Murat Çetin (Endüstriyel Sistemler)",
                    KabulTarihi = DateTime.Now.AddDays(-5),
                    TahminiTeslimTarihi = DateTime.Now.AddDays(1),
                    BayiyeIletildiMi = true,
                    SonIletimZamani = DateTime.Now.AddHours(-3)
                },
                new ServisAracCihazKaydi
                {
                    ServisTakipNo = "SRV-2026-042",
                    BayiId = "bayi-4",
                    BayiAdi = "Akdeniz Yazılım & Donanım A.Ş.",
                    CariKodu = "CARİ-AKDENIZ-04",
                    AracCihazTipi = "El Terminali / Barkod Okuyucu",
                    PlakaVeyaSeriNo = "SN: DTL-440-33120",
                    ModelTanim = "Datalogic Gryphon GFS4400 Sabit 2D Karekod Okuyucu",
                    NeredeKonum = "Kargo Sevkiyat Alanı (Yurtiçi Kargo: YK-8812401)",
                    DurumAsamasi = "Servisten Çıktı - Bayiye Sevk Edildi",
                    TahminiTutarTL = 600m,
                    ParcaTutarTL = 0m,
                    IscilikTutarTL = 600m,
                    GarantiDurumu = "Garantili (0 TL)",
                    ArizaVeYapilanIslemler = "Optik lens camı temizliği, firmware v3.12 güncellemesi ve USB-HID arabirim soket değişimi yapılarak testlerden %100 başarıyla geçti.",
                    AtananTeknisyen = "Hakan Demir (Donanım Uzmanı)",
                    KabulTarihi = DateTime.Now.AddDays(-7),
                    TahminiTeslimTarihi = DateTime.Now.AddDays(-1),
                    BayiyeIletildiMi = true,
                    SonIletimZamani = DateTime.Now.AddDays(-1)
                }
            });

            // 3. GELEN TALEP VE ŞİKAYETLER
            Talepler.AddRange(new[]
            {
                new BayiTalepSikayetKaydi
                {
                    TalepNo = "TKT-2026-9104",
                    BayiId = "bayi-1",
                    BayiAdi = "Marmara POS Sistemleri A.Ş.",
                    CariKodu = "CARİ-MARMARA-01",
                    Baslik = "Dün Verilen Siparişte Koli İçi Eksik Ribon Çıkması",
                    Kategori = "Kargo & Sevkiyat Eksikliği",
                    Oncelik = "Yüksek",
                    Durum = "İnceleniyor",
                    OlusturmaTarihi = DateTime.Now.AddDays(-1),
                    SonGuncellemeTarihi = DateTime.Now.AddHours(-2),
                    Mesaj = "HYT-SIP-2026-8812 nolu siparişimizde gelen kolide 1 adet 110x300m resin ribon eksik çıkmıştır. Kargo hasar tespit tutanağı ekte mevcuttur.",
                    DosyaEki = "hasar_tespit_tutanagi.pdf",
                    AdminCevap = "Depo paketleme kamera kayıtları incelenmektedir, eksik ürün için ek kargo çıkışı hazırlanıyor.",
                    AtananPersonel = "Lojistik & Depo Sorumlusu"
                },
                new BayiTalepSikayetKaydi
                {
                    TalepNo = "TKT-2026-9042",
                    BayiId = "bayi-2",
                    BayiAdi = "Ege Barkod & Otomasyon Ltd.",
                    CariKodu = "CARİ-EGE-02",
                    Baslik = "Sanal POS Ödeme Ekranında 3D Doğrulama Zaman Aşımı",
                    Kategori = "Sanal POS & Finans",
                    Oncelik = "Normal",
                    Durum = "Çözüldü & Kapatıldı",
                    OlusturmaTarihi = DateTime.Now.AddDays(-6),
                    SonGuncellemeTarihi = DateTime.Now.AddDays(-4),
                    Mesaj = "Garanti BBVA sanal posu ile ödeme yaparken SMS kodu ekranı 30 saniye sonra time out verdi ve provizyon beklemeye düştü.",
                    DosyaEki = "pos_hata_ekrani.png",
                    AdminCevap = "Banka sanal POS gateway oturum süreleri 120 saniyeye yükseltildi, provizyon başarıyla çözülmüştür.",
                    AtananPersonel = "Finans & Muhasebe"
                },
                new BayiTalepSikayetKaydi
                {
                    TalepNo = "TKT-2026-8980",
                    BayiId = "bayi-3",
                    BayiAdi = "İç Anadolu Teknoloji Dağıtım",
                    CariKodu = "CARİ-ANADOLU-03",
                    Baslik = "E-Arşiv Faturasında Vergi Numarası & Unvan Düzeltme Talebi",
                    Kategori = "Fatura & Cari İtiraz",
                    Oncelik = "Normal",
                    Durum = "Çözüldü & Kapatıldı",
                    OlusturmaTarihi = DateTime.Now.AddDays(-14),
                    SonGuncellemeTarihi = DateTime.Now.AddDays(-12),
                    Mesaj = "Geçtiğimiz ay kesilen faturada şirket ünvanımızdaki A.Ş. kısaltması Ltd. Şti. kalmıştır.",
                    DosyaEki = "yeni_vergi_levhasi.pdf",
                    AdminCevap = "FTR-2026-00412 nolu faturanız iptal edilip düzeltilmiş e-arşiv fatura muhasebe e-postanıza iletilmiştir.",
                    AtananPersonel = "E-Fatura Masası"
                },
                new BayiTalepSikayetKaydi
                {
                    TalepNo = "TKT-2026-8910",
                    BayiId = "bayi-4",
                    BayiAdi = "Akdeniz Yazılım & Donanım A.Ş.",
                    CariKodu = "CARİ-AKDENIZ-04",
                    Baslik = "HiCariTakip v2.6.2 Bulut API Token Yetki Hatası (403 Forbidden)",
                    Kategori = "Yazılım Lisans Aktivasyon",
                    Oncelik = "Yüksek",
                    Durum = "İnceleniyor",
                    OlusturmaTarihi = DateTime.Now.AddDays(-2),
                    SonGuncellemeTarihi = DateTime.Now.AddHours(-5),
                    Mesaj = "E-Fatura gönderim modülü API çağrısında 403 Forbidden alıyoruz, lisans anahtarının güncellenmesi gerekiyor.",
                    DosyaEki = "api_error_log.txt",
                    AdminCevap = "API anahtar izinleri 'Tam Yetkili Çözüm Ortağı' rolüne yükseltildi, lütfen tekrar test ediniz.",
                    AtananPersonel = "Yazılım Destek Ekibi"
                }
            });

            // 4. BAYİ ÖDEMELERİ & CARİ HAREKETLER
            Odemeler.AddRange(new[]
            {
                new BayiOdemeKaydi
                {
                    IslemNo = "OD-2026-4402",
                    BayiId = "bayi-1",
                    BayiAdi = "Marmara POS Sistemleri A.Ş.",
                    CariKodu = "CARİ-MARMARA-01",
                    OdemeTuru = "Sanal POS (3D Secure)",
                    OdemeTarihi = DateTime.Now.AddDays(-1),
                    TutarTL = 45000m,
                    DekontBankaReferans = "GPOS-20261019-9941",
                    BankaAdi = "Garanti BBVA",
                    KalanCariBakiyeTL = 38450m,
                    Durum = "Onaylandı & Muhasebeleşti",
                    Aciklama = "Ekim ayı sarf ve donanım siparişleri cari hesap ödemesi"
                },
                new BayiOdemeKaydi
                {
                    IslemNo = "OD-2026-4389",
                    BayiId = "bayi-2",
                    BayiAdi = "Ege Barkod & Otomasyon Ltd.",
                    CariKodu = "CARİ-EGE-02",
                    OdemeTuru = "Havale / EFT",
                    OdemeTarihi = DateTime.Now.AddDays(-3),
                    TutarTL = 78500m,
                    DekontBankaReferans = "EFT-ISBK-8840129",
                    BankaAdi = "İş Bankası",
                    KalanCariBakiyeTL = 12200m,
                    Durum = "Onaylandı & Muhasebeleşti",
                    Aciklama = "Sunmi POS toplu alım faturası kapama ödemesi"
                },
                new BayiOdemeKaydi
                {
                    IslemNo = "OD-2026-4350",
                    BayiId = "bayi-3",
                    BayiAdi = "İç Anadolu Teknoloji Dağıtım",
                    CariKodu = "CARİ-ANADOLU-03",
                    OdemeTuru = "DBS Doğrudan Borçlandırma",
                    OdemeTarihi = DateTime.Now.AddDays(-6),
                    TutarTL = 120000m,
                    DekontBankaReferans = "DBS-AKB-2026-092",
                    BankaAdi = "Akbank",
                    KalanCariBakiyeTL = 0m,
                    Durum = "Onaylandı & Muhasebeleşti",
                    Aciklama = "Haftalık otomatik DBS cari tahsilatı"
                },
                new BayiOdemeKaydi
                {
                    IslemNo = "OD-2026-4312",
                    BayiId = "bayi-4",
                    BayiAdi = "Akdeniz Yazılım & Donanım A.Ş.",
                    CariKodu = "CARİ-AKDENIZ-04",
                    OdemeTuru = "Sanal POS (3D Secure)",
                    OdemeTarihi = DateTime.Now.AddDays(-10),
                    TutarTL = 32400m,
                    DekontBankaReferans = "YKB-POS-881240",
                    BankaAdi = "Yapı Kredi",
                    KalanCariBakiyeTL = 24800m,
                    Durum = "Onaylandı & Muhasebeleşti",
                    Aciklama = "HiCariTakip Cloud ERP yıllık lisans yenileme bedeli"
                }
            });

            // 5. BAYİ SATIŞ GEÇMİŞİ (NE SATTIK, NE ZAMAN SATTIK)
            Satislar.AddRange(new[]
            {
                new BayiSatisKaydi
                {
                    SatisNo = "SAT-2026-1082",
                    FaturaNo = "GIB-FTR-2026-008812",
                    BayiId = "bayi-1",
                    BayiAdi = "Marmara POS Sistemleri A.Ş.",
                    CariKodu = "CARİ-MARMARA-01",
                    SatisTarihi = DateTime.Now.AddDays(-2),
                    SatilanUrunlerOzet = "5x Sunmi D2s Android POS Terminali (SN: D2S-101..105), 20x 80x40 Termal Kasa Rulosu Koli",
                    Kategori = "Donanım & Sarf",
                    ToplamAdet = 25,
                    BirimFiyatTL = 34500m,
                    IskontoOrani = 35m,
                    ToplamTutarTL = 112125m,
                    SevkiyatDurumu = "Teslim Edildi",
                    KargoTakipNo = "YK-8891244",
                    FaturaDurumu = "E-Fatura Kesildi (GİB Onaylı)"
                },
                new BayiSatisKaydi
                {
                    SatisNo = "SAT-2026-1070",
                    FaturaNo = "GIB-FTR-2026-008740",
                    BayiId = "bayi-2",
                    BayiAdi = "Ege Barkod & Otomasyon Ltd.",
                    CariKodu = "CARİ-EGE-02",
                    SatisTarihi = DateTime.Now.AddDays(-7),
                    SatilanUrunlerOzet = "2x Zebra ZT411 Endüstriyel Barkod Yazıcı (SN: ZT-901..902), 50x Wax-Resin Ribon 110x300m",
                    Kategori = "Donanım & Sarf",
                    ToplamAdet = 52,
                    BirimFiyatTL = 52000m,
                    IskontoOrani = 35m,
                    ToplamTutarTL = 67600m,
                    SevkiyatDurumu = "Teslim Edildi",
                    KargoTakipNo = "ARAS-7712041",
                    FaturaDurumu = "E-Fatura Kesildi (GİB Onaylı)"
                },
                new BayiSatisKaydi
                {
                    SatisNo = "SAT-2026-1055",
                    FaturaNo = "GIB-FTR-2026-008691",
                    BayiId = "bayi-3",
                    BayiAdi = "İç Anadolu Teknoloji Dağıtım",
                    CariKodu = "CARİ-ANADOLU-03",
                    SatisTarihi = DateTime.Now.AddDays(-14),
                    SatilanUrunlerOzet = "10x Sunmi V2s Mobil Android POS El Terminali (SN: V2S-501..510)",
                    Kategori = "Donanım",
                    ToplamAdet = 10,
                    BirimFiyatTL = 21000m,
                    IskontoOrani = 35m,
                    ToplamTutarTL = 136500m,
                    SevkiyatDurumu = "Teslim Edildi",
                    KargoTakipNo = "YK-8840192",
                    FaturaDurumu = "E-Fatura Kesildi (GİB Onaylı)"
                },
                new BayiSatisKaydi
                {
                    SatisNo = "SAT-2026-1040",
                    FaturaNo = "GIB-FTR-2026-008544",
                    BayiId = "bayi-4",
                    BayiAdi = "Akdeniz Yazılım & Donanım A.Ş.",
                    CariKodu = "CARİ-AKDENIZ-04",
                    SatisTarihi = DateTime.Now.AddDays(-21),
                    SatilanUrunlerOzet = "3x HiCariTakip Bulut ERP 5 Kullanıcı Yıllık Lisans Paketi + E-Fatura Entegrasyon Modülü",
                    Kategori = "Yazılım Lisans",
                    ToplamAdet = 3,
                    BirimFiyatTL = 18000m,
                    IskontoOrani = 40m,
                    ToplamTutarTL = 32400m,
                    SevkiyatDurumu = "Dijital Lisans Teslim Edildi",
                    KargoTakipNo = "E-LISANS-AKTIF",
                    FaturaDurumu = "E-Fatura Kesildi (GİB Onaylı)"
                }
            });

            // 6. BOZUK CİHAZ VE DEĞİŞİM (SWAP / DOA)
            SwapKayitlari.AddRange(new[]
            {
                new BozukCihazSwapKaydi
                {
                    SwapNo = "SWAP-2026-039",
                    BayiId = "bayi-1",
                    BayiAdi = "Marmara POS Sistemleri A.Ş.",
                    CariKodu = "CARİ-MARMARA-01",
                    BozukCihazModeli = "Sunmi V2s Mobil POS Terminali",
                    BozukCihazSeriNo = "SN: V2S-98412-TR",
                    ArizaNedeni = "Barkod lazer okuyucu lazer ünitesi yanıt vermiyor, kutudan arızalı çıktı (DOA İlk 30 Gün)",
                    DegisimTuru = "DOA (İlk 30 Gün Birebir Sıfır)",
                    VerilenYeniCihazSeriNo = "SN: V2S-99015-TR (Sıfır Cihaz Sevk Edildi)",
                    TalepTarihi = DateTime.Now.AddDays(-2),
                    DegisimTarihi = DateTime.Now.AddDays(-1),
                    Durum = "Onaylandı & Yeni Cihaz Sevk Edildi",
                    OnaylayanAdmin = "Teknik Servis Müdürü",
                    KargoTakipNo = "YK-9941203"
                },
                new BozukCihazSwapKaydi
                {
                    SwapNo = "SWAP-2026-035",
                    BayiId = "bayi-2",
                    BayiAdi = "Ege Barkod & Otomasyon Ltd.",
                    CariKodu = "CARİ-EGE-02",
                    BozukCihazModeli = "KodPOS T500 All-in-One Dokunmatik POS",
                    BozukCihazSeriNo = "SN: KOD-T500-2210",
                    ArizaNedeni = "Anakart güç devresinde voltaj dalgalanması sebebiyle ekrana görüntü gelmiyor",
                    DegisimTuru = "Garanti Kapsamı Swap (Konsinye İkame)",
                    VerilenYeniCihazSeriNo = "SN: KOD-T500-2490 (Konsinye İkame)",
                    TalepTarihi = DateTime.Now.AddDays(-5),
                    DegisimTarihi = DateTime.Now.AddDays(-4),
                    Durum = "Onaylandı & Konsinye Sevk Edildi",
                    OnaylayanAdmin = "Teknik Servis Müdürü",
                    KargoTakipNo = "ARAS-661204"
                },
                new BozukCihazSwapKaydi
                {
                    SwapNo = "SWAP-2026-028",
                    BayiId = "bayi-3",
                    BayiAdi = "İç Anadolu Teknoloji Dağıtım",
                    CariKodu = "CARİ-ANADOLU-03",
                    BozukCihazModeli = "Zebra ZD421 Masaüstü Barkod Yazıcı",
                    BozukCihazSeriNo = "SN: ZBR-421-7781",
                    ArizaNedeni = "Sıvı teması sonucu termal anakart kısa devre yanması",
                    DegisimTuru = "Kullanıcı Hatası - İndirimli Revize Değişim",
                    VerilenYeniCihazSeriNo = "SN: ZBR-421-8902 (Revize Değişim)",
                    TalepTarihi = DateTime.Now.AddDays(-12),
                    DegisimTarihi = DateTime.Now.AddDays(-10),
                    Durum = "Onaylandı & Teslim Edildi",
                    OnaylayanAdmin = "Servis Müdürü",
                    KargoTakipNo = "YK-7712044"
                }
            });

            // 7. ESKİ MÜŞTERİLER & PASİF BAYİLER (YAZILIM, DONANIM VEYA İKİSİ)
            EskiMusteriler.AddRange(new[]
            {
                new EskiBayiMusteriKaydi
                {
                    Id = "ESK-001",
                    FirmaVeyaKisiAdi = "Anadolu Perakende & Barkod Otomasyon Ltd.",
                    YetkiliAdSoyad = "Metin Kaya",
                    CariKodu = "CARİ-ESK-01",
                    MusteriTipi = "Eski Bayi",
                    Kategori = "Donanım + Yazılım (İkisi)",
                    Telefon = "0532 555 10 20",
                    Eposta = "metin.kaya@anadolubarkod.com",
                    Sehir = "Kayseri",
                    NelerAldiDetay = "8x Sunmi D2s Android POS Terminali, 4x Zebra ZD421 Barkod Yazıcı, 1x HiCariTakip ERP 10 Kullanıcı Bulut Lisans",
                    OdenenToplamTutarTL = 148500m,
                    FaturaDekontOzet = "FTR-2023-0912 & FTR-2024-0104 (Banka Havale/EFT ile Tahsil Edildi)",
                    AlisTarihi = new DateTime(2023, 2, 10),
                    BitisTarihi = new DateTime(2024, 3, 15),
                    BitisNedeni = "Firma kurumsal ERP lisans yenilemesini yapmadı; şirket muhasebe altyapısını yerel bir yazılıma taşıdı.",
                    AktiflikDurumu = "Artık Müşterimiz Değil (Pasif Bayi)",
                    GeriKazanimDurumu = "Teklif Hazırlanabilir (%20 İndirimli Yenileme Kampanyası)",
                    SonIletisimNotu = "Teknik destek ile görüştü, yeni Sunmi V2s modelleri için tanıtım kataloğu talep etti."
                },
                new EskiBayiMusteriKaydi
                {
                    Id = "ESK-002",
                    FirmaVeyaKisiAdi = "Boğaziçi Market Zincirleri A.Ş.",
                    YetkiliAdSoyad = "Engin Demir",
                    CariKodu = "CARİ-ESK-02",
                    MusteriTipi = "Eski B2B Müşteri",
                    Kategori = "Yazılım",
                    Telefon = "0212 444 88 90",
                    Eposta = "muhasebe@bogazicimarket.com.tr",
                    Sehir = "İstanbul",
                    NelerAldiDetay = "HiCariTakip Cloud ERP Enterprise Sınırsız Kullanıcı Lisansı + E-Fatura / E-Arşiv API Entegrasyon Modülü",
                    OdenenToplamTutarTL = 95000m,
                    FaturaDekontOzet = "FTR-2023-1188 (Sanal POS 3D Secure Tahsilat)",
                    AlisTarihi = new DateTime(2023, 5, 1),
                    BitisTarihi = new DateTime(2024, 5, 1),
                    BitisNedeni = "1 Yıllık SaaS abonelik süresi doldu; şirket mağaza sayısını tek şubeye indirdiği için kurumsal lisansı yenilemedi.",
                    AktiflikDurumu = "Süresi Doldu (Eski Kurumsal Müşteri)",
                    GeriKazanimDurumu = "Teklif Gönderildi (Küçük İşletme / Esnaf ERP Paketi)",
                    SonIletisimNotu = "Teklif inceleniyor, önümüzdeki çeyrekte yeniden bulut ERP'ye dönüş düşünülebilir."
                },
                new EskiBayiMusteriKaydi
                {
                    Id = "ESK-003",
                    FirmaVeyaKisiAdi = "Toros Otomasyon & POS Çözümleri",
                    YetkiliAdSoyad = "Süleyman Aksoy",
                    CariKodu = "CARİ-ESK-03",
                    MusteriTipi = "Eski Donanım Bayisi",
                    Kategori = "Donanım",
                    Telefon = "0242 321 40 50",
                    Eposta = "suleyman@torospos.com",
                    Sehir = "Antalya",
                    NelerAldiDetay = "15x KodPOS T500 All-in-One Dokunmatik POS Kasa + 15x Datalogic Gryphon 2D Karekod Okuyucu + 100 Koli Termal Rulo",
                    OdenenToplamTutarTL = 215000m,
                    FaturaDekontOzet = "FTR-2022-0441 (DBS Otomatik Tahsilat)",
                    AlisTarihi = new DateTime(2022, 8, 20),
                    BitisTarihi = new DateTime(2024, 8, 20),
                    BitisNedeni = "2 Yıllık distribütör donanım garanti ve bakım sözleşmesi süresi doldu; yeni toplu cihaz alımı yapmadı.",
                    AktiflikDurumu = "Sözleşme Bitti (Pasif Donanım Bayisi)",
                    GeriKazanimDurumu = "Teklif Hazırlanabilir (Yeni Nesil KodPOS T500 & Sunmi Değişim Kampanyası)",
                    SonIletisimNotu = "Mevcut kasalar çalışıyor ancak yeni sezonda yedek koli rulo ve 5 adet ilave mobil POS düşünebilir."
                },
                new EskiBayiMusteriKaydi
                {
                    Id = "ESK-004",
                    FirmaVeyaKisiAdi = "Güneydoğu Bilgisayar & Yazarkasa",
                    YetkiliAdSoyad = "Harun Çelik",
                    CariKodu = "CARİ-ESK-04",
                    MusteriTipi = "Eski Bayi",
                    Kategori = "Donanım + Yazılım (İkisi)",
                    Telefon = "0412 228 90 10",
                    Eposta = "info@guneydogupos.com",
                    Sehir = "Diyarbakır",
                    NelerAldiDetay = "4x PAX A910 Yeni Nesil Mobil Yazarkasa POS + HiCariTakip Mobil Restoran Satış Modülü (3 Yıllık)",
                    OdenenToplamTutarTL = 72000m,
                    FaturaDekontOzet = "FTR-2023-0105 (Banka Havalesi)",
                    AlisTarihi = new DateTime(2023, 1, 12),
                    BitisTarihi = new DateTime(2024, 6, 30),
                    BitisNedeni = "Ortaklar ayrıldığı için tüzel kişilik tasfiyeye girdi; bayilik sözleşmesi karşılıklı mutabakatla feshedildi.",
                    AktiflikDurumu = "Şirket Kapandı / Sözleşme Feshedildi",
                    GeriKazanimDurumu = "Düşük (Tüzel kişilik kapandı, yetkili yeni şirket kurarsa iletişime geçilecek)",
                    SonIletisimNotu = "Yetkili Harun Bey bireysel danışmanlık yapıyor, yeni firma kurarsa haber vereceğini iletti."
                },
                new EskiBayiMusteriKaydi
                {
                    Id = "ESK-005",
                    FirmaVeyaKisiAdi = "Kuzey E-Ticaret ve Barkod Sistemleri",
                    YetkiliAdSoyad = "Banu Yılmaz",
                    CariKodu = "CARİ-ESK-05",
                    MusteriTipi = "Eski B2B Müşteri",
                    Kategori = "Yazılım",
                    Telefon = "0462 330 15 25",
                    Eposta = "banu@kuzeybarkod.com",
                    Sehir = "Trabzon",
                    NelerAldiDetay = "HiCariTakip B2B E-Ticaret Entegrasyon Lisansı & Pazaryeri Modülü (Trendyol / Hepsiburada Senkronizasyonu)",
                    OdenenToplamTutarTL = 42000m,
                    FaturaDekontOzet = "FTR-2023-0780 (Kredi Kartı 3 Taksit)",
                    AlisTarihi = new DateTime(2023, 9, 15),
                    BitisTarihi = new DateTime(2024, 9, 15),
                    BitisNedeni = "Pazaryeri e-ticaret mağaza operasyonlarını sonlandırdı, yıllık yenileme sözleşmesini uzatmadı.",
                    AktiflikDurumu = "Abonelik Sona Erdi",
                    GeriKazanimDurumu = "Teklif Gönderildi (Sadece Muhasebe & E-Arşiv Lisansı)",
                    SonIletisimNotu = "Geri arama talep etti, mali müşaviri ile değerlendirecek."
                },
                new EskiBayiMusteriKaydi
                {
                    Id = "ESK-006",
                    FirmaVeyaKisiAdi = "Batı Lojistik ve Depo Barkodlama A.Ş.",
                    YetkiliAdSoyad = "Caner Aydın",
                    CariKodu = "CARİ-ESK-06",
                    MusteriTipi = "Eski B2B Kurumsal Müşteri",
                    Kategori = "Donanım",
                    Telefon = "0224 441 33 22",
                    Eposta = "satinAlma@batilojistik.com.tr",
                    Sehir = "Bursa",
                    NelerAldiDetay = "6x Zebra ZT411 Endüstriyel Metal Barkod Yazıcı (203 DPI) + 120x Wax/Resin Ribon Koli",
                    OdenenToplamTutarTL = 168000m,
                    FaturaDekontOzet = "FTR-2023-0312 (Havale / EFT)",
                    AlisTarihi = new DateTime(2023, 4, 18),
                    BitisTarihi = new DateTime(2025, 4, 18),
                    BitisNedeni = "Depo kurulum projesi tamamlandı; donanım satın alımı ve ilk garanti süresi bitti, sarf alımlarını yerel bayiden karşılıyor.",
                    AktiflikDurumu = "Proje Tamamlandı (Pasif Müşteri)",
                    GeriKazanimDurumu = "Teklif Hazırlanabilir (Periyodik Kafa Bakımı & Orijinal Ribon Kampanyası)",
                    SonIletisimNotu = "Satın alma sorumlusu değişti, yeni yetkiliye kurumsal indirim listemiz mail atıldı."
                }
            });
        }

        public static void DuyuruEkle(DuyuruBildirimKaydi duyuru)
        {
            lock (_lock)
            {
                Duyurular.Insert(0, duyuru);
            }
        }

        public static void ServisKaydiGuncelleVeyaEkle(ServisAracCihazKaydi kayit)
        {
            lock (_lock)
            {
                var idx = ServisKayitlari.FindIndex(s => s.ServisTakipNo == kayit.ServisTakipNo);
                if (idx >= 0)
                {
                    ServisKayitlari[idx] = kayit;
                }
                else
                {
                    ServisKayitlari.Insert(0, kayit);
                }
            }
        }

        public static List<DuyuruBildirimKaydi> GetDuyurular(string? bayiId, string? q)
        {
            lock (_lock)
            {
                var list = Duyurular.AsEnumerable();
                if (!string.IsNullOrWhiteSpace(bayiId))
                {
                    list = list.Where(d => d.HedefTipi == "TumBayiler" || (d.HedefBayiIdleri != null && d.HedefBayiIdleri.Contains(bayiId)));
                }
                if (!string.IsNullOrWhiteSpace(q))
                {
                    var kw = q.ToLowerInvariant();
                    list = list.Where(d => d.Baslik.ToLowerInvariant().Contains(kw) || d.Icerik.ToLowerInvariant().Contains(kw));
                }
                return list.ToList();
            }
        }

        public static List<ServisAracCihazKaydi> GetServisKayitlari(string? bayiId, string? q)
        {
            lock (_lock)
            {
                var list = ServisKayitlari.AsEnumerable();
                if (!string.IsNullOrWhiteSpace(bayiId))
                {
                    list = list.Where(s => s.BayiId == bayiId);
                }
                if (!string.IsNullOrWhiteSpace(q))
                {
                    var kw = q.ToLowerInvariant();
                    list = list.Where(s => s.ServisTakipNo.ToLowerInvariant().Contains(kw) ||
                                           s.PlakaVeyaSeriNo.ToLowerInvariant().Contains(kw) ||
                                           s.ModelTanim.ToLowerInvariant().Contains(kw) ||
                                           s.BayiAdi.ToLowerInvariant().Contains(kw));
                }
                return list.ToList();
            }
        }

        public static List<BayiTalepSikayetKaydi> GetTalepler(string? bayiId, string? q)
        {
            lock (_lock)
            {
                var list = Talepler.AsEnumerable();
                if (!string.IsNullOrWhiteSpace(bayiId))
                {
                    list = list.Where(t => t.BayiId == bayiId);
                }
                if (!string.IsNullOrWhiteSpace(q))
                {
                    var kw = q.ToLowerInvariant();
                    list = list.Where(t => t.TalepNo.ToLowerInvariant().Contains(kw) ||
                                           t.Baslik.ToLowerInvariant().Contains(kw) ||
                                           t.BayiAdi.ToLowerInvariant().Contains(kw));
                }
                return list.ToList();
            }
        }

        public static List<BayiOdemeKaydi> GetOdemeler(string? bayiId, string? q)
        {
            lock (_lock)
            {
                var list = Odemeler.AsEnumerable();
                if (!string.IsNullOrWhiteSpace(bayiId))
                {
                    list = list.Where(o => o.BayiId == bayiId);
                }
                if (!string.IsNullOrWhiteSpace(q))
                {
                    var kw = q.ToLowerInvariant();
                    list = list.Where(o => o.IslemNo.ToLowerInvariant().Contains(kw) ||
                                           o.DekontBankaReferans.ToLowerInvariant().Contains(kw) ||
                                           o.BayiAdi.ToLowerInvariant().Contains(kw));
                }
                return list.ToList();
            }
        }

        public static List<BayiSatisKaydi> GetSatislar(string? bayiId, string? q)
        {
            lock (_lock)
            {
                var list = Satislar.AsEnumerable();
                if (!string.IsNullOrWhiteSpace(bayiId))
                {
                    list = list.Where(s => s.BayiId == bayiId);
                }
                if (!string.IsNullOrWhiteSpace(q))
                {
                    var kw = q.ToLowerInvariant();
                    list = list.Where(s => s.SatisNo.ToLowerInvariant().Contains(kw) ||
                                           s.FaturaNo.ToLowerInvariant().Contains(kw) ||
                                           s.SatilanUrunlerOzet.ToLowerInvariant().Contains(kw) ||
                                           s.BayiAdi.ToLowerInvariant().Contains(kw));
                }
                return list.ToList();
            }
        }

        public static List<BozukCihazSwapKaydi> GetSwapKayitlari(string? bayiId, string? q)
        {
            lock (_lock)
            {
                var list = SwapKayitlari.AsEnumerable();
                if (!string.IsNullOrWhiteSpace(bayiId))
                {
                    list = list.Where(sw => sw.BayiId == bayiId);
                }
                if (!string.IsNullOrWhiteSpace(q))
                {
                    var kw = q.ToLowerInvariant();
                    list = list.Where(sw => sw.SwapNo.ToLowerInvariant().Contains(kw) ||
                                            sw.BozukCihazSeriNo.ToLowerInvariant().Contains(kw) ||
                                            sw.VerilenYeniCihazSeriNo.ToLowerInvariant().Contains(kw) ||
                                            sw.BayiAdi.ToLowerInvariant().Contains(kw));
                }
                return list.ToList();
            }
        }

        public static List<EskiBayiMusteriKaydi> GetEskiMusteriler(string? kategori, string? q)
        {
            lock (_lock)
            {
                var list = EskiMusteriler.AsEnumerable();
                if (!string.IsNullOrWhiteSpace(kategori) && kategori != "hepsi" && kategori != "tum")
                {
                    var kat = kategori.ToLowerInvariant();
                    if (kat == "yazilim")
                    {
                        list = list.Where(x => x.Kategori == "Yazılım");
                    }
                    else if (kat == "donanim")
                    {
                        list = list.Where(x => x.Kategori == "Donanım");
                    }
                    else if (kat == "ikisi")
                    {
                        list = list.Where(x => x.Kategori.Contains("İkisi"));
                    }
                }
                if (!string.IsNullOrWhiteSpace(q))
                {
                    var kw = q.ToLowerInvariant();
                    list = list.Where(x => x.FirmaVeyaKisiAdi.ToLowerInvariant().Contains(kw) ||
                                           (x.YetkiliAdSoyad != null && x.YetkiliAdSoyad.ToLowerInvariant().Contains(kw)) ||
                                           x.NelerAldiDetay.ToLowerInvariant().Contains(kw) ||
                                           x.BitisNedeni.ToLowerInvariant().Contains(kw) ||
                                           x.CariKodu.ToLowerInvariant().Contains(kw) ||
                                           x.Sehir.ToLowerInvariant().Contains(kw));
                }
                return list.ToList();
            }
        }
    }
}


