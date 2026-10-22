using WebApplication1.Models;

namespace WebApplication1.Services
{
    public static class B2BDocumentGenerator
    {
        public static byte[] GenerateEFaturaPdf(string faturaNo, string? siparisNo, decimal tutar, string? urunOzet, ApplicationUser? user)
        {
            var pdf = new B2BPdfEngine();

            // Üst Arka Plan Banner
            pdf.DrawRect(0, 800, 595.28, 41.89, 0.043, 0.341, 0.816, true); // #0b57d0
            pdf.DrawText("HAYAT ISLERI BILISIM & OTOMASYON SISTEMLERI", 36, 816, 12, true, 1, 1, 1);
            pdf.DrawText("GIB E-ARSIV FATURA / E-FATURA PORTALI", 370, 816, 10, false, 1, 1, 1);

            // Başlık Çerçevesi
            pdf.DrawRect(36, 680, 523.28, 105, 0.98, 0.98, 0.99, true, true, 0.85, 0.87, 0.91, 1);

            // Satıcı Bilgileri
            pdf.DrawText("SATICI:", 46, 768, 9, true, 0.043, 0.341, 0.816);
            pdf.DrawText("Hayat Isleri Kurumsal Yazilim & Bilisim Otomasyon A.S.", 46, 754, 9, true, 0.1, 0.1, 0.1);
            pdf.DrawText("Adres: Izzetpasa Mah. Sehit Yzb. Tahir Cad. No:14 Merkez / Elazig", 46, 742, 8, false, 0.3, 0.3, 0.3);
            pdf.DrawText("Vergi Dairesi: Harput V.D.  |  VKN: 4620194821  |  Mersis: 0462019482100001", 46, 730, 8, false, 0.3, 0.3, 0.3);
            pdf.DrawText("Web: https://hayatisleri.com  |  E-Posta: info@hayatisleri.com  |  Tel: 0501 014 6623", 46, 718, 8, false, 0.3, 0.3, 0.3);
            pdf.DrawText("Ticaret Sicil No: 28412 / Elazig", 46, 706, 8, false, 0.3, 0.3, 0.3);

            // Fatura Meta Bilgileri (Sağ Taraf)
            pdf.DrawRect(355, 688, 196, 90, 0.95, 0.97, 1.0, true, true, 0.75, 0.82, 0.95, 1);
            pdf.DrawText("E-ARSIV FATURA", 365, 764, 11, true, 0.043, 0.341, 0.816);
            pdf.DrawText($"Fatura No: {faturaNo}", 365, 750, 9, true, 0.1, 0.1, 0.1);
            pdf.DrawText($"Tarih: {DateTime.Now:dd.MM.yyyy HH:mm}", 365, 738, 8, false, 0.2, 0.2, 0.2);
            pdf.DrawText($"ETTN: {Guid.NewGuid()}", 365, 726, 7, false, 0.4, 0.4, 0.4);
            pdf.DrawText("Senaryo: E-ARSIV FATURA (GIB Onayli)", 365, 714, 8, false, 0.2, 0.2, 0.2);
            pdf.DrawText("Fatura Tipi: SATIS  |  Para Birimi: TRY", 365, 702, 8, false, 0.2, 0.2, 0.2);

            // Alıcı / Bayi Bilgileri
            pdf.DrawRect(36, 580, 523.28, 88, 0.99, 0.99, 0.99, true, true, 0.85, 0.87, 0.91, 1);
            pdf.DrawText("ALICI / YETKILI BAYI:", 46, 652, 9, true, 0.043, 0.341, 0.816);
            var aliciUnvan = user?.FirmaAdi ?? user?.AdSoyad ?? "Yetkili B2B Bayisi";
            pdf.DrawText(aliciUnvan, 46, 638, 10, true, 0.1, 0.1, 0.1);
            var cariKodu = user?.CariKodu ?? "CARİ-B2B-HYT";
            var tipMetin = user?.BayiPortalTipi switch
            {
                1 => "Donanım & POS Bayisi",
                2 => "Yazılım & ERP Bayisi",
                3 => "Tam Yetkili Çözüm Ortağı",
                _ => "Genel B2B Bayisi"
            };
            pdf.DrawText($"B2B Cari Kodu: {cariKodu}  |  Bayi Tipi: {tipMetin}", 46, 626, 8, false, 0.3, 0.3, 0.3);
            pdf.DrawText($"E-Posta: {user?.Email ?? "bayi@firma.com"}  |  Telefon: {user?.PhoneNumber ?? "0500 000 00 00"}", 46, 614, 8, false, 0.3, 0.3, 0.3);
            pdf.DrawText($"Irsaliye / Siparis No: {siparisNo ?? "HYT-SIP-2026-8812"}  |  Sevk Yontemi: Anlasmali Kargo (Yurtici / Aras)", 46, 602, 8, false, 0.3, 0.3, 0.3);
            pdf.DrawText("Not: Irsaliye yerine gecer. 509 Sira No'lu VUK Genel Tebligi uyarinca duzenlenmistir.", 46, 590, 7.5, false, 0.5, 0.5, 0.5);

            // Mal / Hizmet Kalemleri Tablosu
            double tableTop = 550;
            pdf.DrawRect(36, tableTop - 20, 523.28, 22, 0.043, 0.341, 0.816, true);
            pdf.DrawText("S.NO", 42, tableTop - 14, 8, true, 1, 1, 1);
            pdf.DrawText("MAL / HIZMET ACIKLAMASI", 75, tableTop - 14, 8, true, 1, 1, 1);
            pdf.DrawText("MIKTAR", 320, tableTop - 14, 8, true, 1, 1, 1);
            pdf.DrawText("BIRIM FIYAT", 370, tableTop - 14, 8, true, 1, 1, 1);
            pdf.DrawText("KDV", 440, tableTop - 14, 8, true, 1, 1, 1);
            pdf.DrawText("TOPLAM TUTAR", 480, tableTop - 14, 8, true, 1, 1, 1);

            // Kalem Satırları
            var matrah = Math.Round(tutar / 1.20m, 2);
            var kdv = tutar - matrah;
            var ozet = string.IsNullOrWhiteSpace(urunOzet) ? "B2B Kurumsal Donanim / Lisans Satisi" : urunOzet;

            double rowY = tableTop - 45;
            pdf.DrawRect(36, rowY - 6, 523.28, 26, 0.98, 0.98, 0.99, true, true, 0.9, 0.9, 0.92, 0.5);
            pdf.DrawText("1", 45, rowY + 6, 8, false, 0.2, 0.2, 0.2);

            double wrapY = rowY + 6;
            pdf.DrawWrappedText(ozet, 75, ref wrapY, 230, 8, true, 0.1, 0.1, 0.1, 10);
            pdf.DrawText("1 Paket", 320, rowY + 6, 8, false, 0.2, 0.2, 0.2);
            pdf.DrawText($"{matrah:N2} TL", 370, rowY + 6, 8, false, 0.2, 0.2, 0.2);
            pdf.DrawText("%20", 440, rowY + 6, 8, false, 0.2, 0.2, 0.2);
            pdf.DrawText($"{matrah:N2} TL", 480, rowY + 6, 8, true, 0.1, 0.1, 0.1);

            // Çizgiler ve Toplam Kutusu
            double sumTop = 400;
            pdf.DrawRect(320, sumTop - 80, 239.28, 85, 0.96, 0.97, 0.99, true, true, 0.8, 0.85, 0.92, 1);

            pdf.DrawText("Mal / Hizmet Toplam Tutari:", 330, sumTop - 18, 8.5, false, 0.3, 0.3, 0.3);
            pdf.DrawText($"{matrah:N2} TL", 475, sumTop - 18, 8.5, true, 0.1, 0.1, 0.1);

            pdf.DrawText("Hesaplanan KDV (%20):", 330, sumTop - 36, 8.5, false, 0.3, 0.3, 0.3);
            pdf.DrawText($"{kdv:N2} TL", 475, sumTop - 36, 8.5, true, 0.1, 0.1, 0.1);

            pdf.DrawLine(330, sumTop - 46, 550, sumTop - 46, 0.8, 0.8, 0.8, 1);

            pdf.DrawText("ODENECEK GENEL TOPLAM:", 330, sumTop - 64, 9.5, true, 0.043, 0.341, 0.816);
            pdf.DrawText($"{tutar:N2} TL", 470, sumTop - 64, 11, true, 0.043, 0.341, 0.816);

            // Sol Taraf Bilgi ve Güvenlik Kaşesi
            pdf.DrawRect(36, sumTop - 80, 270, 85, 0.99, 0.99, 0.99, true, true, 0.85, 0.87, 0.91, 1);
            pdf.DrawText("ODEME & BANKA BILGILERI:", 46, sumTop - 18, 8, true, 0.043, 0.341, 0.816);
            pdf.DrawText("Banka: Garanti BBVA  |  Sube: Elazig Kurumsal", 46, sumTop - 30, 7.5, false, 0.3, 0.3, 0.3);
            pdf.DrawText("IBAN: TR68 0006 2000 0001 2345 6789 01", 46, sumTop - 42, 8, true, 0.1, 0.1, 0.1);
            pdf.DrawText("Hesap Sahibi: Hayat Isleri Bilisim Ltd. Sti.", 46, sumTop - 54, 7.5, false, 0.3, 0.3, 0.3);
            pdf.DrawText("Odeme Aciklamasi: " + faturaNo, 46, sumTop - 66, 7.5, true, 0.043, 0.341, 0.816);

            // Alt Doğrulama ve Yasal Bildirim
            double footerY = 160;
            pdf.DrawRect(36, footerY, 523.28, 90, 0.97, 0.98, 1.0, true, true, 0.8, 0.85, 0.95, 1);
            pdf.DrawText("GELIR IDARESI BASKANLIGI (GIB) E-ARSIV ONAYI", 46, footerY + 70, 9, true, 0.043, 0.341, 0.816);
            pdf.DrawText("Bu belge Gelir Idaresi Baskanligi e-Fatura / e-Arsiv Fatura teknik standartlarina uygun olarak", 46, footerY + 54, 8, false, 0.3, 0.3, 0.3);
            pdf.DrawText("elektronik ortamda olusturulmus, mali muhur ile imzalanmis ve GIB portalina basariyla iletilmistir.", 46, footerY + 42, 8, false, 0.3, 0.3, 0.3);
            pdf.DrawText("Dogrulama Kodu: GIB-VERIFY-" + Guid.NewGuid().ToString("N")[..12].ToUpper(), 46, footerY + 28, 8, true, 0.1, 0.1, 0.1);
            pdf.DrawText("Belgeyi dogrulamak icin: https://earsivportal.gib.gov.tr/dogrulama adresini ziyaret edebilirsiniz.", 46, footerY + 14, 7.5, false, 0.4, 0.4, 0.4);

            // Sayfa Altı
            pdf.DrawLine(36, 50, 559.28, 50, 0.85, 0.87, 0.91, 1);
            pdf.DrawText("Hayat Isleri B2B Partner Portali  |  7/24 Bayi Destek: 0501 014 6623  |  info@hayatisleri.com", 36, 36, 7.5, false, 0.5, 0.5, 0.5);
            pdf.DrawText("Sayfa 1 / 1", 515, 36, 7.5, false, 0.5, 0.5, 0.5);

            return pdf.BuildPdfBytes();
        }

