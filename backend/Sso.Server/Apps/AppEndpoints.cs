using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Sso.Server.Data;

namespace Sso.Server.Apps;

public static class AppEndpoints
{
    public static IEndpointRouteBuilder MapApps(this IEndpointRouteBuilder app)
    {
        var apps = app.MapGroup("/apps").WithTags("Apps").RequireAuthorization();

        apps.MapGet("", ListApps);
        apps.MapGet("/{key}/icon", GetIcon);
        apps.MapGet("/{key}/launch", Launch);

        return app;
    }

    /// <summary>Lists the apps the user can open.</summary>
    public static Ok<AppResponse[]> ListApps([FromServices] ClientAppCatalog catalog) =>
        TypedResults.Ok(catalog.All
            .Select(app => new AppResponse(app.Key, app.Name, catalog.FindIcon(app) is null ? null : IconUrl(app)))
            .ToArray());

    /// <summary>Serves the app's icon.</summary>
    public static Results<PhysicalFileHttpResult, NotFound> GetIcon(string key, [FromServices] ClientAppCatalog catalog) =>
        catalog.Find(key) is { } app && catalog.FindIcon(app) is { } path
            ? TypedResults.PhysicalFile(path, ClientAppCatalog.IconContentType(path))
            : TypedResults.NotFound();

    /// <summary>
    /// Redirects the user to the app's endpoint with a token in its <c>token</c> query parameter, which logs them in to
    /// the app once, before the token expires.
    /// </summary>
    public static async Task<Results<RedirectHttpResult, NotFound, UnauthorizedHttpResult>> Launch(
        string key,
        ClaimsPrincipal principal,
        UserManager<IdentityUser> users,
        AppDbContext db,
        [FromServices] ClientAppCatalog catalog,
        [FromServices] LaunchTokens tokens,
        CancellationToken cancellationToken)
    {
        if (catalog.Find(key) is not { } app)
        {
            return TypedResults.NotFound();
        }
        if (await users.GetUserAsync(principal) is not { } user)
        {
            return TypedResults.Unauthorized();
        }

        var token = tokens.Create(user, app, await db.NextLaunchTokenNumberAsync(cancellationToken));
        return TypedResults.Redirect(QueryHelpers.AddQueryString(app.Endpoint.AbsoluteUri, "token", token));
    }

    private static string IconUrl(ClientApp app) => $"/api/apps/{app.Key}/icon";
}
