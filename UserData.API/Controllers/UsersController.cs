using Common;
using Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using UserData.Storage;
using UserData.Storage.Models;

namespace UserData.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ILogger<UsersController> _logger;
        private readonly UserDataContext _context;

        public UsersController(ILogger<UsersController> logger, UserDataContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Поиск пользователя по id
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <returns>User?</returns>
        [Route("{userId:int}")]
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUser(int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) { return NotFound(); }
            user.Password = "";
            return Ok(new { user.Id, user.Login });
        }

        /// <summary>
        /// Поиск пользователя по логину
        /// </summary>
        /// <param name="login">Логин пользователя</param>
        /// <returns>Users</returns>
        [Route("{login}")]
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUsers(string login)
        {
            var users = await _context.Users.Where(u => u.Login.StartsWith(login)).ToListAsync();
            return Ok(users.Select(u => new UserModel() { Id = u.Id, Login = u.Login }));
        }

        /// <summary>
        /// Получить список пользователей
        /// </summary>
        /// <param name="page">Номер страницы</param>
        /// <param name="pageSize">Количество элементов на странице</param>
        /// <returns>SearchResult с найденными записями</returns>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUsers(int page = 1, int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
            {
                return BadRequest("Page and pageSize cannot be negative or equals to 0.");
            }
            var users = await _context.Users
                                        .OrderBy(u => u.Id)
                                        .Skip((page - 1) * pageSize)
                                        .Take(pageSize)
                                        .Select(u => new UserModel()
                                        {
                                            Id = u.Id,
                                            Login = u.Login
                                        })
                                        .ToListAsync();
            var result = new SearchResult<UserModel>()
            {
                RequestedObjectsCount = pageSize,
                RequestedStartIndex = page,
                Objects = users,
                Total = await _context.Users.CountAsync()
            };
            return Ok(result);
        }

        /// <summary>
        /// Метод авторизации
        /// </summary>
        /// <param name="login">Логин</param>
        /// <param name="password">Пароль</param>
        /// <returns>JWT токен и логин</returns>
        [HttpPost("/login")]
        public async Task<IActionResult> Login(string login, string password)
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                return BadRequest(new { message = "Username or password is null." });
            }
            var identity = await GetIdentityAsync(login, Auth.GetPasswordHash(password));
            if (identity == default)
            {
                return BadRequest(new { message = "Invalid username or password." });
            }

            var now = DateTime.UtcNow;
            var jwt = new JwtSecurityToken(
                    issuer: Auth.ISSUER,
                    audience: Auth.AUDIENCE,
                    notBefore: now,
                    claims: identity.Claims,
                    expires: now.Add(TimeSpan.FromMinutes(Auth.LIFETIME)),
                    signingCredentials: new SigningCredentials(Auth.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            var response = new
            {
                access_token = encodedJwt,
                login = identity.Name
            };

            return new JsonResult(response);
        }

        /// <summary>
        /// Регистрация нового пользователя
        /// </summary>
        /// <param name="login">Логин</param>
        /// <param name="password">Пароль</param>
        [HttpPost]
        public async Task<IActionResult> Register(string login, string password)
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                return BadRequest(new { message = "Username or password is null." });
            }
            if (login.Length < 2 || password.Length < 8)
            {
                return BadRequest(new { message = "Username or password is too short." });
            }
            if (_context.Users.Any(u => u.Login == login))
            {
                return Conflict(new { message = "User with specified login already exists." });
            }

            var newUser = new User
            {
                Login = login,
                Password = Auth.GetPasswordHash(password)
            };
            _context.Users.Add(newUser);
            var savedChanges = await _context.SaveChangesAsync();

            if (savedChanges > 0)
            {
                return Ok(new { message = "User registered successfully." });
            }

            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to add user, try later.");
        }

        private async Task<ClaimsIdentity?> GetIdentityAsync(string username, string passwordHash)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Login == username && x.Password == passwordHash);
            if (user == null)
            {
                return null;
            }
            var claims = new List<Claim>
            {
                new (ClaimsIdentity.DefaultNameClaimType, user.Login),
                new ("id", user.Id.ToString()),
            };
            var claimsIdentity = new ClaimsIdentity(claims, 
                                                    "Token",
                                                    ClaimsIdentity.DefaultNameClaimType,
                                                    ClaimsIdentity.DefaultRoleClaimType);
            return claimsIdentity;
        }
    }
}