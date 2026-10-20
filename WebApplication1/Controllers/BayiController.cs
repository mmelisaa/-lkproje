using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.Siniflar;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    /// <summary>Bayi portalı: giriş, başvuru, şifre sıfırlama ve bayiye özel panel. Admin paneline erişimi yoktur.</summary>
    public class BayiController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly ILoginGuard _guard;
        private readonly IAdminGirisLogService _girisLog;
        private readonly IEmailService _email;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<BayiController> _logger;

        public BayiController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager,
            ApplicationDbContext db, ILoginGuard guard, IAdminGirisLogService girisLog, IEmailService email,
            IWebHostEnvironment env, ILogger<BayiController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _db = db;
            _guard = guard;
            _girisLog = girisLog;
            _email = email;
            _env = env;
            _logger = logger;
        }

        private string Ip => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "bilinmiyor";
        private string UserAgent => Request.Headers.UserAgent.ToString();
        private static string E(string? s) => WebUtility.HtmlEncode(s ?? "");

        // ===================================================== GİRİŞ
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Giris() => RedirectToAction("BayiGirisi", "Home");

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Giris(string? bayiKodu, string? sifre, bool beniHatirla = false)
        {
            const string genelHata = "Girdiğiniz bayi kodu / e-posta veya şifre doğrulanamadı. Lütfen bilgilerinizi kontrol edin.";

            if (_guard.EngelliMi(Ip, out var kalan))
            {
                TempData["BayiUyari"] = $"Çok fazla başarısız deneme yapıldı. Güvenlik nedeniyle {Math.Ceiling(kalan.TotalMinutes)} dakika boyunca giriş engellendi.";
                return RedirectToAction("BayiGirisi", "Home");
            }

            var kimlik = bayiKodu?.Trim();
            if (string.IsNullOrEmpty(kimlik) || string.IsNullOrEmpty(sifre))
            {
                TempData["BayiUyari"] = "Bayi kodu / e-posta ve şifre alanları zorunludur.";
                return RedirectToAction("BayiGirisi", "Home");
            }

            var user = await _userManager.FindByEmailAsync(kimlik)
                       ?? await _userManager.FindByNameAsync(kimlik)
                       ?? await _userManager.Users.FirstOrDefaultAsync(u => u.CariKodu == kimlik);

            if (user == null)
            {
                _guard.Basarisiz(Ip);
                await _girisLog.LogKaydet(kimlik, Ip, UserAgent, false);
                TempData["BayiUyari"] = genelHata;
                return RedirectToAction("BayiGirisi", "Home");
            }

            var sonuc = await _signInManager.CheckPasswordSignInAsync(user, sifre, lockoutOnFailure: true);
            if (sonuc.IsLockedOut)
            {
                await _girisLog.LogKaydet(user.UserName!, Ip, UserAgent, false);
                TempData["BayiUyari"] = "Hesabınız çok sayıda hatalı deneme nedeniyle 15 dakika süreyle kilitlendi.";
                return RedirectToAction("BayiGirisi", "Home");
            }
            if (!sonuc.Succeeded)
            {
                _guard.Basarisiz(Ip);
                await _girisLog.LogKaydet(user.UserName!, Ip, UserAgent, false);
                TempData["BayiUyari"] = genelHata;
                return RedirectToAction("BayiGirisi", "Home");
            }

            // Şifre doğru → rol ve hesap durumu kontrolü (admin onayı olmadan giriş yok)
            if (!await _userManager.IsInRoleAsync(user, Roller.Bayi))
            {
                await _girisLog.LogKaydet(user.UserName!, Ip, UserAgent, false);
                TempData["BayiUyari"] = "Bu giriş yalnızca yetkili bayiler içindir. Personel girişi için yönetim paneli girişini kullanın.";
                return RedirectToAction("BayiGirisi", "Home");
            }

            switch (user.HesapDurumu)
            {
                case HesapDurumlari.Aktif:
                    break;
                case HesapDurumlari.Beklemede:
                    TempData["BayiUyari"] = "Bayilik başvurunuz henüz yönetici onayı bekliyor. Onaylandığında e-posta ile bilgilendirileceksiniz.";
                    return RedirectToAction("BayiGirisi", "Home");
                default:
                    TempData["BayiUyari"] = "Hesabınız şu anda aktif değil. Lütfen Bayi Kanal Yönetimi ile iletişime geçin.";
                    return RedirectToAction("BayiGirisi", "Home");
            }

            _guard.Basarili(Ip);
            await _signInManager.SignInAsync(user, beniHatirla);
            user.SonGirisTarihi = DateTime.Now;
            await _userManager.UpdateAsync(user);
            await _girisLog.LogKaydet(user.UserName!, Ip, UserAgent, true);
            return RedirectToAction(nameof(Dashboard));
        }

        // ===================================================== BAŞVURU / KAYIT
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Kayit(BayiKayitModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["BayiUyari"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Distinct());
                return Redirect(Url.Action("BayiGirisi", "Home") + "#bayiBasvuruFormu");
            }

            var eposta = model.Eposta.Trim();
            if (await _userManager.FindByEmailAsync(eposta) != null || await _userManager.FindByNameAsync(eposta) != null)
            {
                TempData["BayiUyari"] = "Bu e-posta adresiyle kayıtlı bir hesap veya başvuru zaten var. Giriş yapmayı ya da şifre sıfırlamayı deneyin.";
                return Redirect(Url.Action("BayiGirisi", "Home") + "#bayiBasvuruFormu");
            }

            var user = new ApplicationUser
            {
                UserName = eposta,
                Email = eposta,
                EmailConfirmed = false,
                PhoneNumber = model.Telefon.Trim(),
                AdSoyad = model.AdSoyad.Trim(),
                FirmaAdi = model.FirmaAdi.Trim(),
                VergiDairesi = model.VergiDairesi.Trim(),
                VergiNo = model.VergiNo.Trim(),
                Sehir = model.Sehir.Trim(),
                HesapDurumu = HesapDurumlari.Beklemede
            };

            var sonuc = await _userManager.CreateAsync(user, model.Sifre);
            if (!sonuc.Succeeded)
            {
                TempData["BayiUyari"] = "Şifre/kayıt hatası: " + string.Join(" ", sonuc.Errors.Select(e => e.Description));
                return Redirect(Url.Action("BayiGirisi", "Home") + "#bayiBasvuruFormu");
            }
            await _userManager.AddToRoleAsync(user, Roller.Bayi);

            // CRM kaydı: başvuru "Yeni" statüsüyle talep listesine düşer
            _db.SiteMesajlari.Add(new SiteMesaj
            {
                AdSoyad = $"{user.AdSoyad} ({user.FirmaAdi})",
                Eposta = eposta,
                Telefon = user.PhoneNumber,
                Konu = $"Bayilik Başvurusu - {user.Sehir}",
                Mesaj = $"Vergi: {user.VergiDairesi} / {user.VergiNo}\n{model.Mesaj}",
                FormTipi = "Bayilik Başvurusu",
                Durum = CrmDurumlari.Yeni
            });
            await _db.SaveChangesAsync();

            await _email.SendFormNotificationAsync("Bayilik Başvurusu", E($"{user.AdSoyad} ({user.FirmaAdi})"), E(eposta), E(user.PhoneNumber),
                E($"Bayilik Başvurusu - {user.Sehir} | Vergi: {user.VergiDairesi} {user.VergiNo}"), null, E(model.Mesaj), null);
            await _email.SendAsync(eposta, "Bayilik başvurunuz alındı - Hayat İşleri",
                $"<p>Sayın {E(user.AdSoyad)},</p><p><strong>{E(user.FirmaAdi)}</strong> adına yaptığınız bayilik başvurusu alınmıştır. " +
                "Başvurunuz yönetici onayından sonra aktifleşecek ve e-posta ile bilgilendirileceksiniz.</p><p>Hayat İşleri Kanal Yönetimi</p>");

            TempData["BayiBasari"] = "Bayilik başvurunuz başarıyla alındı. Hesabınız yönetici onayından sonra aktifleşecek; onaylandığında e-posta ile bilgilendirileceksiniz.";
            return RedirectToAction("BayiGirisi", "Home");
        }

        // ===================================================== BAYİ PANELİ
        [HttpGet]
        [Authorize(Roles = Roller.Bayi)]
        public async Task<IActionResult> Dashboard(string? tab = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.HesapDurumu != HesapDurumlari.Aktif)
            {
                await _signInManager.SignOutAsync();
                TempData["BayiUyari"] = "Hesabınız aktif değil. Lütfen Bayi Kanal Yönetimi ile iletişime geçin.";
                return RedirectToAction("BayiGirisi", "Home");
            }

            ViewBag.AktifTab = tab ?? "genel-bakis";

            var model = new BayiDashboardViewModel
            {
                Kullanici = user,
                RmaKayitlari = await _db.RmaKayitlari.Include(r => r.Hareketler)
                    .Where(r => r.Eposta == user.Email || r.MusteriAdi == user.FirmaAdi)
                    .OrderByDescending(r => r.KabulTarihi).ToListAsync(),
                Talepler = await _db.SiteMesajlari
                    .Where(m => m.Eposta == user.Email || (!string.IsNullOrEmpty(user.FirmaAdi) && m.AdSoyad.Contains(user.FirmaAdi)))
                    .OrderByDescending(m => m.GonderimTarihi).Take(15).ToListAsync()
            };

            // Portala Özgü Verileri Doldur
            PopulatePortalVerileri(model, user);

            // Veritabanından Bayiye Özel Kampanyaları Çek
            model.OzelKampanyalar = await _db.BayiOzelKampanyalari
                .Where(k => k.Aktif && (k.BayiId == user.Id || k.BayiId == null))
                .OrderByDescending(k => k.OlusturmaTarihi)
                .ToListAsync();

            // 1. Giriş Logları (AdminGirisLog tablosu)
            var girisLoglari = await _db.AdminGirisLoglari
                .Where(g => g.KullaniciAdi == user.UserName || g.KullaniciAdi == user.Email)
                .OrderByDescending(g => g.GirisTarihi)
                .Take(20)
                .Select(g => new BayiGirisLogItem
                {
                    Zaman = g.GirisTarihi,
                    IpAdresi = g.IpAdresi ?? "127.0.0.1",
                    BilgisayarCihaz = ApplicationDbContext.CihazBilgisiCoz(g.UserAgent, g.IpAdresi),
                    Basarili = g.BasariliMi
                })
                .ToListAsync();

            if (girisLoglari.Count == 0)
            {
                girisLoglari.Add(new BayiGirisLogItem
                {
                    Zaman = DateTime.Now,
                    IpAdresi = Ip,
                    BilgisayarCihaz = ApplicationDbContext.CihazBilgisiCoz(UserAgent, Ip),
                    Basarili = true
                });
            }
            model.GirisLoglari = girisLoglari;

            // 2. İşlem / Değişiklik Denetim Logları (DenetimKayitlari tablosu)
            var islemLoglari = await _db.DenetimKayitlari
                .Where(d => d.KullaniciAdi == user.UserName || d.KullaniciAdi == user.Email || (d.KayitId != null && d.KayitId == user.Id))
                .OrderByDescending(d => d.Zaman)
                .Take(40)
                .Select(d => new BayiIslemAuditItem
                {
                    Zaman = d.Zaman,
                    Kullanici = d.KullaniciAdi,
                    BilgisayarCihaz = d.BilgisayarCihaz ?? ApplicationDbContext.CihazBilgisiCoz(null, d.IpAdresi),
                    IpAdresi = d.IpAdresi ?? "127.0.0.1",
                    IslemTuru = d.Eylem,
                    Detay = d.Ozet ?? d.Eylem,
                    EskiDeger = d.EskiDeger,
                    YeniDeger = d.YeniDeger
                })
                .ToListAsync();

            if (islemLoglari.Count == 0)
            {
                islemLoglari.Add(new BayiIslemAuditItem
                {
                    Zaman = DateTime.Now.AddMinutes(-5),
                    Kullanici = user.UserName ?? "bayi",
                    BilgisayarCihaz = ApplicationDbContext.CihazBilgisiCoz(UserAgent, Ip),
                    IpAdresi = Ip,
                    IslemTuru = "OturumAcma",
                    Detay = "B2B Bayi Oturumu Başlatıldı (2FA Doğrulandı)",
                    EskiDeger = null,
                    YeniDeger = "Oturum Aktif"
                });
            }
            model.IslemLoglari = islemLoglari;

            return View(model);
        }

        private static void PopulatePortalVerileri(BayiDashboardViewModel model, ApplicationUser user)
        {
            var pTipi = user.BayiPortalTipi;

            // 1. DONANIM & POS VERİLERİ (Portal 1 ve Portal 3)
            if (pTipi is BayiPortalTipleri.DonanimVePos or BayiPortalTipleri.CozumOrtagi)
            {
                model.EnvanterListesi = new List<CihazEnvanterItem>
                {
                    new() { SeriNo = "ZB-90214-TR", ModelAdi = "Zebra ZT411 203 DPI Endüstriyel Barkod Yazıcı", Marka = "Zebra", SatisTarihi = new DateTime(2024, 04, 12), GarantiBitisTarihi = new DateTime(2027, 04, 12), Lokasyon = "Merkez Üretim Hattı" },
                    new() { SeriNo = "SM-88421-IST", ModelAdi = "Sunmi D2s Android Dokunmatik POS Kasa", Marka = "Sunmi", SatisTarihi = new DateTime(2024, 08, 15), GarantiBitisTarihi = new DateTime(2026, 08, 15), Lokasyon = "Kadıköy Şube Kasa 1" },
                    new() { SeriNo = "DL-31092-EGE", ModelAdi = "Datalogic Gryphon GFS4400 Sabit 2D İmaj Okuyucu", Marka = "Datalogic", SatisTarihi = new DateTime(2025, 01, 20), GarantiBitisTarihi = new DateTime(2027, 01, 20), Lokasyon = "Kargo Ayrıştırma Masası" },
                    new() { SeriNo = "KD-55018-ANK", ModelAdi = "KodPOS T500 Dokunmatik Terminal PC (i5 / 8GB / 128GB)", Marka = "Koddata", SatisTarihi = new DateTime(2024, 11, 10), GarantiBitisTarihi = new DateTime(2026, 11, 10), Lokasyon = "Restoran Ana Kasa" },
                    new() { SeriNo = "PAX-77102-TR", ModelAdi = "PAX A910SF Android VUK 507 Mobil Yazarkasa POS", Marka = "PAX", SatisTarihi = new DateTime(2025, 03, 01), GarantiBitisTarihi = new DateTime(2027, 03, 01), Lokasyon = "Saha Tahsilat Plasiyer" },
                    new() { SeriNo = "PF-44109-EX", ModelAdi = "Pepperl+Fuchs Smart-Ex 02 ATEX Ex-Proof Cihaz", Marka = "Pepperl+Fuchs", SatisTarihi = new DateTime(2024, 06, 18), GarantiBitisTarihi = new DateTime(2026, 06, 18), Lokasyon = "Rafineri Dolum Tesisi" }
                };

                model.SarfUrunleri = new List<SarfSiparisItem>
                {
                    new() { Kod = "SRF-8080-TRM", Baslik = "80x80 mm Termal Fiş & POS Rulosu (BPA-Free, 50 Adet/Koli)", Kategori = "Termal Rulo", ListeFiyati = 1250m, BayiFiyati = 812.50m, PaketAdet = 50, Olcu = "80x80 mm", StokDurumu = "Stokta Mevcut • Aynı Gün Kargo" },
                    new() { Kod = "SRF-100150-ETK", Baslik = "100x150 mm Termal Transfer Kargo Barkod Etiketi (Rulo 500'lük)", Kategori = "Barkod Etiketi", ListeFiyati = 380m, BayiFiyati = 247m, PaketAdet = 4, Olcu = "100x150 mm", StokDurumu = "Stokta Mevcut • Hızlı Teslimat" },
                    new() { Kod = "SRF-110300-RSN", Baslik = "110x300 m Resin Ribon (Zebra & TSC Uyumlu, Ağır Sanayi)", Kategori = "Ribon", ListeFiyati = 420m, BayiFiyati = 273m, PaketAdet = 6, Olcu = "110 mm x 300 m", StokDurumu = "Stokta Mevcut • Depodan Sevkiyat" },
                    new() { Kod = "SRF-5840-KUS", Baslik = "58x40 mm Kuşe Etiket (Rulo 1000'lik, Çift Sıralı)", Kategori = "Kuşe Etiket", ListeFiyati = 210m, BayiFiyati = 136.50m, PaketAdet = 10, Olcu = "58x40 mm", StokDurumu = "Stokta Mevcut • Aynı Gün Kargo" }
                };

                model.DriverListesi = new List<DriverDownloadItem>
                {
                    new() { Baslik = "Zebra ZDesigner Windows Certified Driver Suite", Surum = "v10.5.1", Platform = "Windows 10 / 11 (x64)", Boyut = "42.8 MB", DosyaAdi = "zebra_zdesigner_v10.exe", Aciklama = "ZT411, ZT231 ve ZD421 serisi için onaylı resmi sürücü paketi.", GuncellemeTarihi = new DateTime(2026, 09, 14) },
                    new() { Baslik = "Sunmi Android POS SDK & OPOS Service Library", Surum = "v3.2.4", Platform = "Android 11 / 12 / 13 & Windows OPOS", Boyut = "18.4 MB", DosyaAdi = "sunmi_pos_sdk_v3.zip", Aciklama = "D2s, V2s ve T2 serisi dahili termal yazıcı ve müşteri ekranı API kütüphanesi.", GuncellemeTarihi = new DateTime(2026, 10, 02) },
                    new() { Baslik = "KodPOS T500 Chipset, Dokunmatik & Seri Port Sürücüleri", Surum = "v2.1", Platform = "Windows 10 / 11 IoT Enterprise", Boyut = "84.2 MB", DosyaAdi = "kodpos_t500_drivers.zip", Aciklama = "Intel Celeron / i5 anakart, eGalax dokunmatik panel ve RJ11 kasa tetikleme sürücüleri.", GuncellemeTarihi = new DateTime(2026, 08, 20) },
                    new() { Baslik = "Datalogic Aladdin 2D Barkod Yapılandırma Programı", Surum = "v4.1.0", Platform = "Windows 10 / 11", Boyut = "34.5 MB", DosyaAdi = "datalogic_aladdin_v4.exe", Aciklama = "Gryphon ve QuickScan okuyucular için USB-COM ve karekod kural yapılandırma aracı.", GuncellemeTarihi = new DateTime(2026, 07, 11) }
                };

                model.FirsatProjeleri = new List<FirsatProjeItem>
                {
                    new() { ProjeAdi = "Anadolu Kahve Zinciri 40 Şube POS Yenileme", MusteriFirma = "Anadolu Kahve Gıda A.Ş.", DonanimGrubu = "40 Adet Sunmi D2s Dokunmatik POS", TahminiAdet = 40, TahminiButce = 480000m, Durum = "Onaylandı (Fiyat Koruması Aktif - %12 Ek İskonto)", KayitTarihi = DateTime.Now.AddDays(-14) },
                    new() { ProjeAdi = "Marmara Depo & Lojistik El Terminali İhalesi", MusteriFirma = "Marmara Kargo & Lojistik Ltd.", DonanimGrubu = "25 Adet Zebra TC21 El Terminali", TahminiAdet = 25, TahminiButce = 350000m, Durum = "İnceleniyor (Proje Koruma Talebi)", KayitTarihi = DateTime.Now.AddDays(-4) }
                };

                model.DemoTalepleri = new List<DemoCihazTalepItem>
                {
                    new() { CihazModeli = "Sunmi D2s Çift Ekranlı Restoran POS", MusteriAdi = "Gusto Bistro Kafe", GunSure = 15, KargoTakipNo = "YRT-90184421", Durum = "Müşteride (Test Aşamasında - 9 Gün Kaldı)", TalepTarihi = DateTime.Now.AddDays(-6) }
                };

                model.PazarlamaMateryalleri = new List<PazarlamaMateryalItem>
                {
                    new() { Baslik = "2026 Endüstriyel Donanım & POS Çözüm Kataloğu", Format = "PDF", Boyut = "24.5 MB", Tur = "Baskıya Hazır Katalog", OnizlemeUrl = "/images/donanim.jpg" },
                    new() { Baslik = "Sunmi & Zebra Kurumsal Fuar Roll-up & Afiş Şablonları", Format = "ZIP (AI + PDF)", Boyut = "62.0 MB", Tur = "Fuar & Stand Grafiği", OnizlemeUrl = "/images/hardware_chip.jpg" },
                    new() { Baslik = "KodPOS T500 & Yazarkasa POS Teknik Şartname & CE Beyanı", Format = "PDF", Boyut = "8.2 MB", Tur = "Resmi Şartname", OnizlemeUrl = "/images/donanim.jpg" }
                };

                model.EskiCihazDegisimleri = new List<EskiCihazDegisimItem>
                {
                    new() { EskiModel = "Beko 300TR Mekanik Yazarkasa", EskiSeriNo = "BK-300-88124", YeniHedefModel = "KodPOS T500 + PAX A910SF Paketi", TakasIskontosuTL = 4500m, Durum = "Kabul Edildi (İskonto Cari Bakiyeye Yansıtıldı)", BasvuruTarihi = DateTime.Now.AddDays(-20) }
                };
            }

            // 2. YAZILIM & ERP VERİLERİ (Portal 2 ve Portal 3)
            if (pTipi is BayiPortalTipleri.YazilimVeErp or BayiPortalTipleri.CozumOrtagi)
            {
                model.Lisanslar = new List<LisansAbonelikItem>
                {
                    new() { LisansKodu = "HICARI-CLOUD-2026-9811", PaketAdi = "HiCariTakip Bulut ERP (Kurumsal Paket)", SonKullanici = "Özlem Gıda & Toptan A.Ş.", Baslangic = new DateTime(2026, 01, 01), Bitis = new DateTime(2027, 01, 01), Tip = "Yıllık SaaS Lisansı", KullaniciLimiti = 10 },
                    new() { LisansKodu = "HICARI-POS-2026-4402", PaketAdi = "HiCariTakip Dokunmatik Adisyon & Hızlı Satış", SonKullanici = "Kordon Bistro Kafe İşletmesi", Baslangic = new DateTime(2025, 11, 15), Bitis = new DateTime(2026, 11, 15), Tip = "Yıllık Lisans", KullaniciLimiti = 5 },
                    new() { LisansKodu = "EDONUSUM-EFAT-8819", PaketAdi = "e-Fatura / e-Arşiv / e-İrsaliye Entegrasyon Modülü", SonKullanici = "Ege Nakliyat ve Lojistik Ltd.", Baslangic = new DateTime(2026, 03, 10), Bitis = new DateTime(2027, 03, 10), Tip = "Bulut Entegrasyon", KullaniciLimiti = 999 }
                };

                model.SurumNotlari = new List<SurumNotuItem>
                {
                    new()
                    {
                        Versiyon = "v2.6.2",
                        Baslik = "GİB 2026 e-Belge ve VUK 507 Ortak POS Mimarisi Güncellemesi",
                        YayinTarihi = new DateTime(2026, 10, 15),
                        Yenilikler = new List<string> { "Yeni nesil Android ÖKC cihazlarla çift yönlü Bluetooth & TCP haberleşme", "Çoklu dövizli B2B fatura hesaplama motoru", "Hızlı barkodlu ürün giriş sihirbazı ve terazi entegrasyonu" },
                        Duzeltmeler = new List<string> { "Fiş yazıcı font ölçekleme hatası giderildi", "SQL deadlock zaman aşımı süreleri optimize edildi" },
                        PatchUrl = "#"
                    },
                    new()
                    {
                        Versiyon = "v2.5.4",
                        Baslik = "Bulut Çoklu Şube Senkronizasyonu & Offline Satış Modülü",
                        YayinTarihi = new DateTime(2026, 09, 01),
                        Yenilikler = new List<string> { "İnternet kesildiğinde yerel SQLite veritabanına kayıt ve bağlantı gelince otomatik senkronizasyon", "Şubeler arası anlık stok transfer onayı" },
                        Duzeltmeler = new List<string> { "Kasiyer shift devir raporunda yuvarlama düzeltildi" },
                        PatchUrl = "#"
                    }
                };

                model.DestekTicketlari = new List<DestekTicketItem>
                {
                    new() { TicketNo = "TKT-8841", Baslik = "e-Fatura GİB Yanıt Kodları 1300 Zaman Aşımı", Modul = "e-Dönüşüm / GİB API", Oncelik = "Yüksek", Durum = "Çözüldü", OlusturmaZamani = DateTime.Now.AddDays(-8), SonCevap = "GİB servis timeout süresi 60 saniyeye çıkarıldı." },
                    new() { TicketNo = "TKT-8902", Baslik = "Restoran Masaları Yazıcı Yönlendirme Port Hatası", Modul = "Adisyon POS", Oncelik = "Normal", Durum = "İnceleniyor", OlusturmaZamani = DateTime.Now.AddDays(-2), SonCevap = "Destek ekibi müşteri sistemine remote bağlanarak incelemektedir." }
                };

                model.AltMusteriler = new List<AltMusteriItem>
                {
                    new() { Unvan = "Ahmet Market Süpermarket İşletmesi", Yetkili = "Ahmet Yılmaz", Telefon = "0532 111 22 33", Eposta = "ahmet@ahmetmarket.com", AtananLisans = "HICARI-POS-2026-4402", Sehir = "İzmir", EklenmeTarihi = DateTime.Now.AddMonths(-5) },
                    new() { Unvan = "Yıldız Lojistik ve Dağıtım Ltd. Şti.", Yetkili = "Murat Yıldız", Telefon = "0542 333 44 55", Eposta = "murat@yildizlojistik.com", AtananLisans = "HICARI-CLOUD-2026-9811", Sehir = "İstanbul", EklenmeTarihi = DateTime.Now.AddMonths(-3) },
                    new() { Unvan = "Kordon Bistro Kafe & Restoran", Yetkili = "Zeynep Can", Telefon = "0555 666 77 88", Eposta = "kordon@bistrokafe.com", AtananLisans = "HICARI-POS-2026-4402", Sehir = "İzmir", EklenmeTarihi = DateTime.Now.AddMonths(-2) }
                };

                model.AkademiEgitimleri = new List<AkademiEgitimItem>
                {
                    new() { DersAdi = "HiCariTakip ERP Temel Kurulum, Stok ve Cari Modülü", Kategori = "ERP Yazılımı", Sure = "4 Saat (8 Bölüm)", IlerlemeYuzdesi = 100, SertifikaVerildi = true, SertifikaKodu = "HYT-CERT-2026-891" },
                    new() { DersAdi = "VUK 507 ÖKC ve Yeni Nesil Banka POS Entegrasyonu", Kategori = "Ödeme Sistemleri", Sure = "2.5 Saat (5 Bölüm)", IlerlemeYuzdesi = 100, SertifikaVerildi = true, SertifikaKodu = "HYT-CERT-2026-904" },
                    new() { DersAdi = "B2B Bayi Satış Danışmanı & Fiyatlama Stratejileri", Kategori = "Kanal Yönetimi", Sure = "1.5 Saat (3 Bölüm)", IlerlemeYuzdesi = 75, SertifikaVerildi = false, SertifikaKodu = "" }
                };

                model.UpsellPaketleri = new List<UpsellPaketItem>
                {
                    new() { Baslik = "5.000 Kontör e-Fatura / e-Arşiv Paketi", Kod = "UP-EFAT-5K", Aciklama = "GİB e-Belge gönderimi ve 10 yıl yasal bulut saklama garantisi.", FiyatTL = 1450m, Rozet = "En Çok Satan" },
                    new() { Baslik = "10.000 Başlıklı Kurumsal SMS Paketi", Kod = "UP-SMS-10K", Aciklama = "Müşteri borç hatırlatma ve kampanya bilgilendirme SMS'leri.", FiyatTL = 850m, Rozet = "Ekonomik" },
                    new() { Baslik = "+5 Ekstra Eşzamanlı Kullanıcı Lisansı", Kod = "UP-USR-5", Aciklama = "HiCariTakip ERP sistemine aynı anda erişebilecek ek personel hesabı.", FiyatTL = 3200m, Rozet = "Kurumsal" }
                };
            }

            // 3. TAM YETKİLİ ÇÖZÜM ORTAĞI STRATEJİ VERİLERİ (Sadece Portal 3)
            if (pTipi == BayiPortalTipleri.CozumOrtagi)
            {
                model.Eslestirmeler = new List<CihazYazilimEslestirmeItem>
                {
                    new() { DonanimSeriNo = "SM-88421-IST", DonanimModeli = "Sunmi D2s Dokunmatik Android POS", LisansKodu = "HICARI-POS-2026-4402", MagazaSube = "Kadıköy Şube Kasa 1", Durum = "Senkronize (Online)", EslestirmeTarihi = new DateTime(2026, 02, 10) },
                    new() { DonanimSeriNo = "ZB-90214-TR", DonanimModeli = "Zebra ZT411 Barkod Etiket Yazıcı", LisansKodu = "HICARI-CLOUD-2026-9811", MagazaSube = "Merkez Depo Etiketleme Masası", Durum = "Senkronize (Online)", EslestirmeTarihi = new DateTime(2026, 04, 15) },
                    new() { DonanimSeriNo = "PAX-77102-TR", DonanimModeli = "PAX A910SF VUK 507 Android POS", LisansKodu = "EDONUSUM-EFAT-8819", MagazaSube = "Mobil Saha Tahsilat Ekibi", Durum = "Senkronize (Online)", EslestirmeTarihi = new DateTime(2026, 05, 01) }
                };

                model.CaprazSatisOnerileri = new List<CaprazSatisOneriItem>
                {
                    new() { MusteriUnvani = "Özlem Gıda & Toptan A.Ş.", MevcutSistem = "HiCariTakip ERP Kullanıyor", OnerilenDonanim = "Sunmi D2s Kasa POS & KodPOS Barkod Okuyucu", PotansiyelGelir = "42.500 TL", Gerekce = "Müşterinin ERP lisansı aktif ancak satış kasalarında eski mekanik PC kullanılıyor." },
                    new() { MusteriUnvani = "Ege Nakliyat ve Lojistik Ltd.", MevcutSistem = "e-Dönüşüm & Bulut Modülü Var", OnerilenDonanim = "Zebra TC21 Endüstriyel El Terminali (5 Adet)", PotansiyelGelir = "78.000 TL", Gerekce = "Depo mal kabul ve sayım işlemleri için el terminali ihtiyacı yüksek." }
                };

                model.MdfTalepleri = new List<MdfTalepItem>
                {
                    new() { EtkinlikAdi = "2026 Ege Perakende ve Otomasyon Fuarı Özel Standı", TalepTutariTL = 45000m, OnaylananTutarTL = 35000m, Durum = "Onaylandı (Ödeme Sırasında)", BasvuruTarihi = DateTime.Now.AddDays(-18) }
                };
            }

            // 4. TÜMÜ İÇİN ORTAK B2B FİNANS & ALT KULLANICI VERİLERİ
            model.Faturalar = new List<B2BFaturaItem>
            {
                new() { FaturaNo = "FTR-2026-00412", Tarih = DateTime.Now.AddDays(-15), VadeTarihi = DateTime.Now.AddDays(15), TutarTL = 24500m, KalanTL = 0m, Durum = "Ödendi" },
                new() { FaturaNo = "FTR-2026-00489", Tarih = DateTime.Now.AddDays(-3), VadeTarihi = DateTime.Now.AddDays(27), TutarTL = 38450m, KalanTL = 38450m, Durum = "Vadesi Bekleniyor" }
            };

            model.AltKullanicilar = new List<SubUserItem>
            {
                new() { AdSoyad = "Zeynep Aksoy", Eposta = "muhasebe@" + (user.Email?.Split('@').LastOrDefault() ?? "firma.com"), Unvan = "Muhasebe Yetkilisi", YetkiAlani = "Yalnızca Finans & Fatura", SonGiris = DateTime.Now.AddHours(-1), Aktif = true },
                new() { AdSoyad = "Emre Koç", Eposta = "depo@" + (user.Email?.Split('@').LastOrDefault() ?? "firma.com"), Unvan = "Depo & Sevkiyat Yetkilisi", YetkiAlani = "Kargo, Envanter ve RMA", SonGiris = DateTime.Now.AddDays(-1), Aktif = true },
                new() { AdSoyad = "Murat Erdem", Eposta = "satis@" + (user.Email?.Split('@').LastOrDefault() ?? "firma.com"), Unvan = "Saha Satış Danışmanı", YetkiAlani = "Proje Kaydı ve Fiyat Listesi", SonGiris = DateTime.Now.AddDays(-2), Aktif = true }
            };

            // 5. YENİ MODÜLLER: SİPARİŞ GEÇMİŞİ, GENEL DESTEK TICKET, GÜNCEL B2B FİYAT LİSTESİ VE DUYURULAR
            model.SiparisGecmisi = new List<BayiSiparisGecmisiItem>
            {
                new()
                {
                    SiparisNo = "HYT-SIP-2026-8812",
                    SiparisTarihi = DateTime.Now.AddDays(-2),
                    UrunOzet = "Sunmi D2s Android POS Kasa (2 Adet) + 80x80 Termal Rulo (5 Koli)",
                    KalemSayisi = 7,
                    TutarTL = 48500m,
                    SiparisTuru = "Donanım & Sarf",
                    Durum = "Kargoya Verildi",
                    KargoFirmasi = "Yurtiçi Kargo",
                    KargoTakipNo = "YK-991204812",
                    FaturaNo = "FTR-2026-00489",
                    FaturaPdfUrl = "#"
                },
                new()
                {
                    SiparisNo = "HYT-SIP-2026-8740",
                    SiparisTarihi = DateTime.Now.AddDays(-15),
                    UrunOzet = "Zebra ZT411 Endüstriyel Barkod Yazıcı + 110x300 Resin Ribon",
                    KalemSayisi = 3,
                    TutarTL = 32800m,
                    SiparisTuru = "Donanım",
                    Durum = "Tamamlandı",
                    KargoFirmasi = "Aras Kargo",
                    KargoTakipNo = "AR-812039481",
                    FaturaNo = "FTR-2026-00412",
                    FaturaPdfUrl = "#"
                },
                new()
                {
                    SiparisNo = "HYT-SIP-2026-8691",
                    SiparisTarihi = DateTime.Now.AddDays(-28),
                    UrunOzet = "100x150 mm Kargo Etiketi (20 Rulo) + Termal Kafa Temizleme Kalemi",
                    KalemSayisi = 21,
                    TutarTL = 6450m,
                    SiparisTuru = "Sarf Malzeme",
                    Durum = "Tamamlandı",
                    KargoFirmasi = "MNG Kargo",
                    KargoTakipNo = "MN-301984210",
                    FaturaNo = "FTR-2026-00385",
                    FaturaPdfUrl = "#"
                },
                new()
                {
                    SiparisNo = "HYT-SIP-2026-8550",
                    SiparisTarihi = DateTime.Now.AddDays(-45),
                    UrunOzet = "HiCariTakip ERP 1 Yıllık SaaS Lisansı (5 Kullanıcı)",
                    KalemSayisi = 1,
                    TutarTL = 18500m,
                    SiparisTuru = "Yazılım Lisans",
                    Durum = "Tamamlandı",
                    KargoFirmasi = "Dijital Teslimat",
                    KargoTakipNo = "LIS-AUTO-ACTIVATE",
                    FaturaNo = "FTR-2026-00320",
                    FaturaPdfUrl = "#"
                }
            };

            model.DestekTalepleri = new List<DestekTicketItem>
            {
                new()
                {
                    TicketNo = "TKT-GEN-9104",
                    Baslik = "Dün Verilen Siparişte Koli İçi Eksik Ribon Çıkması",
                    Modul = "Kargo & Sevkiyat Eksikliği",
                    Oncelik = "Yüksek",
                    Durum = "İnceleniyor",
                    OlusturmaZamani = DateTime.Now.AddDays(-1),
                    SonCevap = "Depo paketleme kamera kayıtları incelenmektedir, eksik ürün için ek kargo çıkışı hazırlanıyor.",
                    Mesaj = "HYT-SIP-2026-8812 no'lu siparişte koliden 1 adet resin ribon eksik çıktı. Kargo hasar tutanağı eklenmiştir.",
                    DosyaEki = "hasar_tespit_tutanagi.pdf"
                },
                new()
                {
                    TicketNo = "TKT-GEN-9042",
                    Baslik = "Sanal POS Ödeme Ekranında 3D Doğrulama Zaman Aşımı",
                    Modul = "Sanal POS & Finans",
                    Oncelik = "Normal",
                    Durum = "Çözüldü",
                    OlusturmaZamani = DateTime.Now.AddDays(-6),
                    SonCevap = "Banka sanal POS gateway oturum süreleri optimize edildi, sorun giderilmiştir.",
                    Mesaj = "Garanti BBVA sanal posu ile ödeme yaparken SMS kodu ekranı 30 saniye sonra time out verdi.",
                    DosyaEki = "pos_hata_ekrani.png"
                },
                new()
                {
                    TicketNo = "TKT-GEN-8980",
                    Baslik = "E-Arşiv Faturasında Vergi Numarası & Unvan Düzeltme Talebi",
                    Modul = "Fatura & Cari İşlemler",
                    Oncelik = "Normal",
                    Durum = "Çözüldü",
                    OlusturmaZamani = DateTime.Now.AddDays(-14),
                    SonCevap = "FTR-2026-00412 nolu faturanız iptal edilip düzeltilmiş e-arşiv fatura muhasebe e-postanıza iletilmiştir.",
                    Mesaj = "Geçtiğimiz ay kesilen faturada şirket ünvanımızdaki A.Ş. kısaltması sehven Ltd. Şti. kalmıştır.",
                    DosyaEki = "yeni_vergi_levhasi.pdf"
                }
            };

            var isk = user.IskontoOrani > 0 ? user.IskontoOrani : 35m;
            decimal CalcBayi(decimal liste) => Math.Round(liste * (1m - (isk / 100m)), 2);

            model.B2BFiyatListesi = new List<B2BFiyatListesiItem>
            {
                new() { UrunKodu = "SUN-D2S-COMBO", Marka = "Sunmi", UrunAdi = "Sunmi D2s Android 15.6\" Dokunmatik POS Terminali + Entegre 80mm Yazıcı", Kategori = "Android POS Terminali", TavsiyeEdilenSatisTL = 34500m, BayiIskontoOrani = isk, BayiAlisFiyatiTL = CalcBayi(34500m), StokDurumu = "Stokta Mevcut • Aynı Gün Sevk", GarantiSuresi = "2 Yıl Distribütör Garantili" },
                new() { UrunKodu = "SUN-V2S-MOBIL", Marka = "Sunmi", UrunAdi = "Sunmi V2s El Tipi Mobil Android POS (Dahili Barkod Okuyucu & Fiş Yazıcı)", Kategori = "Mobil POS", TavsiyeEdilenSatisTL = 21000m, BayiIskontoOrani = isk, BayiAlisFiyatiTL = CalcBayi(21000m), StokDurumu = "Stokta Mevcut • Hızlı Teslimat", GarantiSuresi = "2 Yıl Distribütör Garantili" },
                new() { UrunKodu = "ZBR-ZT411-IND", Marka = "Zebra", UrunAdi = "Zebra ZT411 Endüstriyel Barkod Yazıcı (203 DPI / Termal Transfer / Metal Kasa)", Kategori = "Barkod & Etiket Yazıcı", TavsiyeEdilenSatisTL = 52000m, BayiIskontoOrani = isk, BayiAlisFiyatiTL = CalcBayi(52000m), StokDurumu = "Stokta Mevcut • Aynı Gün Sevk", GarantiSuresi = "2 Yıl Resmi Zebra Garantisi" },
                new() { UrunKodu = "ZBR-ZD421-DSK", Marka = "Zebra", UrunAdi = "Zebra ZD421 Masaüstü Barkod & Kargo Etiket Yazıcısı (USB + Ethernet)", Kategori = "Barkod & Etiket Yazıcı", TavsiyeEdilenSatisTL = 18500m, BayiIskontoOrani = isk, BayiAlisFiyatiTL = CalcBayi(18500m), StokDurumu = "Stokta Mevcut • Depodan Sevkiyat", GarantiSuresi = "2 Yıl Resmi Zebra Garantisi" },
                new() { UrunKodu = "DAT-GRY4400-2D", Marka = "Datalogic", UrunAdi = "Datalogic Gryphon GFS4400 Sabit 2D Karekod & Barkod İmaj Okuyucu", Kategori = "Barkod Okuyucu", TavsiyeEdilenSatisTL = 12500m, BayiIskontoOrani = isk, BayiAlisFiyatiTL = CalcBayi(12500m), StokDurumu = "Stokta Mevcut • Aynı Gün Sevk", GarantiSuresi = "3 Yıl Genişletilmiş Garanti" },
                new() { UrunKodu = "KOD-T500-PC", Marka = "KodPOS", UrunAdi = "KodPOS T500 Ağır Hizmet Dokunmatik All-in-One PC Kasa (i5 / 8GB / 128GB SSD)", Kategori = "Dokunmatik Kasa PC", TavsiyeEdilenSatisTL = 28000m, BayiIskontoOrani = isk, BayiAlisFiyatiTL = CalcBayi(28000m), StokDurumu = "Stokta Mevcut • Aynı Gün Sevk", GarantiSuresi = "2 Yıl Distribütör Garantili" },
                new() { UrunKodu = "PAX-A910-VUK", Marka = "PAX", UrunAdi = "PAX A910SF Yeni Nesil Android VUK 507 Mobil Yazarkasa POS", Kategori = "Mobil Yazarkasa POS", TavsiyeEdilenSatisTL = 24000m, BayiIskontoOrani = isk, BayiAlisFiyatiTL = CalcBayi(24000m), StokDurumu = "Stokta Mevcut • Mali Mühürlü", GarantiSuresi = "2 Yıl Yetkili Servis" }
            };

            model.Duyurular = new List<BayiDuyuruItem>
            {
                new()
                {
                    Baslik = "Yeni KodPOS T500 Modellerimiz Merkez Depo Stoklarımıza Girmiştir",
                    Icerik = "Intel Core i5 işlemcili, alüminyum döküm gövdeli yeni nesil KodPOS T500 All-in-One dokunmatik kasa terminalleri B2B bayilerimize özel tanıtım fiyatlarıyla satışa sunulmuştur. 'Donanım Fiyat Listesi' sekmesinden güncel iskontolu fiyatları inceleyebilirsiniz.",
                    Kategori = "Yeni Ürün Lansmanı",
                    Oncelik = "Önemli",
                    Tarih = DateTime.Now.AddDays(-1),
                    Rozet = "Yeni Ürün",
                    Ikon = "bi-box-seam-fill"
                },
                new()
                {
                    Baslik = "Resmi Tatil Nedeniyle Kargo Çıkış Takvimi Bilgilendirmesi",
                    Icerik = "Önümüzdeki resmi tatil dönemi sebebiyle anlaşmalı olduğumuz Yurtiçi Kargo ve Aras Kargo son sipariş toplama saati Perşembe günü saat 16:00 olarak belirlenmiştir. Bu saatten sonra onaylanan siparişlerin çıkışı Pazartesi sabahı ilk sırada yapılacaktır.",
                    Kategori = "Lojistik & Kargo",
                    Oncelik = "Acil",
                    Tarih = DateTime.Now.AddDays(-3),
                    Rozet = "Lojistik Uyarısı",
                    Ikon = "bi-truck"
                },
                new()
                {
                    Baslik = "Hafta Sonu Planlı Bulut ERP & B2B Portalı Bakım Çalışması",
                    Icerik = "Pazar günü 02:00 - 05:00 saatleri arasında veritabanı performans optimizasyonu ve GİB e-Belge API altyapı güncellemeleri gerçekleştirilecektir. Bakım süresince portala kısa süreli kesintiler yaşanabilir.",
                    Kategori = "Sistem Bakımı",
                    Oncelik = "Bilgi",
                    Tarih = DateTime.Now.AddDays(-5),
                    Rozet = "Sistem Bakımı",
                    Ikon = "bi-wrench-adjustable-circle-fill"
                },
                new()
                {
                    Baslik = "2026 Q4 Donanım ve POS Ciro Teşvik Kampanyası Başladı",
                    Icerik = "Yılın son çeyreğinde Sunmi ve Zebra ürün gruplarında belirlenen ciro hedefine ulaşan yetkili bayilerimize %5 ek ciro primi ve 1 yıl ücretsiz kurumsal ERP lisansı hediye edilecektir. Detaylar için 'Size Özel Kampanyalar' sayfasını ziyaret edin.",
                    Kategori = "Önemli Duyuru",
                    Oncelik = "Önemli",
                    Tarih = DateTime.Now.AddDays(-12),
                    Rozet = "Kampanya",
                    Ikon = "bi-award-fill"
                }
            };
        }

        // =========================================================================
        // ETKİLEŞİMLİ İŞLEM VE AUDIT LOG KAYIT METODLARI
        // =========================================================================
        private async Task AuditLogEkleAsync(ApplicationUser user, string islemTuru, string detay, string? eskiDeger, string? yeniDeger)
        {
            var ip = Ip;
            var ua = UserAgent;
            var cihaz = ApplicationDbContext.CihazBilgisiCoz(ua, ip);

            _db.DenetimKayitlari.Add(new DenetimKaydi
            {
                Zaman = DateTime.Now,
                KullaniciAdi = user.UserName ?? user.Email ?? "bayi",
                Rol = Roller.Bayi,
                IpAdresi = ip,
                BilgisayarCihaz = cihaz,
                Eylem = islemTuru,
                TabloAdi = "BayiPortal",
                KayitId = user.Id,
                Ozet = detay,
                EskiDeger = eskiDeger,
                YeniDeger = yeniDeger
            });

            await _db.SaveChangesAsync();
        }

        [HttpPost]
        [Authorize(Roles = Roller.Bayi)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> YeniRmaTalebi(string seriNo, string modelAdi, string arizaDetayi)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("BayiGirisi", "Home");

            var rmaNo = $"RMA-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
            var kayit = new RmaKaydi
            {
                RmaNo = rmaNo,
                MusteriAdi = user.FirmaAdi ?? user.AdSoyad ?? "Bayi",
                Eposta = user.Email ?? "",
                Telefon = user.PhoneNumber ?? "",
                CihazModeli = modelAdi?.Trim() ?? "Belirtilmedi",
                SeriNo = seriNo?.Trim() ?? "SN-YOK",
                KabulTarihi = DateTime.Now,
                ArizaAciklamasi = arizaDetayi?.Trim() ?? "Arıza bildirimi yapıldı.",
                Durum = RmaSabitleri.Durumlar[0],
                Cozum = "Teknik servis incelemesine alındı."
            };
            _db.RmaKayitlari.Add(kayit);
            await _db.SaveChangesAsync();

            await AuditLogEkleAsync(user, "RMATalebiAcma",
                $"{rmaNo} numaralı RMA servis talebi açıldı. Model: {kayit.CihazModeli}, SN: {kayit.SeriNo}. Arıza: {kayit.ArizaAciklamasi}",
                null, $"RmaNo: {rmaNo}, Durum: {kayit.Durum}");

            TempData["BayiBasari"] = $"{rmaNo} numaralı RMA teknik servis talebiniz oluşturuldu. Yetkili kargo takip kodu üretildi.";
            return RedirectToAction(nameof(Dashboard), new { tab = "rma" });
        }

        [HttpPost]
        [Authorize(Roles = Roller.Bayi)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> B2BOdemeYap(decimal tutar, string kartSahibi, string kartNo)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("BayiGirisi", "Home");

            if (tutar <= 0) tutar = 5000m;
            var maskeliKart = string.IsNullOrWhiteSpace(kartNo) || kartNo.Length < 4
                ? "**** **** **** 4242"
                : $"**** **** **** {kartNo.Trim()[^4..]}";

            await AuditLogEkleAsync(user, "B2BOnlineOdeme",
                $"3D Secure Sanal POS üzerinden {tutar:N2} TL tutarında tahsilat yapıldı. Kart Sahibi: {kartSahibi?.Trim() ?? "Firma Kartı"}, Kart: {maskeliKart}",
                "CariBakiye: 38.450,00 TL", $"Tahsilat: {tutar:N2} TL (Onay Kodu: 914821)");

            TempData["BayiBasari"] = $"Tebrikler! {tutar:N2} TL tutarındaki ödemeniz 3D Secure ile başarıyla tamamlandı. Dekont cari hesabınıza işlendi.";
            return RedirectToAction(nameof(Dashboard), new { tab = "finans" });
        }

        [HttpPost]
        [Authorize(Roles = Roller.Bayi)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> YeniAltKullanici(string adSoyad, string eposta, string rol)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("BayiGirisi", "Home");

            await AuditLogEkleAsync(user, "AltKullaniciEkleme",
                $"Yeni alt kullanıcı eklendi: {adSoyad} ({eposta}) - Yetki: {rol}",
                null, $"AdSoyad: {adSoyad}, Rol: {rol}");

            TempData["BayiBasari"] = $"{adSoyad} isimli alt kullanıcı hesabı '{rol}' yetkisiyle başarıyla tanımlandı.";
            return RedirectToAction(nameof(Dashboard), new { tab = "alt-kullanicilar" });
        }

        [HttpPost]
        [Authorize(Roles = Roller.Bayi)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> YeniFirsatProje(string projeAdi, string musteriFirma, string donanimGrubu, int adet, decimal butce)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("BayiGirisi", "Home");

            await AuditLogEkleAsync(user, "FirsatProjeKaydi",
                $"Fırsat & Proje Kaydı açıldı: '{projeAdi}' - Müşteri: {musteriFirma}, Donanım: {donanimGrubu} ({adet} Adet, Bütçe: {butce:N0} TL)",
                null, "Durum: Fiyat Koruması Onaylandı");

            TempData["BayiBasari"] = $"'{projeAdi}' fırsat kaydı alındı. Hayat İşleri fiyat koruması ve proje iskonto onayı aktifleşti.";
            return RedirectToAction(nameof(Dashboard), new { tab = "proje-kaydi" });
        }

        [HttpPost]
        [Authorize(Roles = Roller.Bayi)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> YeniDemoTalep(string cihazModeli, string musteriAdi, int gunSure)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("BayiGirisi", "Home");

            await AuditLogEkleAsync(user, "DemoCihazTalebi",
                $"Demo test cihazı talep edildi: {cihazModeli} (Müşteri: {musteriAdi}, Süre: {gunSure} Gün)",
                null, "Durum: Depo Sevk Hazırlığında");

            TempData["BayiBasari"] = $"{cihazModeli} için {gunSure} günlük demo test cihazı talebiniz onaylandı, kargo sevkiyatına alındı.";
            return RedirectToAction(nameof(Dashboard), new { tab = "demo-talebi" });
        }

        [HttpPost]
        [Authorize(Roles = Roller.Bayi)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> YeniDestekTicket(string baslik, string modul, string oncelik, string aciklama)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("BayiGirisi", "Home");

            var ticketNo = $"TKT-{Random.Shared.Next(8000, 9999)}";
            await AuditLogEkleAsync(user, "YazilimDestekTicket",
                $"{ticketNo} numaralı yazılım destek talebi açıldı: '{baslik}' (Modül: {modul}, Öncelik: {oncelik})",
                null, $"Ticket: {ticketNo}, Durum: İnceleniyor");

            TempData["BayiBasari"] = $"{ticketNo} numaralı teknik destek talebiniz oluşturuldu. ERP yazılım uzmanımız incelemeye başladı.";
            return RedirectToAction(nameof(Dashboard), new { tab = "destek-ticket" });
        }

        [HttpPost]
        [Authorize(Roles = Roller.Bayi)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> YeniDestekTalebi(string baslik, string kategori, string oncelik, string aciklama, string? dosyaAdi)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("BayiGirisi", "Home");

            var ticketNo = $"TKT-GEN-{Random.Shared.Next(8000, 9999)}";
            await AuditLogEkleAsync(user, "GenelDestekTicket",
                $"{ticketNo} numaralı yardım masası destek talebi açıldı: '{baslik}' (Kategori: {kategori}, Öncelik: {oncelik})",
                null, $"Ticket: {ticketNo}, Durum: İnceleniyor, Ek: {dosyaAdi ?? "Yok"}");

            TempData["BayiBasari"] = $"{ticketNo} numaralı destek talebiniz başarıyla iletildi. İlgili operasyon ve finans ekibimiz incelemeye aldı.";
            return RedirectToAction(nameof(Dashboard), new { tab = "genel-destek" });
        }

        [HttpPost]
        [Authorize(Roles = Roller.Bayi)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AltMusteriEkle(string unvan, string yetkili, string telefon, string eposta, string lisansTipi, string sehir)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("BayiGirisi", "Home");

            await AuditLogEkleAsync(user, "AltMusteriLisansAtama",
                $"Yeni alt müşteri tanımlandı ve lisans atandı: {unvan} ({yetkili}) - Lisans: {lisansTipi}, Şehir: {sehir}",
                null, $"Musteri: {unvan}, Lisans: {lisansTipi}");

            TempData["BayiBasari"] = $"{unvan} firması alt müşteri listenize eklendi ve {lisansTipi} lisansı tanımlandı.";
            return RedirectToAction(nameof(Dashboard), new { tab = "alt-musteriler" });
        }

        [HttpPost]
        [Authorize(Roles = Roller.Bayi)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CihazYazilimEslestir(string donanimSeriNo, string donanimModeli, string lisansKodu, string magazaSube)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("BayiGirisi", "Home");

            await AuditLogEkleAsync(user, "CihazYazilimSenkronizasyon",
                $"Cihaz - Yazılım eşleştirmesi yapıldı: SN '{donanimSeriNo}' ({donanimModeli}) <-> Lisans: '{lisansKodu}' (Şube: {magazaSube})",
                null, "Durum: Senkronize (Online)");

            TempData["BayiBasari"] = $"{donanimModeli} cihazı ile {lisansKodu} lisansı başarıyla eşleştirildi ve {magazaSube} için aktif edildi.";
            return RedirectToAction(nameof(Dashboard), new { tab = "eslestirme" });
        }

        [HttpPost]
        [Authorize(Roles = Roller.Bayi)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MdfTalepGonder(string etkinlikAdi, decimal talepTutari)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("BayiGirisi", "Home");

            await AuditLogEkleAsync(user, "MdfPazarlamaFonuTalebi",
                $"Ortak Pazarlama Fonu (MDF) talebi gönderildi: '{etkinlikAdi}' - Talep: {talepTutari:N0} TL",
                null, "Durum: Kanal Yönetimi İncelemesinde");

            TempData["BayiBasari"] = $"'{etkinlikAdi}' için {talepTutari:N0} TL tutarındaki MDF pazarlama fonu başvurunuz alındı.";
            return RedirectToAction(nameof(Dashboard), new { tab = "mdf-fonu" });
        }

        [HttpPost]
        [Authorize(Roles = Roller.Bayi)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EskiCihazTakas(string eskiModel, string eskiSeriNo, string yeniModel)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("BayiGirisi", "Home");

            await AuditLogEkleAsync(user, "EskiCihazGeriAlim",
                $"Eski cihaz değişim programı (Refresh) başvurusu yapıldı: Eski '{eskiModel}' (SN: {eskiSeriNo}) -> Yeni Hedef: '{yeniModel}'",
                null, "Durum: Takas İskontosu Onaylandı (4.500 TL)");

            TempData["BayiBasari"] = $"{eskiModel} cihazınız için takas geri alım başvurusu onaylandı. 4.500 TL takas indirimi tanımlandı.";
            return RedirectToAction(nameof(Dashboard), new { tab = "eski-cihaz-degisim" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cikis()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("BayiGirisi", "Home");
        }

        // ===================================================== ŞİFRE SIFIRLAMA
        [HttpGet]
        [AllowAnonymous]
        public IActionResult SifreUnuttum() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SifreUnuttum(string? eposta)
        {
            // Hesap var/yok bilgisini sızdırmamak için her durumda aynı mesaj gösterilir.
            if (!string.IsNullOrWhiteSpace(eposta))
            {
                var user = await _userManager.FindByEmailAsync(eposta.Trim());
                if (user != null && await _userManager.IsInRoleAsync(user, Roller.Bayi) && user.HesapDurumu == HesapDurumlari.Aktif)
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var link = Url.Action(nameof(SifreSifirla), "Bayi", new { email = user.Email, token }, Request.Scheme)!;
                    var gonderildi = await _email.SendAsync(user.Email!, "Şifre sıfırlama - Hayat İşleri Bayi Portalı",
                        $"<p>Sayın {E(user.AdSoyad)},</p><p>Şifrenizi sıfırlamak için aşağıdaki bağlantıya tıklayın (1 saat geçerlidir):</p>" +
                        $"<p><a href=\"{E(link)}\">Şifremi sıfırla</a></p><p>Bu talebi siz yapmadıysanız e-postayı yok sayabilirsiniz.</p>");
                    if (!gonderildi && _env.IsDevelopment())
                        _logger.LogWarning("[GELİŞTİRME] SMTP yapılandırılmadığı için şifre sıfırlama bağlantısı e-posta ile gönderilemedi: {Link}", link);
                }
            }

            TempData["BayiBasari"] = "E-posta adresiniz kayıtlıysa şifre sıfırlama bağlantısı gönderildi. Lütfen gelen kutunuzu kontrol edin.";
            return RedirectToAction("BayiGirisi", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult SifreSifirla(string? email, string? token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                TempData["BayiUyari"] = "Şifre sıfırlama bağlantısı geçersiz.";
                return RedirectToAction("BayiGirisi", "Home");
            }
            return View(new SifreSifirlaModel { Email = email, Token = token });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SifreSifirla(SifreSifirlaModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["BayiUyari"] = "Şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş.";
                return RedirectToAction("BayiGirisi", "Home");
            }

            var sonuc = await _userManager.ResetPasswordAsync(user, model.Token, model.Sifre);
            if (!sonuc.Succeeded)
            {
                foreach (var hata in sonuc.Errors) ModelState.AddModelError(string.Empty, hata.Description);
                return View(model);
            }

            TempData["BayiBasari"] = "Şifreniz güncellendi. Yeni şifrenizle giriş yapabilirsiniz.";
            return RedirectToAction("BayiGirisi", "Home");
        }
    }
}
