using Coraline.Core;
using NAudio.Wave;

namespace Coraline.Audio;

public sealed class NAudioPlayer : IAudioService, IDisposable
{
    private sealed record ActivePlayback(WaveOutEvent Output, AudioFileReader Reader);

    private readonly string _audioDirectory;
    private readonly List<ActivePlayback> _activePlayers = [];
    private readonly object _lock = new();
    private bool _disposed;

    public NAudioPlayer(string? audioDirectory = null)
    {
        _audioDirectory = audioDirectory ?? Path.Combine(AppContext.BaseDirectory, "Assets", "Audio");
    }

    public void Play(string soundName)
    {
        var fileName = soundName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ? soundName : $"{soundName}.wav";
        var fullPath = Path.Combine(_audioDirectory, fileName);
        if (!File.Exists(fullPath)) return;

        ActivePlayback? entry = null;
        try
        {
            var reader = new AudioFileReader(fullPath);
            var output = new WaveOutEvent();
            entry = new ActivePlayback(output, reader);

            output.Init(reader);
            output.PlaybackStopped += (_, _) =>
            {
                bool shouldDispose;
                lock (_lock)
                {
                    shouldDispose = _activePlayers.Remove(entry);
                }

                if (shouldDispose)
                {
                    SafeDisposePlayback(entry, stopFirst: false);
                }
            };

            lock (_lock)
            {
                if (_disposed)
                {
                    SafeDisposePlayback(entry, stopFirst: false);
                    return;
                }
                _activePlayers.Add(entry);
            }

            output.Play();
        }
        catch
        {
            if (entry is not null)
            {
                lock (_lock) { _activePlayers.Remove(entry); }
                SafeDisposePlayback(entry, stopFirst: false);
            }
        }
    }

    public void Dispose()
    {
        List<ActivePlayback> toDispose;
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            toDispose = [.. _activePlayers];
            _activePlayers.Clear();
        }

        foreach (var entry in toDispose)
        {
            SafeDisposePlayback(entry, stopFirst: true);
        }
    }

    private static void SafeDisposePlayback(ActivePlayback entry, bool stopFirst)
    {
        if (stopFirst)
        {
            try { entry.Output.Stop(); } catch { }
        }
        try { entry.Output.Dispose(); } catch { }
        try { entry.Reader.Dispose(); } catch { }
    }
}
