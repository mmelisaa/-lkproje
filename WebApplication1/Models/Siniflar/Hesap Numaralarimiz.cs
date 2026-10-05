namespace WebApplication1.Models.Siniflar
{
    public class HesapNumaralarimiz
    {
        public string UstBaslik { get; set; }
        public string AnaBaslik { get; set; }
        public string Aciklama { get; set; }

        // Banka Kartları için liste veya tekil alanlar kullanabiliriz
        public List<BankaBilgisi> Bankalar { get; set; }

        public string UyariBaslik { get; set; }
        public string UyariMetni { get; set; }
    }

    public class BankaBilgisi
    {
        BankaBilgisi() { }
        public BankaBilgisi(string bankaAdi, string aliciAdi, string iban)
        {
            BankaAdi = bankaAdi;
            AliciAdi = aliciAdi;
            Iban = iban;
        }

        public string BankaAdi { get; set; }
        public string AliciAdi { get; set; }
        public string Iban { get; set; }
    }
}