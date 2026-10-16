using Microsoft.AspNetCore.Identity;

namespace Sso.Server;

/// <summary>Sends Identity's emails with links that open the SPA, writing them to the log.</summary>
public static class Emails
{
    public static IServiceCollection AddEmails(this IServiceCollection services) =>
        services
            .AddKeyedTransient<IEmailSender<IdentityUser>, LoggingEmailSender>(SpaLinkEmailSender.TransportKey)
            .AddTransient<IEmailSender<IdentityUser>, SpaLinkEmailSender>();
}
