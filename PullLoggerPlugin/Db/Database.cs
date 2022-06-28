using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PullLogger.Db;

public class Database : DbContext
{
    public DbSet<Pull> Pulls { get; set; } = null!;
    private string DbPath { get; }

    public Database(string dir)
    {
        DbPath = Path.Combine(dir, "pulls.db");
    }

    public void Init()
    {
        Database.Migrate();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<Pull>()
            .Property(e => e.EventName)
            .HasConversion(new ValueConverter<PullEvent, string>(
                v => v.ToString(),
                v => (PullEvent)Enum.Parse(typeof(PullEvent), v)));
        modelBuilder
            .Entity<Pull>()
            .Property(e => e.Source)
            .HasConversion(new ValueConverter<Source, string>(
                v => v.ToString(),
                v => (Source)Enum.Parse(typeof(Source), v)));
    }

    protected override void OnConfiguring(DbContextOptionsBuilder o) => o.UseSqlite($"Data Source={DbPath}");
}