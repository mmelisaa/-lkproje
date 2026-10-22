using System.ComponentModel.DataAnnotations;
using WebApplication1.Models.Siniflar;

namespace WebApplication1.Models
{
    public class BayiKayitModel
    {
        [Required(ErrorMessage = "Yetkili adı soyadı gerekli.")]
        public string AdSoyad { get; set; } = "";

        [Required(ErrorMessage = "Firma unvanı gerekli.")]
        public string FirmaAdi { get; set; } = "";

        [Required(ErrorMessage = "Kurumsal e-posta gerekli."), EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        public string Eposta { get; set; } = "";

        [Required(ErrorMessage = "Telefon gerekli.")]
        public string Telefon { get; set; } = "";

        [Required(ErrorMessage = "Şehir gerekli.")]
        public string Sehir { get; set; } = "";

        [Required(ErrorMessage = "Vergi dairesi gerekli.")]
        public string VergiDairesi { get; set; } = "";

        [Required(ErrorMessage = "Vergi numarası gerekli."), RegularExpression(@"^\d{10,11}$", ErrorMessage = "Vergi numarası 10 veya 11 haneli olmalıdır.")]
        public string VergiNo { get; set; } = "";

        public string? Mesaj { get; set; }

        [Required(ErrorMessage = "Portal şifresi gerekli.")]
        public string Sifre { get; set; } = "";

        [Required(ErrorMessage = "Şifre tekrarı gerekli."), Compare(nameof(Sifre), ErrorMessage = "Şifreler eşleşmiyor.")]
        public string SifreTekrar { get; set; } = "";
    }

    public class SifreSifirlaModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string Token { get; set; } = "";

        [Required(ErrorMessage = "Yeni şifre gerekli.")]
        [DataType(DataType.Password)]
        public string Sifre { get; set; } = "";

        [Required(ErrorMessage = "Şifre tekrarı gerekli."), Compare(nameof(Sifre), ErrorMessage = "Şifreler eşleşmiyor.")]
        [DataType(DataType.Password)]
        public string SifreTekrar { get; set; } = "";
    }

    public static class BayiPortalTipleri
    {
        public const int DonanimVePos = 1;
        public const int YazilimVeErp = 2;
        public const int CozumOrtagi = 3;

        public static string Tanim(int tip) => tip switch
        {
            DonanimVePos => "Donanım ve POS Bayisi Paneli (Zebra, Sunmi, Datalogic, KodPOS)",
            YazilimVeErp => "Yazılım ve ERP Bayisi Paneli (HiCariTakip Cloud & B2B)",
            CozumOrtagi => "Tam Yetkili Çözüm Ortağı Paneli (Donanım + Yazılım Bir Arada)",
            _ => "Donanım ve POS Bayisi Paneli"
        };

        public static string KisaAd(int tip) => tip switch
        {
            DonanimVePos => "Donanım & POS Bayisi",
            YazilimVeErp => "Yazılım & ERP Bayisi",
            CozumOrtagi => "Tam Yetkili Çözüm Ortağı",
            _ => "Standart Bayi"
        };
    }

    // =========================================================================
    // 1. DONANIM & POS MODÜL MODELLERİ
    // =========================================================================
    public class CihazEnvanterItem
    {
        public string SeriNo { get; set; } = "";
        public string ModelAdi { get; set; } = "";
        public string Marka { get; set; } = "";
        public DateTime SatisTarihi { get; set; }
        public DateTime GarantiBitisTarihi { get; set; }
        public bool GarantiDevamEdiyor => GarantiBitisTarihi >= DateTime.Today;
        public int KalanGun => (GarantiBitisTarihi - DateTime.Today).Days;
        public string Lokasyon { get; set; } = "Merkez Depo / Kasa 1";
    }

    public class SarfSiparisItem
    {
        public string Kod { get; set; } = "";
        public string Baslik { get; set; } = "";
        public string Kategori { get; set; } = "";
        public decimal ListeFiyati { get; set; }
        public decimal BayiFiyati { get; set; }
        public int PaketAdet { get; set; }
        public string Olcu { get; set; } = "";
        public string StokDurumu { get; set; } = "Stokta Var";
        public string Gorsel { get; set; } = "/images/donanim.jpg";
    }

