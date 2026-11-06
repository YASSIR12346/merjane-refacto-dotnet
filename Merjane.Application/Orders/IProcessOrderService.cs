namespace Merjane.Application.Orders;

public interface IProcessOrderService
{
    ProcessOrderResult ProcessOrder(long orderId);
}
