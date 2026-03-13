// Services/WowDirectoryDetector.cs
namespace WarcraftPulseUploader.Services;

public static class WowDirectoryDetector
{
    // Ordered by likelihood. Season of Discovery installs in _classic_ on some clients.
    private static readonly string[] CandidatePaths =
    [
        @"C:\Program Files (x86)\World of Warcraft\_classic_era_\Logs",
        @"C:\Program Files (x86)\World of Warcraft\_classic_\Logs",
        @"C:\Program Files\World of Warcraft\_classic_era_\Logs",
        @"C:\Program Files\World of Warcraft\_classic_\Logs",
    ];

    /// <summary>
    /// Returns the first existing candidate path, or null if WoW is not found in a standard location.
    /// </summary>
    public static string? Detect() => CandidatePaths.FirstOrDefault(Directory.Exists);
}
