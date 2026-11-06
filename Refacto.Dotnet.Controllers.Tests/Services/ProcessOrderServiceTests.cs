using Merjane.Application.Abstractions;
using Merjane.Application.Orders;
using Moq;
using Merjane.Domain.Entities;
using Refacto.DotNet.Controllers.Services;
using Merjane.Application.Products;

namespace Refacto.Dotnet.Controllers.Tests.Services;

public class ProcessOrderServiceTests
{
    private readonly Mock<IOrderRepository> _repository = new(MockBehavior.Strict);
    private readonly Mock<INotificationService> _notifications = new(MockBehavior.Strict);
    private readonly List<string> _events = new();

    [Fact]
    public void Normal_WithStock_DecrementsAndSaves()
    {
        var product = new Product(available: 5, leadTime: 10) { Type = "NORMAL", Name = "Cable" };

        ProcessAndAssert(product, 4, "MARK", "SAVE");
    }

    [Fact]
    public void Normal_WithoutStock_NotifiesDelayAfterSaving()
    {
        var product = new Product(available: 0, leadTime: 10) { Type = "NORMAL", Name = "Cable" };
        _notifications.Setup(n => n.SendDelayNotification(10, "Cable"))
            .Callback(() => _events.Add("DELAY"));

        ProcessAndAssert(product, 0, "SAVE", "DELAY");
    }

    [Fact]
    public void Seasonal_WithinSeasonWithStock_DecrementsAndSaves()
    {
        var product = SeasonalProduct(5);

        ProcessAndAssert(product, 4, "SAVE");
    }

    [Fact]
    public void Seasonal_WithoutStockReplenishedWithinSeason_NotifiesDelayAfterSaving()
    {
        var product = SeasonalProduct(0);
        _notifications.Setup(n => n.SendDelayNotification(10, "Watermelon"))
            .Callback(() => _events.Add("DELAY"));

        ProcessAndAssert(product, 0, "SAVE", "DELAY");
    }

    [Fact]
    public void Seasonal_ReplenishmentBeyondSeason_NotifiesThenClearsStockAndSaves()
    {
        var product = SeasonalProduct(5);
        product.SeasonStartDate = DateTime.Today.AddDays(10);
        product.SeasonEndDate = DateTime.Today.AddDays(20);
        product.UpdateLeadTime(30);
        _notifications.Setup(n => n.SendOutOfStockNotification("Watermelon"))
            .Callback(() =>
            {
                Assert.Equal(5, product.Available);
                _events.Add("UNAVAILABLE");
            });

        ProcessAndAssert(product, 0, "UNAVAILABLE", "SAVE");
    }

    [Fact]
    public void Seasonal_BeforeSeasonReplenishedWithinSeason_PreservesStockAndNotifies()
    {
        var product = SeasonalProduct(5);
        product.SeasonStartDate = DateTime.Today.AddDays(10);
        product.UpdateLeadTime(20);
        _notifications.Setup(n => n.SendOutOfStockNotification("Watermelon"))
            .Callback(() =>
            {
                Assert.Equal(5, product.Available);
                _events.Add("UNAVAILABLE");
            });

        ProcessAndAssert(product, 5, "UNAVAILABLE", "SAVE");
    }

    [Fact]
    public void Expirable_ExpiredWithStock_NotifiesThenClearsStockAndSaves()
    {
        DateTime expiryDate = DateTime.Today.AddDays(-10);
        var product = new Product(available: 5, leadTime: 10)
        {
            Type = "EXPIRABLE", Name = "Milk", ExpiryDate = expiryDate
        };
        _notifications.Setup(n => n.SendExpirationNotification("Milk", expiryDate))
            .Callback(() =>
            {
                Assert.Equal(5, product.Available);
                _events.Add("EXPIRED");
            });

        ProcessAndAssert(product, 0, "EXPIRED", "SAVE");
    }

    private static Product SeasonalProduct(int available) => new(available: available, leadTime: 10)
    {
        Type = "SEASONAL", Name = "Watermelon",
        SeasonStartDate = DateTime.Today.AddDays(-30),
        SeasonEndDate = DateTime.Today.AddDays(60)
    };

    private void ProcessAndAssert(Product product, int expectedStock, params string[] expectedEvents)
    {
        int originalLeadTime = product.LeadTime;
        var order = new Order { Id = 42, Items = new List<Product> { product } };
        _repository.Setup(r => r.GetWithProducts(order.Id)).Returns(order);
        _repository.Setup(r => r.MarkProductModified(product)).Callback(() =>
        {
            Assert.Equal(expectedStock, product.Available);
            _events.Add("MARK");
        });
        _repository.Setup(r => r.SaveChanges()).Callback(() =>
        {
            Assert.Equal(expectedStock, product.Available);
            _events.Add("SAVE");
        });
        var products = new ProductService(_notifications.Object, _repository.Object);
        var processor = new ProcessOrderService(_repository.Object, products);

        ProcessOrderResult result = processor.ProcessOrder(order.Id);

        Assert.Equal(ProcessingStatus.Success, result.Status);
        Assert.Equal(order.Id, result.OrderId);
        Assert.Equal(expectedStock, product.Available);
        Assert.Equal(originalLeadTime, product.LeadTime);
        Assert.Equal(expectedEvents, _events);
        _repository.Verify(r => r.GetWithProducts(order.Id), Times.Once);
        _repository.Verify(r => r.SaveChanges(), Times.Once);
        _repository.Verify(r => r.MarkProductModified(product),
            expectedEvents.Contains("MARK") ? Times.Once() : Times.Never());
        _notifications.VerifyAll();
        Assert.Equal(expectedEvents.Count(e => e is "DELAY" or "UNAVAILABLE" or "EXPIRED"),
            _notifications.Invocations.Count);
        _repository.VerifyNoOtherCalls();
        _notifications.VerifyNoOtherCalls();
    }
}
