namespace Lab3.Api.Repositories;

using Lab3.Api.Models;

public interface IProductRepository
{
    // Get all products
    IEnumerable<Product> GetAll();

    // Get product by id
    Product? GetById(int id);

    // Add a new product
    Product Add(Product product);

    // Update existing product
    bool Update(int id, Product product);

    // Delete product by id
    bool Delete(int id);
}
