using Coraline.Core;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Coraline.Rendering;

public sealed class OpenTkInputProvider : IInputProvider
{
    private KeyboardState? _keyboard;

    public void UpdateState(KeyboardState keyboardState)
    {
        _keyboard = keyboardState;
    }

    public Vector2 GetMovementAxes()
    {
        if (_keyboard is null)
        {
            return Vector2.Zero;
        }

        var x = 0f;
        var y = 0f;
        if (_keyboard.IsKeyDown(Keys.D) || _keyboard.IsKeyDown(Keys.Right)) x += 1f;
        if (_keyboard.IsKeyDown(Keys.A) || _keyboard.IsKeyDown(Keys.Left)) x -= 1f;
        if (_keyboard.IsKeyDown(Keys.S) || _keyboard.IsKeyDown(Keys.Down)) y += 1f;
        if (_keyboard.IsKeyDown(Keys.W) || _keyboard.IsKeyDown(Keys.Up)) y -= 1f;
        return new Vector2(x, y);
    }

    public bool IsSprintHeld() =>
        _keyboard is not null && (_keyboard.IsKeyDown(Keys.LeftShift) || _keyboard.IsKeyDown(Keys.RightShift));

    public bool IsToggleWorldPressed() =>
        _keyboard is not null && _keyboard.IsKeyPressed(Keys.Tab);

    public bool IsInteractHeld() =>
        _keyboard is not null && _keyboard.IsKeyDown(Keys.E);

    public bool IsDistractionPressed() =>
        _keyboard is not null && _keyboard.IsKeyPressed(Keys.Q);
}
