namespace Lab2.Core;

public record Order(int Id, int CustomerId, string CustomerEmail,
    List<OrderItem> Items, decimal TotalAmount, OrderStatus Status);

public record OrderItem(string ProductName, int Quantity, decimal Price);

public enum OrderStatus { Pending, Confirmed, Shipped, Cancelled }

public record PaymentResult(bool Success, string TransactionId, string ErrorMessage);
