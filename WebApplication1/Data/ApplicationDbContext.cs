using System.Text.Json;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using WebApplication1.Models;
using WebApplication1.Models.Siniflar; // Product sınıfının bulunduğu klasör
using WebApplication1.Models.Siniflar.Kurumsal;

namespace WebApplication1.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor? httpContextAccessor = null)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<Product> Products { get; set; } // Yeni Eklenen Ürünler Tablosu
        public DbSet<BlogPost> BlogPosts { get; set; }
        public DbSet<Hakkimizda> Hakkimizda { get; set; }
        public DbSet<ReferansProje> ReferansProjeler { get; set; }
        public DbSet<InsanKaynaklari> InsanKaynaklari { get; set; }
        public DbSet<HesapNumaralarimiz> HesapNumaralarimiz { get; set; }
        public DbSet<BankaBilgisi> BankaBilgileri { get; set; }
        public DbSet<SiteMesaj> SiteMesajlari { get; set; }
        public DbSet<ZiyaretciLog> ZiyaretciLoglari { get; set; }
        public DbSet<AdminGirisLog> AdminGirisLoglari { get; set; }
        public DbSet<DenetimKaydi> DenetimKayitlari { get; set; }
        public DbSet<SeoAyari> SeoAyarlari { get; set; }
        public DbSet<RmaKaydi> RmaKayitlari { get; set; }
        public DbSet<RmaHareket> RmaHareketleri { get; set; }
        public DbSet<BayiOzelKampanya> BayiOzelKampanyalari { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(e =>
            {
                e.Property(x => x.IskontoOrani).HasPrecision(5, 2);
                e.Property(x => x.HesapDurumu).HasMaxLength(20);
                e.Property(x => x.CariKodu).HasMaxLength(30);
                e.Property(x => x.VergiNo).HasMaxLength(20);
                e.HasIndex(x => x.CariKodu).IsUnique().HasFilter("[CariKodu] IS NOT NULL");
            });

            builder.Entity<SeoAyari>(e =>
            {
                e.HasIndex(x => x.SayfaKodu).IsUnique();
                e.HasIndex(x => x.Slug).IsUnique().HasFilter("[Slug] IS NOT NULL");
            });

            builder.Entity<RmaKaydi>(e =>
            {
                e.HasIndex(x => x.RmaNo).IsUnique();
                e.Property(x => x.Ucret).HasPrecision(18, 2);
                e.HasMany(x => x.Hareketler).WithOne().HasForeignKey(h => h.RmaKaydiId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<DenetimKaydi>(e =>
            {
                e.HasIndex(x => x.Zaman);
                e.HasIndex(x => x.TabloAdi);
                e.Property(x => x.KullaniciAdi).HasMaxLength(256);
                e.Property(x => x.Eylem).HasMaxLength(30);
                e.Property(x => x.TabloAdi).HasMaxLength(100);
                e.Property(x => x.KayitId).HasMaxLength(100);
                e.Property(x => x.Ozet).HasMaxLength(600);
                e.Property(x => x.BilgisayarCihaz).HasMaxLength(250);
            });

            builder.Entity<BayiOzelKampanya>(e =>
            {
                e.Property(x => x.IskontoOrani).HasPrecision(5, 2);
                e.Property(x => x.OzelFiyatTL).HasPrecision(18, 2);
                e.HasIndex(x => x.BayiId);
            });
        }

        // =====================================================================
        //  DENETİM İZİ (AUDIT LOG): Her SaveChanges çağrısında kim, ne zaman,
        //  hangi tablodaki hangi kaydı değiştirdi otomatik olarak yazılır.
        // =====================================================================
        private static readonly HashSet<Type> DenetimDisi = new()
        {
            typeof(DenetimKaydi), typeof(ZiyaretciLog), typeof(AdminGirisLog), typeof(RmaHareket)
        };

        // Her girişte değişen, denetim açısından anlamsız alanlar
        private static readonly HashSet<string> Gurultu = new(StringComparer.OrdinalIgnoreCase)
        {
            "AccessFailedCount", "LockoutEnd", "SecurityStamp", "ConcurrencyStamp", "SonGirisTarihi"
        };

        private static readonly HashSet<string> Gizli = new(StringComparer.OrdinalIgnoreCase) { "PasswordHash" };

        private sealed class DenetimTaslagi
        {
            public EntityEntry Entry { get; init; } = null!;
            public string Tablo { get; init; } = "";
            public string Eylem { get; init; } = "";
            public string? KayitId { get; set; }
            public string? Eski { get; init; }
            public string? Yeni { get; init; }
            public string? DegisenAlanlar { get; init; }
        }

        private List<DenetimTaslagi> DenetimTopla()
        {
            var liste = new List<DenetimTaslagi>();

            // HTTP isteği dışındaki işlemler (başlangıç tohum verisi gibi) denetim kaydına yazılmaz.
            if (_httpContextAccessor?.HttpContext == null) return liste;

            ChangeTracker.DetectChanges();
            var entries = ChangeTracker.Entries()
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .Where(e => !DenetimDisi.Contains(e.Entity.GetType()))
                .ToList();

            foreach (var e in entries)
            {
                var eski = new Dictionary<string, object?>();
                var yeni = new Dictionary<string, object?>();

                foreach (var p in e.Properties)
                {
                    var ad = p.Metadata.Name;
                    if (Gurultu.Contains(ad)) continue;
                    var gizli = Gizli.Contains(ad);
                    object? Maske(object? v) => gizli ? "***" : v;

                    switch (e.State)
                    {
                        case EntityState.Added:
                            if (!p.IsTemporary) yeni[ad] = Maske(p.CurrentValue);
                            break;
                        case EntityState.Deleted:
                            eski[ad] = Maske(p.OriginalValue);
                            break;
                        case EntityState.Modified:
                            if (p.IsModified && !Equals(p.OriginalValue, p.CurrentValue))
                            {
                                eski[ad] = Maske(p.OriginalValue);
                                yeni[ad] = Maske(p.CurrentValue);
                            }
                            break;
                    }
                }

                if (e.State == EntityState.Modified && yeni.Count == 0) continue;

                liste.Add(new DenetimTaslagi
                {
                    Entry = e,
                    Tablo = e.Metadata.GetTableName() ?? e.Entity.GetType().Name,
                    Eylem = e.State switch
                    {
                        EntityState.Added => "Ekleme",
                        EntityState.Deleted => "Silme",
                        _ => "Güncelleme"
                    },
                    KayitId = e.State == EntityState.Added ? null : AnahtarOku(e, orijinal: e.State == EntityState.Deleted),
                    Eski = eski.Count > 0 ? JsonSerializer.Serialize(eski) : null,
                    Yeni = yeni.Count > 0 ? JsonSerializer.Serialize(yeni) : null,
                    DegisenAlanlar = e.State == EntityState.Modified ? string.Join(", ", yeni.Keys) : null
                });
            }

            return liste;
        }

        private static string? AnahtarOku(EntityEntry e, bool orijinal = false)
        {
            var key = e.Metadata.FindPrimaryKey();
            if (key == null) return null;
            return string.Join("/", key.Properties.Select(kp =>
            {
                var pe = e.Property(kp.Name);
                return (orijinal ? pe.OriginalValue : pe.CurrentValue)?.ToString();
            }));
        }

        private void DenetimYaz(List<DenetimTaslagi> taslaklar)
        {
            var ctx = _httpContextAccessor!.HttpContext!;
            var kullanici = ctx.User?.Identity?.IsAuthenticated == true ? ctx.User.Identity!.Name! : "Ziyaretçi (anonim)";
            var rol = ctx.User?.Identity?.IsAuthenticated == true
                ? string.Join(", ", ctx.User.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value))
                : null;
            var ip = ctx.Connection.RemoteIpAddress?.ToString();
            var ua = ctx.Request.Headers.UserAgent.ToString();
            var cihaz = CihazBilgisiCoz(ua, ip);

            foreach (var t in taslaklar)
            {
                var id = t.KayitId ?? AnahtarOku(t.Entry);
                var ozet = $"{t.Tablo} #{id} - {t.Eylem}";
                if (!string.IsNullOrEmpty(t.DegisenAlanlar)) ozet += $" (Değişen alanlar: {t.DegisenAlanlar})";

                DenetimKayitlari.Add(new DenetimKaydi
                {
                    Zaman = DateTime.Now,
                    KullaniciAdi = kullanici,
                    Rol = string.IsNullOrWhiteSpace(rol) ? null : rol,
                    IpAdresi = ip,
                    BilgisayarCihaz = cihaz,
                    Eylem = t.Eylem,
                    TabloAdi = t.Tablo,
                    KayitId = id != null && id.Length > 400 ? id[..400] : id,
                    Ozet = ozet,
                    EskiDeger = t.Eski,
                    YeniDeger = t.Yeni
                });
            }
        }

        public static string CihazBilgisiCoz(string? ua, string? ip)
        {
            if (string.IsNullOrWhiteSpace(ua)) return $"Bilinmeyen Cihaz (IP: {ip ?? "Yerel"})";
            string os = "Bilinmeyen OS";
            if (ua.Contains("Windows NT 10.0")) os = "Windows 10/11";
            else if (ua.Contains("Windows")) os = "Windows PC";
            else if (ua.Contains("Macintosh") || ua.Contains("Mac OS")) os = "macOS";
            else if (ua.Contains("Android")) os = "Android Cihaz";
            else if (ua.Contains("iPhone") || ua.Contains("iPad")) os = "iOS Cihaz";
            else if (ua.Contains("Linux")) os = "Linux";

            string tarayici = "Tarayıcı";
            if (ua.Contains("Edg/")) tarayici = "Microsoft Edge";
            else if (ua.Contains("Chrome/")) tarayici = "Google Chrome";
            else if (ua.Contains("Firefox/")) tarayici = "Mozilla Firefox";
            else if (ua.Contains("Safari/") && !ua.Contains("Chrome/")) tarayici = "Apple Safari";

            return $"{os} ({tarayici})";
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            var taslaklar = DenetimTopla();
            var sonuc = base.SaveChanges(acceptAllChangesOnSuccess);
            if (taslaklar.Count > 0)
            {
                DenetimYaz(taslaklar);
                base.SaveChanges(true);
            }
            return sonuc;
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            var taslaklar = DenetimTopla();
            var sonuc = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            if (taslaklar.Count > 0)
            {
                DenetimYaz(taslaklar);
                await base.SaveChangesAsync(true, cancellationToken);
            }
            return sonuc;
        }
    }
}