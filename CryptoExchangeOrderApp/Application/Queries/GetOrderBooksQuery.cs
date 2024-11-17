using System.Collections.Generic;
using Domain.Entities;
using MediatR;

namespace Application.Queries
{
    public class GetOrderBooksQuery : IRequest<List<OrderBook>>
    {
    }
}