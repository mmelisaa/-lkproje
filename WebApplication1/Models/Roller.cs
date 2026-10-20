using System.Security.Claims;

namespace WebApplication1.Models
{
    public static class Roller
    {
        public const string Admin = "Admin";
        public const string Editor = "Editor";
        public const string DestekUzmani = "DestekUzmani";
        public const string MusteriTemsilcisi = "MusteriTemsilcisi";
        public const string Bayi = "Bayi";

        /// <summary>Admin paneline girebilen tüm personel rolleri ([Authorize(Roles = ...)] için).</summary>
        public const string Personel = "Admin,Editor,DestekUzmani,MusteriTemsilcisi";

        public static readonly string[] PersonelListesi = { Admin, Editor, DestekUzmani, MusteriTemsilcisi };
        public static readonly string[] Hepsi = { Admin, Editor, DestekUzmani, MusteriTemsilcisi, Bayi };

        public static string Etiket(string? rol) => rol switch
        {
            Admin => "Yönetici (Admin)",
            Editor => "Editör",
            DestekUzmani => "Destek Uzmanı",
            MusteriTemsilcisi => "Müşteri Temsilcisi",
            Bayi => "Bayi",
            _ => rol ?? "-"
        };

        public static bool PersonelMi(ClaimsPrincipal user) => PersonelListesi.Any(user.IsInRole);
    }

    /// <summary>
    /// Panel modüllerinin hangi rollere açık olduğunu tek yerde tutar (menü ve sunucu tarafı kontrolü aynı kaynağı kullanır).
    /// Tanımsız modüller yalnızca Admin'e açıktır (varsayılan: reddet).
    /// </summary>
    public static class PanelYetki
    {
        private static readonly string[] SadeceAdmin = { Roller.Admin };
        private static readonly string[] Icerik = { Roller.Admin, Roller.Editor };
        private static readonly string[] Destek = { Roller.Admin, Roller.DestekUzmani };
        private static readonly string[] Satis = { Roller.Admin, Roller.MusteriTemsilcisi };
        private static readonly string[] Crm = { Roller.Admin, Roller.DestekUzmani, Roller.MusteriTemsilcisi };

        private static readonly Dictionary<string, string[]> Moduller = new(StringComparer.OrdinalIgnoreCase)
        {
            // İçerik / vitrin → Editör
            ["Slider"] = Icerik, ["AnasayfaAksiyonKarti"] = Icerik, ["CozumUrunu"] = Icerik, ["SirketOlcegi"] = Icerik,
            ["Sektor"] = Icerik, ["Hizmetler"] = Icerik, ["OzelProjeler"] = Icerik, ["Dokumanlar"] = Icerik,
            ["Kampanyalar"] = Icerik, ["Menuler"] = Icerik, ["YasalMetinler"] = Icerik,

            // Satış / bayi ağı → Müşteri Temsilcisi
            ["YazilimUrunleri"] = Satis, ["DonanimUrunleri"] = Satis, ["Markalar"] = Satis, ["Kategoriler"] = Satis,
            ["Bayiler"] = Satis, ["BayiGruplari"] = Satis, ["BankaHesaplari"] = SadeceAdmin, ["Lisanslar"] = Satis,
            ["Siparisler"] = Satis, ["TeklifCpq"] = Satis, ["PaketlemeBundle"] = Satis, ["IskontoOnay"] = Satis,
            ["SatisPrimYonetimi"] = Satis, ["SaasMrrArr"] = Satis, ["ModularLisans"] = Satis, ["SurumPatch"] = Satis,
            ["KampanyaBasvurulari"] = Crm, ["DinamikFormlar"] = Crm, ["IsIlanlari"] = Crm,

            // Servis / saha → Destek Uzmanı
            ["SeriNoTakip"] = Destek, ["CihazYasamDongusu"] = Destek, ["GarantiBakim"] = Destek, ["RmaServis"] = Destek,
            ["KritikStok"] = Destek, ["DepoLokasyonYonetimi"] = Destek, ["SahaKurulum"] = Destek, ["SlaTakip"] = Destek,
            ["DestekTalepleri"] = Crm,

            // Finans, güvenlik ve sistem → yalnızca Admin
            ["CariRiskLimiti"] = SadeceAdmin, ["VadeKontrol"] = SadeceAdmin, ["IadeMutabakatYonetimi"] = SadeceAdmin,
            ["DovizKurYonetimi"] = SadeceAdmin, ["TopluIletisimMerkezi"] = Crm, ["BayiSozlesmeOnaylari"] = SadeceAdmin,
            ["SiteAyarlari"] = SadeceAdmin, ["RbacRoller"] = SadeceAdmin, ["KritikIslemLoglari"] = SadeceAdmin,
            ["ApiEntegrasyonLoglari"] = SadeceAdmin
        };

        public static string[] ModulRolleri(string modul) =>
            Moduller.TryGetValue(modul ?? "", out var roller) ? roller : SadeceAdmin;

        public static bool ModulIzni(ClaimsPrincipal user, string modul) => ModulRolleri(modul).Any(user.IsInRole);

        public static bool Herhangi(ClaimsPrincipal user, params string[] roller) => roller.Any(user.IsInRole);
    }
}
