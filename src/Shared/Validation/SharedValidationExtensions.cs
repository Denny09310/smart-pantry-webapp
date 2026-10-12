using Microsoft.Extensions.DependencyInjection;

namespace Shared.Validation;

/// <summary>
/// Ensures validation services are registered for the transport contracts.
/// The source generator emits metadata per assembly for
/// <c>[ValidatableType]</c> roots; this call (like the host's own
/// <c>AddValidation()</c>) registers the runtime services that consume it.
/// Called from the host app alongside its own <c>AddValidation()</c>.
/// </summary>
public static class SharedValidationExtensions
{
    public static IServiceCollection AddSharedValidation(this IServiceCollection services)
        => services.AddValidation();
}
