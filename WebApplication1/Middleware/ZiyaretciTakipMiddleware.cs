using WebApplication1.Data;
using WebApplication1.Models.Siniflar;

namespace WebApplication1.Middleware
{
    public class ZiyaretciTakipMiddleware
    {
        private readonly RequestDelegate _next;

        public ZiyaretciTakipMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
        {
            // Sadece sayfa isteklerini logla (statik dosyaları, API'leri vs. hariç tut)
            var path = context.Request.Path.Value ?? "";
            if (!path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase) &&
                !path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) &&
                !path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase) &&
                !path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase) &&
                !path.StartsWith("/Identity/", StringComparison.OrdinalIgnoreCase) &&
                !path.StartsWith("/Admin/Ziyaretci", StringComparison.OrdinalIgnoreCase) &&
                !path.Contains(".") &&
                context.Request.Method == "GET")
            {
                // KVKK & Çerez Uyumu: Kullanıcı analitik çerez izni vermemişse veya reddetmişse izleme yapma
                var cookieConsent = context.Request.Cookies["hayat_cookie_consent"];
                bool analyticsAllowed = false;
                if (!string.IsNullOrEmpty(cookieConsent))
                {
                    try
                    {
                        var decoded = System.Net.WebUtility.UrlDecode(cookieConsent);
                        analyticsAllowed = decoded.Contains("\"analytics\":true", StringComparison.OrdinalIgnoreCase);
                    }
                    catch
                    {
                        analyticsAllowed = false;
                    }
                }

                if (analyticsAllowed)
                {
                    var log = new ZiyaretciLog
                    {
                        SayfaYolu = path,
                        IpAdresi = context.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = context.Request.Headers.UserAgent.ToString(),
                        ZiyaretTarihi = DateTime.Now
                    };
                    db.ZiyaretciLoglari.Add(log);
                    await db.SaveChangesAsync();
                }
            }

            await _next(context);
        }
    }
}
