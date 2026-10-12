using System.ComponentModel.DataAnnotations;

namespace Mercury.Core.Configuration;

/// <summary>
/// Clerk settings used to validate incoming bearer tokens and to call the Clerk backend API.
/// </summary>
public sealed class ClerkOptions : IValidatableObject
{
    public const string SectionName = "Clerk";

    /// <summary>Clerk frontend API origin that issues and signs tokens, for example https://your-app.clerk.accounts.dev.</summary>
    [Required(
        AllowEmptyStrings = false,
        ErrorMessage = "Clerk:Authority is required. Supply it with: dotnet user-secrets --project src/backend/Mercury.Api set \"Clerk:Authority\" \"<https url>\"")]
    public string Authority { get; set; } = string.Empty;

    [Required(
        AllowEmptyStrings = false,
        ErrorMessage = "Clerk:SecretKey is required. Supply it with: dotnet user-secrets --project src/backend/Mercury.Api set \"Clerk:SecretKey\" \"<secret key>\"")]
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Expected token audience. Optional; when unset the authority alone identifies the issuer.</summary>
    public string? Audience { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Authority))
        {
            yield break;
        }

        if (!Uri.TryCreate(Authority, UriKind.Absolute, out var authority))
        {
            yield return new ValidationResult(
                "Clerk:Authority must be an absolute URL.",
                [nameof(Authority)]);
            yield break;
        }

        // Token signing keys are fetched from this origin, so plaintext transport would allow key substitution.
        if (authority.Scheme != Uri.UriSchemeHttps)
        {
            yield return new ValidationResult(
                "Clerk:Authority must use https.",
                [nameof(Authority)]);
        }
    }

    public override string ToString() =>
        $"{nameof(ClerkOptions)} {{ Authority = {Authority}, SecretKey = <redacted>, Audience = {Audience ?? "(none)"} }}";
}
