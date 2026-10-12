using System.ComponentModel.DataAnnotations;

namespace Mercury.Core.Configuration;

/// <summary>
/// Stripe credentials for subscription billing and webhook verification.
/// </summary>
/// <remarks>
/// Disabled by default so the application runs locally without Stripe credentials.
/// </remarks>
public sealed class StripeOptions : IValidatableObject
{
    public const string SectionName = "Stripe";

    public bool Enabled { get; set; }

    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Signing secret used to verify that an inbound webhook genuinely came from Stripe.</summary>
    public string WebhookSecret { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enabled)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(SecretKey))
        {
            yield return new ValidationResult(
                "Stripe:SecretKey is required when Stripe:Enabled is true. Supply it with: dotnet user-secrets --project src/backend/Mercury.Api set \"Stripe:SecretKey\" \"<secret key>\"",
                [nameof(SecretKey)]);
        }
        else if (!SecretKey.StartsWith("sk_", StringComparison.Ordinal))
        {
            yield return new ValidationResult(
                "Stripe:SecretKey must start with 'sk_'. A publishable key is not accepted here.",
                [nameof(SecretKey)]);
        }

        if (string.IsNullOrWhiteSpace(WebhookSecret))
        {
            yield return new ValidationResult(
                "Stripe:WebhookSecret is required when Stripe:Enabled is true. Supply it with: dotnet user-secrets --project src/backend/Mercury.Api set \"Stripe:WebhookSecret\" \"<webhook secret>\"",
                [nameof(WebhookSecret)]);
        }
        else if (!WebhookSecret.StartsWith("whsec_", StringComparison.Ordinal))
        {
            yield return new ValidationResult(
                "Stripe:WebhookSecret must start with 'whsec_'.",
                [nameof(WebhookSecret)]);
        }
    }

    public override string ToString() =>
        $"{nameof(StripeOptions)} {{ Enabled = {Enabled}, SecretKey = <redacted>, WebhookSecret = <redacted> }}";
}
