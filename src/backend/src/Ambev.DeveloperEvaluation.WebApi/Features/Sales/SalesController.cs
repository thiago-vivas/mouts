using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;
using Ambev.DeveloperEvaluation.Application.Sales.Common;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Application.Sales.GetSales;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.GetSales;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales;

/// <summary>CRUD + cancellation endpoints for sales records. Requires authentication.</summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class SalesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SalesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Creates a new sale (raises SaleCreated).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResult>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSaleCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            new ApiResponseWithData<SaleResult>
            {
                Success = true,
                Message = "Sale created successfully",
                Data = result
            });
    }

    /// <summary>Lists sales with pagination, ordering and filtering.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<SaleResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] GetSalesRequest request, CancellationToken cancellationToken)
    {
        var query = new GetSalesQuery
        {
            Page = request.Page,
            Size = request.Size,
            Order = request.Order,
            CustomerName = request.CustomerName,
            BranchName = request.BranchName,
            IsCancelled = request.IsCancelled,
            MinTotalAmount = request.MinTotalAmount,
            MaxTotalAmount = request.MaxTotalAmount,
            MinSaleDate = request.MinSaleDate,
            MaxSaleDate = request.MaxSaleDate
        };

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(new PaginatedResponse<SaleResult>
        {
            Success = true,
            Message = "Sales retrieved successfully",
            Data = result.Sales,
            CurrentPage = result.CurrentPage,
            TotalPages = result.TotalPages,
            TotalCount = result.TotalCount,
            TotalItems = result.TotalCount
        });
    }

    /// <summary>Retrieves a single sale by id.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSaleQuery(id), cancellationToken);
        return Ok(new ApiResponseWithData<SaleResult>
        {
            Success = true,
            Message = "Sale retrieved successfully",
            Data = result
        });
    }

    /// <summary>Updates a sale, replacing its items (raises SaleModified).</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateSaleCommand command, CancellationToken cancellationToken)
    {
        command.Id = id;
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponseWithData<SaleResult>
        {
            Success = true,
            Message = "Sale updated successfully",
            Data = result
        });
    }

    /// <summary>Permanently deletes a sale.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteSaleCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Cancels an entire sale (raises SaleCancelled).</summary>
    [HttpPatch("{id}/cancel")]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelSaleCommand(id), cancellationToken);
        return Ok(new ApiResponseWithData<SaleResult>
        {
            Success = true,
            Message = "Sale cancelled successfully",
            Data = result
        });
    }

    /// <summary>Cancels a single item within a sale (raises ItemCancelled).</summary>
    [HttpPatch("{id}/items/{itemId}/cancel")]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelItem([FromRoute] Guid id, [FromRoute] Guid itemId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelSaleItemCommand(id, itemId), cancellationToken);
        return Ok(new ApiResponseWithData<SaleResult>
        {
            Success = true,
            Message = "Sale item cancelled successfully",
            Data = result
        });
    }
}
