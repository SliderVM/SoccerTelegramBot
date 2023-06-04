using Microsoft.EntityFrameworkCore;
using SoccerTelegramBot.Entities;

namespace SoccerTelegramBot
{
    public class DatabaseContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Signed> Signeds { get; set; }
        public DbSet<Configuration> Configurations { get; set; }

        public DatabaseContext(DbContextOptions options) : base(options)
        {  
            //Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Subscription>().Property(s => s.IsActive).HasDefaultValue(false);

            modelBuilder.Entity<Configuration>().HasData(new Configuration[]
            {
                new Configuration {Id = 1, Name="День игры", Value="3", Label="gameday"},
                new Configuration {Id = 2, Name="Стоимость одной игрый", Value="250", Label="costonegame"},
                new Configuration {Id = 3, Name="Стоимоить абонимента", Value="1000", Label="costsubscribe"},
                new Configuration {Id = 4, Name="Время игры", Value="19:50", Label="gametime"},
                new Configuration {Id = 5, Name="Суперпользователь", Value="217340949", Label="su"}
            });
        }
    }
}
