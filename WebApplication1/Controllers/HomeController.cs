using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebApplication1.Models;
using WebApplication1.Models.Siniflar;
using WebApplication1.Data;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
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
        public IActionResult Hakkimizda()
        {
            var model = new WebApplication1.Models.Siniflar.Kurumsal.Hakkimizda
            {
                Baslik = "HakkÄ±mÄ±zda",
                Aciklama = "ElazÄ±ÄŸ merkezli olarak kurulan Hayat Ä°ÅŸleri, web tasarÄ±mÄ±, Ã¶zel yazÄ±lÄ±m, e-ticaret, hosting ve SEO alanlarÄ±nda profesyonel kurumsal Ã§Ã¶zÃ¼mler sunar.",
                GorselUrl = "https://images.unsplash.com/photo-1555066931-4365d14bab8c?auto=format&fit=crop&w=800&q=80",
                HizmetAlanlari = "Web tasarÄ±m, Ã¶zel yazÄ±lÄ±m geliÅŸtirme, e-ticaret sistemleri ve kurumsal hosting hizmetleri sunuyoruz.",
                KurumsalGucumuz = "GenÃ§, dinamik ve alanÄ±nda uzman ekibimizle en gÃ¼ncel teknolojileri projelendiriyoruz.",
                CalismaIlkelerimiz = "ÅeffaflÄ±k, zamanÄ±nda teslim, mÃ¼ÅŸteri memnuniyeti ve sÃ¼rdÃ¼rÃ¼lebilir destek.",
                Vizyonumuz = "BÃ¶lgesel gÃ¼cÃ¼mÃ¼zÃ¼ uluslararasÄ± standartlarda yazÄ±lÄ±m Ã§Ã¶zÃ¼mleriyle birleÅŸtirerek sektÃ¶rÃ¼n lideri olmak.",
                Misyonumuz = "Ä°ÅŸletmelerin dijital dÃ¶nÃ¼ÅŸÃ¼m sÃ¼reÃ§lerini gÃ¼venli, hÄ±zlÄ± ve estetik altyapÄ±larla desteklemek.",
                Onceliklerimiz = "DonanÄ±m satÄ±ÅŸÄ± veya tamiri ile vakit kaybetmeden, tamamen yazÄ±lÄ±m ve web teknolojilerine odaklanmak."
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult InsanKaynaklari()
        {
            var model = new WebApplication1.Models.Siniflar.InsanKaynaklari
            {
                UstBaslik = "KARÄ°YER & Ä°NSAN KAYNAKLARI",
                AnaBaslik = "Ekibimizin Bir ParÃ§asÄ± Olun",
                Aciklama = "BÃ¼yÃ¼yen ve geliÅŸen Hayat Ä°ÅŸleri ailesinde siz de yerinizi almak istiyorsanÄ±z, aÅŸaÄŸÄ±daki formu doldurarak baÅŸvuru yapabilirsiniz.",
                FormBaslik = "GENEL BAÅVURU VEYA POZÄ°SYON BÄ°LDÄ°RÄ°MÄ°",
                FormAltBaslik = "Yeteneklerinizi, deneyimlerinizi ve kariyer hedeflerinizi bizimle paylaÅŸÄ±n."
            };

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
                FormTipi = "Ä°ÅŸ BaÅŸvurusu",
                GonderimTarihi = DateTime.Now
            };
            _context.SiteMesajlari.Add(mesaj);
            await _context.SaveChangesAsync();
            TempData["FormMesaj"] = "BaÅŸvurunuz baÅŸarÄ±yla gÃ¶nderildi. En kÄ±sa sÃ¼rede sizinle iletiÅŸime geÃ§eceÄŸiz.";
            return RedirectToAction(nameof(InsanKaynaklari));
        }

        [HttpGet]
        public IActionResult HesapNumaralarimiz()
        {
            var model = new HesapNumaralarimiz
            {
                UstBaslik = "Finansal Ä°ÅŸlemler",
                AnaBaslik = "Hesap NumaralarÄ±mÄ±z",
                Aciklama = "Ã–demeleriniz ve havale/EFT iÅŸlemleriniz iÃ§in kurumsal banka hesap bilgilerimize aÅŸaÄŸÄ±dan ulaÅŸabilirsiniz.",
                UyariBaslik = "Ã–nemli Bilgilendirme",
                UyariMetni = "LÃ¼tfen havale veya EFT yaparken aÃ§Ä±klama kÄ±smÄ±na Firma AdÄ±nÄ±zÄ± / UnvanÄ±nÄ±zÄ± veya size bildirilen SipariÅŸ / Proje Kodunu yazmayÄ± unutmayÄ±nÄ±z. Ã–deme yapÄ±ldÄ±ktan sonra dekontunuzu info@hayatisleri.com adresine iletebilirsiniz."
            };
            return View(model);
        }

        [HttpGet]
        public IActionResult Hizmetlerimiz()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Referanslarimiz()
        {
            var projeler = new List<WebApplication1.Models.Siniflar.ReferansProje>
            {
                new WebApplication1.Models.Siniflar.ReferansProje
                {
                    Id = 1,
                    Baslik = "ElazÄ±ÄŸ Global Lojistik - E-Ticaret ve Takip Sistemi",
                    Kategori = "Ã–zel YazÄ±lÄ±m & E-Ticaret",
                    Aciklama = "BÃ¶lgesel lojistik sÃ¼reÃ§lerini dijitalleÅŸtiren, uÃ§tan uca kargo takip ve sipariÅŸ yÃ¶netim altyapÄ±sÄ±.",
                    GorselUrl = "https://images.unsplash.com/photo-1586528116311-ad8dd3c8310d?auto=format&fit=crop&w=800&q=80",
                    Etiketler = "ASP.NET Core, SQL Server, Bootstrap"
                },
                new WebApplication1.Models.Siniflar.ReferansProje
                {
                    Id = 2,
                    Baslik = "FÄ±rat MimarlÄ±k - Kurumsal Portfolyo Sitesi",
                    Kategori = "Web TasarÄ±m",
                    Aciklama = "Minimalist ve Apple tarzÄ± tasarÄ±m Ã§izgileriyle, mimari projelerin sergilendiÄŸi yÃ¼ksek performanslÄ± web arayÃ¼zÃ¼.",
                    GorselUrl = "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=800&q=80",
                    Etiketler = "UI/UX, HTML5, CSS3, JavaScript"
                },
                new WebApplication1.Models.Siniflar.ReferansProje
                {
                    Id = 3,
                    Baslik = "Anadolu TarÄ±m Market - Ã‡ok SatÄ±cÄ±lÄ± E-Ticaret",
                    Kategori = "E-Ticaret Sistemleri",
                    Aciklama = "Yerel Ã¼reticilerin Ã¼rÃ¼nlerini ulusal pazara taÅŸÄ±dÄ±ÄŸÄ±, geliÅŸmiÅŸ Ã¶deme entegrasyonlu e-ticaret platformu.",
                    GorselUrl = "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=800&q=80",
                    Etiketler = "E-Ticaret, API Entegrasyonu, GÃ¼venlik"
                },
                new WebApplication1.Models.Siniflar.ReferansProje
                {
                    Id = 4,
                    Baslik = "Kriter Akademi - Uzaktan EÄŸitim ve SÄ±nav Otomasyonu",
                    Kategori = "Kurumsal YazÄ±lÄ±m",
                    Aciklama = "Ã–ÄŸrencilerin deneme sÄ±navÄ± analizlerini yapabildiÄŸi, video iÃ§erik ve canlÄ± ders yÃ¶netim sistemi.",
                    GorselUrl = "https://images.unsplash.com/photo-1516321318423-f06f85e504b3?auto=format&fit=crop&w=800&q=80",
                    Etiketler = "YazÄ±lÄ±m GeliÅŸtirme, Bulut Hosting"
                }
            };

            return View(projeler);
        }

        [HttpGet]
        public IActionResult Blog()
        {
            var yazilar = new List<BlogPost>
            {
                new BlogPost
                {
                    Id = 1,
                    Baslik = "YazÄ±lÄ±m DÃ¼nyasÄ±ndaki Yenilikler ve Kurumsal Ã‡Ã¶zÃ¼mler",
                    Ozet = "Profesyonel ekip ve gÃ¼Ã§lÃ¼ altyapÄ±yla yÃ¼rÃ¼tÃ¼len Ã§alÄ±ÅŸmalar, yazÄ±lÄ±m projelerinde etkin sonuÃ§lar ortaya koyuyor. Firma aynÄ± zamanda sÃ¼rdÃ¼rÃ¼lebilir teknoloji sunuyor.",
                    Tarih = "27 EylÃ¼l 2026",
                    Kategori = "Duyuru",
                    GorselUrl = "https://images.unsplash.com/photo-1519389950473-47ba0277781c?auto=format&fit=crop&w=800&q=80"
                },
                new BlogPost
                {
                    Id = 2,
                    Baslik = "Web TasarÄ±mÄ±nda Minimalizm ve Performans",
                    Ozet = "Test blog yazÄ±sÄ± kÄ±sa aÃ§Ä±klamalarÄ±nÄ±n yer alacaÄŸÄ± alanÄ±mÄ±z burasÄ±dÄ±r. YazÄ± iÃ§inden yada harici olarak kÄ±sa bir aÃ§Ä±klama girilecek.",
                    Tarih = "27 EylÃ¼l 2026",
                    Kategori = "Teknoloji",
                    GorselUrl = "https://images.unsplash.com/photo-1460925895917-afdab827c52f?auto=format&fit=crop&w=800&q=80"
                }
            };

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
                FormTipi = "Ä°letiÅŸim",
                GonderimTarihi = DateTime.Now
            };
            _context.SiteMesajlari.Add(siteMesaj);
            await _context.SaveChangesAsync();
            TempData["FormMesaj"] = "MesajÄ±nÄ±z baÅŸarÄ±yla gÃ¶nderildi. En kÄ±sa sÃ¼rede size dÃ¶nÃ¼ÅŸ yapacaÄŸÄ±z.";
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
            TempData["FormMesaj"] = "Teklif talebiniz alÄ±ndÄ±. Ekibimiz en kÄ±sa sÃ¼rede sizinle iletiÅŸime geÃ§ecektir.";
            return RedirectToAction(nameof(TeklifAl));
        }
    }
}
