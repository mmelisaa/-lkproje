namespace WebApplication1.Models.Siniflar
{
    public class ReferansProje
    {
        public int Id { get; set; }
        public string Baslik { get; set; }
        public string Kategori { get; set; } // Örn: E-Ticaret, Kurumsal Yazılım, Web Tasarım
        public string Aciklama { get; set; }
        public string GorselUrl { get; set; }
        public string ProjeLinki { get; set; }
        public string Etiketler { get; set; } // Örn: ASP.NET Core, React, Özel Arayüz
    }
}