using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.Siniflar
{
    /// <summary>RMA (arıza / iade / değişim) servis kaydı. Cihazın nerede olduğu, kargo bilgisi ve garanti durumu tutulur.</summary>
    public class RmaKaydi
    {
        public int Id { get; set; }

        [StringLength(30)]
        public string RmaNo { get; set; } = "";

        [Required(ErrorMessage = "Müşteri / firma adı gerekli."), StringLength(150)]
        public string MusteriAdi { get; set; } = "";
        [StringLength(30)] public string? Telefon { get; set; }
        [StringLength(150)] public string? Eposta { get; set; }

        [Required(ErrorMessage = "Cihaz modeli gerekli."), StringLength(150)]
        public string CihazModeli { get; set; } = "";
        [StringLength(100)] public string? SeriNo { get; set; }

        [Required(ErrorMessage = "Arıza açıklaması gerekli."), StringLength(1000)]
        public string ArizaAciklamasi { get; set; } = "";
        [StringLength(1000)] public string? Teshis { get; set; }

        // ---- Garanti ----
        public bool GarantiKapsaminda { get; set; }
        public DateTime? GarantiBitisTarihi { get; set; }
        [StringLength(300)] public string? GarantiNotu { get; set; }

        // ---- Çözüm / parça ----
        [StringLength(40)] public string Cozum { get; set; } = RmaSabitleri.Cozumler[0];
        [StringLength(300)] public string? DegisenParca { get; set; }
        public decimal Ucret { get; set; }

        // ---- Durum ve konum (cihaz şu an nerede?) ----
        [StringLength(40)] public string Durum { get; set; } = RmaSabitleri.Durumlar[0];
        [StringLength(60)] public string Konum { get; set; } = RmaSabitleri.Konumlar[1];
        [StringLength(20)] public string Oncelik { get; set; } = "Normal";

        // ---- Kargo ----
        [StringLength(40)] public string KargoYonu { get; set; } = RmaSabitleri.KargoYonleri[0];
        [StringLength(60)] public string? KargoFirmasi { get; set; }
        [StringLength(60)] public string? KargoTakipNo { get; set; }
        public DateTime? KargoTarihi { get; set; }

        [StringLength(100)] public string? AtananTeknisyen { get; set; }
        public DateTime KabulTarihi { get; set; } = DateTime.Now;
        public DateTime? TahminiTeslimTarihi { get; set; }
        public DateTime? KapanisTarihi { get; set; }
        public DateTime SonGuncelleme { get; set; } = DateTime.Now;

        public List<RmaHareket> Hareketler { get; set; } = new();

        public bool Kapali => Durum == "Teslim Edildi" || Durum == "İade Edildi" || Durum == "İptal";
        public bool KargodaMi => Konum.StartsWith("Kargoda", StringComparison.Ordinal);
    }

    public class RmaHareket
    {
        public int Id { get; set; }
        public int RmaKaydiId { get; set; }
        public DateTime Zaman { get; set; } = DateTime.Now;
        [StringLength(40)] public string Durum { get; set; } = "";
        [StringLength(60)] public string Konum { get; set; } = "";
        [StringLength(500)] public string? Aciklama { get; set; }
        [StringLength(100)] public string? Kullanici { get; set; }
    }

    public static class RmaSabitleri
    {
        public static readonly string[] Durumlar =
        {
            "Teslim Alındı", "Arıza Teşhisinde", "Garanti Onayı Bekleniyor", "Parça Bekleniyor", "Onarımda",
            "Üretici Servisinde", "Kargoda", "Cihaz Değişimi Hazırlanıyor", "Teslime Hazır",
            "Teslim Edildi", "İade Edildi", "İptal"
        };

        public static readonly string[] Konumlar =
        {
            "Müşteride", "Merkez Servis (Elazığ)", "Üretici / Distribütör Servisi",
            "Kargoda - Üreticiye Gidiyor", "Kargoda - Müşteriye Gidiyor", "Depoda (Yedek Stok)"
        };

        public static readonly string[] Cozumler =
        {
            "Belirlenmedi", "Onarım", "Parça Değişimi", "Cihaz Değişimi (Birebir)", "Ücretli Onarım",
            "İade / Para İadesi", "Arıza Bulunamadı"
        };

        public static readonly string[] KargoYonleri = { "Kargo Yok", "Üreticiye Gönderim", "Müşteriye Gönderim" };
        public static readonly string[] Oncelikler = { "Normal", "Yüksek", "Acil" };
    }
}
