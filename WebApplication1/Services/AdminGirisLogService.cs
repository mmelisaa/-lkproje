using WebApplication1.Data;
using WebApplication1.Models.Siniflar;

namespace WebApplication1.Services
{
    public interface IAdminGirisLogService
    {
        Task LogKaydet(string kullaniciAdi, string? ipAdresi, string? userAgent, bool basarili);
    }

    public class AdminGirisLogService : IAdminGirisLogService
    {
        private readonly ApplicationDbContext _context;

        public AdminGirisLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogKaydet(string kullaniciAdi, string? ipAdresi, string? userAgent, bool basarili)
        {
            var log = new AdminGirisLog
            {
                KullaniciAdi = kullaniciAdi,
                IpAdresi = ipAdresi,
                UserAgent = userAgent,
                Tarayici = ParseTarayici(userAgent),
                IsletimSistemi = ParseIsletimSistemi(userAgent),
                BasariliMi = basarili,
                GirisTarihi = DateTime.Now
            };

            _context.AdminGirisLoglari.Add(log);
            await _context.SaveChangesAsync();
        }

        private static string ParseTarayici(string? ua)
        {
            if (string.IsNullOrEmpty(ua)) return "Bilinmiyor";
            if (ua.Contains("Edg/")) return "Microsoft Edge";
            if (ua.Contains("Chrome/")) return "Chrome";
            if (ua.Contains("Firefox/")) return "Firefox";
            if (ua.Contains("Safari/") && !ua.Contains("Chrome")) return "Safari";
            if (ua.Contains("OPR/") || ua.Contains("Opera/")) return "Opera";
            return "Diğer";
        }

        private static string ParseIsletimSistemi(string? ua)
        {
            if (string.IsNullOrEmpty(ua)) return "Bilinmiyor";
            if (ua.Contains("Windows NT 10.0")) return "Windows 10/11";
            if (ua.Contains("Windows NT 6.3")) return "Windows 8.1";
            if (ua.Contains("Windows NT 6.1")) return "Windows 7";
            if (ua.Contains("Windows")) return "Windows";
            if (ua.Contains("Mac OS X")) return "macOS";
            if (ua.Contains("Linux")) return "Linux";
            if (ua.Contains("Android")) return "Android";
            if (ua.Contains("iPhone") || ua.Contains("iPad")) return "iOS";
            return "Diğer";
        }
    }
}
