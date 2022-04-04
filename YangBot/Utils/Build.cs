using System.Reflection;
using Konata.Core;

#pragma warning disable CS8602

namespace YangBot.Utils;

public static class BuildStamp
{
    private static readonly string[] Stamp
        = typeof(Bot).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(x => x.Key == "BuildStamp").Value.Split(";");

    public static string Branch
        => Stamp[0];

    public static string CommitHash
        => Stamp[1][..16];

    public static string BuildTime
        => Stamp[2];

    public static string Version { get; } = typeof(Bot).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
}