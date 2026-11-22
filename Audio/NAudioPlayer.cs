using Coraline.Core;
using NAudio.Wave;

namespace Coraline.Audio;

public sealed class NAudioPlayer : IAudioService, IDisposable
{
    private readonly string _audioDirectory;
    private readonly List<WaveOutEvent> _activePlayers = [];
    private readonly object _lock = new();
    private bool _disposed;

    public NAudioPlayer(string? audioDirectory = null)
    {
        _audioDirectory = audioDirectory ?? Path.Combine(AppContext.BaseDirectory, "Assets", "Audio");
    }

    public void Play(string soundName)
    {
        if (_disposed) return;

        var fileName = soundName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ? soundName : $"{soundName}.wav";
        var fullPath = Path.Combine(_audioDirectory, fileName);
        if (!File.Exists(fullPath)) return;

        try
        {
            var reader = new AudioFileReader(fullPath);
            var output = new WaveOutEvent();
            output.Init(reader);
            output.PlaybackStopped += (_, _) =>
            {
                lock (_lock)
                {
                    _activePlayers.Remove(output);
                }
                output.Dispose();
                reader.Dispose();
            };

            lock (_lock)
            {
                _activePlayers.Add(output);
            }
            output.Play();
        }
        catch
        {
            // Gracefully ignore audio hardware initialization errors in headless or virtual environments
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_lock)
        {
            foreach (var player in _activePlayers)
            {
                try
                {
                    player.Stop();
                    player.Dispose();
                }
                catch
                {
                }
            }
            _activePlayers.Clear();
        }
    }
}
