using Merjane.Application.Abstractions;
using Refacto.DotNet.Controllers.Services;
using Merjane.Domain.Entities;

namespace Merjane.Application.Products
{
    // KEEP EACH SAVE AND NOTIFICATION IN ITS ORIGINAL EXECUTION ORDER.
    public class ProductService
    {
        private readonly INotificationService _notifications;
        private readonly IOrderRepository _orders;

        public ProductService(INotificationService notifications, IOrderRepository orders)
        {
            _notifications = notifications;
            _orders = orders;
        }

        public void NotifyDelay(int leadTime, Product product)
        {
            string name = product.Name
                ?? throw new InvalidOperationException("Product name is missing.");
            product.UpdateLeadTime(leadTime);
            _orders.SaveChanges();
            _notifications.SendDelayNotification(leadTime, name);
        }

        public void HandleSeasonalProduct(Product product)
        {
            string name = product.Name
                ?? throw new InvalidOperationException("Product name is missing.");
            if (product.ReplenishmentExceedsSeason())
            {
                _notifications.SendOutOfStockNotification(name);
                product.ClearStock();
                _orders.SaveChanges();
            }
            else if (product.SeasonHasNotStarted())
            {
                _notifications.SendOutOfStockNotification(name);
                _orders.SaveChanges();
            }
            else
            {
                NotifyDelay(product.LeadTime, product);
            }
        }

        public void HandleUnavailableExpirableProduct(Product product)
        {
            string name = product.Name
                ?? throw new InvalidOperationException("Product name is missing.");
            DateTime expiryDate = product.ExpiryDate
                ?? throw new InvalidOperationException("Expiration date is missing.");
            if (product.CanSellInExpirationHandler())
            {
                product.DecreaseStock();
                _orders.SaveChanges();
            }
            else
            {
                _notifications.SendExpirationNotification(name, expiryDate);
                product.ClearStock();
                _orders.SaveChanges();
            }
        }
    }
}
