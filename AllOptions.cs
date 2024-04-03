using NeoForgeUpdater.Minecraft;
using NeoForgeUpdater.Neoforge;

namespace NeoForgeUpdater;

public class AllOptions
{
    public required MinecraftOptions Minecraft { get; set; }

    public required NeoforgeOptions Neoforge { get; set; }
}