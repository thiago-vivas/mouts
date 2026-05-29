using Ambev.DeveloperEvaluation.Domain.Entities;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Application.Sales.Common;

/// <summary>Read model for a sale, shared across Create/Get/List/Update results.</summary>
public class SaleResult
{
    public Guid Id { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public ExternalReferenceDto Customer { get; set; } = new();
    public ExternalReferenceDto Branch { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<SaleItemResult> Items { get; set; } = new();
}

/// <summary>Read model for a sale line item.</summary>
public class SaleItemResult
{
    public Guid Id { get; set; }
    public ExternalReferenceDto Product { get; set; } = new();
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsCancelled { get; set; }
}

/// <summary>AutoMapper profile mapping domain entities/VOs to the shared read models.</summary>
public class SaleResultProfile : Profile
{
    public SaleResultProfile()
    {
        CreateMap<Domain.ValueObjects.ExternalReference, ExternalReferenceDto>();
        CreateMap<SaleItem, SaleItemResult>();
        CreateMap<Sale, SaleResult>()
            .ForMember(d => d.Items, opt => opt.MapFrom(s => s.Items));
    }
}
