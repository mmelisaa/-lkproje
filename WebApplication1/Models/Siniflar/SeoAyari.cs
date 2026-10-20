using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.Siniflar
{
    /// <summary>Her herkese açık sayfa için panelden düzenlenen SEO bilgileri ve URL (slug).</summary>
    public class SeoAyari
    {
        public int Id { get; set; }

        /// <summary>Örn: Home/Hizmetlerimiz (Controller/Action)</summary>
        [Required, StringLength(100)]
        public string SayfaKodu { get; set; } = "";

        [Required, StringLength(120)]
        public string SayfaAdi { get; set; } = "";

        /// <summary>Örn: hizmetlerimiz. Boşsa sayfa varsayılan MVC adresinden yayınlanır.</summary>
        [StringLength(120)]
        public string? Slug { get; set; }

        [StringLength(70)]
        public string? MetaBaslik { get; set; }

        [StringLength(320)]
        public string? MetaAciklama { get; set; }

        [StringLength(300)]
        public string? AnahtarKelimeler { get; set; }

        [StringLength(400)]
        public string? OgGorselUrl { get; set; }

        [StringLength(40)]
        public string Robots { get; set; } = "index, follow";

        public bool Aktif { get; set; } = true;
        public DateTime GuncellemeTarihi { get; set; } = DateTime.Now;
        public string? GuncelleyenKullanici { get; set; }
    }
}
