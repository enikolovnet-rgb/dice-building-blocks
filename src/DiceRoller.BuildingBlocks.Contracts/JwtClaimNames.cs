namespace DiceRoller.BuildingBlocks.Contracts;

/// <summary>
/// Names of the JWT claims that DiceRoller services issue and read. Inbound claim mapping is disabled,
/// so these are the raw names as they appear in the token.
/// </summary>
public static class JwtClaimNames
{
    /// <summary>The user identifier (<c>sub</c>).</summary>
    public const string Subject = "sub";

    /// <summary>The user's email address (<c>email</c>).</summary>
    public const string Email = "email";

    /// <summary>The user's given name (<c>given_name</c>).</summary>
    public const string GivenName = "given_name";

    /// <summary>The user's family name (<c>family_name</c>).</summary>
    public const string FamilyName = "family_name";

    /// <summary>The unique token identifier (<c>jti</c>).</summary>
    public const string JwtId = "jti";
}
