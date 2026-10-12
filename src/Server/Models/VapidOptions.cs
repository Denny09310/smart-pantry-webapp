using System.ComponentModel.DataAnnotations;

namespace Server.Models;

/// <summary>
/// VAPID keys for Web Push. Both keys are optional: without them the
/// public-key endpoint 404s and push sending is skipped. The private key
/// must never appear in source — user-secrets locally, environment or
/// vault configuration in deployed environments.
/// </summary>
public sealed class VapidOptions
{
    public const string SectionName = "Vapid";

    [StringLength(87, MinimumLength = 87,
        ErrorMessage = "Vapid:PublicKey must be the 87-character base64url public key.")]
    public string? PublicKey { get; set; }

    [StringLength(43, MinimumLength = 43,
        ErrorMessage = "Vapid:PrivateKey must be the 43-character base64url private key.")]
    public string? PrivateKey { get; set; }

    [Required]
    public string Subject { get; set; } = "mailto:smart-pantry@localhost";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(PublicKey) && !string.IsNullOrWhiteSpace(PrivateKey);
}

public static class VapidOptionsExtensions
{
    public static IServiceCollection AddVapidOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<VapidOptions>()
            .Bind(configuration.GetSection(VapidOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                static o => o.Subject.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
                    || Uri.TryCreate(o.Subject, UriKind.Absolute, out var uri)
                        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
                "Vapid:Subject must be a mailto: address or an absolute http(s) URL.")
            .ValidateOnStart();

        return services;
    }
}
