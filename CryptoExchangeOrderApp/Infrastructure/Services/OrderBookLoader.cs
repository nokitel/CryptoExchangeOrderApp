using Application.Common.Interfaces;
using System.Text.Json;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using System;

namespace Infrastructure.Services
{
    public class OrderBookLoader
    {
        private readonly IOrderBookRepository _repository;
        private readonly string _dataFolderPath;

        public OrderBookLoader(string dataFolderPath, IOrderBookRepository repository)
        {
            _dataFolderPath = dataFolderPath ?? throw new ArgumentNullException(nameof(dataFolderPath));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task LoadAllOrderBooksIntoDatabaseAsync()
        {
            for (int i = 1; i <= 10; i++)
            {
                var fileName = Path.Combine(_dataFolderPath, $"exchange-{i:D2}.json");
                if (!File.Exists(fileName)) continue;

                var fileContent = await File.ReadAllTextAsync(fileName);
                var orderBook = JsonSerializer.Deserialize<OrderBook>(fileContent);

                if (orderBook != null)
                {
                    await _repository.AddAsync(orderBook, CancellationToken.None);
                }
            }
        }
    }
}