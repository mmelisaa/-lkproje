using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Middleware;
using WebApplication1.Models;
using WebApplication1.Services;

var builder = WebApplication.CreateBuilder(args);

// Kestrel sunucu imzasını gizle
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// B2B kullanıcı modeli (ApplicationUser) + roller: Admin, Editor, DestekUzmani, MusteriTemsilcisi, Bayi.
// AddIdentity kullanıldığı için hazır Identity arayüzü (herkese açık /Identity/Account/Register vb.) yayınlanmaz;
// yeni bayiler yalnızca Bayi Başvuru formu üzerinden gelir ve admin onayına kadar "Beklemede" kalır.
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Hesap aktivasyonu e-posta ile değil, admin onaylı "HesapDurumu" alanıyla yönetilir (bkz. AccountController / BayiController).
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Askıya alınan / rolü değişen kullanıcı en geç 1 dakika içinde oturumdan düşsün
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(1));

// Çerez güvenlik ayarları (Secure, HttpOnly, SameSite) ve oturum zaman aşımı
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = ".HayatIsleri.Auth";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);   // 30 dk hareketsizlikte oturum kapanır
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAdminGirisLogService, AdminGirisLogService>();
builder.Services.AddScoped<ISeoService, SeoService>();
builder.Services.AddSingleton<ILoginGuard, LoginGuard>();
builder.Services.AddScoped<WebApplication1.Filters.AdminGirisLogFilter>();
builder.Services.AddScoped<WebApplication1.Filters.SeoFilter>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<WebApplication1.Filters.SeoFilter>();
});

var app = builder.Build();

// SADECE BU BİLGİSAYAR VE AYNI Wİ-Fİ / YEREL AĞA BAĞLI CİHAZLARIN ERİŞİMİNE İZİN VER
app.Use(async (context, next) =>
{
    var remoteIp = context.Connection.RemoteIpAddress;
    if (remoteIp != null)
    {
        if (remoteIp.IsIPv4MappedToIPv6)
        {
            remoteIp = remoteIp.MapToIPv4();
        }

        // 1. Bilgisayarın kendisi (localhost / 127.0.0.1 / ::1)
        if (System.Net.IPAddress.IsLoopback(remoteIp))
        {
            await next();
            return;
        }

        var ipBytes = remoteIp.GetAddressBytes();

        // 2. Aynı Wi-Fi / Yerel Ağ Cihazları (192.168.x.x, 10.x.x.x, 172.16-31.x.x veya IPv6 Link-Local)
        bool isWifiDevice = (ipBytes.Length == 4 && (
            (ipBytes[0] == 192 && ipBytes[1] == 168) ||
            ipBytes[0] == 10 ||
            (ipBytes[0] == 172 && ipBytes[1] >= 16 && ipBytes[1] <= 31)
        )) || remoteIp.IsIPv6LinkLocal;

        if (!isWifiDevice)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("403 Erişim Reddedildi: Bu web sitesine yalnızca bu bilgisayarın bağlı olduğu Wi-Fi ağı üzerindeki cihazlar erişebilir.");
            return;
        }
    }

    await next();
});

// Güvenlik başlıklarını ekleyen middleware (Development dahil her ortamda çalışır)
app.Use(async (context, next) =>
{
    context.Response.Headers.Remove("X-Frame-Options");
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";

    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

    context.Response.Headers.Remove("Content-Security-Policy");
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; font-src 'self' https://cdn.jsdelivr.net data:; img-src 'self' data: https:; frame-ancestors 'self'; form-action 'self';";

    await next();
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseMiddleware<SeoSlugMiddleware>();
app.UseMiddleware<ZiyaretciTakipMiddleware>();
app.UseSession();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Veritabanını otomatik oluştur; rolleri, demo hesapları, SEO kayıtlarını ve örnek RMA verisini hazırla
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await VeriTohumlama.BaslatAsync(services, app.Environment, app.Configuration);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the DB.");
    }
}

app.Run();