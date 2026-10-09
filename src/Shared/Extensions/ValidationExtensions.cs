namespace Microsoft.Extensions.DependencyInjection;

public static class ValidationExtensions
{
    public static IServiceCollection AddSharedValidation(this IServiceCollection services)
    {
        return services.AddValidation();
    }
}