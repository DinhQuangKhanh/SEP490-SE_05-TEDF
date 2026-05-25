// This file is deprecated. Use EntityNotFoundException instead.
// Kept as an alias for backward compatibility.

namespace TEDF.Domain.Common.Exceptions
{
    /// <summary>
    /// DEPRECATED: Use EntityNotFoundException instead.
    /// This class is kept as an alias for backward compatibility.
    /// </summary>
    [Obsolete("Use EntityNotFoundException instead. This class will be removed in a future version.")]
    public class NotFoundException : EntityNotFoundException
    {
        public NotFoundException(string message)
            : base("Resource", message)
        {
        }

        public NotFoundException(string resourceType, string resourceId)
            : base(resourceType, resourceId)
        {
        }

        public NotFoundException(string resourceType, object resourceId)
            : base(resourceType, resourceId)
        {
        }
    }
}
