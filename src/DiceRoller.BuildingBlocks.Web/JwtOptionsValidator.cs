using System.Text;
using Microsoft.Extensions.Options;

namespace DiceRoller.BuildingBlocks.Web;

/// <summary>Validates <see cref="JwtOptions"/> at startup.</summary>
internal sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            failures.Add($"{JwtOptions.SectionName}:{nameof(JwtOptions.Issuer)} is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add($"{JwtOptions.SectionName}:{nameof(JwtOptions.Audience)} is required.");
        }

        if (Encoding.UTF8.GetByteCount(options.SigningKey ?? string.Empty) < JwtOptions.MinSigningKeyBytes)
        {
            failures.Add(
                $"{JwtOptions.SectionName}:{nameof(JwtOptions.SigningKey)} must be at least {JwtOptions.MinSigningKeyBytes} bytes.");
        }

        if (options.ExpiryMinutes <= 0)
        {
            failures.Add($"{JwtOptions.SectionName}:{nameof(JwtOptions.ExpiryMinutes)} must be positive.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
