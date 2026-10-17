using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Sso.Client;

public static class SsoEndpoints
{
    /// <summary>The Identity endpoints that create or change credentials, which the SSO owns instead.</summary>
    private static readonly (string Method, string Route)[] LocalCredentialRoutes =
    [
        ("POST", "/login"),
        ("POST", "/register"),
        ("POST", "/refresh"),
        ("GET", "/confirmEmail"),
        ("POST", "/resendConfirmationEmail"),
        ("POST", "/forgotPassword"),
        ("POST", "/resetPassword"),
        ("POST", "/manage/info"),
        ("POST", "/manage/2fa"),
    ];

    /// <summary>
    /// Maps Identity's endpoints without the ones that log in with or change local credentials, which answer
    /// <c>404 Not Found</c>, and maps <c>GET {callbackPattern}?token=</c>: the endpoint the SSO sends users to. It
    /// logs them in with the SSO's token, creating or updating their local copy, then redirects them to
    /// <paramref name="redirectTo"/>.
    /// </summary>
    /// <param name="ssoPublicKey">
    /// The key the SSO's tokens are signed with; by default, the PEM public key at <c>Sso:PublicKeyPath</c>.
    /// </param>
    /// <param name="appPrivateKey">
    /// The app's key the SSO encrypts its tokens with; by default, the PEM private key at <c>Sso:AppPrivateKeyPath</c>.
    /// </param>
    /// <param name="appKey">
    /// The app's key in the SSO's <c>ClientApps</c>, which the SSO addresses its tokens to; by default, the
    /// <c>Sso:AppKey</c> setting. Tokens addressed to any other app are rejected.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// When a key or the app's key isn't given and its setting is missing, or the user store can't keep authentication tokens, which
    /// the callback needs to accept each token once.
    /// </exception>
    public static IEndpointConventionBuilder MapSsoIdentityApi<TUser>(
        this IEndpointRouteBuilder endpoints,
        string callbackPattern = "/sso",
        string redirectTo = "/",
        SecurityKey? ssoPublicKey = null,
        SecurityKey? appPrivateKey = null,
        string? appKey = null)
        where TUser : IdentityUser, new()
    {
        var configuration = endpoints.ServiceProvider.GetRequiredService<IConfiguration>();
        RequireAuthenticationTokens<TUser>(endpoints.ServiceProvider);
        var callback = new SsoCallback<TUser>(
            ssoPublicKey ?? LoadRsaKey(configuration, "Sso:PublicKeyPath"),
            appPrivateKey ?? LoadRsaKey(configuration, "Sso:AppPrivateKeyPath"),
            appKey ?? Require(configuration, "Sso:AppKey"),
            redirectTo);

        var sso = endpoints.MapGroup("");
        sso.MapIdentityApi<TUser>().Add(DisableLocalCredentials);
        sso.MapGet(callbackPattern, callback.HandleAsync).ExcludeFromDescription();
        return sso;
    }

    /// <summary>
    /// Replaces the handler of a local credential endpoint before it binds the request, so it answers
    /// <c>404 Not Found</c> to any request.
    /// </summary>
    private static void DisableLocalCredentials(EndpointBuilder endpoint)
    {
        if (endpoint is RouteEndpointBuilder { RoutePattern.RawText: { } route } && IsLocalCredentialRoute(endpoint, route))
        {
            endpoint.RequestDelegate = context =>
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            };
            endpoint.Metadata.Add(new ExcludeFromDescriptionAttribute());
        }
    }

    private static bool IsLocalCredentialRoute(EndpointBuilder endpoint, string route)
    {
        var methods = endpoint.Metadata.OfType<HttpMethodMetadata>().SelectMany(metadata => metadata.HttpMethods);
        return LocalCredentialRoutes.Any(local => route.EndsWith(local.Route) && methods.Contains(local.Method));
    }

    private static void RequireAuthenticationTokens<TUser>(IServiceProvider services) where TUser : class
    {
        using var scope = services.CreateScope();
        if (!scope.ServiceProvider.GetRequiredService<UserManager<TUser>>().SupportsUserAuthenticationTokens)
        {
            throw new InvalidOperationException(
                "The user store must keep authentication tokens, which the SSO callback remembers the last token with.");
        }
    }

    private static RsaSecurityKey LoadRsaKey(IConfiguration configuration, string key)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(Require(configuration, key)));
        return new RsaSecurityKey(rsa);
    }

    private static string Require(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"The {key} setting is required, or pass its value to MapSsoIdentityApi.");
        }
        return value;
    }
}
