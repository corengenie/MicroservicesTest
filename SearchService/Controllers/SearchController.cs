using Common;
using Common.Exceptions;
using Common.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SearchService.API.Models;
using System.Net.Http.Headers;

namespace SearchService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ILogger<SearchController> _logger;
        private readonly IConfiguration _configuration;

        public SearchController(ILogger<SearchController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Поиск по продуктам и пользователям
        /// </summary>
        /// <param name="query">Запрос для поиска</param>
        /// <returns>Записи, найденные по запросу</returns>
        [Route("{query}")]
        [HttpGet]
        [JwtAuthorization]
        public async Task<IActionResult> GetProduct(string query)
        {
            var result = new List<SearchResultItem>();
            try
            {
                string token = HttpContext.Request.Headers["Authorization"]!;
                string jwt = token[7..];

                var apiUrl = _configuration["UserDataServiceUrl"];
                if (string.IsNullOrEmpty(apiUrl))
                {
                    throw new Exception("Failed to recieve Users");
                }
                var users = await GetSearchResultsAsync<UserModel>(apiUrl + $"Users/{query}", jwt);
                result.AddRange(users);


                apiUrl = _configuration["ProductsServiceUrl"] ?? null;
                if (string.IsNullOrEmpty(apiUrl))
                {
                    throw new Exception("Failed to recieve product prices");
                }
                var products = await GetSearchResultsAsync<ProductModel>(apiUrl + $"Products/{query}", jwt);
                result.AddRange(products);
            }
            catch (ExternalServiceFailException ex)
            {
                return StatusCode((int)ex.StatusCode, ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }

            return Ok(result);
        }

        private static async Task<IEnumerable<SearchResultItem>> GetSearchResultsAsync<TModel>(string url, string token) where TModel : EntityWithId
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                throw new ExternalServiceFailException(response.StatusCode, response.ReasonPhrase);
            }

            var contentString = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<IEnumerable<TModel>>(contentString)
                                        .Select(u => new SearchResultItem()
                                        {
                                            Id = u.Id,
                                            Name = u is UserModel user ? user.Login :
                                                    (u is ProductModel product ? product.Name : ""),
                                            Type = u is UserModel ? "User" : "Product"
                                        });

            return result;
        }
    }
}