using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;

namespace Sso.Server.Apps;

/// <summary>
/// Reads the apps under <c>ClientApps</c>, keyed by the app's key, each with a <c>Name</c>, the <c>Endpoint</c> that
/// receives its tokens and the <c>PublicKeyPath</c> of the PEM key they are encrypted with. Their icons are read from
/// <c>AppIcons:Directory</c>, and tokens are signed with the PEM private key at <c>SigningKey:Path</c>. Fails on start
/// when a setting is missing or invalid, or when two apps share a public key, which would let each read the other's
/// tokens.
/// </summary>
public static partial class ClientApps
{
    private const string Section = "ClientApps";

    public static IServiceCollection AddClientApps(this IServiceCollection services, IConfiguration configuration)
    {
        var apps = ReadApps(configuration);
        var iconDirectory = configuration["AppIcons:Directory"];
        var signingKey = LoadRsaKey(Require(configuration, "SigningKey:Path"));

        return services
            .AddSingleton(new ClientAppCatalog(apps, string.IsNullOrEmpty(iconDirectory) ? null : Path.GetFullPath(iconDirectory)))
            .AddSingleton(new LaunchTokens(new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256)));
    }

    private static List<ClientApp> ReadApps(IConfiguration configuration)
    {
        var apps = configuration.GetSection(Section).GetChildren().Select(ReadApp).ToList();
        var sharedKey = apps
            .GroupBy(app => Convert.ToHexString(PublicKeyOf(app)))
            .FirstOrDefault(group => group.Count() > 1);
        if (sharedKey is not null)
        {
            throw new InvalidOperationException(
                $"{Section} {string.Join(" and ", sharedKey.Select(app => app.Key))} share a public key; give each app its own.");
        }
        return apps;
    }

    private static ClientApp ReadApp(IConfigurationSection section)
    {
        if (!AppKey().IsMatch(section.Key))
        {
            throw new InvalidOperationException(
                $"{section.Path} isn't a valid app key; use lowercase letters, digits and dashes.");
        }

        var endpoint = new Uri(Require(section, "Endpoint"), UriKind.Absolute);
        var publicKey = LoadRsaKey(Require(section, "PublicKeyPath"));
        var encryptingCredentials = new EncryptingCredentials(
            publicKey, SecurityAlgorithms.RsaOAEP, SecurityAlgorithms.Aes256CbcHmacSha512);
        return new ClientApp(section.Key, Require(section, "Name"), endpoint, encryptingCredentials);
    }

    private static byte[] PublicKeyOf(ClientApp app) =>
        ((RsaSecurityKey)app.EncryptingCredentials.Key).Rsa.ExportSubjectPublicKeyInfo();

    private static RsaSecurityKey LoadRsaKey(string path)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(path));
        return new RsaSecurityKey(rsa);
    }

    private static string Require(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrEmpty(value))
        {
            var path = configuration is IConfigurationSection section ? $"{section.Path}:{key}" : key;
            throw new InvalidOperationException($"The {path} setting is required.");
        }
        return value;
    }

    [GeneratedRegex("^[a-z0-9-]+$")]
    private static partial Regex AppKey();
}
