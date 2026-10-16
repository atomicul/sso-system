using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Sso.Server.Apps;

/// <summary>
/// Writes the tokens that log users in to apps: JWTs signed with the SSO's key, then encrypted with the app's public
/// key. Each carries the user's id and email, and a number that only grows, so the app can accept each token once.
/// </summary>
public class LaunchTokens(SigningCredentials signingCredentials)
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(60);

    private readonly JsonWebTokenHandler handler = new();

    public string Create(IdentityUser user, ClientApp app, long number) =>
        handler.CreateToken(new SecurityTokenDescriptor
        {
            Audience = app.Key,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id,
                [JwtRegisteredClaimNames.Email] = user.Email!,
                [JwtRegisteredClaimNames.Jti] = number.ToString(CultureInfo.InvariantCulture),
            },
            Expires = DateTime.UtcNow + Lifetime,
            SigningCredentials = signingCredentials,
            EncryptingCredentials = app.EncryptingCredentials,
        });
}
