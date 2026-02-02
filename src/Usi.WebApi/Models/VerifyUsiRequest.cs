using System.ComponentModel.DataAnnotations;

namespace Usi.WebApi.Models;

/// <summary>
/// Request model for single USI verification
/// </summary>
public class VerifyUsiRequest
{
    /// <summary>
    /// The USI to verify (10 characters)
    /// </summary>
    [Required]
    [StringLength(10, MinimumLength = 10)]
    public string Usi { get; set; } = string.Empty;

    /// <summary>
    /// Date of birth in ISO format (YYYY-MM-DD)
    /// </summary>
    [Required]
    public DateTime DateOfBirth { get; set; }

    /// <summary>
    /// First name (required if not using SingleName)
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Family name (required if using FirstName)
    /// </summary>
    public string? FamilyName { get; set; }

    /// <summary>
    /// Single name (use this OR FirstName/FamilyName combination)
    /// </summary>
    public string? SingleName { get; set; }
}
