using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models.Siniflar;
using WebApplication1.Models.Siniflar.Kurumsal;

namespace WebApplication1.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<BlogPost> BlogPosts { get; set; }
        public DbSet<Hakkimizda> Hakkimizda { get; set; }
        public DbSet<ReferansProje> ReferansProjeler { get; set; }
        public DbSet<InsanKaynaklari> InsanKaynaklari { get; set; }
        public DbSet<HesapNumaralarimiz> HesapNumaralarimiz { get; set; }
        public DbSet<BankaBilgisi> BankaBilgileri { get; set; }
    }
}
