// Services/UploadHistory.cs
using System.Security.Cryptography;
using System.Text.Json;

namespace WarcraftPulseUploader.Services;

public sealed class UploadEntry
{
    public string FileName    { get; set; } = "";
    public string FileHash    { get; set; } = "";   // SHA-256 of file contents
    public string ZoneName    { get; set; } = "";
    public int    FightCount  { get; set; }
    public int    KillCount   { get; set; }
    public string ReportCode  { get; set; } = "";
    public string StatusUrl   { get; set; } = "";
    public DateTime UploadedAt { get; set; }
}

public sealed class UploadHistory
{
    private const int MaxEntries = 50;

    private static readonly string HistoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WarcraftPulseUploader",
        "history.json"
    );

    private List<UploadEntry> _entries = [];

    public IReadOnlyList<UploadEntry> Entries => _entries;

    public static UploadHistory Load()
    {
        var h = new UploadHistory();
        if (!File.Exists(HistoryPath)) return h;
        try
        {
            var json = File.ReadAllText(HistoryPath);
            h._entries = JsonSerializer.Deserialize<List<UploadEntry>>(json) ?? [];
        }
        catch (Exception) { /* corrupt history — start fresh */ }
        return h;
    }

    public bool IsDuplicate(string fileHash) =>
        _entries.Any(e => e.FileHash == fileHash);

    public void Add(UploadEntry entry)
    {
        _entries.Insert(0, entry);
        if (_entries.Count > MaxEntries)
            _entries = _entries[..MaxEntries];
        try { Save(); } catch (Exception) { /* non-critical — in-memory history is still updated */ }
    }

    private static readonly JsonSerializerOptions WriteIndented = new() { WriteIndented = true };

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(HistoryPath)!);
        File.WriteAllText(HistoryPath, JsonSerializer.Serialize(_entries, WriteIndented));
    }

    public static string HashFile(string path)
    {
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(sha.ComputeHash(stream));
    }
}
