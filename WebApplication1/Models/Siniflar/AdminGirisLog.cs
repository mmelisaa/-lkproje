namespace WebApplication1.Models.Siniflar
{
    public class AdminGirisLog
    {
        public int Id { get; set; }
        public string? KullaniciAdi { get; set; }
        public string? IpAdresi { get; set; }
        public string? UserAgent { get; set; }
        public string? Tarayici { get; set; }
        public string? IsletimSistemi { get; set; }
        public bool BasariliMi { get; set; } = true;
        public DateTime GirisTarihi { get; set; } = DateTime.Now;
    }
}
