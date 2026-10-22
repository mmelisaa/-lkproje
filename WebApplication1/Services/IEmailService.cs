namespace WebApplication1.Services
{
    public interface IEmailService
    {
        Task SendFormNotificationAsync(string formTipi, string adSoyad, string eposta, string? telefon, string? konu, string? pozisyon, string? mesaj, string? onYazi, string? dosyaYolu = null, string? dosyaAdi = null);

        /// <summary>Herhangi bir alıcıya HTML e-posta gönderir. SMTP yapılandırılmamışsa false döner.</summary>
        Task<bool> SendAsync(string to, string subject, string htmlBody);
    }
}
