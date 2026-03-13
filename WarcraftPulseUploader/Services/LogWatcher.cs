// Services/LogWatcher.cs
namespace WarcraftPulseUploader.Services;

public sealed class LogWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;

    /// <summary>Raised on the FSW thread — marshal to UI thread before touching controls.</summary>
    public event EventHandler<string>? NewLogDetected;

    public LogWatcher(string directory)
    {
        _watcher = new FileSystemWatcher(directory, "WoWCombatLog*.txt")
        {
            NotifyFilter        = NotifyFilters.FileName | NotifyFilters.LastWrite,
            EnableRaisingEvents = false,
        };
        _watcher.Created += (_, e) => NewLogDetected?.Invoke(this, e.FullPath);
        _watcher.Renamed += (_, e) => NewLogDetected?.Invoke(this, e.FullPath);
    }

    public void Start() => _watcher.EnableRaisingEvents = true;
    public void Stop()  => _watcher.EnableRaisingEvents = false;

    public void Dispose() => _watcher.Dispose();
}
