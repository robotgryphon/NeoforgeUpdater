using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NeoForgeUpdater;
using NeoForgeUpdater.Minecraft;
using NeoForgeUpdater.Neoforge;
using Spectre.Console;
using System.Text.Json;

// Check settings.json
try { await CheckSettingsFile(); }
catch (InvalidOperationException) { return -1; }

using var http = new HttpClient();

#region Setup Config
var config = new ConfigurationBuilder()
    .AddJsonFile("settings.json", false)
    .Build();

var services = new ServiceCollection()
    .AddSingleton<HttpClient>(http)
    .AddSingleton<ProfileManager>()
    .AddSingleton<Neoforge>()
    .Configure<MinecraftOptions>(config.GetRequiredSection("Minecraft"))
    .Configure<NeoforgeOptions>(config.GetRequiredSection("Neoforge"))
    .BuildServiceProvider();
#endregion

var neoforge = services.GetRequiredService<Neoforge>();
var profileManager = services.GetRequiredService<ProfileManager>();

var managerReady = await profileManager.Initialize();
if (!managerReady) 
    return -2;

string? latest = await neoforge.GetLatestVersion();
if(latest is null)
{
    AnsiConsole.MarkupLine("[bold red]Failed to fetch version information from Neoforge servers. Please try again later.[/]");
    return -3;
}

var profiles = await profileManager.Profiles();
var prompt = new SelectionPrompt<KeyValuePair<string, LauncherProfile>>()
    .Title($"Select a profile to update to [yellow]{latest}[/]")
    .PageSize(5)
    .AddChoices(profiles)
    .UseConverter(profile =>
    {
        string profileName = string.IsNullOrWhiteSpace(profile.Value.name) ? "<no name>" : profile.Value.name.Trim();
        if (profileName.Length >= 22)
            profileName = profileName[..22] + "...";

        return $"[white]{profileName,-25}[/] [gray]({profile.Key} - {profile.Value.lastVersionId})[/]";
    });

var targetProfile = AnsiConsole.Prompt(prompt);

AnsiConsole.MarkupLineInterpolated($"Targeting profile: [deepskyblue3]{targetProfile.Value.name}[/]");

if (targetProfile.Value.lastVersionId == latest)
{
    AnsiConsole.MarkupLineInterpolated($"[gray]Profile version is {targetProfile.Value.lastVersionId}, matching the latest Neo version. Nothing to do!");
    return 0;
}

var progress = AnsiConsole.Progress()
    .AutoClear(false)
    .HideCompleted(false);

await progress.StartAsync((ctx) => ProfileProgress(ctx, latest, targetProfile, profileManager, neoforge));

static async Task ProfileProgress(ProgressContext ctx, string latest, KeyValuePair<string, LauncherProfile> targetProfile, ProfileManager profiles, Neoforge neoforge)
{
    var downloadNeo = ctx.AddTask($"Downloading Neoforge [yellow]{latest}[/]");
    downloadNeo.IsIndeterminate = true;

    var installClient = ctx.AddTask("Set up Minecraft Launcher");
    var backupProfile = ctx.AddTask("Back up profiles");
    var updateProfile = ctx.AddTask($"Update profile");

    var downloadTask = neoforge.DownloadAsync(latest);
    Task<Neoforge.InstallResult>? installClientVersion = null;

    while (!ctx.IsFinished)
    {
        if (!downloadNeo.IsFinished)
        {
            if (!downloadTask.IsCompleted)
                continue;
            else
            {
                downloadNeo.Value(100);
                downloadNeo.StopTask();
                if (!downloadTask.Result)
                {
                    AnsiConsole.MarkupLine("[bold red]Failed to fetch version information from Neoforge servers. Please try again later.[/]");
                    installClient.StopTask();
                    updateProfile.StopTask();
                    ctx.Refresh();
                    return;
                }
            }
        }

        if (downloadNeo.IsFinished && !installClient.IsFinished)
        {
            if(installClientVersion is null)
                installClientVersion = neoforge.InstallClient(latest);

            if (installClientVersion.IsCompleted)
            {
                if(installClientVersion.Result == Neoforge.InstallResult.Cached)
                    installClient.Description += " (cached)";

                installClient.Increment(100);
                continue;
            }
        }

        if (installClient.IsFinished)
        {
            var backedUp = profiles.Backup();
            if(backedUp)
            {
                backupProfile.Increment(100);

                await profiles.UpdateProfileVersionAsync(targetProfile.Key, latest);
                updateProfile.Increment(100);
            } else
            {
                backupProfile.StopTask();
                updateProfile.StopTask();

                AnsiConsole.MarkupLine("[bold red]Failed to back up launcher profiles. Exiting without changes for safety.[/]");
            }

        }

        await Task.Delay(100);
    }
}

Console.WriteLine("Done!");
return 0;

static async Task CheckSettingsFile()
{
    if (!File.Exists("settings.json"))
    {
        // Create default settings file
        var opts = new AllOptions
        {
            Minecraft = new MinecraftOptions
            {
                InstallDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft")
            },

            Neoforge = new NeoforgeOptions
            {
                LatestVersionEndpoint = new Uri("https://maven.neoforged.net/api/maven/latest/version/releases/net%2Fneoforged%2Fneoforge")
            }
        };

        var writeOpts = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        await File.WriteAllTextAsync("settings.json", JsonSerializer.Serialize(opts, writeOpts));

        AnsiConsole.MarkupLine("[bold red]Settings file not found; a default one has been created. Please enter info before trying again.[/]");
        throw new InvalidOperationException();
    }
}
