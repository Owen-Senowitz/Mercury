using System.ComponentModel.DataAnnotations;

namespace Mercury.Core.Configuration;

/// <summary>
/// PostgreSQL connection settings for the Mercury database.
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    [Required(
        AllowEmptyStrings = false,
        ErrorMessage = "Database:ConnectionString is required. Supply it with: dotnet user-secrets --project src/backend/Mercury.Api set \"Database:ConnectionString\" \"<connection string>\"")]
    public string ConnectionString { get; set; } = string.Empty;

    [Range(0, 10, ErrorMessage = "Database:MaxRetryCount must be between 0 and 10.")]
    public int MaxRetryCount { get; set; } = 3;

    [Range(1, 300, ErrorMessage = "Database:CommandTimeoutSeconds must be between 1 and 300.")]
    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>Redacted so an accidental log or interpolation cannot leak the database password.</summary>
    public override string ToString() =>
        $"{nameof(DatabaseOptions)} {{ ConnectionString = <redacted>, MaxRetryCount = {MaxRetryCount}, CommandTimeoutSeconds = {CommandTimeoutSeconds} }}";
}
