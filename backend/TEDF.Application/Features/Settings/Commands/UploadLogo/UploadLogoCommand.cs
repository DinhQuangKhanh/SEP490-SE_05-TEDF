using TEDF.Application.Common.Abstractions;

namespace TEDF.Application.Features.Settings.Commands.UploadLogo;

/// <summary>Admin-only: uploads a new system logo and stores its URL in the LogoUrl setting.</summary>
public record UploadLogoCommand(Stream Content, string FileName, string ContentType)
    : ICacheInvalidatingCommand<string>
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate => new[] { "settings:" };
}
