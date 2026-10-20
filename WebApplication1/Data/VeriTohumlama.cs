using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using WebApplication1.Models.Siniflar;

namespace WebApplication1.Data
{
    /// <summary>
    /// Uygulama açılışında rolleri, demo hesapları, SEO kayıtlarını ve örnek RMA kayıtlarını hazırlar.
    /// Demo hesapları yalnızca appsettings "Seed:DemoHesaplariOlustur" = true iken oluşturulur;
    /// demo şifreleri yalnızca Development ortamında yeniden eşitlenir. Canlıya çıkarken bu ayarı kapatın.
    /// </summary>
    public static class VeriTohumlama
    {
        public static async Task BaslatAsync(IServiceProvider sp, IWebHostEnvironment env, IConfiguration cfg)
        {
            var db = sp.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();
            await SemaGuncelleAsync(db);

            var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

            foreach (var rol in Roller.Hepsi)
            {
                if (!await roleManager.RoleExistsAsync(rol))
                    await roleManager.CreateAsync(new IdentityRole(rol));
            }

            // Eski (IdentityUser dönemi) kayıtlar için hesap durumunu normalleştir
            var eskiler = await db.Users.Where(u => u.HesapDurumu == "").ToListAsync();
            foreach (var u in eskiler) u.HesapDurumu = HesapDurumlari.Aktif;
            if (eskiler.Count > 0) await db.SaveChangesAsync();

            var demo = cfg.GetValue("Seed:DemoHesaplariOlustur", true);
            if (demo)
            {
                var sifirla = env.IsDevelopment();
                await KullaniciHazirla(userManager, sifirla, "admin", "admin@hayatisleri.com", "admin123", Roller.Admin,
                    u => { u.AdSoyad = "Sistem Yöneticisi"; u.FirmaAdi = "Hayat İşleri"; });
                await KullaniciHazirla(userManager, sifirla, "editor", "editor@hayatisleri.com", "editor123", Roller.Editor,
                    u => { u.AdSoyad = "İçerik Editörü"; u.FirmaAdi = "Hayat İşleri"; });
                await KullaniciHazirla(userManager, sifirla, "destek", "destek@hayatisleri.com", "destek123", Roller.DestekUzmani,
                    u => { u.AdSoyad = "Destek Uzmanı"; u.FirmaAdi = "Hayat İşleri"; });
                await KullaniciHazirla(userManager, sifirla, "temsilci", "temsilci@hayatisleri.com", "temsilci123", Roller.MusteriTemsilcisi,
                    u => { u.AdSoyad = "Müşteri Temsilcisi"; u.FirmaAdi = "Hayat İşleri"; });
                await KullaniciHazirla(userManager, sifirla, "bayii", "bayii@dogulojistik.com", "bayii123", Roller.Bayi, u =>
                {
                    u.AdSoyad = "Mehmet Demir";
                    u.FirmaAdi = "Doğu Lojistik Dağıtım A.Ş.";
                    u.VergiDairesi = "Harput VD";
                    u.VergiNo = "3120048192";
                    u.CariKodu = "HYT-3401";
                    u.IskontoOrani = 35m;
                    u.Sehir = "Elazığ";
                    u.PhoneNumber = "05010146623";
                });
                await KullaniciHazirla(userManager, sifirla, "bayi", "bayi@dogulojistik.com", "bayi123", Roller.Bayi, u =>
                {
                    u.AdSoyad = "Mehmet Demir";
                    u.FirmaAdi = "Doğu Lojistik Dağıtım A.Ş.";
                    u.VergiDairesi = "Harput VD";
                    u.VergiNo = "3120048192";
                    u.CariKodu = "HYT-3401";
                    u.IskontoOrani = 35m;
                    u.Sehir = "Elazığ";
                    u.PhoneNumber = "05010146623";
                });
            }

            await SeoTohumla(db);
            if (demo)
            {
                await RmaTohumla(db);
                await BayiKampanyaTohumla(db, userManager);
            }
        }

