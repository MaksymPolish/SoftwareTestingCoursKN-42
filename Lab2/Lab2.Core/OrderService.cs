namespace Lab2.Core;

public class OrderService
{
    private readonly IOrderRepository _repository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly INotificationService _notificationService;

    public OrderService(IOrderRepository repository, IPaymentGateway paymentGateway, 
        INotificationService notificationService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _paymentGateway = paymentGateway ?? throw new ArgumentNullException(nameof(paymentGateway));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    public Order PlaceOrder(int customerId, string email, List<OrderItem> items, string currency)
    {
        if (string.IsNullOrEmpty(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));
        
        if (items == null || !items.Any())
            throw new ArgumentException("Order must contain at least one item", nameof(items));

        var totalAmount = items.Sum(item => item.Quantity * item.Price);
        
        var paymentResult = _paymentGateway.ProcessPayment(totalAmount, currency);
        
        if (!paymentResult.Success)
        {
            throw new InvalidOperationException($"Payment failed: {paymentResult.ErrorMessage}");
        }

        var order = new Order(
            Id: GenerateOrderId(),
            CustomerId: customerId,
            CustomerEmail: email,
            Items: items,
            TotalAmount: totalAmount,
            Status: OrderStatus.Confirmed
        );

        _repository.Save(order);
        _notificationService.SendOrderConfirmation(email, order.Id);

        return order;
    }

    public void CancelOrder(int orderId)
    {
        var order = _repository.GetById(orderId);
        
        if (order == null)
            throw new InvalidOperationException($"Order with ID {orderId} not found");

        if (order.Status == OrderStatus.Shipped)
            throw new InvalidOperationException("Cannot cancel order that has already been shipped");

        var cancelledOrder = order with { Status = OrderStatus.Cancelled };
        _repository.Save(cancelledOrder);
        _notificationService.SendOrderCancellation(order.CustomerEmail, orderId);
    }

    public IEnumerable<Order> GetOrderHistory(int customerId)
    {
        return _repository.GetByCustomerId(customerId);
    }

    private int GenerateOrderId()
    {
        return (int)(DateTime.UtcNow.Ticks % int.MaxValue);
    }
}
