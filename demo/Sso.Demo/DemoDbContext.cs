using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sso.Client;

namespace Sso.Demo;

/// <summary>
/// The local copies of the users the SSO logs in, with their profiles and the number of the last token each logged in
/// with.
/// </summary>
public class DemoDbContext(DbContextOptions<DemoDbContext> options) : IdentityDbContext<SsoUser>(options);