        private static async Task SemaGuncelleAsync(ApplicationDbContext db)
        {
            const string sql = @"
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF COL_LENGTH('AspNetUsers', 'AdSoyad') IS NULL ALTER TABLE [AspNetUsers] ADD [AdSoyad] nvarchar(120) NULL;
ELSE ALTER TABLE [AspNetUsers] ALTER COLUMN [AdSoyad] nvarchar(120) NULL;

IF COL_LENGTH('AspNetUsers', 'FirmaAdi') IS NULL ALTER TABLE [AspNetUsers] ADD [FirmaAdi] nvarchar(160) NULL;
ELSE ALTER TABLE [AspNetUsers] ALTER COLUMN [FirmaAdi] nvarchar(160) NULL;

IF COL_LENGTH('AspNetUsers', 'VergiDairesi') IS NULL ALTER TABLE [AspNetUsers] ADD [VergiDairesi] nvarchar(80) NULL;
ELSE ALTER TABLE [AspNetUsers] ALTER COLUMN [VergiDairesi] nvarchar(80) NULL;

IF COL_LENGTH('AspNetUsers', 'VergiNo') IS NULL ALTER TABLE [AspNetUsers] ADD [VergiNo] nvarchar(20) NULL;
ELSE ALTER TABLE [AspNetUsers] ALTER COLUMN [VergiNo] nvarchar(20) NULL;

IF COL_LENGTH('AspNetUsers', 'CariKodu') IS NULL ALTER TABLE [AspNetUsers] ADD [CariKodu] nvarchar(40) NULL;
ELSE ALTER TABLE [AspNetUsers] ALTER COLUMN [CariKodu] nvarchar(40) NULL;

IF COL_LENGTH('AspNetUsers', 'IskontoOrani') IS NULL ALTER TABLE [AspNetUsers] ADD [IskontoOrani] decimal(5,2) NOT NULL CONSTRAINT DF_AspNetUsers_IskontoOrani DEFAULT 0;

IF COL_LENGTH('AspNetUsers', 'Sehir') IS NULL ALTER TABLE [AspNetUsers] ADD [Sehir] nvarchar(60) NULL;
ELSE ALTER TABLE [AspNetUsers] ALTER COLUMN [Sehir] nvarchar(60) NULL;

IF COL_LENGTH('AspNetUsers', 'BayiPortalTipi') IS NULL ALTER TABLE [AspNetUsers] ADD [BayiPortalTipi] int NOT NULL CONSTRAINT DF_AspNetUsers_BayiPortalTipi DEFAULT 1;
IF COL_LENGTH('AspNetUsers', 'HesapDurumu') IS NULL ALTER TABLE [AspNetUsers] ADD [HesapDurumu] nvarchar(20) NOT NULL CONSTRAINT DF_AspNetUsers_HesapDurumu DEFAULT 'Aktif';
IF COL_LENGTH('AspNetUsers', 'KayitTarihi') IS NULL ALTER TABLE [AspNetUsers] ADD [KayitTarihi] datetime2 NOT NULL CONSTRAINT DF_AspNetUsers_KayitTarihi DEFAULT SYSDATETIME();
IF COL_LENGTH('AspNetUsers', 'OnayTarihi') IS NULL ALTER TABLE [AspNetUsers] ADD [OnayTarihi] datetime2 NULL;
IF COL_LENGTH('AspNetUsers', 'OnaylayanKullanici') IS NULL ALTER TABLE [AspNetUsers] ADD [OnaylayanKullanici] nvarchar(100) NULL;
IF COL_LENGTH('AspNetUsers', 'SonGirisTarihi') IS NULL ALTER TABLE [AspNetUsers] ADD [SonGirisTarihi] datetime2 NULL;
IF COL_LENGTH('AspNetUsers', 'YoneticiNotu') IS NULL ALTER TABLE [AspNetUsers] ADD [YoneticiNotu] nvarchar(500) NULL;

IF COL_LENGTH('SiteMesajlari', 'Durum') IS NULL ALTER TABLE [SiteMesajlari] ADD [Durum] nvarchar(30) NOT NULL CONSTRAINT DF_SiteMesajlari_Durum DEFAULT 'Yeni';
IF COL_LENGTH('SiteMesajlari', 'AtananKisi') IS NULL ALTER TABLE [SiteMesajlari] ADD [AtananKisi] nvarchar(100) NULL;
IF COL_LENGTH('SiteMesajlari', 'DahiliNotlar') IS NULL ALTER TABLE [SiteMesajlari] ADD [DahiliNotlar] nvarchar(max) NULL;
IF COL_LENGTH('SiteMesajlari', 'CevapMetni') IS NULL ALTER TABLE [SiteMesajlari] ADD [CevapMetni] nvarchar(max) NULL;
IF COL_LENGTH('SiteMesajlari', 'CevapTarihi') IS NULL ALTER TABLE [SiteMesajlari] ADD [CevapTarihi] datetime2 NULL;
IF COL_LENGTH('SiteMesajlari', 'SonIslemTarihi') IS NULL ALTER TABLE [SiteMesajlari] ADD [SonIslemTarihi] datetime2 NULL;

IF OBJECT_ID('DenetimKayitlari', 'U') IS NOT NULL AND COL_LENGTH('DenetimKayitlari', 'Ozet') IS not NULL
BEGIN
    ALTER TABLE [DenetimKayitlari] ALTER COLUMN [Rol] nvarchar(60) NULL;
    ALTER TABLE [DenetimKayitlari] ALTER COLUMN [IpAdresi] nvarchar(60) NULL;
    ALTER TABLE [DenetimKayitlari] ALTER COLUMN [KayitId] nvarchar(60) NULL;
    ALTER TABLE [DenetimKayitlari] ALTER COLUMN [Ozet] nvarchar(400) NULL;
END;

IF OBJECT_ID('DenetimKayitlari', 'U') IS NOT NULL AND COL_LENGTH('DenetimKayitlari', 'BilgisayarCihaz') IS NULL
BEGIN
    ALTER TABLE [DenetimKayitlari] ADD [BilgisayarCihaz] nvarchar(250) NULL;
END;

IF OBJECT_ID('DenetimKayitlari', 'U') IS NULL
BEGIN
    CREATE TABLE [DenetimKayitlari] (
        [Id] int NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [Zaman] datetime2 NOT NULL,
        [KullaniciAdi] nvarchar(100) NOT NULL,
        [Rol] nvarchar(60) NULL,
        [IpAdresi] nvarchar(60) NULL,
        [BilgisayarCihaz] nvarchar(250) NULL,
        [Eylem] nvarchar(40) NOT NULL,
        [TabloAdi] nvarchar(80) NOT NULL,
        [KayitId] nvarchar(60) NULL,
        [Ozet] nvarchar(400) NULL,
        [EskiDeger] nvarchar(max) NULL,
        [YeniDeger] nvarchar(max) NULL
    );
END;

IF OBJECT_ID('SeoAyarlari', 'U') IS NOT NULL AND COL_LENGTH('SeoAyarlari', 'OgGorselUrl') IS NULL
BEGIN
    DROP TABLE [SeoAyarlari];
END;

IF OBJECT_ID('SeoAyarlari', 'U') IS NULL
BEGIN
    CREATE TABLE [SeoAyarlari] (
        [Id] int NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [SayfaKodu] nvarchar(80) NOT NULL,
        [SayfaAdi] nvarchar(120) NOT NULL,
        [Slug] nvarchar(120) NULL,
        [MetaBaslik] nvarchar(160) NOT NULL,
        [MetaAciklama] nvarchar(320) NOT NULL,
        [AnahtarKelimeler] nvarchar(400) NULL,
        [OgGorselUrl] nvarchar(400) NULL,
        [Robots] nvarchar(40) NOT NULL CONSTRAINT DF_SeoAyarlari_Robots DEFAULT 'index, follow',
        [Aktif] bit NOT NULL CONSTRAINT DF_SeoAyarlari_Aktif DEFAULT 1,
        [GuncellemeTarihi] datetime2 NOT NULL CONSTRAINT DF_SeoAyarlari_GuncellemeTarihi DEFAULT SYSDATETIME(),
        [GuncelleyenKullanici] nvarchar(100) NULL
    );
    CREATE UNIQUE INDEX [IX_SeoAyarlari_SayfaKodu] ON [SeoAyarlari]([SayfaKodu]);
    CREATE UNIQUE INDEX [IX_SeoAyarlari_Slug] ON [SeoAyarlari]([Slug]) WHERE [Slug] IS NOT NULL;
END;

IF OBJECT_ID('RmaKayitlari', 'U') IS NOT NULL AND COL_LENGTH('RmaKayitlari', 'KapanisTarihi') IS NULL
BEGIN
    IF OBJECT_ID('RmaHareketleri', 'U') IS NOT NULL DROP TABLE [RmaHareketleri];
    DROP TABLE [RmaKayitlari];
END;

IF OBJECT_ID('RmaKayitlari', 'U') IS NULL
BEGIN
    CREATE TABLE [RmaKayitlari] (
        [Id] int NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [RmaNo] nvarchar(30) NOT NULL,
        [MusteriAdi] nvarchar(150) NOT NULL,
        [Eposta] nvarchar(150) NULL,
        [Telefon] nvarchar(40) NULL,
        [CihazModeli] nvarchar(150) NOT NULL,
        [SeriNo] nvarchar(100) NULL,
        [ArizaAciklamasi] nvarchar(1000) NOT NULL,
        [Teshis] nvarchar(1000) NULL,
        [GarantiKapsaminda] bit NOT NULL,
        [GarantiBitisTarihi] datetime2 NULL,
        [GarantiNotu] nvarchar(300) NULL,
        [Cozum] nvarchar(40) NOT NULL,
        [DegisenParca] nvarchar(300) NULL,
        [Ucret] decimal(18,2) NOT NULL,
        [Durum] nvarchar(50) NOT NULL,
        [Konum] nvarchar(60) NOT NULL,
        [KargoYonu] nvarchar(40) NOT NULL,
        [KargoFirmasi] nvarchar(60) NULL,
        [KargoTakipNo] nvarchar(60) NULL,
        [KargoTarihi] datetime2 NULL,
        [AtananTeknisyen] nvarchar(100) NULL,
        [KabulTarihi] datetime2 NOT NULL,
        [TahminiTeslimTarihi] datetime2 NULL,
        [KapanisTarihi] datetime2 NULL,
        [Oncelik] nvarchar(20) NOT NULL,
        [SonGuncelleme] datetime2 NOT NULL
    );
    CREATE UNIQUE INDEX [IX_RmaKayitlari_RmaNo] ON [RmaKayitlari]([RmaNo]);
END;

IF OBJECT_ID('RmaHareketleri', 'U') IS NULL
BEGIN
    CREATE TABLE [RmaHareketleri] (
        [Id] int NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [RmaKaydiId] int NOT NULL CONSTRAINT FK_RmaHareketleri_RmaKayitlari FOREIGN KEY REFERENCES [RmaKayitlari]([Id]) ON DELETE CASCADE,
        [Zaman] datetime2 NOT NULL,
        [Durum] nvarchar(50) NOT NULL,
        [Konum] nvarchar(60) NOT NULL,
        [Aciklama] nvarchar(500) NULL,
        [Kullanici] nvarchar(100) NULL
    );
END;
";
            await db.Database.ExecuteSqlRawAsync(sql);
        }

