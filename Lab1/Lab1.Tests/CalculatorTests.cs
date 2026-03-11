using Lab1.Core;
using Shouldly;
using Xunit;

namespace Lab1.Tests;

public class CalculatorTests
{
    private readonly Calculator _sut = new();

    [Fact]
    public void Add_TwoPositiveNumbers_ReturnsSum()
    {
        // Arrange
        double a = 2;
        double b = 3;

        // Act
        var result = _sut.Add(a, b);

        // Assert
        result.ShouldBe(5);
    }

    [Fact]
    public void Add_PositiveAndNegative_ReturnsCorrectSum()
    {
        // Arrange
        double a = 10;
        double b = -5;

        // Act
        var result = _sut.Add(a, b);

        // Assert
        result.ShouldBe(5);
    }

    [Fact]
    public void Add_FloatingPointNumbers_ReturnsSum()
    {
        // Arrange
        double a = 0.1;
        double b = 0.2;

        // Act
        var result = _sut.Add(a, b);

        // Assert
        result.ShouldBe(0.3, 0.0001);
    }

    [Fact]
    public void Subtract_LargerFromSmaller_ReturnsNegative()
    {
        // Arrange
        double a = 2;
        double b = 5;

        // Act
        var result = _sut.Subtract(a, b);

        // Assert
        result.ShouldBe(-3);
    }

    [Fact]
    public void Subtract_TwoNumbers_ReturnsCorrectDifference()
    {
        // Arrange
        double a = 10;
        double b = 3;

        // Act
        var result = _sut.Subtract(a, b);

        // Assert
        result.ShouldBe(7);
    }

    [Fact]
    public void Multiply_TwoNumbers_ReturnsProduct()
    {
        // Arrange
        double a = 4;
        double b = 5;

        // Act
        var result = _sut.Multiply(a, b);

        // Assert
        result.ShouldBe(20);
    }

    [Fact]
    public void Multiply_ByZero_ReturnsZero()
    {
        // Arrange
        double a = 15;
        double b = 0;

        // Act
        var result = _sut.Multiply(a, b);

        // Assert
        result.ShouldBe(0);
    }

    [Theory]
    [InlineData(10, 5, 2)]
    [InlineData(-10, 2, -5)]
    [InlineData(0, 1, 0)]
    [InlineData(15, 3, 5)]
    public void Divide_ValidInputs_ReturnsQuotient(double a, double b, double expected)
    {
        // Arrange
        // Values provided by InlineData

        // Act
        var result = _sut.Divide(a, b);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void Divide_ByZero_ThrowsDivideByZeroException()
    {
        // Arrange
        double a = 10;
        double b = 0;

        // Act & Assert
        Should.Throw<DivideByZeroException>(() => _sut.Divide(a, b));
    }
}
