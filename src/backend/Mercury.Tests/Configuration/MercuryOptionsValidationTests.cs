using Mercury.Api.Configuration;
using Mercury.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Mercury.Tests.Configuration;

/// <summary>
/// Covers the startup contract for configuration: the application must refuse to start when a
/// required setting is missing or malformed, and must never echo a secret value while doing so.
/// </summary>
public sealed class MercuryOptionsValidationTests
{
    private const string ValidConnectionString =
        "Host=localhost;Port=5432;Database=mercury;Username=mercury_app;Password=local_dev_only_password";

    private const string ValidClerkAuthority = "https://example.clerk.accounts.dev";
    private const string ValidClerkSecretKey = "sk_test_clerk_placeholder";

    [Fact]
    public void Validate_WithMinimalValidConfiguration_Succeeds()
    {
        var validator = BuildStartupValidator(MinimalValidSettings());

        validator.Validate();
    }

    [Fact]
    public void Validate_WhenConnectionStringMissing_ThrowsNamingTheSetting()
    {
        var settings = MinimalValidSettings();
        settings.Remove("Database:ConnectionString");

        var exception = Assert.Throws<OptionsValidationException>(() => BuildStartupValidator(settings).Validate());

        Assert.Contains("Database:ConnectionString", string.Join(' ', exception.Failures), StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_WhenConnectionStringIsBlank_ThrowsNamingTheSetting()
    {
        var settings = MinimalValidSettings();
        settings["Database:ConnectionString"] = "   ";

        var exception = Assert.Throws<OptionsValidationException>(() => BuildStartupValidator(settings).Validate());

        Assert.Contains("Database:ConnectionString", string.Join(' ', exception.Failures), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Clerk:Authority")]
    [InlineData("Clerk:SecretKey")]
    public void Validate_WhenRequiredClerkSettingMissing_ThrowsNamingTheSetting(string key)
    {
        var settings = MinimalValidSettings();
        settings.Remove(key);

        var exception = Assert.Throws<OptionsValidationException>(() => BuildStartupValidator(settings).Validate());

        Assert.Contains(key, string.Join(' ', exception.Failures), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("http://example.clerk.accounts.dev", "https")]
    [InlineData("not-a-url", "absolute URL")]
    public void Validate_WhenClerkAuthorityIsNotAnHttpsUrl_Throws(string authority, string expectedMessageFragment)
    {
        var settings = MinimalValidSettings();
        settings["Clerk:Authority"] = authority;

        var exception = Assert.Throws<OptionsValidationException>(() => BuildStartupValidator(settings).Validate());

        Assert.Contains(expectedMessageFragment, string.Join(' ', exception.Failures), StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_WhenIntegrationsDisabled_DoesNotRequireTheirCredentials()
    {
        var settings = MinimalValidSettings();
        settings["Twilio:Enabled"] = "false";
        settings["Stripe:Enabled"] = "false";

        BuildStartupValidator(settings).Validate();
    }

    [Fact]
    public void Validate_WhenTwilioEnabledWithoutCredentials_Throws()
    {
        var settings = MinimalValidSettings();
        settings["Twilio:Enabled"] = "true";

        var failures = string.Join(' ', Assert.Throws<OptionsValidationException>(
            () => BuildStartupValidator(settings).Validate()).Failures);

        Assert.Contains("Twilio:AccountSid", failures, StringComparison.Ordinal);
        Assert.Contains("Twilio:AuthToken", failures, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_WhenTwilioAccountSidHasWrongPrefix_Throws()
    {
        var settings = MinimalValidSettings();
        settings["Twilio:Enabled"] = "true";
        settings["Twilio:AccountSid"] = "SK0123456789";
        settings["Twilio:AuthToken"] = "placeholder-auth-token";

        var exception = Assert.Throws<OptionsValidationException>(() => BuildStartupValidator(settings).Validate());

        Assert.Contains("must start with 'AC'", string.Join(' ', exception.Failures), StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_WhenStripeEnabledWithoutCredentials_Throws()
    {
        var settings = MinimalValidSettings();
        settings["Stripe:Enabled"] = "true";

        var failures = string.Join(' ', Assert.Throws<OptionsValidationException>(
            () => BuildStartupValidator(settings).Validate()).Failures);

        Assert.Contains("Stripe:SecretKey", failures, StringComparison.Ordinal);
        Assert.Contains("Stripe:WebhookSecret", failures, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_WhenStripePublishableKeySuppliedAsSecretKey_Throws()
    {
        var settings = MinimalValidSettings();
        settings["Stripe:Enabled"] = "true";
        settings["Stripe:SecretKey"] = "pk_test_publishable";
        settings["Stripe:WebhookSecret"] = "whsec_placeholder";

        var exception = Assert.Throws<OptionsValidationException>(() => BuildStartupValidator(settings).Validate());

        Assert.Contains("must start with 'sk_'", string.Join(' ', exception.Failures), StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_WhenEnabledIntegrationsAreFullyConfigured_Succeeds()
    {
        var settings = MinimalValidSettings();
        settings["Twilio:Enabled"] = "true";
        settings["Twilio:AccountSid"] = "AC0123456789";
        settings["Twilio:AuthToken"] = "placeholder-auth-token";
        settings["Stripe:Enabled"] = "true";
        settings["Stripe:SecretKey"] = "sk_test_placeholder";
        settings["Stripe:WebhookSecret"] = "whsec_placeholder";

        BuildStartupValidator(settings).Validate();
    }

    [Fact]
    public void ValidationFailures_DoNotContainSuppliedSecretValues()
    {
        const string connectionStringWithPassword = "Host=localhost;Password=super-secret-value";
        const string twilioAuthToken = "another-super-secret-value";

        var settings = MinimalValidSettings();
        settings["Database:ConnectionString"] = connectionStringWithPassword;
        settings["Twilio:Enabled"] = "true";
        settings["Twilio:AccountSid"] = "SK-wrong-prefix";
        settings["Twilio:AuthToken"] = twilioAuthToken;

        var exception = Assert.Throws<OptionsValidationException>(() => BuildStartupValidator(settings).Validate());
        var failures = string.Join(' ', exception.Failures);

        Assert.DoesNotContain("super-secret-value", failures, StringComparison.Ordinal);
        Assert.DoesNotContain(twilioAuthToken, failures, StringComparison.Ordinal);
    }

    [Fact]
    public void OptionsToString_RedactsEverySecretValue()
    {
        var database = new DatabaseOptions { ConnectionString = "Host=localhost;Password=db-secret" };
        var clerk = new ClerkOptions { Authority = ValidClerkAuthority, SecretKey = "clerk-secret" };
        var twilio = new TwilioOptions { Enabled = true, AccountSid = "AC123", AuthToken = "twilio-secret" };
        var stripe = new StripeOptions { Enabled = true, SecretKey = "sk_live_secret", WebhookSecret = "whsec_secret" };

        var rendered = string.Join(' ', database, clerk, twilio, stripe);

        Assert.DoesNotContain("db-secret", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("clerk-secret", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("twilio-secret", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("sk_live_secret", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("whsec_secret", rendered, StringComparison.Ordinal);

        // The non-secret authority stays visible so the rendered value remains useful for diagnostics.
        Assert.Contains(ValidClerkAuthority, rendered, StringComparison.Ordinal);
    }

    private static Dictionary<string, string?> MinimalValidSettings() => new()
    {
        ["Database:ConnectionString"] = ValidConnectionString,
        ["Clerk:Authority"] = ValidClerkAuthority,
        ["Clerk:SecretKey"] = ValidClerkSecretKey,
    };

    private static IStartupValidator BuildStartupValidator(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddMercuryOptions();

        return services.BuildServiceProvider().GetRequiredService<IStartupValidator>();
    }
}
