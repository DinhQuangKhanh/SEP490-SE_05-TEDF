
namespace TEDF.Domain.Common.Exceptions
{
    public class DomainException : Exception
    {
        /// <summary>
        /// Gets the error code associated with this exception.
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// Initializes a new instance of the DomainException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="code">The error code.</param>
        public DomainException(string message, string? code = null)
            : base(message)
        {
            Code = code ?? "DOMAIN_ERROR";
        }

        /// <summary>
        /// Initializes a new instance of the DomainException class with an inner exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="code">The error code.</param>
        public DomainException(string message, Exception innerException, string? code = null)
            : base(message, innerException)
        {
            Code = code ?? "DOMAIN_ERROR";
        }
    }
}
