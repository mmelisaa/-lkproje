using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.Siniflar;
using WebApplication1.Models.Siniflar.Kurumsal;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IEmailService emailService)
        {
            _logger = logger;
            _context = context;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Referanslar = await _context.ReferansProjeler.ToListAsync();
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        [Route("yazilimlar")]
        public IActionResult Yazilimlar()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Hakkimizda()
        {
            var model = await _context.Hakkimizda.FirstOrDefaultAsync();
            if (model == null)
            {
                model = new Hakkimizda
                {
                    Baslik = "Hakkımızda",
                    Aciklama = "Elazığ merkezli olarak faaliyet gösteren Hayat İşleri, HiCariTakip ön muhasebe, barkodlu hızlı satış sistemleri, yeni nesil POS, PDKS turnike geçiş ve özel kurumsal yazılım alanlarında anahtar teslim işletme çözümleri sunar.",
                    GorselUrl = "/images/hakkimizda-hero.svg",
                    HizmetAlanlari = "HiCariTakip ön muhasebe ve cari takip, barkod & mağaza otomasyonu, yeni nesil POS çözümleri, PDKS geçiş kontrol, özel yazılım & kurumsal web sistemleri.",
                    KurumsalGucumuz = "Genç, dinamik ve alanında uzman ekibimizle işletmelerin dijitalleşme ve otomasyon süreçlerini yerinde destekliyoruz.",
                    CalismaIlkelerimiz = "Şeffaflık, yerinde ve uzaktan kesintisiz teknik destek, müşteri memnuniyeti ve sürdürülebilir işletme verimliliği.",
                    Vizyonumuz = "Elazığ ve bölge genelinde işletme otomasyonu, ön muhasebe ve yazılım çözümlerinde güvenilir öncü teknoloji ortağı olmak.",
                    Misyonumuz = "KOBİ ve kurumsal işletmelerin satış, stok, muhasebe ve personel süreçlerini modern yazılım ve donanım altyapısıyla kolaylaştırmak.",
                    Onceliklerimiz = "İşletmelerin operasyonel hızını artıran, hatasız ve güvenilir ticari yazılım, barkod ve otomasyon sistemlerine odaklanmak."
                };
                _context.Hakkimizda.Add(model);
                await _context.SaveChangesAsync();
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> InsanKaynaklari()
        {
            var model = await _context.InsanKaynaklari.FirstOrDefaultAsync();
            if (model == null)
            {
                model = new InsanKaynaklari
                {
                    UstBaslik = "KARİYER & İNSAN KAYNAKLARI",
                    AnaBaslik = "Ekibimizin Bir Parçası Olun",
                    Aciklama = "Büyüyen ve gelişen Hayat İşleri ailesinde siz de yerinizi almak istiyorsanız, aşağıdaki formu doldurarak başvuru yapabilirsiniz.",
                    FormBaslik = "GENEL BAŞVURU VEYA POZİSYON BİLDİRİMİ",
                    FormAltBaslik = "Yeteneklerinizi, deneyimlerinizi ve kariyer hedeflerinizi bizimle paylaşın."
                };
                _context.InsanKaynaklari.Add(model);
                await _context.SaveChangesAsync();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InsanKaynaklariBasvuru(string adSoyad, string eposta, string? pozisyon, string? onYazi)
        {
            var mesaj = new SiteMesaj
            {
                AdSoyad = adSoyad,
                Eposta = eposta,
                Pozisyon = pozisyon,
                OnYazi = onYazi,
                FormTipi = "İş Başvurusu",
                GonderimTarihi = DateTime.Now
            };
            _context.SiteMesajlari.Add(mesaj);
            await _context.SaveChangesAsync();

            // E-Posta bildirimi gönder (berkayevrann.1903@gmail.com hesabı için)
            await _emailService.SendFormNotificationAsync("İş Başvurusu", adSoyad, eposta, null, null, pozisyon, null, onYazi);

            TempData["FormMesaj"] = "Başvurunuz başarıyla gönderildi. En kısa sürede sizinle iletişime geçeceğiz.";
            return RedirectToAction(nameof(InsanKaynaklari));
        }

        [HttpGet]
        public IActionResult HesapNumaralarimiz()
        {
            var model = new HesapNumaralarimiz
            {
                UstBaslik = "Finansal İşlemler",
                AnaBaslik = "Hesap Numaralarımız",
                Aciklama = "Ödemeleriniz ve havale/EFT işlemleriniz için kurumsal banka hesap bilgilerimize aşağıdan ulaşabilirsiniz.",
                UyariBaslik = "Önemli Bilgilendirme",
                UyariMetni = "Lütfen havale veya EFT yaparken açıklama kısmına Firma Adınızı / Unvanınızı veya size bildirilen Sipariş / Proje Kodunu yazmayı unutmayınız. Ödeme yapıldıktan sonra dekontunuzu info@hayatisleri.com adresine iletebilirsiniz."
            };
            return View(model);
        }

        [HttpGet]
        public IActionResult Hizmetlerimiz()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Urunler()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Referanslarimiz()
        {
            var projeler = await _context.ReferansProjeler.ToListAsync();
            if (!projeler.Any())
            {
                projeler = new List<ReferansProje>
                {
                    new ReferansProje
                    {
                        Baslik = "Ön Muhasebe Programı ve B2B Cari Yönetimi | HiCariTakip",
                        Kategori = "Özel Yazılım & Finans",
                        Aciklama = "GİB e-Fatura/e-Arşiv entegrasyonu, gerçek zamanlı stok, cari hesap takibi ve çok şubeli gelir-gider analitiği.",
                        GorselUrl = "/images/ref-real-1.jpg",
                        Etiketler = "e-Fatura GİB, Bulut ERP, Stok Yönetimi",
                        ProjeLinki = "https://www.hicari.com.tr/"
                    },
                    new ReferansProje
                    {
                        Baslik = "Fırat Mimarlık - Kurumsal Portfolyo Sitesi",
                        Kategori = "Web Tasarım",
                        Aciklama = "Minimalist ve Apple tarzı tasarım çizgileriyle, mimari projelerin sergilendiği yüksek performanslı web arayüzü.",
                        GorselUrl = "/images/ref-real-2.jpg",
                        Etiketler = "UI/UX, HTML5, CSS3, JavaScript",
                        ProjeLinki = "#"
                    },
                    new ReferansProje
                    {
                        Baslik = "Anadolu Tarım Market - Çok Satıcılı E-Ticaret",
                        Kategori = "E-Ticaret Sistemleri",
                        Aciklama = "Yerel üreticilerin ürünlerini ulusal pazara taşıdığı, gelişmiş ödeme entegrasyonlu e-ticaret platformu.",
                        GorselUrl = "/images/ref-real-3.jpg",
                        Etiketler = "E-Ticaret, API Entegrasyonu, Güvenlik",
                        ProjeLinki = "#"
                    },
                    new ReferansProje
                    {
                        Baslik = "Kriter Akademi - Uzaktan Eğitim ve Sınav Otomasyonu",
                        Kategori = "Kurumsal Yazılım",
                        Aciklama = "Öğrencilerin deneme sınavı analizlerini yapabildiği, video içerik ve canlı ders yönetim sistemi.",
                        GorselUrl = "/images/ref-real-4.jpg",
                        Etiketler = "Yazılım Geliştirme, Bulut Hosting",
                        ProjeLinki = "#"
                    }
                };
                _context.ReferansProjeler.AddRange(projeler);
                await _context.SaveChangesAsync();
            }

            return View(projeler);
        }

        [HttpGet]
        public async Task<IActionResult> Blog()
        {
            var yazilar = await _context.BlogPosts.ToListAsync();
            if (!yazilar.Any())
            {
                yazilar = new List<BlogPost>
                {
                    new BlogPost
                    {
                        Baslik = "Yazılım Dünyasındaki Yenilikler ve Kurumsal Çözümler",
                        Ozet = "Profesyonel ekip ve güçlü altyapıyla yürütülen çalışmalar, yazılım projelerinde etkin sonuçlar ortaya koyuyor. Firma aynı zamanda sürdürülebilir teknoloji sunuyor.",
                        Tarih = "27 Eylül 2026",
                        Kategori = "Duyuru",
                        GorselUrl = "/images/blog-yazilim.svg"
                    },
                    new BlogPost
                    {
                        Baslik = "Web Tasarımında Minimalizm ve Performans",
                        Ozet = "Test blog yazısı kısa açıklamalarının yer alacağı alanımız burasıdır. Yazı içinden yada harici olarak kısa bir açıklama girilecek.",
                        Tarih = "27 Eylül 2026",
                        Kategori = "Teknoloji",
                        GorselUrl = "/images/blog-uiux.svg"
                    }
                };
                _context.BlogPosts.AddRange(yazilar);
                await _context.SaveChangesAsync();
            }

            return View(yazilar);
        }

        [HttpGet]
        public IActionResult Contact()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(string adSoyad, string eposta, string? konu, string? mesaj)
        {
            var siteMesaj = new SiteMesaj
            {
                AdSoyad = adSoyad,
                Eposta = eposta,
                Konu = konu,
                Mesaj = mesaj,
                FormTipi = "İletişim",
                GonderimTarihi = DateTime.Now
            };
            _context.SiteMesajlari.Add(siteMesaj);
            await _context.SaveChangesAsync();

            // E-Posta bildirimi gönder (berkayevrann.1903@gmail.com hesabı için)
            await _emailService.SendFormNotificationAsync("İletişim Mesajı", adSoyad, eposta, null, konu, null, mesaj, null);

            TempData["FormMesaj"] = "Mesajınız başarıyla gönderildi. En kısa sürede size dönüş yapacağız.";
            return RedirectToAction(nameof(Contact));
        }

        [HttpGet]
        public IActionResult TeklifAl()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TeklifAl(string adSoyad, string eposta, string? telefon, string? konu, string? mesaj)
        {
            var siteMesaj = new SiteMesaj
            {
                AdSoyad = adSoyad,
                Eposta = eposta,
                Telefon = telefon,
                Konu = konu,
                Mesaj = mesaj,
                FormTipi = "Teklif Talebi",
                GonderimTarihi = DateTime.Now
            };
            _context.SiteMesajlari.Add(siteMesaj);
            await _context.SaveChangesAsync();

            // E-Posta bildirimi gönder (berkayevrann.1903@gmail.com hesabı için)
            await _emailService.SendFormNotificationAsync("Teklif Talebi", adSoyad, eposta, telefon, konu, null, mesaj, null);

            TempData["FormMesaj"] = "Teklif talebiniz alındı. Ekibimiz en kısa sürede sizinle iletişime geçecektir.";
            return RedirectToAction(nameof(TeklifAl));
        }

        [HttpGet]
        public IActionResult Destek()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Destek(string adSoyad, string eposta, string? telefon, string? konu, string? mesaj)
        {
            var siteMesaj = new SiteMesaj
            {
                AdSoyad = adSoyad,
                Eposta = eposta,
                Telefon = telefon,
                Konu = konu,
                Mesaj = mesaj,
                FormTipi = "Teknik Destek Talebi",
                GonderimTarihi = DateTime.Now
            };
            _context.SiteMesajlari.Add(siteMesaj);
            await _context.SaveChangesAsync();

            await _emailService.SendFormNotificationAsync("Teknik Destek Talebi", adSoyad, eposta, telefon, konu, null, mesaj, null);

            TempData["DestekMesaj"] = "Destek talebiniz başarıyla oluşturuldu. Uzman teknik ekibimiz en kısa sürede sizinle iletişime geçecektir.";
            return RedirectToAction(nameof(Destek));
        }

        [HttpGet]
        public IActionResult BayiGirisi()
        {
            // Giriş yapmış bayi doğrudan kendi paneline gider
            if (User.Identity?.IsAuthenticated == true && User.IsInRole(Roller.Bayi))
                return RedirectToAction("Dashboard", "Bayi");

            return View();
        }

        // Eski formlar bu adrese POST ediyordu; giriş ve başvuru artık BayiController (Giris / Kayit) üzerinden yürür.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BayiGirisi(string? islemTipi)
        {
            return RedirectToAction(nameof(BayiGirisi));
        }
    }
}
