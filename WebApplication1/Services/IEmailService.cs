namespace WebApplication1.Services
{
    public interface IEmailService
    {
        Task SendFormNotificationAsync(string formTipi, string adSoyad, string eposta, string? telefon, string? konu, string? pozisyon, string? mesaj, string? onYazi);
    }
}
