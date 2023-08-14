using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

namespace Common
{
    public class Auth
    {
        public const string ISSUER = "UserDataService";
        public const string AUDIENCE = "microservice";
        private const string KEY = "sFWiw3iNE56QSqZv9WlWvVs2WOVtseTb";
        public const int LIFETIME = 100000;

        public static SymmetricSecurityKey GetSymmetricSecurityKey()
        {
            return new SymmetricSecurityKey(Encoding.ASCII.GetBytes(KEY));
        }

        public static string GetPasswordHash(string password)
        {
            if (string.IsNullOrEmpty(password)) { return ""; }

            using var hashAlgorithm = SHA512.Create();
            var hash = hashAlgorithm.ComputeHash(Encoding.Unicode.GetBytes(password));
            return string.Concat(hash.Select(item => item.ToString("x2")));
        }
    }
}