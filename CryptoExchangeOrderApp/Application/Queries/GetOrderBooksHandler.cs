using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Queries
{
    public class GetOrderBooksHandler : IRequestHandler<GetOrderBooksQuery, List<OrderBook>>
    {
        private readonly IOrderBookRepository _repository;

        public GetOrderBooksHandler(IOrderBookRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<OrderBook>> Handle(GetOrderBooksQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetAllAsync(cancellationToken);
        }
    }
}