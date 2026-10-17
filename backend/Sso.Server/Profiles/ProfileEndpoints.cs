using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Sso.Server.Data;

namespace Sso.Server.Profiles;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfile(this IEndpointRouteBuilder app)
    {
        var profile = app.MapGroup("/profile").WithTags("Profile").RequireAuthorization();

        profile.MapGet("", GetProfile);
        profile.MapPut("", UpdateProfile);

        return app;
    }

    /// <summary>The user's profile.</summary>
    public static async Task<Results<Ok<Profile>, UnauthorizedHttpResult>> GetProfile(
        ClaimsPrincipal principal, UserManager<SsoUser> users) =>
        await users.GetUserAsync(principal) is { } user
            ? TypedResults.Ok(Profile.Of(user))
            : TypedResults.Unauthorized();

    /// <summary>
    /// Replaces the user's profile. Apps receive the new profile the next time the user opens them from the portal.
    /// </summary>
    public static async Task<Results<Ok<Profile>, UnauthorizedHttpResult, ValidationProblem>> UpdateProfile(
        Profile profile, ClaimsPrincipal principal, UserManager<SsoUser> users)
    {
        if (await users.GetUserAsync(principal) is not { } user)
        {
            return TypedResults.Unauthorized();
        }

        profile.CopyTo(user);
        var result = await users.UpdateAsync(user);
        return result.Succeeded
            ? TypedResults.Ok(Profile.Of(user))
            : TypedResults.ValidationProblem(result.Errors.ToDictionary(error => error.Code, error => new[] { error.Description }));
    }
}
