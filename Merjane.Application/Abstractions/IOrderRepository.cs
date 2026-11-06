using Merjane.Domain.Entities;

namespace Merjane.Application.Abstractions;

// APPLICATION DEFINES THE PERSISTENCE CONTRACT; INFRASTRUCTURE IMPLEMENTS IT.
public interface IOrderRepository
{
    Order? GetWithProducts(long orderId);
    void MarkProductModified(Product product);
    void SaveChanges();
}
