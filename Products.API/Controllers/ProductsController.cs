using Common;
using Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Products.Storage;
using Products.Storage.Models;
using System.Linq.Dynamic.Core;

namespace Products.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ILogger<ProductsController> _logger;
        private readonly ProductsContext _context;

        public ProductsController(ILogger<ProductsController> logger, ProductsContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// �������� ������ ���������
        /// </summary>
        /// <param name="page">����� ��������</param>
        /// <param name="pageSize">���������� ��������� �� ��������</param>
        /// <param name="sortOrder">������ ��� ���������� ���� "-����1,����2" ��� ���������� �� �������� ����1 � ����������� ����2</param>
        /// <returns>SearchResult � ���������� ��������</returns>
        [HttpGet]
        public async Task<IActionResult> GetProducts(int page = 1, int pageSize = 10, string sortOrder = "id")
        {
            if (page < 1 || pageSize < 1)
            {
                return BadRequest("Page and pageSize cannot be negative or equals to 0.");
            }

            var productsQuery = _context.Products
                                        .Skip((page - 1) * pageSize)
                                        .Take(pageSize);
            var sortExpressions = sortOrder.Split(',');
            foreach (var sortExpression in sortExpressions)
            {
                var descending = sortExpression.StartsWith("-");
                var propertyName = descending ? sortExpression[1..] : sortExpression;

                productsQuery = productsQuery.OrderBy(propertyName + (descending ? " descending" : ""));
            }

            var objects = await productsQuery.ToListAsync();

            var result = new SearchResult<Product>()
            {
                RequestedObjectsCount = pageSize,
                RequestedStartIndex = page,
                Objects = objects,
                Total = await _context.Products.CountAsync()
            };
            return Ok(result);
        }

        /// <summary>
        /// �������� ���������� � �������� �� id
        /// </summary>
        /// <param name="productId">Id ��������</param>
        /// <returns>Product</returns>
        [Route("{productId:int}")]
        [HttpGet]
        public async Task<IActionResult> GetProduct(int productId)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null) { return NotFound(); }
            return Ok(product);
        }

        /// <summary>
        /// ����� ��������� �� ��������
        /// </summary>
        /// <param name="query">������ ��� ������</param>
        /// <returns>Product</returns>
        [Route("{query}")]
        [HttpGet]
        public async Task<IActionResult> GetProducts(string query)
        {
            var products = await _context.Products.Where(p => p.Name.StartsWith(query)).ToListAsync();
            return Ok(products);
        }

        /// <summary>
        /// ������� ����� �������
        /// </summary>
        /// <param name="name">��������</param>
        /// <param name="price">����</param>
        [HttpPost]
        [JwtAuthorization]
        public async Task<IActionResult> CreateProduct(string name, double price)
        {
            if (string.IsNullOrEmpty(name) || name.Length < 3)
            {
                return BadRequest("Parameter name cannot be null, empty or shorter than 3 symbols.");
            }
            if (price < 0)
            {
                return BadRequest("Price cannot be negative");
            }

            var newProduct = new Product
            {
                Name = name,
                Price = price
            };
            _context.Products.Add(newProduct);
            var savedChanges = await _context.SaveChangesAsync();

            if (savedChanges > 0)
            {
                return Ok(new { message = "Product created successfully." });
            }

            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to add product, try later.");
        }

        /// <summary>
        /// �������� id ������ � ��������������� ����
        /// </summary>
        /// <param name="ids">������ id ���������</param>
        /// <returns>������ ��� (id, name, price)</returns>
        [Route("GetProductPrices")]
        [HttpPost]
        public async Task<IActionResult> GetProductPrices([FromBody] List<int> ids)
        {
            if (ids.Count == 0)
            {
                return BadRequest("Ids must not be empty.");
            }
            try
            {
                ids = ids.Distinct().ToList();
                if (await _context.Products.CountAsync(p => ids.Contains(p.Id)) != ids.Count)
                {
                    return BadRequest("Some of ids do not exist.");
                }
                var result = _context.Products
                                        .Where(p => ids.Contains(p.Id))
                                        .Select(p => new ProductPrice() { ProductId = p.Id, Name = p.Name, Price = p.Price }).ToList();
                return Ok(result);
            }
            catch (Exception)
            {
                return BadRequest("Failed to verify product ids.");
            }
        }
    }
}