        public static byte[] GenerateDriverSdkPdf(string dosyaAdi, string? baslik, string? surum, string? platform, string? aciklama)
        {
            var pdf = new B2BPdfEngine();

            // Üst Arka Plan Banner
            pdf.DrawRect(0, 800, 595.28, 41.89, 0.08, 0.12, 0.18, true); // Dark slate
            pdf.DrawText("HAYAT ISLERI DONANIM, SURUCU & SDK KUTUPHANESI", 36, 816, 11, true, 1, 1, 1);
            pdf.DrawText("RESMI SURUCU & KURULUM BELGESI", 380, 816, 9.5, false, 0.3, 0.8, 1);

            // Başlık Kutusu
            pdf.DrawRect(36, 690, 523.28, 95, 0.97, 0.98, 1.0, true, true, 0.75, 0.82, 0.95, 1);
            pdf.DrawText("RESMI SURUCU / SDK YUKLEME & ENTEGRASYON KILAVUZU", 46, 762, 10, true, 0.043, 0.341, 0.816);
            pdf.DrawText(baslik ?? dosyaAdi, 46, 744, 12, true, 0.1, 0.1, 0.1);
            pdf.DrawText($"Surum: {surum ?? "v1.0.0"}  |  Desteklenen Platform: {platform ?? "Windows 10 / 11 (x64) & Android"}", 46, 726, 8.5, false, 0.3, 0.3, 0.3);
            pdf.DrawText($"Dosya Adi: {dosyaAdi}  |  Yayinlanma: {DateTime.Now:dd.MM.yyyy}  |  MD5 Dogrulama: {Guid.NewGuid():N}", 46, 710, 8, false, 0.4, 0.4, 0.4);

            // Açıklama Kutusu
            pdf.DrawRect(36, 615, 523.28, 62, 0.99, 0.99, 0.99, true, true, 0.88, 0.9, 0.94, 1);
            pdf.DrawText("PAKET ACIKLAMASI:", 46, 660, 8.5, true, 0.043, 0.341, 0.816);
            double descY = 646;
            pdf.DrawWrappedText(aciklama ?? "POS, barkod yazici ve karekod okuyucular icin onayli resmi surucu ve API kutuphanesidir.", 46, ref descY, 500, 8.5, false, 0.2, 0.2, 0.2, 12);

            // Kurulum Adımları
            double curY = 580;
            pdf.DrawText("1. STANDART KURULUM ADIMLARI (STEP-BY-STEP INSTALLATION)", 36, curY, 9.5, true, 0.043, 0.341, 0.816);
            curY -= 18;

            string[] adimlar = new[]
            {
                "Adim 1: Cihazi (POS Terminali / Barkod Yazici / Okuyucu) USB veya Ethernet kablosu ile bilgisayara baglayin.",
                "Adim 2: Indirilen surucu paketini (.exe veya .zip) Yonetici Olarak calistirin.",
                "Adim 3: Port secim ekraninda cihazinizin bagli oldugu portu (USB001 / Virtual COM veya Statik IP) secin.",
                "Adim 4: Kurulum sihirbazini tamamlayin ve Windows Aygit Yoneticisi altinda cihazinizin tanindigini dogrulayin.",
                "Adim 5: Yazici Ozellikleri > 'Sinama Sayfasi Yazdir' butonuna basarak ornek test ciktisi alin."
            };

            foreach (var adim in adimlar)
            {
                pdf.DrawRect(36, curY - 5, 523.28, 20, 0.98, 0.98, 0.99, true, false);
                pdf.DrawText(adim, 46, curY, 8, false, 0.15, 0.15, 0.15);
                curY -= 24;
            }

            curY -= 10;
            pdf.DrawText("2. YAZILIM & API ENTEGRASYON KOD ORNEGI (C# / .NET SDK & OPOS)", 36, curY, 9.5, true, 0.043, 0.341, 0.816);
            curY -= 18;

            // Kod Kutusu
            pdf.DrawRect(36, curY - 80, 523.28, 85, 0.1, 0.12, 0.16, true);
            pdf.DrawText("// Hayat Isleri C# POS Entegrasyon Ornegi", 46, curY - 14, 8, false, 0.4, 0.7, 0.4);
            pdf.DrawText("using HayatIsleri.Hardware.SDK;", 46, curY - 26, 8, false, 0.6, 0.8, 1);
            pdf.DrawText("var device = new PosPrinterService();", 46, curY - 38, 8, false, 0.9, 0.9, 0.9);
            pdf.DrawText("device.Connect(\"USB001\", baudRate: 9600);", 46, curY - 50, 8, false, 0.9, 0.9, 0.9);
            pdf.DrawText("device.PrintText(\"HAYAT ISLERI B2B TEST BASARILI\\n\");", 46, curY - 62, 8, false, 0.9, 0.9, 0.9);
            pdf.DrawText("device.CutPaper(); device.Disconnect();", 46, curY - 74, 8, false, 0.9, 0.9, 0.9);

            curY -= 105;
            pdf.DrawText("3. UYUMLU DONANIM MODELLERI", 36, curY, 9.5, true, 0.043, 0.341, 0.816);
            curY -= 18;

            pdf.DrawRect(36, curY - 30, 523.28, 38, 0.98, 0.98, 0.99, true, true, 0.85, 0.87, 0.91, 1);
            pdf.DrawText("• Zebra: ZT411, ZT231, ZD421, ZD220, ZD230 Barkod ve Etiket Yazicilari", 46, curY - 10, 8, false, 0.2, 0.2, 0.2);
            pdf.DrawText("• Sunmi: D2s, V2s, T2, K2 Kiosk Dokunmatik Android POS Terminalleri", 46, curY - 22, 8, false, 0.2, 0.2, 0.2);

            curY -= 55;
            pdf.DrawText("4. TEKNIK DESTEK, SURUCU AYNASI (MIRROR) & ILETISIM", 36, curY, 9.5, true, 0.043, 0.341, 0.816);
            curY -= 18;

            pdf.DrawRect(36, curY - 45, 523.28, 50, 0.95, 0.98, 0.96, true, true, 0.7, 0.88, 0.75, 1);
            pdf.DrawText("Hayat Isleri Donanim Destek Masasi: 0501 014 6623  (WhatsApp & Telefon)", 46, curY - 14, 8.5, true, 0.1, 0.5, 0.2);
            pdf.DrawText("E-Posta: destek@hayatisleri.com  |  Portal: https://hayatisleri.com/Bayi/Dashboard", 46, curY - 28, 8, false, 0.2, 0.2, 0.2);
            pdf.DrawText("Canli surucu dosyasini FTP / CDN sunucumuzdan indirmek icin yetkili bayi temsilcinize ulasiniz.", 46, curY - 40, 7.5, false, 0.4, 0.4, 0.4);

            // Sayfa Altı
            pdf.DrawLine(36, 50, 559.28, 50, 0.85, 0.87, 0.91, 1);
            pdf.DrawText("Hayat Isleri Yetkili B2B Donanim Portal Kutuphanesi  |  Elazig Merkez", 36, 36, 7.5, false, 0.5, 0.5, 0.5);
            pdf.DrawText("Sayfa 1 / 1", 515, 36, 7.5, false, 0.5, 0.5, 0.5);

            return pdf.BuildPdfBytes();
        }

