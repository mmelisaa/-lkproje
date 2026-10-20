using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.Siniflar
{
    public class SiteMesaj
    {
        public int Id { get; set; }

        [Required]
        public string AdSoyad { get; set; } = "";

        [Required]
        public string Eposta { get; set; } = "";

        public string? Telefon { get; set; }
        public string? Konu { get; set; }
        public string? Mesaj { get; set; }
        public string? Pozisyon { get; set; }
        public string? OnYazi { get; set; }

        /// <summary>
        /// İletişim, İşBaşvurusu, TeklifTalebi, İşbirliği
        /// </summary>
        [Required]
        public string FormTipi { get; set; } = "İletişim";

        public bool Okundu { get; set; } = false;
        public DateTime GonderimTarihi { get; set; } = DateTime.Now;

        // ---- Mini CRM alanları ----
        /// <summary>Yeni / İnceleniyor / Cevaplandı / Kapatıldı</summary>
        [StringLength(20)]
        public string Durum { get; set; } = CrmDurumlari.Yeni;

        [StringLength(100)]
        public string? AtananKisi { get; set; }

        /// <summary>Yalnızca personelin gördüğü, tarih damgalı dahili notlar.</summary>
        public string? DahiliNotlar { get; set; }

        public string? CevapMetni { get; set; }
        public DateTime? CevapTarihi { get; set; }
        public DateTime? SonIslemTarihi { get; set; }
    }

    public static class CrmDurumlari
    {
        public const string Yeni = "Yeni";
        public const string Inceleniyor = "İnceleniyor";
        public const string Cevaplandi = "Cevaplandı";
        public const string Kapatildi = "Kapatıldı";

        public static readonly string[] Hepsi = { Yeni, Inceleniyor, Cevaplandi, Kapatildi };
    }
}
