using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Merjane.Domain.Entities;

namespace Merjane.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        // RETAIN TABLE AND COLUMN METADATA WITHOUT ADDING A RELATIONAL PROVIDER.
        builder.HasAnnotation("Relational:TableName", "orders");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasAnnotation("Relational:ColumnName", "id").ValueGeneratedOnAdd();
        builder.HasMany(order => order.Items).WithMany();
    }
}
