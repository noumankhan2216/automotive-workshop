using AutomotiveWorkshop.Application.DTOs.Invoices;
using AutomotiveWorkshop.Application.Services;
using AutomotiveWorkshop.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutomotiveWorkshop.Api.Controllers;

[ApiController]
[Route("api/v1/invoices")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService) => _invoiceService = invoiceService;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] InvoiceStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _invoiceService.GetAllAsync(status, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var invoice = await _invoiceService.GetByIdAsync(id, ct);
        return invoice is null ? NotFound() : Ok(invoice);
    }

    [HttpPost("from-work-order")]
    public async Task<IActionResult> CreateFromWorkOrder([FromBody] CreateInvoiceFromWorkOrderRequest request, CancellationToken ct)
    {
        try
        {
            var invoice = await _invoiceService.CreateFromWorkOrderAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateInvoiceStatusRequest request, CancellationToken ct)
    {
        var invoice = await _invoiceService.UpdateStatusAsync(id, request, ct);
        return invoice is null ? NotFound() : Ok(invoice);
    }
}
