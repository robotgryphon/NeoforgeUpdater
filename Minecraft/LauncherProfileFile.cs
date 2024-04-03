using System.Text.Json.Serialization;

namespace NeoForgeUpdater.Minecraft;

public class LauncherProfileFile
{
    [JsonPropertyOrder(1)]
    [JsonPropertyName("profiles")]
    public Dictionary<string, LauncherProfile> Profiles { get; set; }

    [JsonPropertyOrder(2)]
    [JsonPropertyName("settings")]
    public ProfileFileSettings Settings { get; set; }

    [JsonPropertyOrder(3)]
    [JsonPropertyName("version")]
    public int Version { get; set; }

    public class ProfileFileSettings
    {
        public bool crashAssistance { get; set; }
        public bool enableAdvanced { get; set; }
        public bool enableAnalytics { get; set; }
        public bool enableHistorical { get; set; }
        public bool enableReleases { get; set; }
        public bool enableSnapshots { get; set; }
        public bool keepLauncherOpen { get; set; }
        public string profileSorting { get; set; }
        public bool showGameLog { get; set; }
        public bool showMenu { get; set; }
        public bool soundOn { get; set; }
    }

}
