using ECommons.Configuration;

namespace ICE.ConfigFiles;

public partial class Config
{
    public int ConfigVersion = 1;
    public int Config_Versioning { get; set; } = 2;
    public bool OldConfigMigrateV1 = false;
    public string LegacyImportFailure { get; set; } = "";
    public string[] LegacyImportWarnings { get; set; } = Array.Empty<string>();

    public void Save()
    {
        EzConfig.Save();
    }

    public void SaveDebounced()
    {
        EzConfigExtensions.SaveDebounced();
    }
}
