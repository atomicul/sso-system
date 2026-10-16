namespace Sso.Server.Apps;

/// <summary>The configured apps, and their icons: the <c>{Key}.jpg</c>, <c>.png</c> or <c>.webp</c> file of each.</summary>
public class ClientAppCatalog(IReadOnlyList<ClientApp> apps, string? iconDirectory)
{
    private static readonly Dictionary<string, string> IconContentTypes = new()
    {
        [".jpg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
    };

    public IReadOnlyList<ClientApp> All => apps;

    public ClientApp? Find(string key) => apps.FirstOrDefault(app => app.Key == key);

    /// <summary>The path of the app's icon in the icon directory, or null when it has none.</summary>
    public string? FindIcon(ClientApp app) =>
        iconDirectory is null
            ? null
            : IconContentTypes.Keys
                .Select(extension => Path.Join(iconDirectory, app.Key + extension))
                .FirstOrDefault(File.Exists);

    public static string IconContentType(string iconPath) => IconContentTypes[Path.GetExtension(iconPath)];
}
