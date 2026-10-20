namespace WebApplication1.Models.Siniflar
{
    /// <summary>
    /// Denetim izi (audit trail): hangi kullanıcı, ne zaman, hangi tablodaki hangi kaydı nasıl değiştirdi.
    /// ApplicationDbContext.SaveChanges içinde otomatik üretilir.
    /// </summary>
    public class DenetimKaydi
    {
        public int Id { get; set; }
        public DateTime Zaman { get; set; } = DateTime.Now;
        public string KullaniciAdi { get; set; } = "";
        public string? Rol { get; set; }
        public string? IpAdresi { get; set; }
        public string? BilgisayarCihaz { get; set; }

        /// <summary>Ekleme / Güncelleme / Silme</summary>
        public string Eylem { get; set; } = "";
        public string TabloAdi { get; set; } = "";
        public string? KayitId { get; set; }
        public string? Ozet { get; set; }
        public string? EskiDeger { get; set; }
        public string? YeniDeger { get; set; }
    }
}
