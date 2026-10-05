using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebApplication1.Models;
using WebApplication1.Models.Siniflar;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
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

            return View(model);
        }

        [HttpGet]
        public IActionResult InsanKaynaklari()
        {
            var model = new WebApplication1.Models.Siniflar.InsanKaynaklari
            {
                UstBaslik = "KARİYER & İNSAN KAYNAKLARI",
                AnaBaslik = "Ekibimizin Bir Parçası Olun",
                Aciklama = "Büyüyen ve gelişen Hayat İşleri ailesinde siz de yerinizi almak istiyorsanız, aşağıdaki formu doldurarak başvuru yapabilirsiniz.",
                FormBaslik = "GENEL BAŞVURU VEYA POZİSYON BİLDİRİMİ",
                FormAltBaslik = "Yeteneklerinizi, deneyimlerinizi ve kariyer hedeflerinizi bizimle paylaşın."
            };

            return View(model);
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
        public IActionResult Referanslarimiz()
        {
            var projeler = new List<WebApplication1.Models.Siniflar.ReferansProje>
            {
                new WebApplication1.Models.Siniflar.ReferansProje
                {
                    Id = 1,
                    Baslik = "Elazığ Global Lojistik - E-Ticaret ve Takip Sistemi",
                    Kategori = "Özel Yazılım & E-Ticaret",
                    Aciklama = "Bölgesel lojistik süreçlerini dijitalleştiren, uçtan uca kargo takip ve sipariş yönetim altyapısı.",
                    GorselUrl = "https://images.unsplash.com/photo-1586528116311-ad8dd3c8310d?auto=format&fit=crop&w=800&q=80",
                    Etiketler = "ASP.NET Core, SQL Server, Bootstrap"
                },
                new WebApplication1.Models.Siniflar.ReferansProje
                {
                    Id = 2,
                    Baslik = "Fırat Mimarlık - Kurumsal Portfolyo Sitesi",
                    Kategori = "Web Tasarım",
                    Aciklama = "Minimalist ve Apple tarzı tasarım çizgileriyle, mimari projelerin sergilendiği yüksek performanslı web arayüzü.",
                    GorselUrl = "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=800&q=80",
                    Etiketler = "UI/UX, HTML5, CSS3, JavaScript"
                },
                new WebApplication1.Models.Siniflar.ReferansProje
                {
                    Id = 3,
                    Baslik = "Anadolu Tarım Market - Çok Satıcılı E-Ticaret",
                    Kategori = "E-Ticaret Sistemleri",
                    Aciklama = "Yerel üreticilerin ürünlerini ulusal pazara taşıdığı, gelişmiş ödeme entegrasyonlu e-ticaret platformu.",
                    GorselUrl = "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=800&q=80",
                    Etiketler = "E-Ticaret, API Entegrasyonu, Güvenlik"
                },
                new WebApplication1.Models.Siniflar.ReferansProje
                {
                    Id = 4,
                    Baslik = "Kriter Akademi - Uzaktan Eğitim ve Sınav Otomasyonu",
                    Kategori = "Kurumsal Yazılım",
                    Aciklama = "Öğrencilerin deneme sınavı analizlerini yapabildiği, video içerik ve canlı ders yönetim sistemi.",
                    GorselUrl = "https://images.unsplash.com/photo-1516321318423-f06f85e504b3?auto=format&fit=crop&w=800&q=80",
                    Etiketler = "Yazılım Geliştirme, Bulut Hosting"
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
                    Baslik = "Yazılım Dünyasındaki Yenilikler ve Kurumsal Çözümler",
                    Ozet = "Profesyonel ekip ve güçlü altyapıyla yürütülen çalışmalar, yazılım projelerinde etkin sonuçlar ortaya koyuyor. Firma aynı zamanda sürdürülebilir teknoloji sunuyor.",
                    Tarih = "27 Eylül 2026",
                    Kategori = "Duyuru",
                    GorselUrl = "https://images.unsplash.com/photo-1519389950473-47ba0277781c?auto=format&fit=crop&w=800&q=80"
                },
                new BlogPost
                {
                    Id = 2,
                    Baslik = "Web Tasarımında Minimalizm ve Performans",
                    Ozet = "Test blog yazısı kısa açıklamalarının yer alacağı alanımız burasıdır. Yazı içinden yada harici olarak kısa bir açıklama girilecek.",
                    Tarih = "27 Eylül 2026",
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

        [HttpGet]
        public IActionResult TeklifAl()
        {
            return View();
        }
    }
}