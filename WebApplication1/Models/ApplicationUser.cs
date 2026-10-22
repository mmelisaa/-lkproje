using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Models
{
    /// <summary>
    /// Hayat İşleri B2B projesi için genişletilmiş kullanıcı. Standart IdentityUser'a
    /// bayi/firma bilgileri, cari kodu, iskonto oranı ve admin onaylı hesap durumu eklenmiştir.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        public string? AdSoyad { get; set; }
        public string? FirmaAdi { get; set; }
        public string? VergiDairesi { get; set; }
        public string? VergiNo { get; set; }
        public string? CariKodu { get; set; }
        public string? Sehir { get; set; }

        /// <summary>Bayi iskonto oranı (yüzde, 0-100).</summary>
        public decimal IskontoOrani { get; set; }

        /// <summary>
        /// 1 = Donanım ve POS Bayisi Paneli (Zebra, Sunmi, Datalogic, KodPOS satanlar)
        /// 2 = Yazılım ve ERP Bayisi Paneli (HiCariTakip kullananlar/satanlar)
        /// 3 = Tam Yetkili Çözüm Ortağı Paneli (Donanım + Yazılım Bir Arada)
        /// </summary>
        public int BayiPortalTipi { get; set; } = 1;

        /// <summary>Beklemede / Aktif / Askıda / Reddedildi. Yalnızca "Aktif" hesaplar giriş yapabilir.</summary>
        public string HesapDurumu { get; set; } = HesapDurumlari.Beklemede;

        public DateTime KayitTarihi { get; set; } = DateTime.Now;
        public DateTime? OnayTarihi { get; set; }
        public string? OnaylayanKullanici { get; set; }
        public DateTime? SonGirisTarihi { get; set; }
        public string? YoneticiNotu { get; set; }

        // =====================================================================
        // BAYİ ALT KULLANICI (SUB-USER) HİYERARŞİSİ VE GÜVENLİK
        // =====================================================================
        /// <summary>
        /// Eğer bu kullanıcı bir alt kullanıcı ise, bağlı olduğu ana bayinin User.Id değeri.
        /// Boş ise bu kullanıcı Ana Bayi (Master User) rolündedir.
        /// </summary>
        public string? AnaBayiId { get; set; }

        public bool IsAltKullanici => !string.IsNullOrEmpty(AnaBayiId);

        /// <summary>Alt kullanıcının görev veya departman unvanı (Örn: Muhasebe Yetkilisi, Depo Sorumlusu, Saha Satış vb.)</summary>
        public string? Unvan { get; set; }

        /// <summary>
        /// Şifre unutulduğunda Admin'in bayiye güvenle iletebilmesi için AES-256 ile şifrelenmiş parola.
        /// Kesinlikle düz metin (plain text) olarak tutulmaz.
        /// </summary>
        public string? SifreKriptolu { get; set; }

        // =====================================================================
        // AYRINTILI MODÜL VE MENÜ ERİŞİM İZİNLERİ (GRANULAR PERMISSIONS)
        // Yalnızca Ana Bayi yetkisi ile yönetilir.
        // =====================================================================
        /// <summary>Cari bakiye, fatura ekstresi ve 3D Secure Sanal POS ödemesi izni (Varsayılan: Kapalı)</summary>
        public bool IzinFinansSanalPos { get; set; } = false;

        /// <summary>Giriş ve işlem denetim izi (Audit Log) görme izni (Varsayılan: Kapalı)</summary>
        public bool IzinDenetimIzi { get; set; } = false;

        /// <summary>Sarf malzeme, donanım satın alma ve hızlı sepet siparişi verme izni (Varsayılan: Kapalı)</summary>
        public bool IzinSiparisVeSarf { get; set; } = false;

        /// <summary>Teknik Servis ve Arızalı Cihaz (RMA) kaydı açma / takip izni</summary>
        public bool IzinTeknikServisRma { get; set; } = true;

        /// <summary>Destek talebi (Ticket) oluşturma ve yanıtlaşma izni</summary>
        public bool IzinDestekTalepleri { get; set; } = true;

        /// <summary>B2B İskontolu Fiyat Listesini inceleme izni</summary>
        public bool IzinFiyatListesi { get; set; } = true;

        /// <summary>Yazılım, Sürücü (Driver) & SDK Kütüphanesine erişim ve indirme izni</summary>
        public bool IzinKutuphaneDriver { get; set; } = true;

        /// <summary>Fırsat ve Proje Kaydı oluşturma izni</summary>
        public bool IzinProjeFirsat { get; set; } = true;

        /// <summary>Alt Müşteri ve Lisans yönetimi izni</summary>
        public bool IzinLisanslar { get; set; } = false;
    }

    public static class HesapDurumlari
    {
        public const string Beklemede = "Beklemede";
        public const string Aktif = "Aktif";
        public const string Askida = "Askıda";
        public const string Reddedildi = "Reddedildi";

        public static readonly string[] Hepsi = { Beklemede, Aktif, Askida, Reddedildi };
    }
}
