namespace Ambev.DeveloperEvaluation.WebApi.Common;

public class PaginatedResponse<T> : ApiResponseWithData<IEnumerable<T>>
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }

    /// <summary>Alias for <see cref="TotalCount"/> using the documented field name (general-api.md).</summary>
    public int TotalItems { get; set; }
}