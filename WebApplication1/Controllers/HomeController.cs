using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.Siniflar;
using WebApplication1.Models.Siniflar.Kurumsal;

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
        public async Task<IActionResult> Hakkimizda()
        {
            var model = await _context.Hakkimizda.FirstOrDefaultAsync();
            if (model == null)
            {
                model = new Hakkimizda
                {
                    Baslik = "Hakkımızda",
                    Aciklama = "Elazığ merkezli olarak kurulan Hayat İşleri, web tasarımı, özel yazılım, e-ticaret, hosting ve SEO alanlarında profesyonel kurumsal çözümler sunar.",
                    GorselUrl = "https://images.unsplash.com/photo-1555066931-4365d14bab8c?auto=format&fit=crop&w=800&q=80",
                    HizmetAlanlari = "Web tasarım, özel yazılım geliştirme, e-ticaret sistemleri ve kurumsal hosting hizmetleri sunuyoruz.",
                    KurumsalGucumuz = "Genç, dinamik ve alanında uzman ekibimizle en güncel teknolojileri projelendiriyoruz.",
                    CalismaIlkelerimiz = "Şeffaflık, zamanında teslim, müşteri memnuniyeti ve sürdürülebilir destek.",
                    Vizyonumuz = "Bölgesel gücümüzü uluslararası standartlarda yazılım çözümleriyle birleştirerek sektörün lideri olmak.",
                    Misyonumuz = "İşletmelerin dijital dönüşüm süreçlerini güvenli, hızlı ve estetik altyapılarla desteklemek.",
                    Onceliklerimiz = "Donanım satışı veya tamiri ile vakit kaybetmeden, tamamen yazılım ve web teknolojilerine odaklanmak."
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
        public async Task<IActionResult> Referanslarimiz()
        {
            var projeler = await _context.ReferansProjeler.ToListAsync();
            if (!projeler.Any())
            {
                projeler = new List<ReferansProje>
                {
                    new ReferansProje
                    {
                        Baslik = "Elazığ Global Lojistik - E-Ticaret ve Takip Sistemi",
                        Kategori = "Özel Yazılım & E-Ticaret",
                        Aciklama = "Bölgesel lojistik süreçlerini dijitalleştiren, uçtan uca kargo takip ve sipariş yönetim altyapısı.",
                        GorselUrl = "https://images.unsplash.com/photo-1586528116311-ad8dd3c8310d?auto=format&fit=crop&w=800&q=80",
                        Etiketler = "ASP.NET Core, SQL Server, Bootstrap",
                        ProjeLinki = "#"
                    },
                    new ReferansProje
                    {
                        Baslik = "Fırat Mimarlık - Kurumsal Portfolyo Sitesi",
                        Kategori = "Web Tasarım",
                        Aciklama = "Minimalist ve Apple tarzı tasarım çizgileriyle, mimari projelerin sergilendiği yüksek performanslı web arayüzü.",
                        GorselUrl = "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=800&q=80",
                        Etiketler = "UI/UX, HTML5, CSS3, JavaScript",
                        ProjeLinki = "#"
                    },
                    new ReferansProje
                    {
                        Baslik = "Anadolu Tarım Market - Çok Satıcılı E-Ticaret",
                        Kategori = "E-Ticaret Sistemleri",
                        Aciklama = "Yerel üreticilerin ürünlerini ulusal pazara taşıdığı, gelişmiş ödeme entegrasyonlu e-ticaret platformu.",
                        GorselUrl = "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=800&q=80",
                        Etiketler = "E-Ticaret, API Entegrasyonu, Güvenlik",
                        ProjeLinki = "#"
                    },
                    new ReferansProje
                    {
                        Baslik = "Kriter Akademi - Uzaktan Eğitim ve Sınav Otomasyonu",
                        Kategori = "Kurumsal Yazılım",
                        Aciklama = "Öğrencilerin deneme sınavı analizlerini yapabildiği, video içerik ve canlı ders yönetim sistemi.",
                        GorselUrl = "https://images.unsplash.com/photo-1516321318423-f06f85e504b3?auto=format&fit=crop&w=800&q=80",
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
                        GorselUrl = "https://images.unsplash.com/photo-1519389950473-47ba0277781c?auto=format&fit=crop&w=800&q=80"
                    },
                    new BlogPost
                    {
                        Baslik = "Web Tasarımında Minimalizm ve Performans",
                        Ozet = "Test blog yazısı kısa açıklamalarının yer alacağı alanımız burasıdır. Yazı içinden yada harici olarak kısa bir açıklama girilecek.",
                        Tarih = "27 Eylül 2026",
                        Kategori = "Teknoloji",
                        GorselUrl = "https://images.unsplash.com/photo-1460925895917-afdab827c52f?auto=format&fit=crop&w=800&q=80"
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
            TempData["FormMesaj"] = "Teklif talebiniz alındı. Ekibimiz en kısa sürede sizinle iletişime geçecektir.";
            return RedirectToAction(nameof(TeklifAl));
        }
    }
}
