namespace Grimoire.Shared.DTOs;

public record ClientVersionDto(
    string Version,
    Dictionary<string, string> DownloadUrls
);