        private static async Task KullaniciHazirla(UserManager<ApplicationUser> um, bool sifreSifirla, string userName, string email,
            string sifre, string rol, Action<ApplicationUser> ozellestir)
        {
            var hasher = new PasswordHasher<ApplicationUser>();
            var user = await um.FindByNameAsync(userName) ?? await um.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = userName,
                    Email = email,
                    EmailConfirmed = true,
                    HesapDurumu = HesapDurumlari.Aktif,
                    OnayTarihi = DateTime.Now,
                    OnaylayanKullanici = "sistem"
                };
                ozellestir(user);
                // Demo şifreleri parola politikasını (büyük harf/sembol) sağlamadığı için hash doğrudan atanır.
                user.PasswordHash = hasher.HashPassword(user, sifre);
                var sonuc = await um.CreateAsync(user);
                if (!sonuc.Succeeded)
                    throw new InvalidOperationException($"'{userName}' demo hesabı oluşturulamadı: {string.Join("; ", sonuc.Errors.Select(e => e.Description))}");
            }
            else if (sifreSifirla)
            {
                if (hasher.VerifyHashedPassword(user, user.PasswordHash ?? "", sifre) == PasswordVerificationResult.Failed)
                {
                    user.PasswordHash = hasher.HashPassword(user, sifre);
                    user.SecurityStamp = Guid.NewGuid().ToString();
                }
                user.HesapDurumu = HesapDurumlari.Aktif;
                user.EmailConfirmed = true;
                user.LockoutEnd = null;
                user.AccessFailedCount = 0;
                await um.UpdateAsync(user);
            }

