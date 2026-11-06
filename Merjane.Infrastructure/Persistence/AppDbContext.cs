using Microsoft.EntityFrameworkCore;
using Merjane.Domain.Entities;

namespace Merjane.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public AppDbContext() : base()
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // EF CORE MAPPINGS BELONG TO INFRASTRUCTURE, NOT DOMAIN ENTITIES.
            modelBuilder.ApplyConfiguration(new Merjane.Infrastructure.Persistence.Configurations.OrderConfiguration());
            modelBuilder.ApplyConfiguration(new Merjane.Infrastructure.Persistence.Configurations.ProductConfiguration());

        }


        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<Product> Products { get; set; }

    }
}
