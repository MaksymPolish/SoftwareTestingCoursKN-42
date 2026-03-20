namespace Lab2.Core;

public interface IOrderRepository
{
    Order GetById(int id);
    void Save(Order order);
    IEnumerable<Order> GetByCustomerId(int customerId);
}

public interface IPaymentGateway
{
    PaymentResult ProcessPayment(decimal amount, string currency);
}

public interface INotificationService
{
    void SendOrderConfirmation(string email, int orderId);
    void SendOrderCancellation(string email, int orderId);
}