            if (!await um.IsInRoleAsync(user, rol))
                await um.AddToRoleAsync(user, rol);
        }

        // ------------------------------------------------------------------ SEO
        private static async Task SeoTohumla(ApplicationDbContext db)
        {
            var varsayilanlar = new (string Kod, string Ad, string? Slug, string Baslik, string Aciklama, string Anahtar)[]
            {
                ("Home/Index", "Ana Sayfa", null, "Hayat İşleri | Elazığ Özel Yazılım, Barkod & POS Sistemleri",
                    "Elazığ merkezli Hayat İşleri; özel yazılım, HiCariTakip ön muhasebe, barkod, POS ve kurumsal dijital çözümlerle işletmenizi büyütür.",
                    "özel yazılım, barkod sistemleri, pos, ön muhasebe, hicaritakip, elazığ"),
                ("Home/Hakkimizda", "Hakkımızda", "hakkimizda", "Hakkımızda | Hayat İşleri Kurumsal Yazılım",
                    "Hayat İşleri'nin hikâyesi, vizyonu ve kurumsal değerleri. Yazılım ve donanımı tek çatı altında sunan çözüm ortağınız.",
                    "hayat işleri hakkında, kurumsal yazılım firması, elazığ yazılım"),
                ("Home/Hizmetlerimiz", "Hizmetlerimiz", "hizmetlerimiz", "Hizmetlerimiz | Özel Yazılım, E-Ticaret, Barkod & POS",
                    "Özel yazılım, web geliştirme, e-ticaret ve sanal POS, barkod otomasyonu ve ön muhasebe hizmetlerimizi inceleyin.",
                    "özel yazılım, e-ticaret, sanal pos, barkod otomasyonu"),
                ("Home/Urunler", "Ürünler", "urunler", "Ürünler | Yazarkasa POS, Barkod ve Ödeme Sistemleri",
                    "Zebra, KodPOS, TSC ve daha fazlası: yazarkasa POS, barkod yazıcı, el terminali ve ödeme sistemleri ürün kataloğu.",
                    "yazarkasa pos, barkod yazıcı, el terminali, zebra, kodpos, tsc"),
                ("Home/Referanslarimiz", "Referanslarımız", "referanslarimiz", "Referanslarımız | Tamamlanan Kurumsal Projeler",
                    "HiCariTakip, barkod-POS ve kurumsal web projelerinde müşterilerimizle hayata geçirdiğimiz referans çalışmalar.",
                    "referanslar, kurumsal projeler, hicaritakip"),
                ("Home/Blog", "Blog & Duyurular", "blog", "Blog & Duyurular | Teknoloji ve Sektör Haberleri",
                    "Yazılım, barkod-POS ve dijital dönüşüm üzerine teknik yazılar, ürün duyuruları ve sektör haberleri.",
                    "teknoloji blogu, yazılım haberleri, ürün duyuruları"),
                ("Home/Contact", "İletişim", "iletisim", "İletişim | Hayat İşleri Elazığ",
                    "Hayat İşleri iletişim bilgileri: adres, telefon, e-posta ve iletişim formu. Projeniz için bize ulaşın.",
                    "iletişim, hayat işleri elazığ, adres, telefon"),
                ("Home/TeklifAl", "Teklif Al", "teklif-al", "Teklif Al | Ücretsiz Keşif ve Fiyat Teklifi",
                    "Projeniz için ücretsiz keşif ve fiyat teklifi alın. Ekibimiz en kısa sürede sizinle iletişime geçsin.",
                    "teklif al, fiyat teklifi, ücretsiz keşif"),
                ("Home/Destek", "Teknik Destek", "destek", "Teknik Destek | Destek Talebi Oluşturun",
                    "Yazılım ve donanım sorunlarınız için teknik destek talebi oluşturun; uzman ekibimiz size dönüş yapsın.",
                    "teknik destek, destek talebi, servis"),
                ("Home/InsanKaynaklari", "İnsan Kaynakları", "insan-kaynaklari", "İnsan Kaynakları | Kariyer ve İş Başvurusu",
                    "Hayat İşleri ekibine katılın: açık pozisyonlar ve iş başvuru formu.",
                    "kariyer, iş ilanları, elazığ yazılım iş"),
                ("Home/HesapNumaralarimiz", "Hesap Numaralarımız", "hesap-numaralarimiz", "Hesap Numaralarımız | Banka IBAN Bilgileri",
                    "Hayat İşleri banka hesap numaraları ve IBAN bilgileri.", "iban, banka hesapları"),
                ("Home/BayiGirisi", "Bayi Girişi & Başvuru", "bayi-girisi", "Bayi Girişi & Çözüm Ortaklığı | Hayat İşleri B2B Portal",
                    "Yetkili bayi girişi ve çözüm ortaklığı başvurusu. B2B portal üzerinden özel fiyatlarla sipariş verin.",
                    "bayi girişi, bayilik başvurusu, b2b portal")
            };

            var mevcut = (await db.SeoAyarlari.Select(s => s.SayfaKodu).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var eklenen = false;
            foreach (var v in varsayilanlar)
            {
                if (mevcut.Contains(v.Kod)) continue;
                db.SeoAyarlari.Add(new SeoAyari
                {
                    SayfaKodu = v.Kod, SayfaAdi = v.Ad, Slug = v.Slug, MetaBaslik = v.Baslik,
                    MetaAciklama = v.Aciklama, AnahtarKelimeler = v.Anahtar, GuncelleyenKullanici = "sistem"
                });
                eklenen = true;
            }
            if (eklenen) await db.SaveChangesAsync();
        }

        // ------------------------------------------------------------------ RMA
        private static async Task RmaTohumla(ApplicationDbContext db)
        {
            if (await db.RmaKayitlari.AnyAsync()) return;

            var simdi = DateTime.Now;
            var yil = simdi.Year;

            RmaKaydi Yeni(int no, string musteri, string? eposta, string telefon, string model, string seri, string ariza,
                string? teshis, bool garanti, int garantiKalanGun, string? garantiNot, string cozum, string? parca, decimal ucret,
                string durum, string konum, string kargoYonu, string? kargoFirma, string? takip, string teknisyen, int kabulGunOnce,
                int? tahminiGun, string oncelik)
            {
                return new RmaKaydi
                {
                    RmaNo = $"RMA-{yil}-{no:D4}", MusteriAdi = musteri, Eposta = eposta, Telefon = telefon, CihazModeli = model,
                    SeriNo = seri, ArizaAciklamasi = ariza, Teshis = teshis, GarantiKapsaminda = garanti,
                    GarantiBitisTarihi = simdi.Date.AddDays(garantiKalanGun), GarantiNotu = garantiNot, Cozum = cozum,
                    DegisenParca = parca, Ucret = ucret, Durum = durum, Konum = konum, KargoYonu = kargoYonu,
                    KargoFirmasi = kargoFirma, KargoTakipNo = takip, KargoTarihi = kargoFirma != null ? simdi.AddDays(-1) : null,
                    AtananTeknisyen = teknisyen, KabulTarihi = simdi.AddDays(-kabulGunOnce),
                    TahminiTeslimTarihi = tahminiGun.HasValue ? simdi.Date.AddDays(tahminiGun.Value) : null,
                    Oncelik = oncelik, SonGuncelleme = simdi
                };
            }

            void Hareket(RmaKaydi r, int gunOnce, string durum, string konum, string aciklama, string kullanici = "destek")
                => r.Hareketler.Add(new RmaHareket { Zaman = simdi.AddDays(-gunOnce), Durum = durum, Konum = konum, Aciklama = aciklama, Kullanici = kullanici });

            var r1 = Yeni(1, "Doğu Lojistik Dağıtım A.Ş.", "bayi@dogulojistik.com", "0501 014 6623", "Zebra ZT411 Endüstriyel Yazıcı",
                "ZBR-ZT411-984210", "Baskıda yatay çizgiler oluşuyor, etiket kalitesi bozuk.", "Termal baskı kafası aşınmış (kalibrasyonla düzelmedi).",
                true, 268, "Fatura 12.01.2026 - 2 yıl resmi garanti, kullanım hatası tespit edilmedi.", "Parça Değişimi",
                "Termal baskı kafası 203 dpi (P1058930-009)", 0m, "Kargoda", "Kargoda - Üreticiye Gidiyor", "Üreticiye Gönderim",
                "Yurtiçi Kargo", "YK-5821930447", "Emre Can", 6, 9, "Yüksek");
            Hareket(r1, 6, "Teslim Alındı", "Merkez Servis (Elazığ)", "Cihaz bayi tarafından servise teslim edildi.");
            Hareket(r1, 5, "Arıza Teşhisinde", "Merkez Servis (Elazığ)", "Test baskısı alındı; baskı kafası aşınması tespit edildi.");
            Hareket(r1, 3, "Garanti Onayı Bekleniyor", "Merkez Servis (Elazığ)", "Garanti kapsamı doğrulandı, üretici RMA numarası talep edildi.");
            Hareket(r1, 1, "Kargoda", "Kargoda - Üreticiye Gidiyor", "Cihaz Zebra yetkili servisine Yurtiçi Kargo ile gönderildi (YK-5821930447).");

            var r2 = Yeni(2, "Fırat Perakende Zinciri A.Ş.", null, "0424 555 01 22", "Sunmi D2s Plus Dokunmatik POS",
                "SUN-D2-550192", "Ana ekran dokunmatik tepki vermiyor, ara ara donuyor.", "Dokunmatik panel kontrolcüsü arızalı; onarım ekonomik değil.",
                true, 120, "Garanti süresi içinde; birebir değişim hakkı mevcut.", "Cihaz Değişimi (Birebir)",
                "Cihaz komple değişimi (yedek stoktan)", 0m, "Cihaz Değişimi Hazırlanıyor", "Depoda (Yedek Stok)", "Müşteriye Gönderim",
                null, null, "Emre Can", 4, 2, "Acil");
            Hareket(r2, 4, "Teslim Alındı", "Merkez Servis (Elazığ)", "Çarşı şubesi Kasa-3 POS cihazı teslim alındı.");
            Hareket(r2, 3, "Arıza Teşhisinde", "Merkez Servis (Elazığ)", "Dokunmatik panel testi başarısız.");
            Hareket(r2, 1, "Cihaz Değişimi Hazırlanıyor", "Depoda (Yedek Stok)", "Birebir değişim onaylandı; yedek stoktan cihaz ayrıldı, yarın sevk edilecek.");

            var r3 = Yeni(3, "Harput Toptan Gıda Ltd.", "satinalma@harputtoptan.com", "0532 410 22 33", "Datalogic Memor 10 El Terminali",
                "DAT-M10-330198", "Cihaz düşmüş, ekran kırık ve batarya şişmiş.", "Fiziksel darbe (düşme) kaynaklı ekran kırığı; batarya hasarlı.",
                false, -40, "Garanti dışı: fiziksel darbe/kullanıcı hatası garanti kapsamı dışındadır.", "Ücretli Onarım",
                "LCD+dokunmatik ekran modülü, batarya", 4850m, "Parça Bekleniyor", "Merkez Servis (Elazığ)", "Kargo Yok",
                null, null, "Burak Kaya", 8, 5, "Normal");
            Hareket(r3, 8, "Teslim Alındı", "Merkez Servis (Elazığ)", "Cihaz mal kabul ekibinden teslim alındı.");
            Hareket(r3, 6, "Arıza Teşhisinde", "Merkez Servis (Elazığ)", "Fiziksel darbe izleri tespit edildi; garanti dışı.");
            Hareket(r3, 5, "Parça Bekleniyor", "Merkez Servis (Elazığ)", "Müşteri 4.850 TL ücretli onarımı onayladı; ekran ve batarya sipariş edildi.");

            var r4 = Yeni(4, "Keban Su Ürünleri İhracat", "muhasebe@kebansu.com", "0424 712 40 18", "Zebra ZD220 Barkod Yazıcı",
                "ZBR-ZD220-771045", "Etiket sensörü sürekli kırmızı yanıyor, etiket çıkarmıyor.", "Etiket aralık sensörü kirli/kalibrasyonu kaymış.",
                true, 410, "Garanti kapsamında.", "Onarım", "Sensör temizliği ve kalibrasyon (parça değişimi gerekmedi)", 0m,
                "Teslime Hazır", "Merkez Servis (Elazığ)", "Kargo Yok", null, null, "Emre Can", 3, 0, "Normal");
            Hareket(r4, 3, "Teslim Alındı", "Merkez Servis (Elazığ)", "Cihaz teslim alındı.");
            Hareket(r4, 2, "Onarımda", "Merkez Servis (Elazığ)", "Sensör temizlendi, medya kalibrasyonu yapıldı.");
            Hareket(r4, 0, "Teslime Hazır", "Merkez Servis (Elazığ)", "Test baskıları başarılı. Müşteri bilgilendirildi, teslim bekleniyor.");

            var r5 = Yeni(5, "Doğu Lojistik Dağıtım A.Ş.", "bayi@dogulojistik.com", "0501 014 6623", "KodPOS Yeni Nesil Yazar Kasa T1",
                "KOD-T1-220981", "Kart okuyucu temassız ödemeyi okumuyor.", "Temassız (NFC) anten modülü arızalı, modül değiştirildi.",
                true, 530, "Garanti kapsamında, mali mühürlü cihaz yetkili serviste onarıldı.", "Parça Değişimi", "NFC anten modülü",
                0m, "Kargoda", "Kargoda - Müşteriye Gidiyor", "Müşteriye Gönderim", "Aras Kargo", "AR-70419283", "Burak Kaya", 11, 2, "Normal");
            Hareket(r5, 11, "Teslim Alındı", "Merkez Servis (Elazığ)", "Cihaz bayiden teslim alındı.");
            Hareket(r5, 9, "Üretici Servisinde", "Üretici / Distribütör Servisi", "Cihaz KodPOS yetkili servisine gönderildi.");
            Hareket(r5, 2, "Onarımda", "Üretici / Distribütör Servisi", "NFC anten modülü değiştirildi, test başarılı.");
            Hareket(r5, 0, "Kargoda", "Kargoda - Müşteriye Gidiyor", "Onarılan cihaz Aras Kargo ile müşteriye gönderildi (AR-70419283).");

            db.RmaKayitlari.AddRange(r1, r2, r3, r4, r5);
            await db.SaveChangesAsync();
        }

        private static async Task BayiKampanyaTohumla(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            if (await db.BayiOzelKampanyalari.AnyAsync()) return;

            var bayi = await userManager.FindByNameAsync("bayi") ?? await userManager.FindByNameAsync("bayii");
            var bayiId = bayi?.Id;
            var bayiAdi = bayi?.FirmaAdi ?? "Doğu Lojistik Dağıtım A.Ş.";

            var kampanyalar = new List<BayiOzelKampanya>
            {
                new()
                {
                    BayiId = bayiId,
                    BayiAdi = bayiAdi,
                    Kategori = "Donanım",
                    Baslik = "Sunmi POS & El Terminallerinde %15 Ekstra Proje İskontosu",
                    Aciklama = "Doğu Lojistik için özel tanımlanmıştır: Q4 dönemi toplu Sunmi D2s ve V2s alımlarında standart bayi iskontonuza ek %15 indirim uygulanır.",
                    UrunGrubu = "Sunmi Android POS & El Terminalleri",
                    IskontoOrani = 15m,
                    OzelFiyatTL = null,
                    BaslangicTarihi = DateTime.Today.AddDays(-5),
                    BitisTarihi = DateTime.Today.AddDays(25),
                    Aktif = true,
                    OlusturmaTarihi = DateTime.Now.AddDays(-5),
                    OlusturanAdmin = "admin"
                },
                new()
                {
                    BayiId = bayiId,
                    BayiAdi = bayiAdi,
                    Kategori = "Yazılım",
                    Baslik = "HiCariTakip ERP Ek Kullanıcı Modüllerinde %20 Tanıtım İndirimi",
                    Aciklama = "Mevcut ERP lisanslarınıza tanımlayacağınız her +5 ek kullanıcı paketinde geçerli 30 günlük özel kampanya.",
                    UrunGrubu = "HiCariTakip Bulut ERP & Kullanıcı Paketleri",
                    IskontoOrani = 20m,
                    OzelFiyatTL = 2560m,
                    BaslangicTarihi = DateTime.Today.AddDays(-2),
                    BitisTarihi = DateTime.Today.AddDays(28),
                    Aktif = true,
                    OlusturmaTarihi = DateTime.Now.AddDays(-2),
                    OlusturanAdmin = "admin"
                },
                new()
                {
                    BayiId = null,
                    BayiAdi = "Tüm Yetkili Bayiler",
                    Kategori = "Donanım",
                    Baslik = "Zebra Barkod Yazıcılarda 1 Koli Termal Rulo Hediye",
                    Aciklama = "Zebra ZT411 veya ZD421 siparişi veren tüm bayilerimize 1 koli (50 rulo) 80x80 termal fiş rulosu sevkiyata bedelsiz eklenir.",
                    UrunGrubu = "Zebra Endüstriyel & Masaüstü Barkod Yazıcılar",
                    IskontoOrani = 10m,
                    OzelFiyatTL = null,
                    BaslangicTarihi = DateTime.Today.AddDays(-10),
                    BitisTarihi = DateTime.Today.AddDays(20),
                    Aktif = true,
                    OlusturmaTarihi = DateTime.Now.AddDays(-10),
                    OlusturanAdmin = "admin"
                }
            };

            db.BayiOzelKampanyalari.AddRange(kampanyalar);
            await db.SaveChangesAsync();
        }
    }
}
