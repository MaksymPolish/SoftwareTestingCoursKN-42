using Lab1.Core;
using Shouldly;
using Xunit;

namespace Lab1.Tests;

public class CollectionUtilsTests
{
    private readonly CollectionUtils _sut = new();

    [Fact]
    public void Average_NormalList_ReturnsCorrectAverage()
    {
        // Arrange
        var numbers = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

        // Act
        var result = _sut.Average(numbers);

        // Assert
        result.ShouldBe(3.0);
    }

    [Fact]
    public void Average_SingleElement_ReturnsThatElement()
    {
        // Arrange
        var numbers = new[] { 42.0 };

        // Act
        var result = _sut.Average(numbers);

        // Assert
        result.ShouldBe(42.0);
    }

    [Fact]
    public void Average_EmptyCollection_ThrowsInvalidOperationException()
    {
        // Arrange
        var numbers = new double[] { };

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _sut.Average(numbers));
    }

    [Fact]
    public void Average_NullCollection_ThrowsArgumentNullException()
    {
        // Arrange
        double[]? numbers = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.Average(numbers!));
    }

    [Fact]
    public void Max_NumbersList_ReturnsMaximumValue()
    {
        // Arrange
        var numbers = new[] { 3, 1, 4, 1, 5 };

        // Act
        var result = _sut.Max(numbers);

        // Assert
        result.ShouldBe(5);
    }

    [Fact]
    public void Max_StringsList_ReturnsMaximumString()
    {
        // Arrange
        var strings = new[] { "apple", "cherry", "banana" };

        // Act
        var result = _sut.Max(strings);

        // Assert
        result.ShouldBe("cherry");
    }

    [Fact]
    public void Max_SingleElement_ReturnsThatElement()
    {
        // Arrange
        var numbers = new[] { 99 };

        // Act
        var result = _sut.Max(numbers);

        // Assert
        result.ShouldBe(99);
    }

    [Fact]
    public void Max_EmptyCollection_ThrowsInvalidOperationException()
    {
        // Arrange
        var numbers = new int[] { };

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _sut.Max(numbers));
    }

    [Fact]
    public void Max_NullCollection_ThrowsArgumentNullException()
    {
        // Arrange
        int[]? numbers = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.Max(numbers!));
    }

    [Fact]
    public void Distinct_ListWithDuplicates_ReturnUniqueElements()
    {
        // Arrange
        var numbers = new[] { 1, 2, 2, 3, 1 };

        // Act
        var result = _sut.Distinct(numbers).ToList();

        // Assert
        result.ShouldBe(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Distinct_ListWithoutDuplicates_ReturnsSameList()
    {
        // Arrange
        var numbers = new[] { 1, 2, 3 };

        // Act
        var result = _sut.Distinct(numbers).ToList();

        // Assert
        result.ShouldBe(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Distinct_SingleElement_ReturnsThatElement()
    {
        // Arrange
        var numbers = new[] { 42 };

        // Act
        var result = _sut.Distinct(numbers).ToList();

        // Assert
        result.ShouldBe(new[] { 42 });
    }

    [Fact]
    public void Distinct_NullCollection_ThrowsArgumentNullException()
    {
        // Arrange
        int[]? numbers = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.Distinct(numbers!));
    }

    [Fact]
    public void Chunk_NormalList_ReturnsChunks()
    {
        // Arrange
        var numbers = new[] { 1, 2, 3, 4, 5 };

        // Act
        var result = _sut.Chunk(numbers, 2).Select(c => c.ToList()).ToList();

        // Assert
        result.Count.ShouldBe(3);
        result[0].ShouldBe(new[] { 1, 2 });
        result[1].ShouldBe(new[] { 3, 4 });
        result[2].ShouldBe(new[] { 5 });
    }

    [Fact]
    public void Chunk_ChunkSizeGreaterThanListSize_ReturnsSingleChunk()
    {
        // Arrange
        var numbers = new[] { 1, 2 };

        // Act
        var result = _sut.Chunk(numbers, 5).Select(c => c.ToList()).ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldBe(new[] { 1, 2 });
    }

    [Fact]
    public void Chunk_SingleElement_ReturnsSingleChunk()
    {
        // Arrange
        var numbers = new[] { 42 };

        // Act
        var result = _sut.Chunk(numbers, 1).Select(c => c.ToList()).ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldBe(new[] { 42 });
    }

    [Fact]
    public void Chunk_ZeroSize_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var numbers = new[] { 1, 2, 3 };

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => _sut.Chunk(numbers, 0));
    }

    [Fact]
    public void Chunk_NegativeSize_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var numbers = new[] { 1, 2, 3 };

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => _sut.Chunk(numbers, -1));
    }

    [Fact]
    public void Chunk_NullCollection_ThrowsArgumentNullException()
    {
        // Arrange
        int[]? numbers = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.Chunk(numbers!, 2));
    }
}
