namespace Grimoire.Server.Data;

public class StorageOptions
{
    public const string SectionName = "Storage";

    public string GamesBasePath { get; set; } = "";
    public string DlcBasePath { get; set; } = "";
    public string UpdatesBasePath { get; set; } = "";
    public string EmulatorsBasePath { get; set; } = "";
    public string FirmwareBasePath { get; set; } = "";
    public string BiosBasePath { get; set; } = "";
    public string CoverImagesBasePath { get; set; } = "";
}
