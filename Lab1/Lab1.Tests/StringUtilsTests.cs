using Lab1.Core;
using Shouldly;
using Xunit;

namespace Lab1.Tests;

public class StringUtilsTests
{
    private readonly StringUtils _sut = new();

    [Fact]
    public void Capitalize_NormalString_CapitalizesFirstLetterOfEachWord()
    {
        // Arrange
        string input = "hello world";

        // Act
        var result = _sut.Capitalize(input);

        // Assert
        result.ShouldBe("Hello World");
    }

    [Fact]
    public void Capitalize_AllUppercase_ConvertsToProperCase()
    {
        // Arrange
        string input = "HELLO";

        // Act
        var result = _sut.Capitalize(input);

        // Assert
        result.ShouldBe("Hello");
    }

    [Fact]
    public void Capitalize_EmptyString_ReturnsEmptyString()
    {
        // Arrange
        string input = "";

        // Act
        var result = _sut.Capitalize(input);

        // Assert
        result.ShouldBe("");
    }

    [Fact]
    public void Capitalize_SingleWord_CapitalizesFirstLetter()
    {
        // Arrange
        string input = "programming";

        // Act
        var result = _sut.Capitalize(input);

        // Assert
        result.ShouldBe("Programming");
    }

    [Theory]
    [InlineData("abcd", "dcba")]
    [InlineData("hello", "olleh")]
    [InlineData("a", "a")]
    [InlineData("racecar", "racecar")]
    public void Reverse_ValidStrings_ReturnsReversedString(string input, string expected)
    {
        // Arrange
        // Input provided by InlineData

        // Act
        var result = _sut.Reverse(input);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void Reverse_NullString_ThrowsArgumentNullException()
    {
        // Arrange
        string? input = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.Reverse(input!));
    }

    [Theory]
    [InlineData("Racecar", true)]
    [InlineData("Hello", false)]
    [InlineData("noon", true)]
    [InlineData("madam", true)]
    public void IsPalindrome_VariousStrings_ReturnsTrueOrFalse(string input, bool expected)
    {
        // Arrange
        // Input provided by InlineData

        // Act
        var result = _sut.IsPalindrome(input);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void IsPalindrome_NullString_ThrowsArgumentNullException()
    {
        // Arrange
        string? input = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.IsPalindrome(input!));
    }

    [Theory]
    [InlineData("Hello World", 5, "He...")]
    [InlineData("Hi", 10, "Hi")]
    [InlineData("Testing", 3, "...")]
    [InlineData("Short", 8, "Short")]
    public void Truncate_VariousInputs_ReturnsProperlyTruncatedString(string input, int maxLength, string expected)
    {
        // Arrange
        // Input provided by InlineData

        // Act
        var result = _sut.Truncate(input, maxLength);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void Truncate_NullString_ThrowsArgumentNullException()
    {
        // Arrange
        string? input = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.Truncate(input!, 5));
    }

    [Fact]
    public void Truncate_NegativeMaxLength_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        string input = "test";

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => _sut.Truncate(input, -1));
    }
}
