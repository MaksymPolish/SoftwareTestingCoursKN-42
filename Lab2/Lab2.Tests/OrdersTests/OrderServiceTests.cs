namespace Lab2.Tests;

using Lab2.Core;
using NSubstitute;
using Xunit;
using Shouldly;

public class OrderServiceTests
{
    private readonly IOrderRepository _repository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly INotificationService _notificationService;
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _repository = Substitute.For<IOrderRepository>();
        _paymentGateway = Substitute.For<IPaymentGateway>();
        _notificationService = Substitute.For<INotificationService>();
        _sut = new OrderService(_repository, _paymentGateway, _notificationService);
    }

    // Test 1: Successful Order Placement with Payment Success

    [Fact]
    public void PlaceOrder_WhenPaymentSucceeds_SavesOrderAndSendsConfirmation()
    {
        // Arrange
        _paymentGateway.ProcessPayment(Arg.Any<decimal>(), "USD")
            .Returns(new PaymentResult(true, "TX-123", null!));

        var items = new List<OrderItem>
        {
            new OrderItem("Laptop", 1, 1000m),
            new OrderItem("Mouse", 2, 25m)
        };
        var customerId = 1;
        var email = "test@example.com";

        // Act
        var result = _sut.PlaceOrder(customerId, email, items, "USD");

        // Assert
        result.Status.ShouldBe(OrderStatus.Confirmed);
        result.TotalAmount.ShouldBe(1050m);
        result.CustomerId.ShouldBe(customerId);
        result.CustomerEmail.ShouldBe(email);

        _repository.Received(1).Save(Arg.Is<Order>(o => 
            o.Status == OrderStatus.Confirmed && 
            o.TotalAmount == 1050m));
        
        _notificationService.Received(1).SendOrderConfirmation(email, Arg.Any<int>());
    }

    // Test 2: Order Placement with Payment Failure

    [Fact]
    public void PlaceOrder_WhenPaymentFails_ThrowsExceptionAndDoesNotSaveOrder()
    {
        // Arrange
        _paymentGateway.ProcessPayment(Arg.Any<decimal>(), "USD")
            .Returns(new PaymentResult(false, null!, "Insufficient funds"));

        var items = new List<OrderItem> { new OrderItem("Product", 1, 100m) };

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() =>
            _sut.PlaceOrder(1, "test@example.com", items, "USD"));

        exception.Message.ShouldContain("Payment failed");

        _repository.DidNotReceive().Save(Arg.Any<Order>());
        _notificationService.DidNotReceive().SendOrderConfirmation(Arg.Any<string>(), Arg.Any<int>());
    }

    // Test 3: Theory Test - Successful Payments with Different Currencies

    [Theory]
    [InlineData("USD", "100")]
    [InlineData("EUR", "150")]
    [InlineData("GBP", "75")]
    public void PlaceOrder_WithDifferentCurrencies_ProcessesPaymentSuccessfully(string currency, string amountStr)
    {
        // Arrange
        var amount = decimal.Parse(amountStr);
        _paymentGateway.ProcessPayment(amount, currency)
            .Returns(new PaymentResult(true, "TX-ABC", null!));

        var items = new List<OrderItem> { new OrderItem("Item", 1, amount) };

        // Act
        var order = _sut.PlaceOrder(1, "customer@example.com", items, currency);

        // Assert
        order.Status.ShouldBe(OrderStatus.Confirmed);
        _repository.Received(1).Save(Arg.Any<Order>());
        _notificationService.Received(1).SendOrderConfirmation("customer@example.com", Arg.Any<int>());
    }

    // Test 4: Order Placement with Invalid Email

    [Fact]
    public void PlaceOrder_WithEmptyEmail_ThrowsArgumentException()
    {
        // Arrange
        var items = new List<OrderItem> { new OrderItem("Product", 1, 100m) };

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            _sut.PlaceOrder(1, "", items, "USD"));

        _paymentGateway.DidNotReceive().ProcessPayment(Arg.Any<decimal>(), Arg.Any<string>());
        _repository.DidNotReceive().Save(Arg.Any<Order>());
    }

    // Test 5: Order Placement with Empty Items List

    [Fact]
    public void PlaceOrder_WithEmptyItems_ThrowsArgumentException()
    {
        // Arrange
        var items = new List<OrderItem>();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            _sut.PlaceOrder(1, "test@example.com", items, "USD"));

        _paymentGateway.DidNotReceive().ProcessPayment(Arg.Any<decimal>(), Arg.Any<string>());
        _repository.DidNotReceive().Save(Arg.Any<Order>());
    }

    // Test 6: Verify Payment Called with Correct Amount

    [Fact]
    public void PlaceOrder_VerifiesPaymentAmountCalculation()
    {
        // Arrange
        _paymentGateway.ProcessPayment(Arg.Any<decimal>(), "USD")
            .Returns(new PaymentResult(true, "TX-123", null!));

        var items = new List<OrderItem>
        {
            new OrderItem("Item1", 2, 50m),
            new OrderItem("Item2", 1, 30m)
        };

        // Act
        _sut.PlaceOrder(1, "test@example.com", items, "USD");

        // Assert - Verify payment was called with correct total (2*50 + 1*30 = 130)
        _paymentGateway.Received(1).ProcessPayment(130m, "USD");
    }

    // Test 7: Cancel Order Successfully

    [Fact]
    public void CancelOrder_WhenOrderExists_CancelsAndNotifies()
    {
        // Arrange
        var order = new Order(
            Id: 1,
            CustomerId: 1,
            CustomerEmail: "test@example.com",
            Items: new List<OrderItem> { new OrderItem("Product", 1, 100m) },
            TotalAmount: 100m,
            Status: OrderStatus.Confirmed
        );

        _repository.GetById(1).Returns(order);

        // Act
        _sut.CancelOrder(1);

        // Assert
        _repository.Received(1).Save(Arg.Is<Order>(o => 
            o.Id == 1 && o.Status == OrderStatus.Cancelled));
        
        _notificationService.Received(1).SendOrderCancellation("test@example.com", 1);
    }

    // Test 8: Cannot Cancel Shipped Order

    [Fact]
    public void CancelOrder_WhenOrderShipped_ThrowsException()
    {
        // Arrange
        var order = new Order(
            Id: 1,
            CustomerId: 1,
            CustomerEmail: "test@example.com",
            Items: new List<OrderItem> { new OrderItem("Product", 1, 100m) },
            TotalAmount: 100m,
            Status: OrderStatus.Shipped
        );

        _repository.GetById(1).Returns(order);

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => _sut.CancelOrder(1));
        exception.Message.ShouldContain("already been shipped");

        _repository.DidNotReceive().Save(Arg.Any<Order>());
        _notificationService.DidNotReceive().SendOrderCancellation(Arg.Any<string>(), Arg.Any<int>());
    }

    // Test 9: Get Order History

    [Fact]
    public void GetOrderHistory_ReturnsCustomerOrders()
    {
        // Arrange
        var customerId = 1;
        var orders = new List<Order>
        {
            new Order(1, customerId, "test@example.com", 
                new List<OrderItem> { new OrderItem("Product1", 1, 100m) }, 
                100m, OrderStatus.Confirmed),
            new Order(2, customerId, "test@example.com",
                new List<OrderItem> { new OrderItem("Product2", 2, 50m) },
                100m, OrderStatus.Confirmed)
        };

        _repository.GetByCustomerId(customerId).Returns(orders);

        // Act
        var result = _sut.GetOrderHistory(customerId);

        // Assert
        var resultList = result.ToList();
        resultList.Count.ShouldBe(2);
        resultList.ShouldAllBe(o => o.CustomerId == customerId);
    }

    // Test 10: Verify Notification Service Call Order

    [Fact]
    public void PlaceOrder_NotificationSentAfterRepositorySave()
    {
        // Arrange
        var callSequence = new List<string>();

        _paymentGateway.ProcessPayment(Arg.Any<decimal>(), "USD")
            .Returns(new PaymentResult(true, "TX-123", null!));

        _repository.When(r => r.Save(Arg.Any<Order>()))
            .Do(c => callSequence.Add("Save"));

        _notificationService.When(n => n.SendOrderConfirmation(Arg.Any<string>(), Arg.Any<int>()))
            .Do(c => callSequence.Add("Notify"));

        var items = new List<OrderItem> { new OrderItem("Product", 1, 100m) };

        // Act
        _sut.PlaceOrder(1, "test@example.com", items, "USD");

        // Assert
        callSequence.Count.ShouldBe(2);
        callSequence[0].ShouldBe("Save");
        callSequence[1].ShouldBe("Notify");
    }

    // Test 11: Argument Validation on Constructor

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new OrderService(null, _paymentGateway, _notificationService));
    }

    // Test 12: Cancel Non-Existent Order

    [Fact]
    public void CancelOrder_WhenOrderNotFound_ThrowsException()
    {
        // Arrange
        _repository.GetById(999).Returns((Order?)null);

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => _sut.CancelOrder(999));
        exception.Message.ShouldContain("not found");
    }
}
