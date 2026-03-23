namespace Lab3.Api.Controllers;

using Lab3.Api.Models;
using Lab3.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _repository;

    public ProductsController(IProductRepository repository)
    {
        _repository = repository;
    }

    // GET api/products
    [HttpGet]
    public ActionResult<IEnumerable<Product>> GetProducts()
    {
        var products = _repository.GetAll();
        return Ok(products);
    }

    // GET api/products/{id}
    [HttpGet("{id}")]
    public ActionResult<Product> GetProductById(int id)
    {
        var product = _repository.GetById(id);
        if (product == null)
            return NotFound();

        return Ok(product);
    }

    // POST api/products
    [HttpPost]
    public ActionResult<Product> CreateProduct([FromBody] CreateProductRequest request)
    {
        var product = new Product
        {
            Name = request.Name,
            Price = request.Price
        };

        var createdProduct = _repository.Add(product);
        return CreatedAtAction(nameof(GetProductById), new { id = createdProduct.Id }, createdProduct);
    }

    // PUT api/products/{id}
    [HttpPut("{id}")]
    public ActionResult<Product> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
    {
        var product = _repository.GetById(id);
        if (product == null)
            return NotFound();

        product.Name = request.Name;
        product.Price = request.Price;

        var success = _repository.Update(id, product);
        if (!success)
            return NotFound();

        return Ok(product);
    }

    // DELETE api/products/{id}
    [HttpDelete("{id}")]
    public IActionResult DeleteProduct(int id)
    {
        var success = _repository.Delete(id);
        if (!success)
            return NotFound();

        return NoContent();
    }
}

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class UpdateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
