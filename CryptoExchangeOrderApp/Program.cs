using Application.Buy;
using Application.Common.Interfaces;
using Application.Sell;
using FluentValidation;
using Application.Common.Behaviors;
using Infrastructure.Data;
using Infrastructure.Services;
using Infrastructure.Repositories;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CryptoExchangeOrderApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Build Configuration
            var configuration = BuildConfiguration();

            // Setup Dependency Injection
            var serviceProvider = ConfigureServices(configuration);

            // Initialize Database and Load Order Books
            await InitializeDatabaseAsync(serviceProvider);

            // Run the main application loop
            await RunApplicationAsync(serviceProvider);
        }

        private static IConfiguration BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) // Ensure correct base path
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
        }

        private static ServiceProvider ConfigureServices(IConfiguration configuration)
        {
            var services = new ServiceCollection();

            // Logging
            services.AddLogging(configure => configure.AddConsole());

            // Configuration
            services.AddSingleton(configuration);

            // Database Context
            services.AddDbContext<ExchangeContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            // Register IDbContext
            services.AddScoped<IDbContext>(provider => provider.GetService<ExchangeContext>());

            // Repositories
            services.AddScoped<IOrderBookRepository, OrderBookRepository>();

            // MediatR
            services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(typeof(BuyBtcCommand).Assembly);
            });

            // FluentValidation
            services.AddValidatorsFromAssembly(typeof(BuyBtcCommandValidator).Assembly);

            // Services
            services.AddScoped(provider =>
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
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            return services.BuildServiceProvider();
        }

        private static async Task InitializeDatabaseAsync(ServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var loader = scope.ServiceProvider.GetRequiredService<OrderBookLoader>();

            try
            {
                // Apply pending migrations
                if (context is ExchangeContext dbContext)
                {
                    await dbContext.Database.MigrateAsync(); // Use migrations for PostgreSQL
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

        private static async Task RunApplicationAsync(ServiceProvider serviceProvider)
        {
            var mediator = serviceProvider.GetRequiredService<IMediator>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            while (true)
            {
                Console.WriteLine("Enter order type (buy/sell) and amount (or 'exit' to quit):");
                var inputLine = Console.ReadLine();

                if (inputLine?.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase) == true)
                {
                    break;
                }

                var input = inputLine?.Split();
                if (input?.Length == 2 && decimal.TryParse(input[1], out var amount))
                {
                    try
                    {
                        if (input[0].Equals("buy", StringComparison.OrdinalIgnoreCase))
                        {
                            var command = new BuyBtcCommand(amount);
                            var result = await mediator.Send(command);

                            DisplayExecutionPlan("Buy", result);
                        }
                        else if (input[0].Equals("sell", StringComparison.OrdinalIgnoreCase))
                        {
                            var command = new SellBtcCommand(amount);
                            var result = await mediator.Send(command);

                            DisplayExecutionPlan("Sell", result);
                        }
                        else
                        {
                            Console.WriteLine("Invalid order type. Please enter 'buy' or 'sell'.");
                        }
                    }
                    catch (ValidationException ex)
                    {
                        logger.LogWarning(ex, "Validation error occurred.");
                        Console.WriteLine($"Validation Error: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "An error occurred while executing the order.");
                        Console.WriteLine($"Error executing order: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("Invalid input. Please enter the order type and amount (e.g., 'buy 1.5').");
                }
            }
        }

        private static void DisplayExecutionPlan(string orderType, ExecutionResult result)
        {
            Console.WriteLine($"Execution Plan for {orderType} Order:");
            foreach (var order in result.Orders)
            {
                Console.WriteLine($"Order ID: {order.Id}, Exchange: {order.ExchangeName}, Amount: {order.Amount:F8}, Price: {order.Price:F2}");
            }

            if (orderType.Equals("Buy", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Total Cost: {result.TotalCost:F2} EUR");
            }
            else if (orderType.Equals("Sell", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Total Revenue: {result.TotalRevenue:F2} EUR");
            }
        }
    }
}