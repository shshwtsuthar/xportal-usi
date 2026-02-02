namespace Usi.WebApi.Models;

/// <summary>
/// Response model for USI verification
/// </summary>
public class VerifyUsiResponse
{
    /// <summary>
    /// Indicates whether the USI is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// The USI that was verified
    /// </summary>
    public string Usi { get; set; } = string.Empty;

    /// <summary>
    /// Verification status from the service
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
