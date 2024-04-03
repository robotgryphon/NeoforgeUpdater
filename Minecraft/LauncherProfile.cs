namespace NeoForgeUpdater.Minecraft;

public struct LauncherProfile
{
    public DateTime created { get; set; }
    public string gameDir { get; set; }
    public string icon { get; set; }
    public DateTime lastUsed { get; set; }
    public string lastVersionId { get; set; }
    public string name { get; set; }
}
