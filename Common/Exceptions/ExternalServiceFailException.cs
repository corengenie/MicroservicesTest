using System.Net;

namespace Common.Exceptions
{
    /// <summary>
    /// Ошибка произошедшая во время обращения к внешнему ресурсу
    /// </summary>
    public class ExternalServiceFailException : Exception
    {
        /// <summary>
        /// Код ответа полученного при запросе
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }
        public ExternalServiceFailException() { }
        public ExternalServiceFailException(HttpStatusCode statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
