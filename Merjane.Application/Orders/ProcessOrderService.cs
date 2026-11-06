using Merjane.Application.Abstractions;
using Merjane.Domain.Entities;
using Merjane.Application.Products;

namespace Merjane.Application.Orders;

// APPLICATION ORCHESTRATES PROCESSING WITHOUT HTTP OR EF CORE DEPENDENCIES.
public sealed class ProcessOrderService : IProcessOrderService
{
    private readonly IOrderRepository _orders;
    private readonly ProductService _products;

    public ProcessOrderService(IOrderRepository orders, ProductService products)
    {
        _orders = orders;
        _products = products;
    }

    public ProcessOrderResult ProcessOrder(long orderId)
    {
        Order? order = _orders.GetWithProducts(orderId);

        if (order is null)
            return new(ProcessingStatus.NotFound);

        ICollection<Product>? products = order.Items;
        if (products is null)
            return new(ProcessingStatus.InvalidData, Error: "Order items are missing.");

        // VALIDATE THE WHOLE ORDER BEFORE ANY STOCK, SAVE, OR NOTIFICATION SIDE EFFECT.
        string? error = OrderProcessingValidator.Validate(products);
        if (error is not null)
            return new(ProcessingStatus.InvalidData, Error: error);

        Console.WriteLine(order);

        foreach (Product product in products)
        {
            switch (product.Type)
            {
                case "NORMAL":
                    ProcessNormal(product);
                    break;
                case "SEASONAL":
                    ProcessSeasonal(product);
                    break;
                case "EXPIRABLE":
                    ProcessExpirable(product);
                    break;
            }
        }

        return new(ProcessingStatus.Success, order.Id);
    }

    private void ProcessNormal(Product product)
    {
        if (product.CanSellNormal())
        {
            product.DecreaseStock();
            _orders.MarkProductModified(product);
            _orders.SaveChanges();
        }
        else if (product.LeadTime > 0)
        {
            _products.NotifyDelay(product.LeadTime, product);
        }
    }

    private void ProcessSeasonal(Product product)
    {
        if (product.CanSellSeasonal())
        {
            product.DecreaseStock();
            _orders.SaveChanges();
        }
        else
        {
            _products.HandleSeasonalProduct(product);
        }
    }

    private void ProcessExpirable(Product product)
    {
        if (product.CanSellExpirable())
        {
            product.DecreaseStock();
            _orders.SaveChanges();
        }
        else
        {
            _products.HandleUnavailableExpirableProduct(product);
        }
    }
}
