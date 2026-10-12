using System.ComponentModel.DataAnnotations;

namespace Mercury.Core.Configuration;

/// <summary>
/// Twilio credentials for dispatching outbound SMS quotes.
/// </summary>
/// <remarks>
/// Disabled by default so the application runs locally without Twilio credentials;
/// a no-op SMS implementation is used while <see cref="Enabled"/> is false.
/// </remarks>
public sealed class TwilioOptions : IValidatableObject
{
    public const string SectionName = "Twilio";

    public bool Enabled { get; set; }

    public string AccountSid { get; set; } = string.Empty;

    public string AuthToken { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enabled)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(AccountSid))
        {
            yield return new ValidationResult(
                "Twilio:AccountSid is required when Twilio:Enabled is true. Supply it with: dotnet user-secrets --project src/backend/Mercury.Api set \"Twilio:AccountSid\" \"<account sid>\"",
                [nameof(AccountSid)]);
        }
        else if (!AccountSid.StartsWith("AC", StringComparison.Ordinal))
        {
            yield return new ValidationResult(
                "Twilio:AccountSid must start with 'AC'.",
                [nameof(AccountSid)]);
        }

        if (string.IsNullOrWhiteSpace(AuthToken))
        {
            yield return new ValidationResult(
                "Twilio:AuthToken is required when Twilio:Enabled is true. Supply it with: dotnet user-secrets --project src/backend/Mercury.Api set \"Twilio:AuthToken\" \"<auth token>\"",
                [nameof(AuthToken)]);
        }
    }

    public override string ToString() =>
        $"{nameof(TwilioOptions)} {{ Enabled = {Enabled}, AccountSid = <redacted>, AuthToken = <redacted> }}";
}
