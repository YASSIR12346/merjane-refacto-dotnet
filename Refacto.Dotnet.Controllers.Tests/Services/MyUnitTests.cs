using Merjane.Application.Abstractions;
using Moq;
using Merjane.Domain.Entities;
using Refacto.DotNet.Controllers.Services;
using Merjane.Application.Products;

namespace Refacto.DotNet.Controllers.Tests.Services
{
    public class MyUnitTests
    {
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly Mock<IOrderRepository> _mockOrders;
        private readonly ProductService _productService;

        public MyUnitTests()
        {
            _mockNotificationService = new Mock<INotificationService>();
            _mockOrders = new Mock<IOrderRepository>();
            _productService = new ProductService(_mockNotificationService.Object, _mockOrders.Object);
        }

        [Fact]
        public void Test()
        {
            // GIVEN
            Product product = new(available: 0, leadTime: 15)
            {
                Type = "NORMAL",
                Name = "RJ45 Cable"
            };

            // WHEN
            _productService.NotifyDelay(product.LeadTime, product);

            // THEN
            Assert.Equal(0, product.Available);
            Assert.Equal(15, product.LeadTime);
            _mockOrders.Verify(ctx => ctx.SaveChanges(), Times.Once());
            _mockNotificationService.Verify(service => service.SendDelayNotification(product.LeadTime, product.Name), Times.Once());
        }
    }
}
