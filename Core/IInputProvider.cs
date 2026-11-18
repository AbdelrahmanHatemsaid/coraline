using OpenTK.Mathematics;

namespace Coraline.Core;

public interface IInputProvider
{
    Vector2 GetMovementAxes();
    bool IsSprintHeld();
    bool IsToggleWorldPressed();
    bool IsInteractHeld();
    bool IsDistractionPressed();
}
