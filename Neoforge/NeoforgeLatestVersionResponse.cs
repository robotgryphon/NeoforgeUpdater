
//} else
//{
//    Console.Error.WriteLine("errored");
//}

internal readonly struct NeoforgeLatestVersionResponse
{
    public readonly string version { get; init; }
    public readonly bool isSnapshot { get; init; }
}

