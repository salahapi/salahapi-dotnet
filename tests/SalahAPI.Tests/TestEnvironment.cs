using System.Runtime.CompilerServices;

namespace SalahAPI.Tests;

public static class TestEnvironment
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        // IqamaCalculator and TimeHelpers resolve DST through TimeZoneInfo.Local (the .NET
        // equivalent of PHP's per-DateTime timezone). Pin the process timezone to
        // America/Los_Angeles so the DST fixtures behave identically on any machine.
        if (Environment.GetEnvironmentVariable("TZ") is null)
            Environment.SetEnvironmentVariable("TZ", "America/Los_Angeles");

        TimeZoneInfo.ClearCachedData();
    }
}
