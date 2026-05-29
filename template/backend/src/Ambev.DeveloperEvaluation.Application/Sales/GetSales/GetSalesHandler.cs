using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Sales.GetSales;

public class GetSalesHandler : IRequestHandler<GetSalesQuery, GetSalesResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;

    public GetSalesHandler(ISaleRepository saleRepository, IMapper mapper)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
    }

    public async Task<GetSalesResult> Handle(GetSalesQuery request, CancellationToken cancellationToken)
    {
        var options = new SaleQueryOptions
        {
            Page = request.Page,
            Size = request.Size,
            Order = request.Order,
            CustomerName = request.CustomerName,
            BranchName = request.BranchName,
            IsCancelled = request.IsCancelled,
            MinTotalAmount = request.MinTotalAmount,
            MaxTotalAmount = request.MaxTotalAmount
        };

        var (items, totalCount) = await _saleRepository.ListAsync(options, cancellationToken);
        var size = request.Size <= 0 ? 10 : request.Size;

        return new GetSalesResult
        {
            Sales = _mapper.Map<List<SaleResult>>(items),
            TotalCount = totalCount,
            CurrentPage = Math.Max(1, request.Page),
            TotalPages = (int)Math.Ceiling(totalCount / (double)size)
        };
    }
}
