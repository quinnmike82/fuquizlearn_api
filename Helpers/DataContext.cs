﻿using fuquizlearn_api.Entities;
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
    public DbSet<LearnedProgress> LearnedProgress { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        // connect to supabase database
        options.UseNpgsql(Configuration.GetConnectionString("Supabase"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QuizBank>().Property(a => a.Rating)
                .HasConversion(
                    metadata => JsonConvert.SerializeObject(metadata),
                    json => JsonConvert.DeserializeObject<List<Rating>>(json))
                .HasColumnType("jsonb");
        modelBuilder.Entity<QuizBank>().HasMany(qb => qb.Quizes)
            .WithOne(q => q.QuizBank)
            .HasForeignKey(q => q.QuizBankId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Post>().HasMany(qb => qb.Comments)
            .WithOne(q => q.Post)
            .HasForeignKey(q => q.PostId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
    }
}