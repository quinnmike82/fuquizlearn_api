using fuquizlearn_api.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace fuquizlearn_api.Helpers;

public class DataContext : DbContext
{
    private readonly IConfiguration Configuration;

    public DataContext(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public DbSet<Account> Accounts { get; set; }
    public DbSet<Quiz> Quizes { get; set; }
    public DbSet<QuizBank> QuizBanks { get; set; }
    public DbSet<Classroom> Classrooms { get; set; }
    public DbSet<ClassroomMember> ClassroomsMembers { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Comment> Comments { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        // connect to supabase database
        options.UseNpgsql(Configuration.GetConnectionString("Supabase"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QuizBank>().HasMany(qb => qb.Quizes)
            .WithOne(q => q.QuizBank)
            .HasForeignKey(q => q.QuizBankId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
    }
}