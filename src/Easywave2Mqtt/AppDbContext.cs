using Microsoft.EntityFrameworkCore;
using Easywave2Mqtt.Configuration;
using System.Reflection;

namespace Easywave2Mqtt
{
  public class AppDbContext : DbContext
  {
    public AppDbContext(DbContextOptions<AppDbContext> opt) : base(opt)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      _ = optionsBuilder.UseSqlite("Filename=Easywave2Mqtt.db", options=>options.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName))
        .EnableSensitiveDataLogging()
        .EnableDetailedErrors();
      base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      _ = modelBuilder.Entity<Device>().OwnsMany(d => d.ListensTo).HasKey("Address", "KeyCode");
    }

    public DbSet<Device> Devices { get; set; } = null!;

  }
}
