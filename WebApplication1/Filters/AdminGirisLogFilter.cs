using Microsoft.AspNetCore.Mvc.Filters;
using WebApplication1.Data;
using WebApplication1.Models.Siniflar;

namespace WebApplication1.Filters
{
    /// <summary>
    /// Admin paneline ilk erişimde (session başına bir kez) giriş logu kaydeder.
    /// </summary>
    public class AdminGirisLogFilter : IAsyncActionFilter
    {
        private readonly ApplicationDbContext _context;

        public AdminGirisLogFilter(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
        {
            var httpCtx = ctx.HttpContext;
            var kullanici = httpCtx.User?.Identity?.Name;

            // Sadece giriş yapmış kullanıcılar için ve henüz bu session'da log yazılmamışsa
            if (!string.IsNullOrEmpty(kullanici))
            {
                var sessionKey = $"AdminGirisLogged_{kullanici}";
                var session = httpCtx.Session;

                if (session.GetString(sessionKey) == null)
                {
                    session.SetString(sessionKey, DateTime.Now.ToString("o"));

                    var ua = httpCtx.Request.Headers.UserAgent.ToString();
                    var log = new AdminGirisLog
                    {
                        KullaniciAdi = kullanici,
                        IpAdresi = httpCtx.Connection.RemoteIpAddress?.ToString() ?? "Bilinmiyor",
                        UserAgent = ua,
                        Tarayici = ParseTarayici(ua),
                        IsletimSistemi = ParseIsletimSistemi(ua),
                        BasariliMi = true,
                        GirisTarihi = DateTime.Now
                    };

                    _context.AdminGirisLoglari.Add(log);
                    await _context.SaveChangesAsync();
                }
            }

            await next();
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
