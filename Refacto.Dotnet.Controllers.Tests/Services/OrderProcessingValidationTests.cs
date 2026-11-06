using Merjane.Application.Abstractions;
using Merjane.Application.Orders;
using Merjane.Application.Products;
using Merjane.Domain.Entities;
using Moq;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.Dotnet.Controllers.Tests.Services;

public class OrderProcessingValidationTests
{
    private readonly Mock<IOrderRepository> _repository = new(MockBehavior.Strict);
    private readonly Mock<INotificationService> _notifications = new(MockBehavior.Strict);

    [Fact]
    public void NullItems_ReturnsInvalidDataWithoutSideEffects()
    {
        AssertRejected(new Order { Id = 42 }, "Order items are missing.");
    }

    [Fact]
    public void MissingExpiration_RejectsEntireOrderBeforeProcessing()
    {
        var invalid = new Product(available: 5, leadTime: 10)
        {
            Id = 2, Type = "EXPIRABLE", Name = "Milk"
        };

        AssertInvalidProductRejected(invalid, "Product 2 has no expiration date.");
    }

    [Fact]
    public void MissingName_RejectsEntireOrderBeforeProcessing()
    {
        var invalid = new Product(available: 5, leadTime: 10)
        {
            Id = 2, Type = "NORMAL"
        };

        AssertInvalidProductRejected(invalid, "Product 2 has no name.");
    }

    private void AssertInvalidProductRejected(Product invalid, string expectedError)
    {
        var valid = new Product(available: 5, leadTime: 10) { Type = "NORMAL", Name = "Cable" };
        var order = new Order { Id = 42, Items = new List<Product> { valid, invalid } };

        AssertRejected(order, expectedError);

        Assert.Equal(5, valid.Available);
        Assert.Equal(5, invalid.Available);
        Assert.Equal(10, valid.LeadTime);
        Assert.Equal(10, invalid.LeadTime);
    }

    private void AssertRejected(Order order, string expectedError)
    {
        _repository.Setup(r => r.GetWithProducts(order.Id)).Returns(order);
        var products = new ProductService(_notifications.Object, _repository.Object);
        var processor = new ProcessOrderService(_repository.Object, products);

        ProcessOrderResult result = processor.ProcessOrder(order.Id);

        Assert.Equal(ProcessingStatus.InvalidData, result.Status);
        Assert.Null(result.OrderId);
        Assert.Equal(expectedError, result.Error);
        _repository.Verify(r => r.GetWithProducts(order.Id), Times.Once);
        _repository.VerifyNoOtherCalls();
        _notifications.VerifyNoOtherCalls();
    }
}
