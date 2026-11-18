using OpenTK.Mathematics;

namespace Coraline.Core;

public interface IInteractable
{
    int EntityId { get; }
    Vector2 Position { get; }
    bool CanInteract(WorldDimension activeWorld, int collectedEyes);
    void Interact();
}
