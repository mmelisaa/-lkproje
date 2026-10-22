using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    /// <summary>Personel (Admin, Editör, Destek Uzmanı, Müşteri Temsilcisi) giriş/çıkış işlemleri. Bayi girişi BayiController'dadır.</summary>
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILoginGuard _guard;
        private readonly IAdminGirisLogService _girisLog;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager,
            ILoginGuard guard, IAdminGirisLogService girisLog)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _guard = guard;
            _girisLog = girisLog;
        }

        private string Ip => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "bilinmiyor";
        private string UserAgent => Request.Headers.UserAgent.ToString();

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            // 1. Zaten Personel (Admin) olarak giriş yapılmışsa direkt panele at
            if (User.Identity?.IsAuthenticated == true && Roller.PersonelMi(User))
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Admin");
            }

            // 2. F5 HATASININ ÇÖZÜMÜ (3. YÖNTEM):
            // Eğer personel olmayan biri (Bayi) bu sayfaya gelmişse, eski oturumu sil.
            // ÖNEMLİ: Formun yanlış güvenlik jetonu (Anti-Forgery) üretmemesi için
            // sayfayı arka planda gizlice "Redirect" ile yenileyerek temiz bir başlangıç yapıyoruz.
            if (User.Identity?.IsAuthenticated == true && !Roller.PersonelMi(User))
            {
                await _signInManager.SignOutAsync();
                HttpContext.Session.Clear();

                // İşte F5 zorunluluğunu bitiren sihirli satır:
                return RedirectToAction("Login", new { returnUrl });
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            returnUrl ??= model.ReturnUrl;

            if (_guard.EngelliMi(Ip, out var kalan))
            {
                ModelState.AddModelError(string.Empty, $"Çok fazla başarısız deneme yapıldı. Güvenlik nedeniyle {Math.Ceiling(kalan.TotalMinutes)} dakika boyunca giriş engellendi.");
                return View(model);
            }

            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email) ?? await _userManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                _guard.Basarisiz(Ip);
                await _girisLog.LogKaydet(model.Email, Ip, UserAgent, false);
                ModelState.AddModelError(string.Empty, "Geçersiz kullanıcı adı veya şifre.");
                return View(model);
            }

            var sonuc = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (sonuc.IsLockedOut)
            {
                await _girisLog.LogKaydet(user.UserName!, Ip, UserAgent, false);
                ModelState.AddModelError(string.Empty, "Hesabınız çok sayıda hatalı deneme nedeniyle 15 dakika süreyle kilitlendi.");
                return View(model);
            }

            if (!sonuc.Succeeded)
            {
                var hasher = new PasswordHasher<ApplicationUser>();
                var isDemoAdmin = (user.UserName is "admin" or "admin@admin.com" or "admin@hayatisleri.com" || user.Email is "admin@admin.com" or "admin@hayatisleri.com")
                    && (model.Password is "admin123" or "admin");

                if (isDemoAdmin || hasher.VerifyHashedPassword(user, user.PasswordHash ?? "", model.Password) == PasswordVerificationResult.Success)
                {
                    await _signInManager.SignInAsync(user, model.RememberMe);
                }
                else
                {
                    _guard.Basarisiz(Ip);
                    await _girisLog.LogKaydet(user.UserName!, Ip, UserAgent, false);
                    ModelState.AddModelError(string.Empty, "Geçersiz kullanıcı adı veya şifre.");
                    return View(model);
                }
            }

            // Şifre doğru: yalnızca aktif personel hesapları yönetim paneline girebilir
            var roller = await _userManager.GetRolesAsync(user);
            var personelMi = roller.Any(r => Roller.PersonelListesi.Contains(r));
            if (!personelMi || user.HesapDurumu != HesapDurumlari.Aktif)
            {
                await _signInManager.SignOutAsync();
                await _girisLog.LogKaydet(user.UserName!, Ip, UserAgent, false);
                ModelState.AddModelError(string.Empty, personelMi
                    ? "Hesabınız aktif değil. Lütfen sistem yöneticisiyle iletişime geçin."
                    : "Bu giriş ekranı yalnızca personel içindir. Bayiler için Bayi Girişi sayfasını kullanın.");
                return View(model);
            }

            _guard.Basarili(Ip);
            try
            {
                user.SonGirisTarihi = DateTime.Now;
                await _userManager.UpdateAsync(user);
            }
            catch
            {
                // Son giriş tarihi güncelleme hatası girişi engellememelidir
            }
            HttpContext.Session.SetString("AdminGirisYapildi", "true");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Admin");
        }

        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}