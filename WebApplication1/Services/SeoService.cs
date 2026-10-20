using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebApplication1.Data;
using WebApplication1.Models.Siniflar;

namespace WebApplication1.Services
{
    public interface ISeoService
    {
        Task<List<SeoAyari>> TumuAsync();
        Task<SeoAyari?> GetirAsync(string sayfaKodu);
        void Temizle();
    }

    /// <summary>SEO kayıtlarını kısa süreli önbellekte tutar; panelden değişiklik yapılınca Temizle() çağrılır.</summary>
    public class SeoService : ISeoService
    {
        private const string CacheKey = "seo:tumu";
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public SeoService(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<List<SeoAyari>> TumuAsync()
        {
            if (_cache.TryGetValue(CacheKey, out List<SeoAyari>? liste) && liste != null) return liste;

            liste = await _context.SeoAyarlari.AsNoTracking().ToListAsync();
            _cache.Set(CacheKey, liste, TimeSpan.FromMinutes(10));
            return liste;
        }

        public async Task<SeoAyari?> GetirAsync(string sayfaKodu) =>
            (await TumuAsync()).FirstOrDefault(s => s.Aktif && string.Equals(s.SayfaKodu, sayfaKodu, StringComparison.OrdinalIgnoreCase));

        public void Temizle() => _cache.Remove(CacheKey);
    }
}
