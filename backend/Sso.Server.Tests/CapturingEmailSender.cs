using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Identity;
using Sso.Server.Data;

namespace Sso.Server.Tests;

/// <summary>Keeps the confirmation links the API sends, decoded, so tests can follow them.</summary>
public class CapturingEmailSender : IEmailSender<SsoUser>
{
    private readonly ConcurrentQueue<(string Email, Uri Link)> confirmationLinks = new();

    /// <summary>The latest confirmation link sent to the email.</summary>
    public Uri ConfirmationLinkFor(string email) => confirmationLinks.Last(sent => sent.Email == email).Link;

    public Task SendConfirmationLinkAsync(SsoUser user, string email, string confirmationLink)
    {
        confirmationLinks.Enqueue((email, new Uri(WebUtility.HtmlDecode(confirmationLink))));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(SsoUser user, string email, string resetLink) => Task.CompletedTask;

    public Task SendPasswordResetCodeAsync(SsoUser user, string email, string resetCode) => Task.CompletedTask;
}
