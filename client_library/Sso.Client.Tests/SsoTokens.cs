using System.Globalization;
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Sso.Client.Tests;

/// <summary>Writes tokens the way the SSO does: signed with its key, then encrypted with the app's.</summary>
public static class SsoTokens
{
    public static string Write(
        RSA signingKey,
        RSA encryptingKey,
        string userId,
        string email,
        long number,
        DateTime? expires = null,
        string audience = ClientAppHost.ClientAppKey,
        Dictionary<string, object>? profile = null)
    {
        var expiry = expires ?? DateTime.UtcNow.AddMinutes(1);
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Audience = audience,
            Claims = new Dictionary<string, object>(profile ?? [])
            {
                [JwtRegisteredClaimNames.Sub] = userId,
                [JwtRegisteredClaimNames.Email] = email,
                [JwtRegisteredClaimNames.Jti] = number.ToString(CultureInfo.InvariantCulture),
            },
            IssuedAt = expiry.AddMinutes(-1),
            NotBefore = expiry.AddMinutes(-1),
            Expires = expiry,
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(signingKey), SecurityAlgorithms.RsaSha256),
            EncryptingCredentials = new EncryptingCredentials(
                new RsaSecurityKey(encryptingKey), SecurityAlgorithms.RsaOAEP, SecurityAlgorithms.Aes256CbcHmacSha512),
        });
    }

    public static string CallbackUrl(string token) => $"/auth/sso?token={Uri.EscapeDataString(token)}";
}
