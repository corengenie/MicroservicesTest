using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Common
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class JwtAuthorizationAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var key = Auth.GetSymmetricSecurityKey();

            string token = context.HttpContext.Request.Headers["Authorization"]!;
            if (!string.IsNullOrEmpty(token) && token.StartsWith("Bearer "))
            {
                string jwt = token[7..];

                var handler = new JwtSecurityTokenHandler();
                try
                {
                    var validationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = key,
                        ValidateIssuer = true,
                        ValidIssuer = Auth.ISSUER,
                        ValidateAudience = true,
                        ValidAudience = Auth.AUDIENCE,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };

                    var claimsPrincipal = handler.ValidateToken(jwt, validationParameters, out var validatedToken);
                    context.HttpContext.User = claimsPrincipal;
                }
                catch (Exception)
                {
                    context.Result = new UnauthorizedResult();
                }
            }
            else
            {
                context.Result = new UnauthorizedResult();
            }
        }
    }
}