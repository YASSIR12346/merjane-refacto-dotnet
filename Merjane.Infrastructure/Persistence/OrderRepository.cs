using Merjane.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Merjane.Infrastructure.Persistence;
using Merjane.Domain.Entities;

namespace Merjane.Infrastructure.Persistence;

// THE SCOPED CONTEXT PRESERVES TRACKING ACROSS ALL SAVES IN AN ORDER.
public sealed class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public Order? GetWithProducts(long orderId)
        => _context.Orders.Include(order => order.Items)
            .SingleOrDefault(order => order.Id == orderId);

    public void MarkProductModified(Product product)
        => _context.Entry(product).State = EntityState.Modified;

    public void SaveChanges()
        => _context.SaveChanges();
}
