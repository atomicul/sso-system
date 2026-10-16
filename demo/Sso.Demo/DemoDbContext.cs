using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Sso.Demo;

/// <summary>The local copies of the users the SSO logs in, with the number of the last token each logged in with.</summary>
public class DemoDbContext(DbContextOptions<DemoDbContext> options) : IdentityDbContext<IdentityUser>(options);
