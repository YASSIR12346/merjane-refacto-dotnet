using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Merjane.Infrastructure.Persistence;
using Merjane.Domain.Entities;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.Dotnet.Controllers.Tests.Controllers;

[Collection("Sequential")]
public class OrderProcessingBehaviorTests : IDisposable
{
    private readonly Mock<INotificationService> _notifications = new(MockBehavior.Strict);
    private readonly WebApplicationFactory<Program> _factory;

    public OrderProcessingBehaviorTests()
    {
        string databaseName = $"OrderProcessing-{Guid.NewGuid()}";
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.RemoveAll<INotificationService>();
                services.AddScoped<INotificationService>(_ => _notifications.Object);
            }));
    }

    [Fact]
    public async Task Normal_WithStock_ReturnsOrderIdAndPersistsDecrement()
    {
        var product = new Product(available: 5, leadTime: 10) { Type = "NORMAL", Name = "Cable" };

        await ProcessAndAssertPersistedAsync(product, 4);

        _notifications.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Expirable_WithFutureExpirationAndStock_PersistsDecrementWithoutNotification()
    {
        var product = new Product(available: 5, leadTime: 10)
        {
            Type = "EXPIRABLE", Name = "Milk",
            ExpiryDate = DateTime.Today.AddDays(30)
        };

        await ProcessAndAssertPersistedAsync(product, 4);

        _notifications.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Expirable_WithFutureExpirationWithoutStock_NotifiesExpiration()
    {
        DateTime expiryDate = DateTime.Today.AddDays(30);
        var product = new Product(available: 0, leadTime: 10)
        {
            Type = "EXPIRABLE", Name = "Milk",
            ExpiryDate = expiryDate
        };
        // CHARACTERIZE THE EXISTING ZERO-STOCK BEHAVIOR EVEN BEFORE EXPIRATION.
        _notifications.Setup(n => n.SendExpirationNotification("Milk", expiryDate));

        await ProcessAndAssertPersistedAsync(product, 0);

        _notifications.Verify(n => n.SendExpirationNotification("Milk", expiryDate), Times.Once);
        _notifications.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MissingOrder_ReturnsNotFound()
    {
        using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        using HttpResponseMessage response = await client.PostAsync("/orders/999/processOrder", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        _notifications.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UnsupportedProduct_ReturnsBadRequestWithoutChangingAnyStock()
    {
        var valid = new Product(available: 5, leadTime: 10) { Type = "NORMAL", Name = "Cable" };
        var invalid = new Product(available: 5, leadTime: 10) { Type = "UNKNOWN", Name = "Other" };
        var order = new Order { Items = new List<Product> { valid, invalid } };
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.Orders.Add(order);
            await context.SaveChangesAsync();
        }
        using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        using HttpResponseMessage response = await client.PostAsync($"/orders/{order.Id}/processOrder", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal($"Unsupported product type for product {invalid.Id}.",
            body.RootElement.GetProperty("error").GetString());
        using IServiceScope verificationScope = _factory.Services.CreateScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        List<Product> persisted = await verificationContext.Products.AsNoTracking().ToListAsync();
        Assert.Equal(2, persisted.Count);
        Assert.All(persisted, p => Assert.Equal(5, p.Available));
        _notifications.VerifyNoOtherCalls();
    }

    private async Task ProcessAndAssertPersistedAsync(Product product, int expectedStock)
    {
        int originalLeadTime = product.LeadTime;
        var order = new Order { Items = new List<Product> { product } };
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.Orders.Add(order);
            await context.SaveChangesAsync();
        }

        using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        using HttpResponseMessage response = await client.PostAsync($"/orders/{order.Id}/processOrder", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Single(body.RootElement.EnumerateObject());
        Assert.Equal(order.Id, body.RootElement.GetProperty("id").GetInt64());

        using IServiceScope verificationScope = _factory.Services.CreateScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Product persisted = await verificationContext.Products.AsNoTracking()
            .SingleAsync(p => p.Id == product.Id);
        Assert.Equal(expectedStock, persisted.Available);
        Assert.Equal(originalLeadTime, persisted.LeadTime);
        Assert.Equal(product.Type, persisted.Type);
        Assert.Equal(product.Name, persisted.Name);
        Assert.Equal(product.ExpiryDate, persisted.ExpiryDate);
    }

    public void Dispose() => _factory.Dispose();
}
