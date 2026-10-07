using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace WebApplication1.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendFormNotificationAsync(string formTipi, string adSoyad, string eposta, string? telefon, string? konu, string? pozisyon, string? mesaj, string? onYazi)
        {
            try
            {
                var receiverEmail = _configuration["EmailSettings:ReceiverEmail"] ?? "berkayevrann.1903@gmail.com";
                var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPortStr = _configuration["EmailSettings:SmtpPort"] ?? "465";
                int.TryParse(smtpPortStr, out int smtpPort);
                if (smtpPort == 0) smtpPort = 465;

                var senderEmail = _configuration["EmailSettings:SenderEmail"]?.Trim();
                var senderPassword = _configuration["EmailSettings:SenderPassword"]?.Replace(" ", "").Trim();

                // E-Posta şablonunu oluştur (Modern HTML E-Posta)
                var htmlBody = $@"
                <div style='font-family: Arial, sans-serif; background-color: #0b0c10; color: #ffffff; padding: 30px; border-radius: 12px; max-width: 650px; margin: 0 auto; border: 1px solid #1f293d;'>
                    <div style='text-align: center; padding-bottom: 20px; border-bottom: 1px solid #1f293d;'>
                        <span style='background-color: #2563eb; color: #ffffff; padding: 6px 14px; border-radius: 20px; font-size: 12px; font-weight: bold; text-transform: uppercase;'>{formTipi} Bildirimi</span>
                        <h2 style='color: #38bdf8; margin-top: 15px;'>Web Sitesinden Yeni Form Alındı</h2>
                    </div>
                    <div style='padding: 20px 0;'>
                        <table style='width: 100%; border-collapse: collapse;'>
                            <tr>
                                <td style='padding: 10px; color: #94a3b8; font-weight: bold; width: 140px;'>Gönderen:</td>
                                <td style='padding: 10px; color: #ffffff; font-weight: bold;'>{adSoyad}</td>
                            </tr>
                            <tr>
                                <td style='padding: 10px; color: #94a3b8; font-weight: bold;'>E-Posta:</td>
                                <td style='padding: 10px; color: #38bdf8;'>{eposta}</td>
                            </tr>
                            {(string.IsNullOrEmpty(telefon) ? "" : $@"
                            <tr>
                                <td style='padding: 10px; color: #94a3b8; font-weight: bold;'>Telefon:</td>
                                <td style='padding: 10px; color: #ffffff;'>{telefon}</td>
                            </tr>")}
                            {(string.IsNullOrEmpty(pozisyon) ? "" : $@"
                            <tr>
                                <td style='padding: 10px; color: #94a3b8; font-weight: bold;'>Başvurulan Pozisyon:</td>
                                <td style='padding: 10px; color: #f59e0b; font-weight: bold;'>{pozisyon}</td>
                            </tr>")}
                            {(string.IsNullOrEmpty(konu) ? "" : $@"
                            <tr>
                                <td style='padding: 10px; color: #94a3b8; font-weight: bold;'>Konu / Hizmet:</td>
                                <td style='padding: 10px; color: #ffffff;'>{konu}</td>
                            </tr>")}
                            <tr>
                                <td style='padding: 10px; color: #94a3b8; font-weight: bold;'>Tarih:</td>
                                <td style='padding: 10px; color: #94a3b8;'>{DateTime.Now:dd.MM.yyyy HH:mm}</td>
                            </tr>
                        </table>

                        {(string.IsNullOrEmpty(onYazi) ? "" : $@"
                        <div style='margin-top: 20px; padding: 15px; background-color: #121826; border-radius: 8px; border-left: 4px solid #f59e0b;'>
                            <strong style='color: #f59e0b; display: block; margin-bottom: 8px;'>Ön Yazı:</strong>
                            <p style='color: #e2e8f0; margin: 0; white-space: pre-line;'>{onYazi}</p>
                        </div>")}

                        {(string.IsNullOrEmpty(mesaj) ? "" : $@"
                        <div style='margin-top: 20px; padding: 15px; background-color: #121826; border-radius: 8px; border-left: 4px solid #38bdf8;'>
                            <strong style='color: #38bdf8; display: block; margin-bottom: 8px;'>Mesaj / Detaylar:</strong>
                            <p style='color: #e2e8f0; margin: 0; white-space: pre-line;'>{mesaj}</p>
                        </div>")}
                    </div>
                    <div style='text-align: center; padding-top: 20px; border-top: 1px solid #1f293d; color: #64748b; font-size: 12px;'>
                        Bu e-posta Hayat İşleri web sitesi form otomasyonu tarafından <strong>{receiverEmail}</strong> adresine gönderilmiştir.
                    </div>
                </div>";

                if (!string.IsNullOrEmpty(senderEmail) && !string.IsNullOrEmpty(senderPassword))
                {
                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress("Hayat İşleri Web", senderEmail));
                    message.To.Add(new MailboxAddress("Yönetici", receiverEmail));
                    message.Subject = $"[{formTipi}] {adSoyad} - Hayat İşleri Bildirimi";

                    var bodyBuilder = new BodyBuilder
                    {
                        HtmlBody = htmlBody
                    };
                    message.Body = bodyBuilder.ToMessageBody();

                    using var client = new SmtpClient();
                    
                    // Port 465 doğrudan SSL, 587 STARTTLS kullanır. Auto hepsini otomatik yönetir.
                    await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.Auto);
                    await client.AuthenticateAsync(senderEmail, senderPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);

                    _logger.LogInformation("E-posta bildirimi MailKit ile {ReceiverEmail} adresine başarıyla gönderildi.", receiverEmail);
                }
                else
                {
                    _logger.LogWarning("SMTP gönderici bilgileri eksik. E-posta: {SenderEmail}", senderEmail);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[EMAIL_SERVICE_HATASI]: " + ex.ToString());
                Console.ResetColor();
                _logger.LogError(ex, "E-posta gönderimi sırasında MailKit hatası oluştu: {Message}", ex.Message);
            }
        }
    }
}
