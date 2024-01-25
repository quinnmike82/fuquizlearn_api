using fuquizlearn_api.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace fuquizlearn_api.Helpers
{
    public class DataContext : DbContext
    {
        public DbSet<Account> Accounts { get; set; }
        public DbSet<QuizBank> QuizBanks { get; set; }

        private readonly IConfiguration Configuration;

        public DataContext(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // connect to supabase database
            options.UseNpgsql(Configuration.GetConnectionString("Supabase"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<QuizBank>().OwnsMany(qb => qb.Quizes, q =>
            {
                q.WithOwner().HasForeignKey("QuizBankId");
                q.HasKey("Id");
                q.Property(a => a.Choices)
                .HasConversion(
                    metadata => JsonConvert.SerializeObject(metadata),
                    json => JsonConvert.DeserializeObject<List<Choice>>(json))
                .HasColumnType("jsonb");
            });
        }
    }
}
