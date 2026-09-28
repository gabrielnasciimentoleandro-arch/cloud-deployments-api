using System.ComponentModel.DataAnnotations;

namespace CloudDeploy.Api.Contracts;

public sealed class CreateDeploymentRequest
{
    [Required]
    [StringLength(80, MinimumLength = 2)]
    [RegularExpression(
        @"^[A-Za-z0-9][A-Za-z0-9._ -]*$",
        ErrorMessage = "Application contains unsupported characters.")]
    public string Application { get; init; } = string.Empty;

    [Required]
    [RegularExpression(
        @"^(?i:development|staging|production)$",
        ErrorMessage = "Environment must be development, staging, or production.")]
    public string Environment { get; init; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 1)]
    [RegularExpression(
        @"^[A-Za-z0-9][A-Za-z0-9._+-]*$",
        ErrorMessage = "Version contains unsupported characters.")]
    public string Version { get; init; } = string.Empty;

    [StringLength(300)]
    public string? Notes { get; init; }
}
