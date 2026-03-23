namespace Lab3.Api.Repositories;

using Lab3.Api.Models;

public class InMemoryProductRepository : IProductRepository
{
    private readonly Dictionary<int, Product> _products = new();
    private int _nextId = 1;

    public IEnumerable<Product> GetAll()
    {
        return _products.Values.ToList();
    }

    public Product? GetById(int id)
    {
        _products.TryGetValue(id, out var product);
        return product;
    }

    public Product Add(Product product)
    {
        product.Id = _nextId++;
        _products[product.Id] = product;
        return product;
    }

    public bool Update(int id, Product product)
    {
        if (!_products.ContainsKey(id))
            return false;

        product.Id = id;
        _products[id] = product;
        return true;
    }

    public bool Delete(int id)
    {
        return _products.Remove(id);
    }
}
