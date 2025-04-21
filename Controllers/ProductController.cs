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
    public class ProductsController : ControllerBase
    {
        private readonly ProductService _productService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UserSessionService _userSessionService;

        public ProductsController(
            ProductService productService, 
            UserManager<ApplicationUser> userManager,
            UserSessionService userSessionService)
        {
            _productService = productService;
            _userManager = userManager;
            _userSessionService = userSessionService;
        }

        // GET: api/products - Accessible by both Admin and User roles
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
        {
            try
            {
                // Check if any user is logged in
                if (!_userSessionService.AnyUserLoggedIn())
                {
                    return Unauthorized(new { message = "Please login first or token is not valid" });
                }

                var products = await _productService.GetAllAsync();
                return Ok(products.Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    UserId = p.UserId.GetHashCode()
                }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAll: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving products");
            }
        }

        // GET: api/products/{id} - Accessible by any authenticated user
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<ProductDto>> GetById(string id)
        {
            try
            {
                // Check if any user is logged in
                if (!_userSessionService.AnyUserLoggedIn())
                {
                    return Unauthorized(new { message = "Please login first or token is not valid" });
                }

                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest("Invalid product ID");
                }

                var product = await _productService.GetByIdAsync(id);
                if (product == null)
                {
                    return NotFound($"Product with ID {id} not found");
                }

                return Ok(new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    UserId = product.UserId.GetHashCode()
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetById: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving the product");
            }
        }

        // POST: api/products - Admin only
        [HttpPost]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
        public async Task<ActionResult<ProductDto>> Create(ProductCreateDto productDto)
        {
            try
            {
                // Check if any user is logged in
                if (!_userSessionService.AnyUserLoggedIn())
                {
                    return Unauthorized(new { message = "Please login first or token is not valid" });
                }
                
                // Get the current user and verify if admin role
                var activeUser = _userSessionService.GetCurrentUser();
                if (activeUser == null)
                {
                    return Unauthorized(new { message = "Unable to identify current user" });
                }
                
                // Check if user has admin role
                var user = await _userManager.FindByIdAsync(activeUser);
                if (user == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }
                
                // Check if user has admin role
                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains("Admin"))
                {
                    return Forbid();
                }

                var product = new Product
                {
                    Name = productDto.Name,
                    Description = productDto.Description,
                    UserId = Guid.Parse(activeUser)
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
                        UserId = product.UserId.GetHashCode()
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Create: {ex.Message}");
                return StatusCode(500, "An error occurred while creating the product");
            }
        }

        // PUT: api/products/{id} - Admin only
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Update(string id, ProductUpdateDto productDto)
        {
            try
            {
                // Check if any user is logged in
                if (!_userSessionService.AnyUserLoggedIn())
                {
                    return Unauthorized(new { message = "Please login first or token is not valid" });
                }
                
                // Get the current user and verify if admin role
                var activeUser = _userSessionService.GetCurrentUser();
                if (activeUser == null)
                {
                    return Unauthorized(new { message = "Unable to identify current user" });
                }
                
                // Check if user has admin role
                var user = await _userManager.FindByIdAsync(activeUser);
                if (user == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }
                
                // Check if user has admin role
                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains("Admin"))
                {
                    return Forbid();
                }

                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest("Invalid product ID");
                }

                var product = await _productService.GetByIdAsync(id);
                if (product == null)
                {
                    return NotFound($"Product with ID {id} not found");
                }

                // Update product properties
                product.Name = productDto.Name;
                product.Description = productDto.Description;

                await _productService.UpdateAsync(id, product);

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Update: {ex.Message}");
                return StatusCode(500, "An error occurred while updating the product");
            }
        }

        // DELETE: api/products/{id} - Admin only
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                // Check if any user is logged in
                if (!_userSessionService.AnyUserLoggedIn())
                {
                    return Unauthorized(new { message = "Please login first or token is not valid" });
                }
                
                // Get the current user and verify if admin role
                var activeUser = _userSessionService.GetCurrentUser();
                if (activeUser == null)
                {
                    return Unauthorized(new { message = "Unable to identify current user" });
                }
                
                // Check if user has admin role
                var user = await _userManager.FindByIdAsync(activeUser);
                if (user == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }
                
                // Check if user has admin role
                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains("Admin"))
                {
                    return Forbid();
                }

                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest("Invalid product ID");
                }

                var product = await _productService.GetByIdAsync(id);
                if (product == null)
                {
                    return NotFound($"Product with ID {id} not found");
                }

                await _productService.DeleteAsync(id);

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Delete: {ex.Message}");
                return StatusCode(500, "An error occurred while deleting the product");
            }
        }
    }
}