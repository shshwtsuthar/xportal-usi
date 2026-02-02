using System.ComponentModel.DataAnnotations;

namespace Usi.WebApi.Models;

/// <summary>
/// Request model for bulk USI verification
/// </summary>
public class BulkVerifyUsiRequest
{
    /// <summary>
    /// List of USI verification requests
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<VerifyUsiRequest> Verifications { get; set; } = new();
}
