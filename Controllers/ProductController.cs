using AuthApi.Dtos;
using AuthApi.Models;
using AuthApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AuthApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requires authentication for all actions
    public class ProductsController : ControllerBase
    {
        private readonly ProductService _productService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProductsController(ProductService productService, UserManager<ApplicationUser> userManager)
        {
            _productService = productService;
            _userManager = userManager;
        }

        // GET: api/products - Accessible by all authenticated users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
        {
            var products = await _productService.GetAllAsync();
            return Ok(products.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                UserId = p.UserId.ToString()
            }));
        }

        // GET: api/products/{id} - Accessible by all authenticated users
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetById(string id)
        {
            var product = await _productService.GetByIdAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                UserId = product.UserId.ToString()
            };
        }

        // POST: api/products - Admin only
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProductDto>> Create(ProductCreateDto productDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var product = new Product
            {
                Name = productDto.Name,
                Description = productDto.Description,
                UserId = Guid.Parse(userId)
            };

            await _productService.CreateAsync(product);

            return CreatedAtAction(
                nameof(GetById),
                new { id = product.Id },
                new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    UserId = product.UserId.ToString()
                });
        }

        // PUT: api/products/{id} - Admin only
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Update(string id, ProductUpdateDto productDto)
        {
            var existingProduct = await _productService.GetByIdAsync(id);

            if (existingProduct == null)
            {
                return NotFound();
            }

            existingProduct.Name = productDto.Name;
            existingProduct.Description = productDto.Description;

            var success = await _productService.UpdateAsync(id, existingProduct);

            if (!success)
            {
                return BadRequest("Error updating product");
            }

            return NoContent();
        }

        // DELETE: api/products/{id} - Admin only
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(string id)
        {
            var product = await _productService.GetByIdAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            var success = await _productService.DeleteAsync(id);

            if (!success)
            {
                return BadRequest("Error deleting product");
            }

            return NoContent();
        }
    }
}