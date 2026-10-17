using System.Globalization;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Sso.Server.Data;

namespace Sso.Server.Apps;

/// <summary>
/// Writes the tokens that log users in to apps: JWTs signed with the SSO's key, then encrypted with the app's public
/// key. Each carries the user's id, email and profile, and a number that only grows, so the app can accept each token
/// once, and expires after <paramref name="lifetime"/>.
/// </summary>
public class LaunchTokens(SigningCredentials signingCredentials, TimeSpan lifetime)
{
    public TimeSpan Lifetime => lifetime;

    private readonly JsonWebTokenHandler handler = new();

    public string Create(SsoUser user, ClientApp app, long number) =>
        handler.CreateToken(new SecurityTokenDescriptor
        {
            Audience = app.Key,
            Claims = WithoutNulls(new()
            {
                [JwtRegisteredClaimNames.Sub] = user.Id,
                [JwtRegisteredClaimNames.Email] = user.Email,
                [JwtRegisteredClaimNames.Jti] = number.ToString(CultureInfo.InvariantCulture),
                [JwtRegisteredClaimNames.GivenName] = user.GivenName,
                [JwtRegisteredClaimNames.FamilyName] = user.FamilyName,
                [JwtRegisteredClaimNames.Birthdate] = user.Birthdate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                [JwtRegisteredClaimNames.PhoneNumber] = user.PhoneNumber,
                [JwtRegisteredClaimNames.Address] = AddressClaim(user.Address),
            }),
            Expires = DateTime.UtcNow + lifetime,
            SigningCredentials = signingCredentials,
            EncryptingCredentials = app.EncryptingCredentials,
        });

    /// <summary>The OpenID Connect <c>address</c> claim, a JSON object of the address's parts, or null when it has none.</summary>
    private static Dictionary<string, object>? AddressClaim(PostalAddress address)
    {
        var parts = WithoutNulls(new()
        {
            ["street_address"] = address.StreetAddress,
            ["locality"] = address.Locality,
            ["region"] = address.Region,
            ["postal_code"] = address.PostalCode,
            ["country"] = address.Country,
        });
        return parts.Count > 0 ? parts : null;
    }

    /// <summary>Leaves out the claims the user hasn't set, rather than writing them as null.</summary>
    private static Dictionary<string, object> WithoutNulls(Dictionary<string, object?> claims) =>
        claims.Where(claim => claim.Value is not null).ToDictionary(claim => claim.Key, claim => claim.Value!);
}
