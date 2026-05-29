namespace Ambev.DeveloperEvaluation.Application.Sales.Common;

/// <summary>DTO mirroring an External Identity reference (id + denormalized name).</summary>
public class ExternalReferenceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
