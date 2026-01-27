using Microsoft.EntityFrameworkCore;
using SunriseClinic.Models;

namespace SunriseClinic.Data
{
    public class SunriseDbContext : DbContext
    {
        public SunriseDbContext(DbContextOptions<SunriseDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}