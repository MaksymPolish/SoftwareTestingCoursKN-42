namespace Lab3.Tests;

using Lab3.Api.Models;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using Xunit;

public class ProductsApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProductsApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "valid-test-key");
        return client;
    }

    // Task 1.2: Test GET /api/products returns all seeded products
    [Fact]
    public async Task GetProducts_ReturnsAllSeededProducts()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/products", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var products = await response.Content.ReadFromJsonAsync<List<Product>>(TestContext.Current.CancellationToken);
        products.ShouldNotBeNull();
        // Check that we have products (at least the seeded ones)
        products.Count.ShouldBeGreaterThan(0);
        // Check for at least one of the seeded products (Mouse is never modified)
        var productNames = products.Select(p => p.Name).ToList();
        productNames.ShouldContain("Mouse");
        // Verify products have valid IDs and prices
        products.ShouldAllBe(p => p.Id > 0 && p.Price > 0);
    }

    // Task 1.3: Test GET /api/products/{id} returns 200 for existing product
    [Fact]
    public async Task GetProductById_WithValidId_Returns200()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/products/1", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<Product>(TestContext.Current.CancellationToken);
        product.ShouldNotBeNull();
        product.Id.ShouldBe(1);
        product.Name.ShouldBe("Laptop");
        product.Price.ShouldBe(999.99m);
    }

    // Task 1.3: Test GET /api/products/{id} returns 404 for non-existing product
    [Fact]
    public async Task GetProductById_WithInvalidId_Returns404()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/products/999", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // Task 1.4: Test POST /api/products creates product and returns 201
    [Fact]
    public async Task CreateProduct_WithValidData_Returns201()
    {
        // Arrange
        var client = CreateClient();
        var newProduct = new { Name = "Keyboard", Price = 49.99m };

        // Act
        var response = await client.PostAsJsonAsync("/api/products", newProduct, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();

        var product = await response.Content.ReadFromJsonAsync<Product>(TestContext.Current.CancellationToken);
        product.ShouldNotBeNull();
        product.Name.ShouldBe("Keyboard");
        product.Price.ShouldBe(49.99m);
        product.Id.ShouldBeGreaterThan(0);
    }

    // Additional test: Test PUT /api/products/{id} updates product
    [Fact]
    public async Task UpdateProduct_WithValidId_Returns200()
    {
        // Arrange
        var client = CreateClient();
        var updatedProduct = new { Name = "Updated Laptop", Price = 1099.99m };

        // Act
        var response = await client.PutAsJsonAsync("/api/products/1", updatedProduct, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<Product>(TestContext.Current.CancellationToken);
        product.ShouldNotBeNull();
        product.Name.ShouldBe("Updated Laptop");
        product.Price.ShouldBe(1099.99m);
    }

    // Additional test: Test DELETE /api/products/{id}
    [Fact]
    public async Task DeleteProduct_WithValidId_Returns204()
    {
        // Arrange  
        var client = CreateClient();

        // First create a product to delete
        var createResponse = await client.PostAsJsonAsync("/api/products", 
            new { Name = "TestDelete", Price = 99.99m }, TestContext.Current.CancellationToken);
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<Product>(TestContext.Current.CancellationToken);
        var productId = createdProduct!.Id;

        // Act
        var deleteResponse = await client.DeleteAsync($"/api/products/{productId}", TestContext.Current.CancellationToken);

        // Assert
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify deletion
        var getResponse = await client.GetAsync($"/api/products/{productId}", TestContext.Current.CancellationToken);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
