using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebApplication1.Services;

namespace WebApplication1.Filters
{
    /// <summary>
    /// Herkese açık Home sayfaları için panelden girilen SEO bilgilerini ViewData'ya koyar.
    /// _Layout bu değerleri title/meta/canonical/Open Graph etiketlerinde kullanır.
    /// </summary>
    public class SeoFilter : IAsyncActionFilter
    {
        private readonly ISeoService _seo;

        public SeoFilter(ISeoService seo) => _seo = seo;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.Controller is Controller c &&
                string.Equals(context.RouteData.Values["controller"]?.ToString(), "Home", StringComparison.OrdinalIgnoreCase) &&
                HttpMethods.IsGet(context.HttpContext.Request.Method))
            {
                var action = context.RouteData.Values["action"]?.ToString() ?? "Index";
                var kayit = await _seo.GetirAsync($"Home/{action}");
                if (kayit != null)
                {
                    c.ViewData["SeoBaslik"] = kayit.MetaBaslik;
                    c.ViewData["SeoAciklama"] = kayit.MetaAciklama;
                    c.ViewData["SeoAnahtar"] = kayit.AnahtarKelimeler;
                    c.ViewData["SeoRobots"] = kayit.Robots;
                    c.ViewData["SeoGorsel"] = kayit.OgGorselUrl;
                    c.ViewData["SeoSlug"] = kayit.Slug;
                }
            }

            await next();
        }
    }
}
