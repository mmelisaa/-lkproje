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
