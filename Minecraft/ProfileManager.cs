using Microsoft.Extensions.Options;
using Spectre.Console;
using System.Text.Json;

namespace NeoForgeUpdater.Minecraft;
internal class ProfileManager(IOptions<MinecraftOptions> mcOptions)
{
    internal readonly string INSTALL_DIR = mcOptions.Value.InstallDirectory;
    internal readonly string LAUNCHER_PROFILES = Path.Combine(mcOptions.Value.InstallDirectory, "launcher_profiles.json");
    private readonly JsonSerializerOptions PROFILE_FILE_OPTS = new JsonSerializerOptions()
    {
        WriteIndented = true
    };

    public bool ProfileFileExists => File.Exists(LAUNCHER_PROFILES);

    private Dictionary<string, LauncherProfile>? ProfileCache;

    public async ValueTask<bool> Initialize()
    {
        if (!ProfileFileExists)
        {
            AnsiConsole.MarkupLineInterpolated($"[gray]No launcher profile file found. Check your installation directory: {INSTALL_DIR}[/]");
            return false;
        }

        try
        {
            var profileInfo = await Profiles();
            if (profileInfo.Count == 0)
            {
                AnsiConsole.MarkupLine("[bold red]No profiles appear to exist; check the version and file and try again. Expected Version: 3[/]");
                return false;
            }
        }

        catch (Exception)
        {
            AnsiConsole.MarkupLine("[bold red]Launcher profiles file appears to be in a bad format; check the version and file and try again. Expected Version: 3[/]");
            return false;
        }

        return true;
    }

    public async ValueTask<Dictionary<string, LauncherProfile>> Profiles()
    {
        if (ProfileCache is not null)
            return ProfileCache;

        LauncherProfileFile? profileInfo = await LoadProfileFile();
        if (profileInfo is not null)
        {
            ProfileCache = profileInfo.Profiles;
            return ProfileCache;
        }

        return [];
    }

    private async ValueTask<LauncherProfileFile?> LoadProfileFile()
    {
        using var profiles = File.OpenRead(LAUNCHER_PROFILES);
        return await JsonSerializer.DeserializeAsync<LauncherProfileFile>(profiles);
    }

    public void FlushCache()
    {
        ProfileCache = null;
    }

    public bool Backup()
    {
        try
        {
            File.Copy(LAUNCHER_PROFILES, LAUNCHER_PROFILES + $".backup-{DateTime.UtcNow:yyyyMMddHHmmss}");
            return true;
        }

        catch
        {
            return false;
        }
    }

    public async Task UpdateProfileVersionAsync(string id, string newVersion)
    {
        var allProfiles = await LoadProfileFile();
        if (allProfiles is null)
            return;

        if (!allProfiles.Profiles.ContainsKey(id))
            return;

        LauncherProfile profile = allProfiles.Profiles[id];
        allProfiles.Profiles.Remove(id);
        profile.lastVersionId = "neoforge-" + newVersion;
        allProfiles.Profiles.Add(id, profile);

        using var fs = File.Open(Path.Combine(INSTALL_DIR, "launcher_profiles.json"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        await JsonSerializer.SerializeAsync(fs, allProfiles, PROFILE_FILE_OPTS);
        await fs.FlushAsync();
        fs.Close();
    }

    public async Task<LauncherProfile?> GetProfile(string id)
    {
        var allProfiles = await Profiles();
        return allProfiles.TryGetValue(id, out var profile) ? profile : null;
    }
}
