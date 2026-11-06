using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Merjane.Domain.Entities;

namespace Merjane.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        // RETAIN TABLE AND COLUMN METADATA WITHOUT ADDING A RELATIONAL PROVIDER.
        builder.HasAnnotation("Relational:TableName", "products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasAnnotation("Relational:ColumnName", "id").ValueGeneratedOnAdd();
        builder.Property(p => p.LeadTime).HasAnnotation("Relational:ColumnName", "lead_time");
        builder.Property(p => p.Available).HasAnnotation("Relational:ColumnName", "available");
        builder.Property(p => p.Type).HasAnnotation("Relational:ColumnName", "type");
        builder.Property(p => p.Name).HasAnnotation("Relational:ColumnName", "name");
        builder.Property(p => p.ExpiryDate).HasAnnotation("Relational:ColumnName", "expiry_date");
        builder.Property(p => p.SeasonStartDate).HasAnnotation("Relational:ColumnName", "season_start_date");
        builder.Property(p => p.SeasonEndDate).HasAnnotation("Relational:ColumnName", "season_end_date");
    }
}
