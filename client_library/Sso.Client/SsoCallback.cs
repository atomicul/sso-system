using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Sso.Client;

/// <summary>
/// Logs a user in with a token of the SSO: a JWT the SSO signed for this app (its <c>aud</c> is the app's key),
/// encrypted with the app's key, holding the user's id (<c>sub</c>), <c>email</c>, and a number (<c>jti</c>) that
/// grows with every token the SSO writes. The audience is what binds a token to the app: encryption alone doesn't,
/// since another app can decrypt its own token and re-encrypt it with this app's public key. The user's last
/// accepted number is kept with their authentication tokens, and a token is only accepted when its number is greater,
/// so each token logs in once.
/// </summary>
internal sealed class SsoCallback<TUser>(
    SecurityKey ssoPublicKey, SecurityKey appPrivateKey, string appKey, string redirectTo)
    where TUser : IdentityUser, new()
{
    private const string LoginProvider = "Sso";
    private const string LastTokenNumber = nameof(LastTokenNumber);

    private readonly JsonWebTokenHandler handler = new();

    private readonly TokenValidationParameters validationParameters = new()
    {
        IssuerSigningKey = ssoPublicKey,
        TokenDecryptionKey = appPrivateKey,
        ValidateIssuer = false,
        ValidAudience = appKey,
        ClockSkew = TimeSpan.FromSeconds(30),
    };

    public async Task<Results<RedirectHttpResult, UnauthorizedHttpResult>> HandleAsync(
        string token, UserManager<TUser> users, SignInManager<TUser> signInManager)
    {
        if (await ReadAsync(token) is not { } identity
            || await SaveAsync(users, identity) is not { } user)
        {
            return TypedResults.Unauthorized();
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return TypedResults.Redirect(redirectTo);
    }

    private async Task<SsoIdentity?> ReadAsync(string token)
    {
        var result = await handler.ValidateTokenAsync(token, validationParameters);
        if (!result.IsValid
            || !long.TryParse(result.Claims[JwtRegisteredClaimNames.Jti] as string, CultureInfo.InvariantCulture, out var number))
        {
            return null;
        }

        return new SsoIdentity(
            (string)result.Claims[JwtRegisteredClaimNames.Sub],
            (string)result.Claims[JwtRegisteredClaimNames.Email],
            number);
    }

    /// <summary>
    /// Creates or updates the user's local copy and remembers the token's number, in the one update that also checks
    /// the user's concurrency stamp, so two requests with the same token can't both pass. Returns null when the token
    /// isn't newer than the last one, or saving fails.
    /// </summary>
    private static async Task<TUser?> SaveAsync(UserManager<TUser> users, SsoIdentity identity)
    {
        var user = await users.FindByIdAsync(identity.UserId);
        if (user is not null && !IsNewer(identity.Number, await users.GetAuthenticationTokenAsync(user, LoginProvider, LastTokenNumber)))
        {
            return null;
        }

        if (user is null)
        {
            user = new TUser { Id = identity.UserId };
            CopyIdentity(user, identity);
            if (!(await users.CreateAsync(user)).Succeeded)
            {
                return null;
            }
        }
        else
        {
            CopyIdentity(user, identity);
        }

        var number = identity.Number.ToString(CultureInfo.InvariantCulture);
        var saved = await users.SetAuthenticationTokenAsync(user, LoginProvider, LastTokenNumber, number);
        return saved.Succeeded ? user : null;
    }

    private static bool IsNewer(long number, string? lastNumber) =>
        !long.TryParse(lastNumber, CultureInfo.InvariantCulture, out var last) || number > last;

    private static void CopyIdentity(TUser user, SsoIdentity identity)
    {
        user.UserName = identity.Email;
        user.Email = identity.Email;
        user.EmailConfirmed = true;
    }

    private sealed record SsoIdentity(string UserId, string Email, long Number);
}
