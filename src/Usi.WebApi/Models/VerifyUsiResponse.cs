namespace Usi.WebApi.Models;

/// <summary>
/// Response model for USI verification
/// </summary>
public class VerifyUsiResponse
{
    /// <summary>
    /// True only when the USI is active AND all provided personal details match
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// The USI that was verified
    /// </summary>
    public string Usi { get; set; } = string.Empty;

    /// <summary>
    /// Raw USI status from the registry (Valid / Invalid / Deactivated)
    /// </summary>
    public string? UsiStatus { get; set; }

    /// <summary>
    /// Whether the provided first name matched the registry record (Match / NoMatch / null if not supplied)
    /// </summary>
    public string? FirstNameMatch { get; set; }

    /// <summary>
    /// Whether the provided family name matched the registry record (Match / NoMatch / null if not supplied)
    /// </summary>
    public string? FamilyNameMatch { get; set; }

    /// <summary>
    /// Whether the provided single name matched the registry record (Match / NoMatch / null if not supplied)
    /// </summary>
    public string? SingleNameMatch { get; set; }

    /// <summary>
    /// Whether the provided date of birth matched the registry record (Match / NoMatch / null if not returned)
    /// </summary>
    public string? DateOfBirthMatch { get; set; }

    /// <summary>
    /// Human-readable summary of the overall verification outcome
    /// </summary>
    public string? VerificationStatus { get; set; }

    /// <summary>
    /// Additional message or error details
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Record ID for tracking
    /// </summary>
    public int RecordId { get; set; }
}
