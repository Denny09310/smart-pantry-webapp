using Microsoft.Extensions.DependencyInjection;

namespace Shared.Validation;

/// <summary>
/// Registers the Shared assembly with the validation source generator so that
/// Minimal API validation metadata is produced for the transport contracts.
/// Called from the host app alongside its own <c>AddValidation()</c>.
/// </summary>
public static class SharedValidationExtensions
{
    public static IServiceCollection AddSharedValidation(this IServiceCollection services)
        => services.AddValidation();
}
