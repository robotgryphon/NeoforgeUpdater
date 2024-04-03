using Microsoft.Extensions.Options;
using NeoForgeUpdater.Minecraft;
using Spectre.Console;
using System.Diagnostics;
using System.Net.Http.Json;

namespace NeoForgeUpdater.Neoforge;
public class Neoforge(HttpClient client, IOptions<MinecraftOptions> mcOptions, IOptions<NeoforgeOptions> neoOptions)
{
    public readonly Uri UPDATE_URL = neoOptions.Value.LatestVersionEndpoint;
    private readonly string INSTALL_DIR = mcOptions.Value.InstallDirectory;
    private readonly HttpClient client = client;

    public async Task<string?> GetLatestVersion()
    {
        string? version = null;

        await AnsiConsole.Status()
        .AutoRefresh(false)
        .Spinner(Spinner.Known.Dots2)
        .SpinnerStyle(Style.Parse("green bold"))
        .StartAsync("Getting latest Neoforge version...", async ctx =>
        {
            // Omitted
            var response = await client.GetFromJsonAsync<NeoforgeLatestVersionResponse>(UPDATE_URL);
            version = response.version;
            ctx.Refresh();
        });

        return version;
    }

    public enum InstallResult
    {
        FreshlyInstalled,
        Cached,
        Errored
    }

    public async Task<InstallResult> InstallClient(string version)
    {
        string installLocation = Path.Combine(INSTALL_DIR, "versions", "neoforge-" + version);
        if (Directory.Exists(installLocation))
            return InstallResult.Cached;

        try
        {
            string filename = $"neoforge-{version}-installer.jar";
            string diskFilename = Path.Combine("versions", filename);

            var start = new ProcessStartInfo
            {
                FileName = "java.exe",
                Arguments = $"-jar \"{diskFilename}\" --install-client",
                WorkingDirectory = Environment.CurrentDirectory,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = true
            };

            var p = Process.Start(start);
            if (p is not null)
                await p.WaitForExitAsync().ConfigureAwait(false);

            return InstallResult.FreshlyInstalled;
        }

        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return InstallResult.Errored;
        }
    }

    internal async Task<bool> DownloadAsync(string version)
    {
        string filename = $"neoforge-{version}-installer.jar";
        string diskFilename = Path.Combine("versions", filename);
        if (File.Exists(diskFilename))
            return true;

        var downloadUrl = $"https://maven.neoforged.net/releases/net/neoforged/neoforge/{version}/{filename}";

        using HttpResponseMessage fileResponse = await client.GetAsync(downloadUrl);
        if (fileResponse.IsSuccessStatusCode)
        {
            Directory.CreateDirectory("versions");
            using var outputFile = File.OpenWrite(diskFilename);

            await fileResponse.Content.CopyToAsync(outputFile);
            return true;
        }

        return false;
    }
}
