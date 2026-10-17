using Microsoft.AspNetCore.Identity;
using Sso.Server.Data;

namespace Sso.Server;

/// <summary>Sends Identity's emails with links that open the SPA, writing them to the log.</summary>
public static class Emails
{
    public static IServiceCollection AddEmails(this IServiceCollection services) =>
        services
            .AddKeyedTransient<IEmailSender<SsoUser>, LoggingEmailSender>(SpaLinkEmailSender.TransportKey)
            .AddTransient<IEmailSender<SsoUser>, SpaLinkEmailSender>();
}
