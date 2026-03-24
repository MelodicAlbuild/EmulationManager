using Grimoire.Shared.Enums;

namespace Grimoire.Shared.DTOs;

public record DownloadRequestDto(
    DownloadableType Type,
    int Id
);
