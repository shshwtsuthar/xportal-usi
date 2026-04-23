using Common.Configuration;
using Common.ServiceClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Usi.WebApi.Auth;
using Usi.WebApi.Models;

namespace Usi.WebApi.Controllers;

/// <summary>
/// USI verification and management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationOptions.DefaultScheme)]
public class UsiController : ControllerBase
{
    private readonly IUSIService _usiService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UsiController> _logger;

    public UsiController(
        IUSIService usiService,
        IConfiguration configuration,
        ILogger<UsiController> logger)
    {
        _usiService = usiService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Verify a single USI
    /// </summary>
    /// <param name="request">USI verification request</param>
    /// <returns>Verification result</returns>
    [HttpPost("verify")]
    [ProducesResponseType(typeof(VerifyUsiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VerifyUsiResponse>> VerifyUsi([FromBody] VerifyUsiRequest request)
    {
        try
        {
            _logger.LogInformation("Verifying USI: {Usi}", request.Usi);

            // Validate name fields
            if (string.IsNullOrWhiteSpace(request.SingleName) &&
                (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.FamilyName)))
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "Invalid name fields",
                    Details = "Must provide either SingleName OR both FirstName and FamilyName"
                });
            }

            var orgCode = _configuration[SettingsKey.UsiOrgCode] 
                ?? throw new InvalidOperationException("Organization code not configured");

            // Build verification request using existing WCF types
            var verification = new VerificationType
            {
                RecordId = 1,
                USI = request.Usi,
                DateOfBirth = request.DateOfBirth
            };

            // Set name fields based on what was provided
            if (!string.IsNullOrWhiteSpace(request.SingleName))
            {
                verification.Items = new[] { request.SingleName };
                verification.ItemsElementName = new[] { ItemsChoiceType1.SingleName };
            }
            else
            {
                verification.Items = new[] { request.FirstName!, request.FamilyName! };
                verification.ItemsElementName = new[] { ItemsChoiceType1.FirstName, ItemsChoiceType1.FamilyName };
            }

            var bulkRequest = new BulkVerifyUSIRequest
            {
                BulkVerifyUSI = new BulkVerifyUSIType
                {
                    OrgCode = orgCode,
                    NoOfVerifications = 1,
                    Verifications = new[] { verification }
                }
            };

            var response = await _usiService.BulkVerifyUSIAsync(bulkRequest);
            var verificationResult = response.BulkVerifyUSIResponse1.VerificationResponses.FirstOrDefault();

            if (verificationResult == null)
            {
                return StatusCode(500, new ErrorResponse
                {
                    Error = "No verification response received",
                    Details = "The USI service did not return a verification result"
                });
            }

            var result = MapVerificationResponse(verificationResult, request.Usi);

            _logger.LogInformation("USI verification completed: {Usi} is {Status}", 
                request.Usi, result.IsValid ? "Valid" : "Invalid");

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying USI: {Usi}", request.Usi);
            return StatusCode(500, new ErrorResponse
            {
                Error = "USI verification failed",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Verify multiple USIs in bulk
    /// </summary>
    /// <param name="request">Bulk verification request</param>
    /// <returns>Bulk verification results</returns>
    [HttpPost("bulk-verify")]
    [ProducesResponseType(typeof(BulkVerifyUsiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BulkVerifyUsiResponse>> BulkVerifyUsi([FromBody] BulkVerifyUsiRequest request)
    {
        try
        {
            _logger.LogInformation("Bulk verifying {Count} USIs", request.Verifications.Count);

            var orgCode = _configuration[SettingsKey.UsiOrgCode]
                ?? throw new InvalidOperationException("Organization code not configured");

            var verifications = new List<VerificationType>();
            int recordId = 1;

            foreach (var verifyRequest in request.Verifications)
            {
                var verification = new VerificationType
                {
                    RecordId = recordId++,
                    USI = verifyRequest.Usi,
                    DateOfBirth = verifyRequest.DateOfBirth
                };

                if (!string.IsNullOrWhiteSpace(verifyRequest.SingleName))
                {
                    verification.Items = new[] { verifyRequest.SingleName };
                    verification.ItemsElementName = new[] { ItemsChoiceType1.SingleName };
                }
                else if (!string.IsNullOrWhiteSpace(verifyRequest.FirstName) && 
                         !string.IsNullOrWhiteSpace(verifyRequest.FamilyName))
                {
                    verification.Items = new[] { verifyRequest.FirstName, verifyRequest.FamilyName };
                    verification.ItemsElementName = new[] { ItemsChoiceType1.FirstName, ItemsChoiceType1.FamilyName };
                }
                else
                {
                    continue; // Skip invalid entries
                }

                verifications.Add(verification);
            }

            var bulkRequest = new BulkVerifyUSIRequest
            {
                BulkVerifyUSI = new BulkVerifyUSIType
                {
                    OrgCode = orgCode,
                    NoOfVerifications = verifications.Count,
                    Verifications = verifications.ToArray()
                }
            };

            var response = await _usiService.BulkVerifyUSIAsync(bulkRequest);

            var results = response.BulkVerifyUSIResponse1.VerificationResponses
                .Select(vr => MapVerificationResponse(vr, vr.USI ?? string.Empty))
                .ToList();

            var bulkResponse = new BulkVerifyUsiResponse
            {
                TotalRequested = request.Verifications.Count,
                ValidCount = results.Count(r => r.IsValid),
                InvalidCount = results.Count(r => !r.IsValid),
                Results = results
            };

            _logger.LogInformation("Bulk verification completed: {Valid}/{Total} valid", 
                bulkResponse.ValidCount, bulkResponse.TotalRequested);

            return Ok(bulkResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk USI verification");
            return StatusCode(500, new ErrorResponse
            {
                Error = "Bulk USI verification failed",
                Details = ex.Message
            });
        }
    }

    private static VerifyUsiResponse MapVerificationResponse(VerificationResponseType vr, string usi)
    {
        string? firstNameMatch = null;
        string? familyNameMatch = null;
        string? singleNameMatch = null;

        if (vr.Items != null && vr.ItemsElementName != null)
        {
            for (var i = 0; i < Math.Min(vr.Items.Length, vr.ItemsElementName.Length); i++)
            {
                var matchValue = vr.Items[i].ToString();
                switch (vr.ItemsElementName[i])
                {
                    case ItemsChoiceType2.FirstName:
                        firstNameMatch = matchValue;
                        break;
                    case ItemsChoiceType2.FamilyName:
                        familyNameMatch = matchValue;
                        break;
                    case ItemsChoiceType2.SingleName:
                        singleNameMatch = matchValue;
                        break;
                }
            }
        }

        var dateOfBirthMatch = vr.DateOfBirthSpecified ? vr.DateOfBirth.ToString() : null;
        var usiIsActive = vr.USIStatus == VerificationResponseTypeUSIStatus.Valid;

        var allFieldsMatch =
            (firstNameMatch == null || firstNameMatch == nameof(MatchResultType.Match)) &&
            (familyNameMatch == null || familyNameMatch == nameof(MatchResultType.Match)) &&
            (singleNameMatch == null || singleNameMatch == nameof(MatchResultType.Match)) &&
            (dateOfBirthMatch == null || dateOfBirthMatch == nameof(MatchResultType.Match));

        var isValid = usiIsActive && allFieldsMatch;

        var verificationStatus = vr.USIStatus switch
        {
            VerificationResponseTypeUSIStatus.Invalid => "USI not found",
            VerificationResponseTypeUSIStatus.Deactivated => "USI deactivated",
            VerificationResponseTypeUSIStatus.Valid when !allFieldsMatch => "Details do not match",
            VerificationResponseTypeUSIStatus.Valid => "Match",
            _ => vr.USIStatus.ToString()
        };

        return new VerifyUsiResponse
        {
            IsValid = isValid,
            Usi = usi,
            UsiStatus = vr.USIStatus.ToString(),
            FirstNameMatch = firstNameMatch,
            FamilyNameMatch = familyNameMatch,
            SingleNameMatch = singleNameMatch,
            DateOfBirthMatch = dateOfBirthMatch,
            VerificationStatus = verificationStatus,
            Message = null,
            RecordId = vr.RecordId
        };
    }

    /// <summary>
    /// Get country reference data
    /// </summary>
    /// <returns>List of countries</returns>
    [HttpGet("countries")]
    [ProducesResponseType(typeof(IEnumerable<CountryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<CountryDto>>> GetCountries()
    {
        try
        {
            _logger.LogInformation("Fetching country data");

            var orgCode = _configuration[SettingsKey.UsiOrgCode]
                ?? throw new InvalidOperationException("Organization code not configured");

            var request = new GetCountriesRequest
            {
                GetCountries = new GetCountriesType { OrgCode = orgCode }
            };

            var response = await _usiService.GetCountriesAsync(request);

            var countries = response.GetCountriesResponse1.Countries
                .Select(c => new CountryDto
                {
                    Code = c.CountryCode,
                    Name = c.Name
                })
                .ToList();

            _logger.LogInformation("Retrieved {Count} countries", countries.Count);

            return Ok(countries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching country data");
            return StatusCode(500, new ErrorResponse
            {
                Error = "Failed to fetch country data",
                Details = ex.Message
            });
        }
    }
}
