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
            if (!path.StartsWith("/lib/") &&
                !path.StartsWith("/css/") &&
                !path.StartsWith("/js/") &&
                !path.StartsWith("/favicon") &&
                !path.StartsWith("/Identity/") &&
                !path.Contains(".") &&
                context.Request.Method == "GET")
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

            await _next(context);
        }
    }
}