    public class DriverDownloadItem
    {
        public string Baslik { get; set; } = "";
        public string Surum { get; set; } = "";
        public string Platform { get; set; } = "";
        public string Boyut { get; set; } = "";
        public string DosyaAdi { get; set; } = "";
        public string Aciklama { get; set; } = "";
        public DateTime GuncellemeTarihi { get; set; }
    }

    public class FirsatProjeItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpper();
        public string ProjeAdi { get; set; } = "";
        public string MusteriFirma { get; set; } = "";
        public string DonanimGrubu { get; set; } = "";
        public int TahminiAdet { get; set; }
        public decimal TahminiButce { get; set; }
        public string Durum { get; set; } = "Onaylandı (Fiyat Koruması Aktif)";
        public DateTime KayitTarihi { get; set; } = DateTime.Now;
        public string SatisDanismani { get; set; } = "Kanal Yönetimi";
    }

    public class DemoCihazTalepItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpper();
        public string CihazModeli { get; set; } = "";
        public string MusteriAdi { get; set; } = "";
        public int GunSure { get; set; } = 15;
        public string KargoTakipNo { get; set; } = "";
        public string Durum { get; set; } = "Sevk Edildi (Test Aşamasında)";
        public DateTime TalepTarihi { get; set; } = DateTime.Now;
    }

    public class PazarlamaMateryalItem
    {
        public string Baslik { get; set; } = "";
        public string Format { get; set; } = "PDF / ZIP";
        public string Boyut { get; set; } = "";
        public string Tur { get; set; } = "Teknik Şartname";
        public string OnizlemeUrl { get; set; } = "";
    }

    public class EskiCihazDegisimItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpper();
        public string EskiModel { get; set; } = "";
        public string EskiSeriNo { get; set; } = "";
        public string YeniHedefModel { get; set; } = "";
        public decimal TakasIskontosuTL { get; set; }
        public string Durum { get; set; } = "İncelemede (Depo Girişi Bekleniyor)";
        public DateTime BasvuruTarihi { get; set; } = DateTime.Now;
    }

    // =========================================================================
    // 2. YAZILIM & ERP MODÜL MODELLERİ
    // =========================================================================
    public class LisansAbonelikItem
    {
        public string LisansKodu { get; set; } = "";
        public string PaketAdi { get; set; } = "";
        public string SonKullanici { get; set; } = "";
        public DateTime Baslangic { get; set; }
        public DateTime Bitis { get; set; }
        public bool AktifMi => Bitis >= DateTime.Today;
        public int KalanGun => (Bitis - DateTime.Today).Days;
        public string Tip { get; set; } = "Yıllık SaaS";
        public int KullaniciLimiti { get; set; } = 5;
    }

    public class SurumNotuItem
    {
        public string Versiyon { get; set; } = "";
        public string Baslik { get; set; } = "";
        public DateTime YayinTarihi { get; set; }
        public List<string> Yenilikler { get; set; } = new();
        public List<string> Duzeltmeler { get; set; } = new();
        public string PatchUrl { get; set; } = "#";
    }

    public class DestekTicketItem
    {
        public string TicketNo { get; set; } = "";
        public string Baslik { get; set; } = "";
        public string Modul { get; set; } = "";
        public string Oncelik { get; set; } = "Normal";
        public string Durum { get; set; } = "İnceleniyor";
        public DateTime OlusturmaZamani { get; set; } = DateTime.Now;
        public string SonCevap { get; set; } = "";
        public string? Mesaj { get; set; }
        public string? DosyaEki { get; set; }
    }

    public class AltMusteriItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpper();
        public string Unvan { get; set; } = "";
        public string Yetkili { get; set; } = "";
        public string Telefon { get; set; } = "";
        public string Eposta { get; set; } = "";
        public string AtananLisans { get; set; } = "";
        public string Sehir { get; set; } = "";
        public DateTime EklenmeTarihi { get; set; } = DateTime.Now;
    }

    public class AkademiEgitimItem
    {
        public string DersAdi { get; set; } = "";
        public string Kategori { get; set; } = "";
        public string Sure { get; set; } = "";
        public int IlerlemeYuzdesi { get; set; }
        public bool SertifikaVerildi { get; set; }
        public string SertifikaKodu { get; set; } = "";
    }

    public class UpsellPaketItem
    {
        public string Baslik { get; set; } = "";
        public string Kod { get; set; } = "";
        public string Aciklama { get; set; } = "";
        public decimal FiyatTL { get; set; }
        public string Rozet { get; set; } = "Popüler";
    }

    // =========================================================================
    // 3. TAM YETKİLİ ÇÖZÜM ORTAĞI STRATEJİ MODELLERİ
    // =========================================================================
    public class CihazYazilimEslestirmeItem
    {
        public string DonanimSeriNo { get; set; } = "";
        public string DonanimModeli { get; set; } = "";
        public string LisansKodu { get; set; } = "";
        public string MagazaSube { get; set; } = "";
        public string Durum { get; set; } = "Senkronize (Online)";
        public DateTime EslestirmeTarihi { get; set; }
    }

    public class CaprazSatisOneriItem
    {
        public string MusteriUnvani { get; set; } = "";
        public string MevcutSistem { get; set; } = "";
        public string OnerilenDonanim { get; set; } = "";
        public string PotansiyelGelir { get; set; } = "";
        public string Gerekce { get; set; } = "";
    }

    public class MdfTalepItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpper();
        public string EtkinlikAdi { get; set; } = "";
        public decimal TalepTutariTL { get; set; }
        public decimal OnaylananTutarTL { get; set; }
        public string Durum { get; set; } = "Onaylandı (Ödeme Sırasında)";
        public DateTime BasvuruTarihi { get; set; } = DateTime.Now;
    }

    // =========================================================================
    // 4. TÜMÜ İÇİN ORTAK B2B FİNANS & DENETİM MODELLERİ
    // =========================================================================
    public class B2BFaturaItem
    {
        public string FaturaNo { get; set; } = "";
        public DateTime Tarih { get; set; }
        public DateTime VadeTarihi { get; set; }
        public decimal TutarTL { get; set; }
        public decimal KalanTL { get; set; }
        public string Durum { get; set; } = "Ödendi";
    }

    public class SubUserItem
    {
        public string Id { get; set; } = "";
        public string AdSoyad { get; set; } = "";
        public string Eposta { get; set; } = "";
        public string Telefon { get; set; } = "";
        public string Unvan { get; set; } = "Muhasebe Yetkilisi";
        public string YetkiAlani { get; set; } = "Özel Tanımlı";
        public DateTime SonGiris { get; set; }
        public bool Aktif { get; set; } = true;
        public bool IzinFinansSanalPos { get; set; }
        public bool IzinDenetimIzi { get; set; }
        public bool IzinSiparisVeSarf { get; set; }
        public bool IzinTeknikServisRma { get; set; } = true;
        public bool IzinDestekTalepleri { get; set; } = true;
        public bool IzinFiyatListesi { get; set; } = true;
        public bool IzinKutuphaneDriver { get; set; } = true;
        public bool IzinProjeFirsat { get; set; } = true;
    }

    public class BayiGirisLogItem
    {
        public DateTime Zaman { get; set; }
        public string IpAdresi { get; set; } = "";
        public string BilgisayarCihaz { get; set; } = "";
        public bool Basarili { get; set; }
    }

    public class BayiIslemAuditItem
    {
        public DateTime Zaman { get; set; }
        public string Kullanici { get; set; } = "";
        public string BilgisayarCihaz { get; set; } = "";
        public string IpAdresi { get; set; } = "";
        public string IslemTuru { get; set; } = "";
        public string Detay { get; set; } = "";
        public string? EskiDeger { get; set; }
        public string? YeniDeger { get; set; }
    }

    public class AdminAltKullaniciGorunumModel
    {
        public string Id { get; set; } = "";
        public string AnaBayiId { get; set; } = "";
        public string AnaBayiFirmaAdi { get; set; } = "";
        public string AnaBayiCariKodu { get; set; } = "";
        public string AdSoyad { get; set; } = "";
        public string Eposta { get; set; } = "";
        public string Telefon { get; set; } = "";
        public string Unvan { get; set; } = "";
        public bool Aktif { get; set; } = true;
        public DateTime KayitTarihi { get; set; }
        public DateTime? SonGirisTarihi { get; set; }
        public string SifreKriptolu { get; set; } = "";
        public bool IzinFinansSanalPos { get; set; }
        public bool IzinDenetimIzi { get; set; }
        public bool IzinSiparisVeSarf { get; set; }
        public bool IzinTeknikServisRma { get; set; }
        public bool IzinDestekTalepleri { get; set; }
        public bool IzinFiyatListesi { get; set; }
        public bool IzinKutuphaneDriver { get; set; }
        public bool IzinProjeFirsat { get; set; }
    }

    // =========================================================================
    // MERKEZİ BAYİ DASHBOARD VIEW MODEL
    // =========================================================================
    public class BayiDashboardViewModel
    {
        public ApplicationUser Kullanici { get; set; } = null!;
        public ApplicationUser? AnaBayiKullanici { get; set; }
        public bool IsAltKullanici => Kullanici?.IsAltKullanici ?? false;
        public int PortalTipi => (AnaBayiKullanici ?? Kullanici)?.BayiPortalTipi ?? 1;

        // Donanım & POS Verileri (Portal 1 & 3)
        public List<CihazEnvanterItem> EnvanterListesi { get; set; } = new();
        public List<RmaKaydi> RmaKayitlari { get; set; } = new();
        public List<SarfSiparisItem> SarfUrunleri { get; set; } = new();
        public List<DriverDownloadItem> DriverListesi { get; set; } = new();
        public List<FirsatProjeItem> FirsatProjeleri { get; set; } = new();
        public List<DemoCihazTalepItem> DemoTalepleri { get; set; } = new();
        public List<PazarlamaMateryalItem> PazarlamaMateryalleri { get; set; } = new();
        public List<EskiCihazDegisimItem> EskiCihazDegisimleri { get; set; } = new();

        // Yazılım & ERP Verileri (Portal 2 & 3)
        public List<LisansAbonelikItem> Lisanslar { get; set; } = new();
        public List<SurumNotuItem> SurumNotlari { get; set; } = new();
        public List<DestekTicketItem> DestekTicketlari { get; set; } = new();
        public List<AltMusteriItem> AltMusteriler { get; set; } = new();
        public List<AkademiEgitimItem> AkademiEgitimleri { get; set; } = new();
        public List<UpsellPaketItem> UpsellPaketleri { get; set; } = new();
        public string BulutUptime { get; set; } = "%99.98";
        public string SonBulutYedekZamani { get; set; } = DateTime.Now.AddHours(-2).ToString("dd.MM.yyyy HH:mm");
        public string BulutKota { get; set; } = "14.2 GB / 50 GB";
        public string ApiKey { get; set; } = "sk_live_hicari_9824_production_partner";

        // Çözüm Ortağı Verileri (Portal 3)
        public List<CihazYazilimEslestirmeItem> Eslestirmeler { get; set; } = new();
        public List<CaprazSatisOneriItem> CaprazSatisOnerileri { get; set; } = new();
        public List<MdfTalepItem> MdfTalepleri { get; set; } = new();
        public decimal CeyrekHedefTL { get; set; } = 500000m;
        public decimal CeyrekGerceklesenTL { get; set; } = 425000m;
        public decimal CeyrekPrimOrani { get; set; } = 5.0m;

        // B2B Finans & Tahsilat (Ortak)
        public decimal CariBakiyeTL { get; set; } = 38450m;
        public decimal RiskLimitiTL { get; set; } = 250000m;
        public decimal VadesiGecenTL { get; set; } = 0m;
        public List<B2BFaturaItem> Faturalar { get; set; } = new();
        public List<SubUserItem> AltKullanicilar { get; set; } = new();

        // Denetim & Giriş Logları (Ortak)
        public List<BayiGirisLogItem> GirisLoglari { get; set; } = new();
        public List<BayiIslemAuditItem> IslemLoglari { get; set; } = new();

        public List<SiteMesaj> Talepler { get; set; } = new();

        // 5. YENİ EKLENEN BAYİ PORTAL MODÜLLERİ (Size Özel Kampanyalar, Sipariş Geçmişi & E-Fatura, Fiyat Listesi, Duyurular, Genel Destek)
        public List<BayiOzelKampanya> OzelKampanyalar { get; set; } = new();
        public List<BayiSiparisGecmisiItem> SiparisGecmisi { get; set; } = new();
        public List<DestekTicketItem> DestekTalepleri { get; set; } = new();
        public List<B2BFiyatListesiItem> B2BFiyatListesi { get; set; } = new();
        public List<BayiDuyuruItem> Duyurular { get; set; } = new();
    }

    // =========================================================================
    // 5. YENİ MODÜL TANIMLARI
    // =========================================================================
    public class BayiOzelKampanya
    {
        public int Id { get; set; }
        public string? BayiId { get; set; }
        public string? BayiAdi { get; set; }
        public string Kategori { get; set; } = "Donanım"; // "Donanım" veya "Yazılım"
        public string Baslik { get; set; } = "";
        public string? Aciklama { get; set; }
        public string? UrunGrubu { get; set; }
        public decimal IskontoOrani { get; set; }
        public decimal? OzelFiyatTL { get; set; }
        public DateTime BaslangicTarihi { get; set; } = DateTime.Today;
        public DateTime BitisTarihi { get; set; } = DateTime.Today.AddDays(30);
        public bool Aktif { get; set; } = true;
        public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
        public string? OlusturanAdmin { get; set; }

        public bool GecerliMi => Aktif && BitisTarihi >= DateTime.Today;
        public int KalanGun => (BitisTarihi - DateTime.Today).Days >= 0 ? (BitisTarihi - DateTime.Today).Days : 0;
    }

    public class BayiSiparisGecmisiItem
    {
        public string SiparisNo { get; set; } = "";
        public DateTime SiparisTarihi { get; set; } = DateTime.Now;
        public string UrunOzet { get; set; } = "";
        public int KalemSayisi { get; set; } = 1;
        public decimal TutarTL { get; set; }
        public string SiparisTuru { get; set; } = "Donanım"; // "Donanım", "Sarf Malzeme", "Yazılım Lisans"
        public string Durum { get; set; } = "Kargoya Verildi"; // "Kargoya Verildi", "Tamamlandı", "Hazırlanıyor", "İptal Edildi"
        public string? KargoFirmasi { get; set; } = "Yurtiçi Kargo";
        public string? KargoTakipNo { get; set; }
        public string? FaturaNo { get; set; }
        public string? FaturaPdfUrl { get; set; }
    }

    public class B2BFiyatListesiItem
    {
        public string UrunKodu { get; set; } = "";
        public string UrunAdi { get; set; } = "";
        public string Marka { get; set; } = "";
        public string Kategori { get; set; } = "";
        public decimal TavsiyeEdilenSatisTL { get; set; }
        public decimal BayiIskontoOrani { get; set; }
        public decimal BayiAlisFiyatiTL { get; set; }
        public string StokDurumu { get; set; } = "Stokta Mevcut • Aynı Gün Kargo";
        public string GarantiSuresi { get; set; } = "2 Yıl";
        public string? BrosurUrl { get; set; }
    }

    public class BayiDuyuruItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpper();
        public string Baslik { get; set; } = "";
        public string Icerik { get; set; } = "";
        public string Kategori { get; set; } = "Genel Duyuru"; // "Önemli Duyuru", "Lojistik & Kargo", "Yeni Ürün Lansmanı", "Sistem Bakımı"
        public string Oncelik { get; set; } = "Normal"; // "Acil", "Önemli", "Bilgi"
        public DateTime Tarih { get; set; } = DateTime.Now;
        public string Rozet { get; set; } = "Yeni";
        public bool Okundu { get; set; } = false;
        public string? Ikon { get; set; } = "bi-megaphone";
    }

    // ---------------- Admin paneli ----------------
    public class KullaniciSatiri
    {
        public string Id { get; set; } = "";
        public string UserName { get; set; } = "";
        public string? Email { get; set; }
        public string? AdSoyad { get; set; }
        public string? FirmaAdi { get; set; }
        public string? CariKodu { get; set; }
        public decimal IskontoOrani { get; set; }
        public string Rol { get; set; } = "";
        public string HesapDurumu { get; set; } = "";
        public DateTime KayitTarihi { get; set; }
        public DateTime? SonGirisTarihi { get; set; }
        public bool Kilitli { get; set; }
    }

    public class KullaniciFormModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı gerekli."), StringLength(60)]
        public string UserName { get; set; } = "";

        [Required(ErrorMessage = "E-posta gerekli."), EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
        public string Email { get; set; } = "";

        public string? AdSoyad { get; set; }
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Rol seçin.")]
        public string Rol { get; set; } = Roller.Bayi;

        public string HesapDurumu { get; set; } = HesapDurumlari.Aktif;

        public string? FirmaAdi { get; set; }
        public string? VergiDairesi { get; set; }
        public string? VergiNo { get; set; }
        public string? CariKodu { get; set; }
        public string? Sehir { get; set; }

        [Range(0, 100, ErrorMessage = "İskonto 0-100 arasında olmalıdır.")]
        public decimal IskontoOrani { get; set; }

        public string? YoneticiNotu { get; set; }

        /// <summary>Yeni kullanıcıda zorunlu; düzenlemede doldurulursa şifre değiştirilir.</summary>
        public string? YeniSifre { get; set; }
    }

    // =========================================================================
    // 6. BAYİ YÖNETİM & İLETİŞİM MERKEZİ (ADMİN OPERASYON MODELLERİ)
    // =========================================================================
    public class DuyuruBildirimKaydi
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpper();
        public string Baslik { get; set; } = "";
        public string Icerik { get; set; } = "";
        public string Kategori { get; set; } = "Genel Duyuru"; // "Yeni Ürün Lansmanı", "Lojistik & Kargo", "Sistem Bakımı", "Fiyat & İskonto", "Acil & Kritik"
        public string Oncelik { get; set; } = "Normal"; // "Normal", "Önemli", "Acil & Kritik"
        public string HedefTipi { get; set; } = "TumBayiler"; // "TumBayiler" veya "SeciliBayiler"
        public string HedefBayilerOzeti { get; set; } = "Tüm Bayiler (Genel Dağıtım)";
        public List<string> HedefBayiIdleri { get; set; } = new();
        public DateTime GonderimTarihi { get; set; } = DateTime.Now;
        public string GonderenAdmin { get; set; } = "Sistem Yöneticisi";
        public bool PortalBildirimi { get; set; } = true;
        public bool EpostaGonderildi { get; set; } = true;
        public bool SmsGonderildi { get; set; } = false;
        public int OkunmaSayisi { get; set; } = 0;
        public string Durum { get; set; } = "Yayında & İletildi";
    }

    public class ServisAracCihazKaydi
    {
        public string ServisTakipNo { get; set; } = "";
        public string BayiId { get; set; } = "";
        public string BayiAdi { get; set; } = "";
        public string CariKodu { get; set; } = "";
        public string AracCihazTipi { get; set; } = "Saha Mobil Servis Aracı"; // "Saha Mobil Servis Aracı", "POS Terminali", "Barkod Yazıcı", "El Terminali"
        public string PlakaVeyaSeriNo { get; set; } = ""; // örn: 34 HYT 802 veya SN: SUN-D2S-88412
        public string ModelTanim { get; set; } = ""; // örn: Ford Transit Gezici Servis Aracı veya Sunmi D2s Dokunmatik POS
        public string NeredeKonum { get; set; } = ""; // örn: Merkez Teknik Laboratuvar (Masa 4), Kadıköy Yetkili Servis, Saha Gezici Aracı #02 Yolda
        public string DurumAsamasi { get; set; } = ""; // örn: İncelemede, Yedek Parça Bekleniyor, Onarımda, Test & Kalibrasyonda, Hazır / Yola Çıktı
        public decimal TahminiTutarTL { get; set; }
        public decimal ParcaTutarTL { get; set; }
        public decimal IscilikTutarTL { get; set; }
        public decimal ToplamTutarTL => (ParcaTutarTL + IscilikTutarTL > 0) ? (ParcaTutarTL + IscilikTutarTL) : TahminiTutarTL;
        public string GarantiDurumu { get; set; } = "Garantili (0 TL)"; // Garantili (0 TL), Ücretli Servis, Saha Bakım Sözleşmesi
        public string ArizaVeYapilanIslemler { get; set; } = ""; // Her şeyin bilgisi / detaylar
        public string AtananTeknisyen { get; set; } = "Hakan Demir (Baş Teknisyen)";
        public DateTime KabulTarihi { get; set; } = DateTime.Now.AddDays(-2);
        public DateTime? TahminiTeslimTarihi { get; set; } = DateTime.Now.AddDays(1);
        public bool BayiyeIletildiMi { get; set; } = true;
        public DateTime? SonIletimZamani { get; set; } = DateTime.Now;
    }

    public class BayiTalepSikayetKaydi
    {
        public string TalepNo { get; set; } = "";
        public string BayiId { get; set; } = "";
        public string BayiAdi { get; set; } = "";
        public string CariKodu { get; set; } = "";
        public string Baslik { get; set; } = "";
        public string Kategori { get; set; } = "Kargo & Sevkiyat"; // Kargo Eksik/Hasar, Sanal POS Hata, Fatura/Cari, Yazılım Aktivasyon, Genel Şikayet
        public string Oncelik { get; set; } = "Normal"; // Düşük, Normal, Yüksek, Acil
        public string Durum { get; set; } = "İnceleniyor"; // Yeni Geldi, İnceleniyor, Bayiden Yanıt Bekleniyor, Çözüldü & Kapatıldı
        public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
        public DateTime SonGuncellemeTarihi { get; set; } = DateTime.Now;
        public string Mesaj { get; set; } = "";
        public string? DosyaEki { get; set; }
        public string? AdminCevap { get; set; }
        public string? AtananPersonel { get; set; } = "Müşteri Hizmetleri";
    }

    public class BayiOdemeKaydi
    {
        public string IslemNo { get; set; } = "";
        public string BayiId { get; set; } = "";
        public string BayiAdi { get; set; } = "";
        public string CariKodu { get; set; } = "";
        public string OdemeTuru { get; set; } = "Sanal POS (3D Secure)"; // Sanal POS, Havale / EFT, DBS Tahsilat, Çek / Senet
        public DateTime OdemeTarihi { get; set; } = DateTime.Now;
        public decimal TutarTL { get; set; }
        public string DekontBankaReferans { get; set; } = "";
        public string BankaAdi { get; set; } = "Garanti BBVA";
        public decimal KalanCariBakiyeTL { get; set; }
        public string Durum { get; set; } = "Onaylandı & Muhasebeleşti"; // Onaylandı, Provizyonda, Beklemede, İade Edildi
        public string? Aciklama { get; set; }
    }

    public class BayiSatisKaydi
    {
        public string SatisNo { get; set; } = "";
        public string FaturaNo { get; set; } = "";
        public string BayiId { get; set; } = "";
        public string BayiAdi { get; set; } = "";
        public string CariKodu { get; set; } = "";
        public DateTime SatisTarihi { get; set; } = DateTime.Now;
        public string SatilanUrunlerOzet { get; set; } = "";
        public string Kategori { get; set; } = "Donanım"; // Donanım, Yazılım Lisans, Sarf Malzeme, Paket
        public int ToplamAdet { get; set; }
        public decimal BirimFiyatTL { get; set; }
        public decimal IskontoOrani { get; set; }
        public decimal ToplamTutarTL { get; set; }
        public string SevkiyatDurumu { get; set; } = "Teslim Edildi"; // Teslim Edildi, Kargoda, Depoda Hazırlanıyor
        public string KargoTakipNo { get; set; } = "";
        public string FaturaDurumu { get; set; } = "E-Fatura Kesildi (GİB Onaylı)";
    }

    public class BozukCihazSwapKaydi
    {
        public string SwapNo { get; set; } = "";
        public string BayiId { get; set; } = "";
        public string BayiAdi { get; set; } = "";
        public string CariKodu { get; set; } = "";
        public string BozukCihazModeli { get; set; } = "";
        public string BozukCihazSeriNo { get; set; } = "";
        public string ArizaNedeni { get; set; } = "";
        public string DegisimTuru { get; set; } = "DOA (İlk 30 Gün Birebir Sıfır)"; // DOA Sıfır Değişim, Garanti Kapsamı Swap, Ücretli Yenilenmiş
        public string VerilenYeniCihazSeriNo { get; set; } = "";
        public DateTime TalepTarihi { get; set; } = DateTime.Now;
        public DateTime? DegisimTarihi { get; set; }
        public string Durum { get; set; } = "Onaylandı & Yeni Cihaz Sevk Edildi"; // Onaylandı, İncelemede, Cihaz Bekleniyor, Reddedildi
        public string OnaylayanAdmin { get; set; } = "Servis Müdürü";
        public string? KargoTakipNo { get; set; }
    }

    public class BayiYonetimMerkeziViewModel
    {
        public string AktifTab { get; set; } = "hepsi"; // hepsi, bildirim, servis, talepler, odemeler, satislar, degisim
        public string? SeciliBayiId { get; set; }
        public string? AramaKelimesi { get; set; }
        public List<ApplicationUser> Bayiler { get; set; } = new();

        public List<DuyuruBildirimKaydi> Duyurular { get; set; } = new();
        public List<ServisAracCihazKaydi> ServisKayitlari { get; set; } = new();
        public List<BayiTalepSikayetKaydi> Talepler { get; set; } = new();
        public List<BayiOdemeKaydi> Odemeler { get; set; } = new();
        public List<BayiSatisKaydi> Satislar { get; set; } = new();
        public List<BozukCihazSwapKaydi> SwapKayitlari { get; set; } = new();

        // Metrikler
        public int ToplamBayiSayisi { get; set; }
        public int ToplamDuyuruSayisi { get; set; }
        public int ServistekiAracCihazSayisi { get; set; }
        public int BekleyenTalepSayisi { get; set; }
        public decimal ToplamTahsilatTL { get; set; }
        public decimal ToplamSatisCiroyuTL { get; set; }
        public int AktifBozukCihazSwapSayisi { get; set; }

        // Eski Müşteriler & Pasif Bayiler (Yazılım, Donanım veya İkisi)
        public List<EskiBayiMusteriKaydi> EskiMusteriler { get; set; } = new();
        public int ToplamEskiMusteriSayisi { get; set; }
        public int EskiMusteriYazilimSayisi { get; set; }
        public int EskiMusteriDonanimSayisi { get; set; }
        public int EskiMusteriIkisiSayisi { get; set; }
        public decimal EskiMusterilerdenToplamOdenenTL { get; set; }
    }

    public class EskiBayiMusteriKaydi
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpper();
        public string FirmaVeyaKisiAdi { get; set; } = "";
        public string? YetkiliAdSoyad { get; set; }
        public string CariKodu { get; set; } = "";
        public string MusteriTipi { get; set; } = "Eski Bayi"; // "Eski Bayi", "Eski B2B Müşteri", "Aboneliği Biten Bireysel/Firma"
        public string Kategori { get; set; } = "Donanım + Yazılım (İkisi)"; // "Yazılım", "Donanım", "Donanım + Yazılım (İkisi)"
        public string Telefon { get; set; } = "";
        public string Eposta { get; set; } = "";
        public string Sehir { get; set; } = "";
        
        // Neler Aldı? (Ürünler, Yazılımlar, Donanımlar)
        public string NelerAldiDetay { get; set; } = "";
        
        // Ne Kadar Ödedi?
        public decimal OdenenToplamTutarTL { get; set; }
        public string FaturaDekontOzet { get; set; } = "";
        
        // Ne Zaman Aldı?
        public DateTime AlisTarihi { get; set; }
        
        // Ne Zaman Bitti?
        public DateTime BitisTarihi { get; set; }
        
        // Bitiş / Ayrılış Nedeni
        public string BitisNedeni { get; set; } = "Lisans yenilemesi yapılmadı";
        public string AktiflikDurumu { get; set; } = "Artık Müşterimiz Değil (Pasif)";
        public string GeriKazanimDurumu { get; set; } = "Teklif Hazırlanabilir"; // "Teklif Gönderildi", "Teklif Hazırlanabilir", "Ulaşılamadı", "Sektör Değiştirdi"
        public string? SonIletisimNotu { get; set; }
    }

    public class B2BSepetTalepModel
    {
        public List<B2BSepetItemModel> Urunler { get; set; } = new();
        public decimal GenelToplam { get; set; }
        public string? SiparisNotu { get; set; }
    }

    public class B2BSepetItemModel
    {
        public string? Id { get; set; }
        public string Baslik { get; set; } = "";
        public string? Kategori { get; set; }
        public int Adet { get; set; } = 1;
        public decimal Fiyat { get; set; }
        public decimal Toplam => Adet * Fiyat;
    }

    public class AdminDemoTalepKaydi
    {
        public string TalepNo { get; set; } = "";
        public string BayiId { get; set; } = "";
        public string BayiAdi { get; set; } = "";
        public string CariKodu { get; set; } = "";
        public string CihazModeli { get; set; } = "";
        public string MusteriAdi { get; set; } = "";
        public int GunSure { get; set; } = 15;
        public DateTime TalepTarihi { get; set; } = DateTime.Now;
        public string KargoTakipNo { get; set; } = "";
        public string Durum { get; set; } = "Müşteride (Test Aşamasında)";
        public string? AdminNotu { get; set; }
    }

    public class AdminPazarlamaMateryalKaydi
    {
        public string Id { get; set; } = "";
        public string Baslik { get; set; } = "";
        public string Format { get; set; } = "PDF";
        public string Boyut { get; set; } = "24.5 MB";
        public string Tur { get; set; } = "Katalog";
        public string OnizlemeUrl { get; set; } = "/images/donanim.jpg";
        public string DosyaUrl { get; set; } = "/images/donanim.jpg";
        public DateTime EklenmeTarihi { get; set; } = DateTime.Now;
        public int IndirilmeSayisi { get; set; } = 0;
    }
}


