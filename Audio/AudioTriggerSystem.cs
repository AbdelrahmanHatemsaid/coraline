using Coraline.Core;

namespace Coraline.Audio;

public sealed class AudioTriggerSystem : IDisposable
{
    private readonly IAudioService _audioService;
    private float _footstepCooldown;

    public AudioTriggerSystem(GameEventBus eventBus, IAudioService audioService)
    {
        _audioService = audioService;
        eventBus.Subscribe<NoiseEmittedEvent>(OnNoiseEmitted);
    }

    public void Update(float deltaTime)
    {
        if (_footstepCooldown > 0f)
        {
            _footstepCooldown -= deltaTime;
        }
    }

    private void OnNoiseEmitted(NoiseEmittedEvent e)
    {
        if (e.SourceTag == "Distraction")
        {
            _audioService.Play("distraction");
        }
        else if (e.SourceTag == "Sprint")
        {
            if (_footstepCooldown <= 0f)
            {
                _audioService.Play("footsteps");
                _footstepCooldown = 0.28f;
            }
        }
        else if (e.SourceTag == "Alert")
        {
            _audioService.Play("alert");
        }
    }

    public void Dispose()
    {
    }
}
