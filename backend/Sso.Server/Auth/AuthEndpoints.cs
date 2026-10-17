using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Sso.Server.Data;

namespace Sso.Server.Auth;

public static class AuthEndpoints
{
    private static readonly string[] EmailSendingRoutes =
        ["/register", "/resendConfirmationEmail", "/forgotPassword", "/manage/info"];

    public static IEndpointRouteBuilder MapAuth(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/auth").WithTags("Auth");

        auth.MapIdentityApi<SsoUser>().Add(LimitEmailSending);
        auth.MapPost("/logout", Logout).RequireAuthorization();

        return app;
    }

    /// <summary>
    /// Puts the Identity endpoints that send an email under the stricter email limit.
    /// <c>POST /manage/info</c> sends one when the user changes their email.
    /// </summary>
    private static void LimitEmailSending(EndpointBuilder endpoint)
    {
        if (endpoint is RouteEndpointBuilder { RoutePattern.RawText: { } route }
            && endpoint.Metadata.OfType<HttpMethodMetadata>().Any(metadata => metadata.HttpMethods.Contains(HttpMethods.Post))
            && EmailSendingRoutes.Any(route.EndsWith))
        {
            endpoint.Metadata.Add(new EnableRateLimitingAttribute(RateLimits.Email));
        }
    }

    /// <summary>Signs the current user out by clearing the Identity cookie.</summary>
    public static async Task<Ok> Logout(SignInManager<SsoUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return TypedResults.Ok();
    }
}
