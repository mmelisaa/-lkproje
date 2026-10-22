using System.Collections.Concurrent;

namespace WebApplication1.Services
{
    /// <summary>
    /// IP bazlı brute-force koruması. Identity'nin hesap bazlı kilidine (5 hata / 15 dk) ek olarak,
    /// aynı IP'den kısa sürede çok sayıda hatalı giriş denemesi yapılırsa o IP geçici olarak engellenir.
    /// </summary>
    public interface ILoginGuard
    {
        bool EngelliMi(string ip, out TimeSpan kalanSure);
        void Basarisiz(string ip);
        void Basarili(string ip);
    }

    public class LoginGuard : ILoginGuard
    {
        private const int MaxHata = 10;
        private static readonly TimeSpan Pencere = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan EngelSuresi = TimeSpan.FromMinutes(15);

        private sealed class Kayit
        {
            public int Sayac;
            public DateTime Baslangic = DateTime.UtcNow;
            public DateTime? EngelBitis;
        }

        private readonly ConcurrentDictionary<string, Kayit> _kayitlar = new();

        public bool EngelliMi(string ip, out TimeSpan kalanSure)
        {
            kalanSure = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(ip) || ip is "127.0.0.1" or "::1" or "localhost" or "bilinmiyor")
            {
                return false;
            }
            if (!_kayitlar.TryGetValue(ip, out var k)) return false;
            lock (k)
            {
                if (k.EngelBitis is { } bitis && bitis > DateTime.UtcNow)
                {
                    kalanSure = bitis - DateTime.UtcNow;
                    return true;
                }
            }
            return false;
        }

        public void Basarisiz(string ip)
        {
            var k = _kayitlar.GetOrAdd(ip, _ => new Kayit());
            lock (k)
            {
                if (DateTime.UtcNow - k.Baslangic > Pencere || (k.EngelBitis is { } b && b <= DateTime.UtcNow))
                {
                    k.Sayac = 0;
                    k.Baslangic = DateTime.UtcNow;
                    k.EngelBitis = null;
                }
                k.Sayac++;
                if (k.Sayac >= MaxHata) k.EngelBitis = DateTime.UtcNow.Add(EngelSuresi);
            }
        }

        public void Basarili(string ip) => _kayitlar.TryRemove(ip, out _);
    }
}
