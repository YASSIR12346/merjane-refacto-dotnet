using Merjane.Application.Orders;
using Microsoft.AspNetCore.Mvc;
using Refacto.DotNet.Controllers.Dtos.Product;

namespace Refacto.DotNet.Controllers.Controllers;

// CONTROLLERS TRANSLATE HTTP CALLS INTO APPLICATION USE CASES.
[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    private readonly IProcessOrderService _processor;

    public OrdersController(IProcessOrderService processor)
    {
        _processor = processor;
    }

    [HttpPost("{orderId}/processOrder")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public ActionResult<ProcessOrderResponse> ProcessOrder(long orderId)
    {
        ProcessOrderResult result = _processor.ProcessOrder(orderId);
        return result.Status switch
        {
            ProcessingStatus.Success => new ProcessOrderResponse(result.OrderId!.Value),
            ProcessingStatus.NotFound => NotFound(),
            ProcessingStatus.InvalidData => BadRequest(new { error = result.Error }),
            _ => throw new InvalidOperationException("Unknown processing status.")
        };
    }
}
