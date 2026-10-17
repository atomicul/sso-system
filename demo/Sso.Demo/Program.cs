using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sso.Client;
using Sso.Demo;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DemoDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<SsoUser>().AddEntityFrameworkStores<DemoDbContext>();
// Cookies don't tell ports apart, so on localhost Identity's default name would clash with the SSO's own cookie.
builder.Services.ConfigureApplicationCookie(options => options.Cookie.Name = ".Sso.Demo");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<DemoDbContext>().Database.Migrate();
}

var auth = app.MapGroup("/api/auth");
auth.MapSsoIdentityApi<SsoUser>();
auth.MapPost("/logout", async (SignInManager<SsoUser> signInManager) => await signInManager.SignOutAsync())
    .RequireAuthorization();

app.MapGet("/", async (ClaimsPrincipal principal, UserManager<SsoUser> users) =>
        await users.GetUserAsync(principal) is { } user
            ? Results.Ok(new { user.Email, user.GivenName, user.FamilyName, user.Birthdate, user.PhoneNumber, user.Address })
            : Results.Unauthorized())
    .RequireAuthorization();

app.Run();
