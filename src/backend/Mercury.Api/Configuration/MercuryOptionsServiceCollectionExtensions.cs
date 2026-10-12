using Mercury.Core.Configuration;

namespace Mercury.Api.Configuration;

/// <summary>
/// Registers every externally configured settings group as validated, strongly typed options.
/// </summary>
public static class MercuryOptionsServiceCollectionExtensions
{
    /// <summary>
    /// Binds and validates all Mercury configuration sections. Validation runs during startup,
    /// so a missing or malformed setting stops the application rather than surfacing on first request.
    /// </summary>
    public static IServiceCollection AddMercuryOptions(this IServiceCollection services)
    {
        services.AddValidatedOptions<DatabaseOptions>(DatabaseOptions.SectionName);
        services.AddValidatedOptions<ClerkOptions>(ClerkOptions.SectionName);
        services.AddValidatedOptions<TwilioOptions>(TwilioOptions.SectionName);
        services.AddValidatedOptions<StripeOptions>(StripeOptions.SectionName);

        return services;
    }

    private static void AddValidatedOptions<TOptions>(this IServiceCollection services, string sectionName)
        where TOptions : class
    {
        services.AddOptions<TOptions>()
            .BindConfiguration(sectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}
