using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace WebApplication1.Services
{
    /// <summary>
    /// Bayi alt kullanıcılarının şifrelerini Admin panelinde unutulduğunda bayiliğe bildirebilmek üzere
    /// AES-256 (CBC, PKCS7, Random IV) ile kriptografik olarak şifreleyen ve yetkili admin için çözen güvenlik servisi.
    /// </summary>
    public static class SubUserSecurityService
    {
        // 256-bit (32 bytes) statik sunucu anahtarı
        private static readonly byte[] EncryptionKey = Encoding.UTF8.GetBytes("HayatIsleri_B2B_SubUser_Sec_2026");

        public static string EncryptPassword(string plainPassword)
        {
            if (string.IsNullOrEmpty(plainPassword)) return "";

            using var aes = Aes.Create();
            aes.Key = EncryptionKey;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            
            // İlk 16 byte olarak IV kaydedilir
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs, Encoding.UTF8))
            {
                sw.Write(plainPassword);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        public static string DecryptPassword(string cipherText)
        {
            if (string.IsNullOrWhiteSpace(cipherText)) return "Belirtilmedi";

            try
            {
                var fullCipher = Convert.FromBase64String(cipherText);
                if (fullCipher.Length < 16) return "Geçersiz Şifre Formatı";

                using var aes = Aes.Create();
                aes.Key = EncryptionKey;

                var iv = new byte[16];
                Array.Copy(fullCipher, 0, iv, 0, 16);
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(fullCipher, 16, fullCipher.Length - 16);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs, Encoding.UTF8);

                return sr.ReadToEnd();
            }
            catch
            {
                return "Şifre Çözülemedi";
            }
        }
    }
}
