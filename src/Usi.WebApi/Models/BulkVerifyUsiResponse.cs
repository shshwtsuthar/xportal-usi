namespace Usi.WebApi.Models;

/// <summary>
/// Response model for bulk USI verification
/// </summary>
public class BulkVerifyUsiResponse
{
    /// <summary>
    /// Total number of verifications requested
    /// </summary>
    public int TotalRequested { get; set; }

    /// <summary>
    /// Number of valid USIs
    /// </summary>
    public int ValidCount { get; set; }

    /// <summary>
    /// Number of invalid USIs
    /// </summary>
    public int InvalidCount { get; set; }

    /// <summary>
    /// Individual verification results
    /// </summary>
    public List<VerifyUsiResponse> Results { get; set; } = new();
}
