using WebApplication1.Services;

namespace WebApplication1.Middleware
{
    /// <summary>
    /// Panelden tanımlanan sayfa adreslerini (slug) uygular:
    ///  - /hizmetlerimiz  → dahili olarak /Home/Hizmetlerimiz sayfasına çevrilir
    ///  - /Home/Hizmetlerimiz → aynı içeriğin tek adreste yayınlanması için 301 ile /hizmetlerimiz adresine yönlendirilir
    /// </summary>
    public class SeoSlugMiddleware
    {
        private readonly RequestDelegate _next;

        public SeoSlugMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context, ISeoService seo)
        {
            var metot = context.Request.Method;
            if (HttpMethods.IsGet(metot) || HttpMethods.IsHead(metot))
            {
                var yol = context.Request.Path.Value ?? "/";
                if (yol.Length > 1)
                {
                    var kayitlar = (await seo.TumuAsync()).Where(k => k.Aktif && !string.IsNullOrWhiteSpace(k.Slug)).ToList();
                    if (kayitlar.Count > 0)
                    {
                        var kirpilmis = yol.Trim('/');

                        var slugEslesen = kayitlar.FirstOrDefault(k => string.Equals(k.Slug, kirpilmis, StringComparison.OrdinalIgnoreCase));
                        if (slugEslesen != null)
                        {
                            context.Request.Path = "/" + slugEslesen.SayfaKodu;
                            context.Items["SeoSlug"] = slugEslesen.Slug;
                            await _next(context);
                            return;
                        }

                        var eskiYol = kayitlar.FirstOrDefault(k => string.Equals("/" + k.SayfaKodu, yol.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));
                        if (eskiYol != null)
                        {
                            context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
                            context.Response.Headers.Location = "/" + eskiYol.Slug + context.Request.QueryString;
                            return;
                        }
                    }
                }
            }

            await _next(context);
        }
    }
}
