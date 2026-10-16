namespace Sso.Server.Apps;

/// <summary>An app the user can open, with the URL of its icon, or null when it has none.</summary>
public record AppResponse(string Key, string Name, string? IconUrl);
