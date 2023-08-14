using Common.Exceptions;
using Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using UserData.API.Models;
using UserData.Storage;
using UserData.Storage.Models;

namespace UserData.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly ILogger<OrdersController> _logger;
        private readonly UserDataContext _context;
        private readonly IConfiguration _configuration;

        public OrdersController(ILogger<OrdersController> logger, UserDataContext context, IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _configuration = configuration;
        }

        /// <summary>
        /// Получить заказ по id
        /// </summary>
        /// <param name="orderId">Id заказа</param>
        /// <returns>Детали заказа</returns>
        [Route("{orderId:int}")]
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetOrder(int orderId)
        {
            var order = await _context.Orders
                                        .Include(o => o.Items)
                                        .FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) { return NotFound(); }

            List<ProductPrice> prices;

            try
            {
                prices = await GetProductsPricesAsync(order.Items.Select(i => i.ProductId));
            }
            catch (ExternalServiceFailException ex)
            {
                return StatusCode((int)ex.StatusCode, ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }

            var resultOrder = new OrderDetails()
            {
                Id = order.Id,
                Created = order.Created,
                TotalPrice = order.TotalPrice,
                Items = order.Items.Select(i =>
                {
                    var orderItem = prices.First(p => p.ProductId == i.ProductId);
                    return new Common.Models.OrderItem
                    {
                        Name = orderItem.Name,
                        Price = orderItem.Price,
                        ProductCount = i.ProductCount,
                        ProductId = i.ProductId,
                    };
                })
            };
            if (resultOrder.TotalPrice == 0)
            {
                resultOrder.TotalPrice = resultOrder.Items.Sum(i => i.ProductCount * i.Price);
            }
            return Ok(resultOrder);
        }

        /// <summary>
        /// Получить историю заказов для конкретного пользователя или всех пользователей
        /// </summary>
        /// <param name="page">Номер страницы</param>
        /// <param name="pageSize">Количество элементов на странице</param>
        /// <param name="userId">Id пользователя</param>
        /// <returns>SearchResult с найденными записями</returns>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetOrdersHistory(int? userId, int page = 1, int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
            {
                return BadRequest("Page and pageSize cannot be negative or equals to 0.");
            }
            var orders = _context.Orders
                                    .Include(o => o.Items)
                                    .OrderByDescending(u => u.Created)
                                    .Skip((page - 1) * pageSize)
                                    .Take(pageSize);
            if (userId != null)
            {
                orders = orders.Where(o => o.UserId == userId);
            }
            var foundOrders = await orders.ToListAsync();

            List<ProductPrice> prices;

            try
            {
                prices = await GetProductsPricesAsync(foundOrders.SelectMany(o => o.Items.Select(i => i.ProductId)));
            }
            catch (ExternalServiceFailException ex)
            {
                return StatusCode((int)ex.StatusCode, ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }

            var orderDetails = foundOrders
                        .SelectMany(order => order.Items, (order, orderItem) => new { order, product = orderItem })
                        .Join(prices,
                              orderProduct => orderProduct.product.ProductId,
                              productPrice => productPrice.ProductId,
                              (orderProduct, productPrice) => new
                              {
                                  OrderId = orderProduct.order.Id,
                                  ProductId = orderProduct.product.ProductId,
                                  ProductName = productPrice.Name,
                                  ProductPrice = productPrice.Price,
                                  ProductCount = orderProduct.product.ProductCount,
                              });
            var orderDetailsList = orderDetails
                .GroupBy(od => od.OrderId)
                .Select(group => new OrderDetails
                {
                    Id = group.Key,
                    Created = foundOrders.First(o => o.Id == group.Key).Created,
                    Items = group.Select(item => new Common.Models.OrderItem
                    {
                        ProductId = item.ProductId,
                        Name = item.ProductName,
                        Price = (double)item.ProductPrice,
                        ProductCount = item.ProductCount,
                    }),
                    TotalPrice = group.Sum(item => (double)item.ProductPrice * item.ProductCount)
                })
                .ToList();

            var result = new SearchResult<OrderDetails>()
            {
                RequestedObjectsCount = pageSize,
                RequestedStartIndex = page,
                Objects = orderDetailsList,
                Total = await _context.Orders.CountAsync()
            };
            return Ok(result);
        }

        /// <summary>
        /// Создать новый заказ
        /// </summary>
        /// <param name="model">Заказ</param>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateOrder(OrderModel model)
        {
            if (!await _context.Users.AnyAsync(u => u.Id == model.UserId))
            {
                return BadRequest("User with specified id does not exist.");
            }
            if (model.Items.Any(i => i.Count < 0))
            {
                return BadRequest("Item count cannot be less than 0.");
            }

            List<ProductPrice> prices;

            try
            {
                prices = await GetProductsPricesAsync(model.Items.Select(i => i.ProductId));
            }
            catch (ExternalServiceFailException ex)
            {
                return StatusCode((int)ex.StatusCode, ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }

            var newOrder = new Order
            {
                Created = DateTime.Now,
                UserId = model.UserId,
                Items = model.Items
                                .Select(i => new UserData.Storage.Models.OrderItem
                                {
                                    ProductCount = i.Count,
                                    ProductId = i.ProductId
                                }).ToList(),
                TotalPrice = model.Items.Sum(i => i.Count * prices.First(p => p.ProductId == i.ProductId).Price)
            };
            _context.Orders.Add(newOrder);
            var savedChanges = await _context.SaveChangesAsync();

            if (savedChanges > 0)
            {
                return Ok(new { message = "Order created successfully." });
            }

            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to add order, try later.");
        }

        private async Task<List<ProductPrice>> GetProductsPricesAsync(IEnumerable<int> ids)
        {

            var requestUrl = _configuration["ProductsServiceUrl"];
            if (string.IsNullOrEmpty(requestUrl))
            {
                throw new Exception("Failed to recieve product prices");
            }

            var productIdsIsValidResponse = await new HttpClient()
                .PostAsync(requestUrl + $"Products/GetProductPrices", JsonContent.Create(ids));
            if (!productIdsIsValidResponse.IsSuccessStatusCode)
            {
                throw new ExternalServiceFailException(productIdsIsValidResponse.StatusCode, productIdsIsValidResponse.ReasonPhrase);
            }
            string contentString = await productIdsIsValidResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<ProductPrice>>(contentString);
        }
    }
}