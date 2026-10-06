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
    }
}
