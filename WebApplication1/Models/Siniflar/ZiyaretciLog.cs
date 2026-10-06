namespace WebApplication1.Models.Siniflar
{
    public class ZiyaretciLog
    {
        public int Id { get; set; }
        public string? SayfaYolu { get; set; }
        public string? IpAdresi { get; set; }
        public string? UserAgent { get; set; }
        public DateTime ZiyaretTarihi { get; set; } = DateTime.Now;
    }
}
