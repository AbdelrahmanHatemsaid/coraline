namespace Coraline.GameLogic;

public interface IPlayerStance
{
    string Name { get; }
    float MoveSpeed { get; }
    float NoiseRadius { get; }
}

public sealed class SneakStance : IPlayerStance
{
    public string Name => "Sneak";
    public float MoveSpeed => 2.8f;
    public float NoiseRadius => 0.0f;
}

public sealed class SprintStance : IPlayerStance
{
    public string Name => "Sprint";
    public float MoveSpeed => 5.4f;
    public float NoiseRadius => 6.0f;
}
