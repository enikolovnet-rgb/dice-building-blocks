namespace DiceRoller.BuildingBlocks.Web;

/// <summary>
/// Settings for issuing and validating JWTs, bound from the <see cref="SectionName"/> configuration section.
/// Validated at startup: a missing issuer or audience, a signing key shorter than <see cref="MinSigningKeyBytes"/>
/// bytes or a non-positive expiry stops the service from starting.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>The configuration section the options are bound from.</summary>
    public const string SectionName = "Jwt";

    /// <summary>The minimum length of <see cref="SigningKey"/> in UTF-8 bytes (256 bits, as HS256 requires).</summary>
    public const int MinSigningKeyBytes = 32;

    /// <summary>The issuer (<c>iss</c>) tokens are issued with and must carry.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>The audience (<c>aud</c>) tokens are issued for and must carry.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>The HS256 signing key. At least <see cref="MinSigningKeyBytes"/> UTF-8 bytes. Never commit it.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>How long an issued token is valid, in minutes. Must be positive.</summary>
    public int ExpiryMinutes { get; set; } = 60;
}
