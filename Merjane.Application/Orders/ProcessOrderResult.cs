namespace Merjane.Application.Orders;

public enum ProcessingStatus
{
    Success,
    NotFound,
    InvalidData
}

public sealed record ProcessOrderResult(
    ProcessingStatus Status,
    long? OrderId = null,
    string? Error = null);
