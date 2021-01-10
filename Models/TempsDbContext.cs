using Microsoft.EntityFrameworkCore;

namespace DisplayHomeTemp.Models
{
    public class TempsDbContext : DbContext
    {
        public TempsDbContext()
        {
        }

        public TempsDbContext(DbContextOptions<TempsDbContext> options)
            : base(options)
        {
        }
        
        public DbSet<TempReading> Temps { get; set; }
    }
}