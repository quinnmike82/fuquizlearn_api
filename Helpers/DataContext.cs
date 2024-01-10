using fuquizlearn_api.Entities;
using Microsoft.EntityFrameworkCore;

namespace fuquizlearn_api.Helpers
{
    public class DataContext : DbContext
    {
        public DbSet<Account> Accounts { get; set; }

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
    }
}
