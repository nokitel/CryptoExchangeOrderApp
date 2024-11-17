using Application.Buy;
using Application.Sell;
using Application.Common.Interfaces;
using Application.Common.Behaviors;
using FluentValidation;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var configuration = builder.Configuration;
builder.Services.AddSingleton<IConfiguration>(configuration);

// Logging
builder.Services.AddLogging();

// Database Context
builder.Services.AddDbContext<ExchangeContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

// Register IDbContext
builder.Services.AddScoped<IDbContext>(provider => provider.GetRequiredService<ExchangeContext>());

// Repositories
builder.Services.AddScoped<IOrderBookRepository, OrderBookRepository>();

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(BuyBtcCommand).Assembly);
});

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(BuyBtcCommandValidator).Assembly);

// Services
builder.Services.AddScoped(provider =>
{
    var folderPath = configuration["DataFolderPath"];
    if (string.IsNullOrEmpty(folderPath))
    {
        throw new ArgumentNullException("DataFolderPath", "Data folder path is not configured in appsettings.json.");
    }
    var context = provider.GetRequiredService<IOrderBookRepository>();
    return new OrderBookLoader(folderPath, context);
});

// MediatR Pipeline Behaviors
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

var app = builder.Build();

// Initialize Database and Load Order Books
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<IDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var loader = scope.ServiceProvider.GetRequiredService<OrderBookLoader>();

    try
    {
        // Apply pending migrations
        if (context is ExchangeContext dbContext)
        {
            await dbContext.Database.MigrateAsync();
        }

        // Load Order Books
        await loader.LoadAllOrderBooksIntoDatabaseAsync();
        logger.LogInformation("Order books loaded into PostgreSQL database.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database initialization.");
        throw;
    }
}

// Define the endpoint
app.MapPost("/api/order", async (
    [FromBody] OrderRequest orderRequest,
    IMediator mediator,
    ILogger<Program> logger) =>
{
    try
    {
        ExecutionResult result;
        if (orderRequest.OrderType.Equals("buy", StringComparison.OrdinalIgnoreCase))
        {
            var command = new BuyBtcCommand(orderRequest.Amount);
            result = await mediator.Send(command);
        }
        else if (orderRequest.OrderType.Equals("sell", StringComparison.OrdinalIgnoreCase))
        {
            var command = new SellBtcCommand(orderRequest.Amount);
            result = await mediator.Send(command);
        }
        else
        {
            return Results.BadRequest("Invalid order type. Use 'buy' or 'sell'.");
        }

        return Results.Ok(result);
    }
    catch (ValidationException ex)
    {
        logger.LogWarning(ex, "Validation error occurred.");
        return Results.BadRequest(new { Error = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while executing the order.");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

app.Run();