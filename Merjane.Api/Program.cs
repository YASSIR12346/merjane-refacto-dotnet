using Merjane.Application.Abstractions;
using Merjane.Application.Orders;
using Merjane.Application.Products;
using Merjane.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Impl;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// API COMPOSES APPLICATION CONTRACTS WITH INFRASTRUCTURE IMPLEMENTATIONS.
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IProcessOrderService, ProcessOrderService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    _ = options.UseInMemoryDatabase($"InMemoryDb");
});

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    _ = app.UseSwagger();
    _ = app.UseSwaggerUI();
}


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