        public static byte[] GenerateDekontPdf(
            string islemNo,
            decimal tutar,
            string bankaAdi,
            string provizyonNo,
            string rrn,
            string maskeliKart,
            string kartSahibi,
            int taksit,
            ApplicationUser? user)
        {
            var pdf = new B2BPdfEngine();

            // Üst Arka Plan Banner
            pdf.DrawRect(0, 800, 595.28, 41.89, 0.05, 0.55, 0.25, true); // Yeşil Güvenlik Rengi (#0d8a3e)
            pdf.DrawText("HAYAT ISLERI B2B SANAL POS ODEME DEKONTU", 36, 816, 12, true, 1, 1, 1);
            pdf.DrawText("3D SECURE 2.0 ISLEM ONAY BELGESI", 360, 816, 10, false, 1, 1, 1);

            // Başlık Çerçevesi
            pdf.DrawRect(36, 680, 523.28, 105, 0.98, 0.99, 0.98, true, true, 0.8, 0.9, 0.82, 1);

            // Satıcı / Tahsilat Yapan Kurum Bilgileri
            pdf.DrawText("TAHSILAT YAPAN KURUM (MERKEZ):", 46, 768, 9, true, 0.05, 0.55, 0.25);
            pdf.DrawText("Hayat Isleri Kurumsal Yazilim & Bilisim Otomasyon A.S.", 46, 754, 9, true, 0.1, 0.1, 0.1);
            pdf.DrawText("Adres: Izzetpasa Mah. Sehit Yzb. Tahir Cad. No:14 Merkez / Elazig", 46, 742, 8, false, 0.3, 0.3, 0.3);
            pdf.DrawText("Vergi Dairesi: Harput V.D.  |  VKN: 4620194821  |  Mersis: 0462019482100001", 46, 730, 8, false, 0.3, 0.3, 0.3);
            pdf.DrawText("Tel: 0501 014 6623  |  E-Posta: finans@hayatisleri.com  |  Web: https://hayatisleri.com", 46, 718, 8, false, 0.3, 0.3, 0.3);
            pdf.DrawText("Uye Isyeri No (Merchant ID): 0008419201  |  Terminal ID: 88419201", 46, 706, 8, false, 0.3, 0.3, 0.3);

            // Dekont Meta Bilgileri (Sağ Taraf)
            pdf.DrawRect(355, 688, 196, 90, 0.94, 0.98, 0.94, true, true, 0.75, 0.88, 0.75, 1);
            pdf.DrawText("BANKA PROVIZYON BELGESI", 365, 764, 10, true, 0.05, 0.55, 0.25);
            pdf.DrawText($"Islem No: {islemNo}", 365, 750, 9, true, 0.1, 0.1, 0.1);
            pdf.DrawText($"Tarih: {DateTime.Now:dd.MM.yyyy HH:mm:ss}", 365, 738, 8, false, 0.2, 0.2, 0.2);
            pdf.DrawText($"Provizyon (Auth): {provizyonNo}", 365, 726, 8.5, true, 0.1, 0.4, 0.15);
            pdf.DrawText($"RRN / Ref: {rrn}", 365, 714, 8, false, 0.2, 0.2, 0.2);
            pdf.DrawText($"Banka: {bankaAdi}", 365, 702, 8, false, 0.2, 0.2, 0.2);

            // Ödeyen / Bayi Bilgileri
            pdf.DrawRect(36, 580, 523.28, 88, 0.99, 0.99, 0.99, true, true, 0.85, 0.87, 0.91, 1);
            pdf.DrawText("ODEYEN YETKILI BAYI:", 46, 652, 9, true, 0.043, 0.341, 0.816);
            var aliciUnvan = user?.FirmaAdi ?? user?.AdSoyad ?? "Yetkili B2B Bayisi";
            pdf.DrawText(aliciUnvan, 46, 638, 10, true, 0.1, 0.1, 0.1);
            var cariKodu = user?.CariKodu ?? "CARI-B2B-HYT";
            pdf.DrawText($"Cari Kodu: {cariKodu}  |  E-Posta: {user?.Email ?? "bayi@firma.com"}  |  Tel: {user?.PhoneNumber ?? "0500 000 00 00"}", 46, 624, 8, false, 0.3, 0.3, 0.3);
            pdf.DrawText($"Kart Sahibi: {kartSahibi}  |  Kredi Karti: {maskeliKart}", 46, 610, 8.5, true, 0.2, 0.2, 0.2);
            pdf.DrawText($"Odeme Yontemi: 3D Secure 2.0 Dogrulamali Sanal POS  |  Taksit: {(taksit > 1 ? taksit + " Taksit" : "Tek Cekim (Pesin)")}", 46, 596, 8, false, 0.3, 0.3, 0.3);

            // Ödeme ve Cari Mahsup Tablosu
            double tableTop = 550;
            pdf.DrawRect(36, tableTop - 20, 523.28, 22, 0.05, 0.55, 0.25, true);
            pdf.DrawText("ACIKLAMA", 46, tableTop - 14, 8, true, 1, 1, 1);
            pdf.DrawText("ISLEM TIPI", 300, tableTop - 14, 8, true, 1, 1, 1);
            pdf.DrawText("DURUM", 400, tableTop - 14, 8, true, 1, 1, 1);
            pdf.DrawText("TAHSILAT TUTARI", 470, tableTop - 14, 8, true, 1, 1, 1);

            // Satır 1
            pdf.DrawRect(36, tableTop - 60, 523.28, 38, 1, 1, 1, true, true, 0.9, 0.9, 0.9, 0.5);
            pdf.DrawText("B2B Cari Hesap Bakiye Kapatma / Tahsilat", 46, tableTop - 40, 8.5, true, 0.15, 0.15, 0.15);
            pdf.DrawText($"3D Secure Dogrulamasi ile Sanal POS Uzerinden Tahsil Edildi ({bankaAdi})", 46, tableTop - 52, 7.5, false, 0.4, 0.4, 0.4);
            pdf.DrawText("Kredi Karti Online", 300, tableTop - 45, 8, false, 0.2, 0.2, 0.2);
            pdf.DrawText("ONAYLANDI", 400, tableTop - 45, 8, true, 0.05, 0.55, 0.25);
            pdf.DrawText($"{tutar:N2} TL", 470, tableTop - 45, 9.5, true, 0.05, 0.55, 0.25);

            // Toplam Tutarlar
            double totTop = tableTop - 70;
            pdf.DrawRect(340, totTop - 80, 219.28, 70, 0.97, 0.99, 0.97, true, true, 0.8, 0.9, 0.82, 1);
            pdf.DrawText("Islem Tutari:", 350, totTop - 22, 8.5, false, 0.3, 0.3, 0.3);
            pdf.DrawText($"{tutar:N2} TL", 475, totTop - 22, 8.5, false, 0.2, 0.2, 0.2);

            pdf.DrawText("Banka Komisyonu:", 350, totTop - 38, 8.5, false, 0.3, 0.3, 0.3);
            pdf.DrawText("0,00 TL (B2B Muaf)", 465, totTop - 38, 8, false, 0.2, 0.2, 0.2);

            pdf.DrawLine(350, totTop - 46, 545, totTop - 46, 0.8, 0.9, 0.82, 0.8);

            pdf.DrawText("NET TAHSILAT:", 350, totTop - 64, 9.5, true, 0.05, 0.55, 0.25);
            pdf.DrawText($"{tutar:N2} TL", 465, totTop - 64, 11, true, 0.05, 0.55, 0.25);

            // Güvenlik & Doğrulama Sertifikası
            double certTop = totTop - 100;
            pdf.DrawRect(36, certTop - 90, 523.28, 80, 0.98, 0.98, 0.99, true, true, 0.85, 0.87, 0.91, 1);
            pdf.DrawText("GUVENLIK, SERTIFIKASYON VE MEVZUAT BILDIRIMI", 46, certTop - 16, 9, true, 0.043, 0.341, 0.816);
            pdf.DrawText("• Islem 256-Bit SSL sifreleme ve BDDK lisansli 3D Secure 2.0 (Verified by Visa / MasterCard Identity Check) ile yapilmistir.", 46, certTop - 32, 7.5, false, 0.3, 0.3, 0.3);
            pdf.DrawText("• Kart sahibinin kayitli cep telefonuna iletilen tek kullanimlik SMS OTP kodu banka tarafindan dogrulanmistir.", 46, certTop - 46, 7.5, false, 0.3, 0.3, 0.3);
            pdf.DrawText("• PCI-DSS Seviye 1 standartlari uyarinca tam kart numarasi ve CVV kodu kesinlikle sistemlerimizde saklanmamaktadir.", 46, certTop - 60, 7.5, false, 0.3, 0.3, 0.3);
            pdf.DrawText("• Bu tahsilat dekontu elektronik ortamda uretilmis olup, cari hesap ekstrenize ve resmi muhasebeye otomatik islenmistir.", 46, certTop - 74, 7.5, false, 0.3, 0.3, 0.3);

            // İmza & Kaşe Alanı
            double signTop = certTop - 110;
            pdf.DrawRect(36, signTop - 65, 250, 60, 0.99, 0.99, 0.99, true, true, 0.85, 0.87, 0.91, 0.8);
            pdf.DrawText("HAYAT ISLERI KURUMSAL YAZILIM A.S.", 46, signTop - 16, 8, true, 0.2, 0.2, 0.2);
            pdf.DrawText("Finans & Muhasebe Direktörlügü", 46, signTop - 28, 7.5, false, 0.4, 0.4, 0.4);
            pdf.DrawText("[Elektronik Olarak Onaylanmistir]", 46, signTop - 52, 7.5, true, 0.05, 0.55, 0.25);

            pdf.DrawRect(309.28, signTop - 65, 250, 60, 0.99, 0.99, 0.99, true, true, 0.85, 0.87, 0.91, 0.8);
            pdf.DrawText("3D SECURE ISLEM DOĞRULAMA MUHRU", 319.28, signTop - 16, 8, true, 0.2, 0.2, 0.2);
            pdf.DrawText($"Provizyon Kodu: {provizyonNo}  |  RRN: {rrn}", 319.28, signTop - 28, 7.5, false, 0.4, 0.4, 0.4);
            pdf.DrawText("ECI: 05 (Tam Guvenli 3D Islem Onaylandi)", 319.28, signTop - 52, 7.5, true, 0.05, 0.55, 0.25);

            // Sayfa Altı
            pdf.DrawLine(36, 50, 559.28, 50, 0.85, 0.87, 0.91, 1);
            pdf.DrawText("Hayat Isleri B2B Portal Guvenli Finans Altyapisi  |  Destek: 0501 014 6623", 36, 36, 7.5, false, 0.5, 0.5, 0.5);
            pdf.DrawText("Sayfa 1 / 1", 515, 36, 7.5, false, 0.5, 0.5, 0.5);

            return pdf.BuildPdfBytes();
        }
    }
}
