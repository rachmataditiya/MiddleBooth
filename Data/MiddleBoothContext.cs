using Microsoft.EntityFrameworkCore;
using MiddleBooth.Models;

namespace MiddleBooth.Data
{
    public class MiddleBoothContext : DbContext
    {
        public DbSet<Setting> Settings { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=middlebooth.db");
    }
}