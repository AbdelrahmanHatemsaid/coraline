using Coraline.Core;
using OpenTK.Mathematics;

namespace Coraline.Rendering;

public enum PropShape
{
    Cube,
    Cylinder
}

public readonly record struct PropPart(
    Vector3 Position,
    Vector3 Scale,
    float RotationYRad,
    PropShape Shape,
    Vector4 Color
);

public static class PropPlacementLayout
{
    public static readonly Vector2[] WebTrapPositions =
    [
        new(6.5f, 10.5f),
        new(11.5f, 11.5f),
        new(19.5f, 7.5f)
    ];

    public static IReadOnlyList<PropPart> BuildRoomFurniture(WorldDimension world)
    {
        var isOther = world == WorldDimension.OtherWorld;
        var wood = isOther ? new Vector4(0.24f, 0.16f, 0.34f, 1f) : new Vector4(0.42f, 0.25f, 0.16f, 1f);
        var darkWood = isOther ? new Vector4(0.16f, 0.11f, 0.26f, 1f) : new Vector4(0.28f, 0.16f, 0.11f, 1f);
        var fabric = isOther ? new Vector4(0.36f, 0.14f, 0.36f, 1f) : new Vector4(0.56f, 0.18f, 0.22f, 1f);
        var brass = isOther ? new Vector4(0.55f, 0.85f, 0.78f, 1f) : new Vector4(0.84f, 0.68f, 0.26f, 1f);
        var stone = isOther ? new Vector4(0.22f, 0.24f, 0.38f, 1f) : new Vector4(0.58f, 0.56f, 0.52f, 1f);

        return
        [
            // Drawing Room: Sofa & round coffee table + candle
            new(new(4.5f, 0.22f, 2.0f), new(1.8f, 0.36f, 0.75f), 0f, PropShape.Cube, fabric),
            new(new(4.5f, 0.48f, 1.7f), new(1.8f, 0.42f, 0.22f), 0f, PropShape.Cube, darkWood),
            new(new(4.5f, 0.24f, 3.6f), new(0.85f, 0.08f, 0.85f), 0f, PropShape.Cylinder, wood),
            new(new(4.5f, 0.12f, 3.6f), new(0.20f, 0.24f, 0.20f), 0f, PropShape.Cylinder, darkWood),
            new(new(4.5f, 0.35f, 3.6f), new(0.10f, 0.14f, 0.10f), 0f, PropShape.Cylinder, brass),
            // Drawing Room: Grandfather clock / tall cabinet
            new(new(1.5f, 0.65f, 13.4f), new(0.65f, 1.30f, 0.65f), 0f, PropShape.Cube, darkWood),
            new(new(1.5f, 1.05f, 13.05f), new(0.34f, 0.34f, 0.08f), 0f, PropShape.Cylinder, brass),

            // Kitchen: Countertop, pot, & center dining table
            new(new(11.5f, 0.40f, 1.45f), new(2.4f, 0.80f, 0.65f), 0f, PropShape.Cube, darkWood),
            new(new(11.5f, 0.83f, 1.45f), new(2.46f, 0.08f, 0.70f), 0f, PropShape.Cube, stone),
            new(new(11.0f, 0.98f, 1.45f), new(0.34f, 0.22f, 0.34f), 0f, PropShape.Cylinder, brass),
            new(new(12.2f, 0.48f, 8.0f), new(1.35f, 0.10f, 0.95f), 0f, PropShape.Cube, wood),
            new(new(12.2f, 0.24f, 8.0f), new(0.35f, 0.48f, 0.35f), 0f, PropShape.Cylinder, darkWood),
            new(new(11.2f, 0.20f, 8.0f), new(0.36f, 0.38f, 0.36f), 0f, PropShape.Cylinder, wood),
            new(new(13.2f, 0.20f, 8.0f), new(0.36f, 0.38f, 0.36f), 0f, PropShape.Cylinder, wood),

            // Bedroom: Gothic bed & wardrobe
            new(new(20.4f, 0.24f, 13.4f), new(1.70f, 0.42f, 1.10f), 0f, PropShape.Cube, fabric),
            new(new(21.2f, 0.56f, 13.4f), new(0.20f, 0.85f, 1.15f), 0f, PropShape.Cube, darkWood),
            new(new(20.85f, 0.49f, 13.4f), new(0.36f, 0.12f, 0.80f), 0f, PropShape.Cube, stone),
            new(new(17.7f, 0.62f, 1.55f), new(0.95f, 1.24f, 0.65f), 0f, PropShape.Cube, darkWood)
        ];
    }

    public static IReadOnlyList<PropPart> BuildSmallDoorArch(Vector2 doorPos, bool unlocked, WorldDimension world)
    {
        var frameCol = world == WorldDimension.OtherWorld
            ? new Vector4(0.28f, 0.18f, 0.42f, 1f)
            : new Vector4(0.36f, 0.22f, 0.14f, 1f);
        var knobCol = unlocked
            ? new Vector4(0.20f, 0.98f, 0.65f, 1f)
            : new Vector4(0.86f, 0.70f, 0.26f, 1f);

        return
        [
            new(new(doorPos.X, 0.55f, doorPos.Y - 0.46f), new(0.18f, 1.10f, 0.18f), 0f, PropShape.Cube, frameCol),
            new(new(doorPos.X, 0.55f, doorPos.Y + 0.46f), new(0.18f, 1.10f, 0.18f), 0f, PropShape.Cube, frameCol),
            new(new(doorPos.X, 1.15f, doorPos.Y), new(0.22f, 0.18f, 1.12f), 0f, PropShape.Cube, frameCol),
            new(new(doorPos.X + 0.14f, 0.55f, doorPos.Y + 0.22f), new(0.14f, 0.14f, 0.14f), 0f, PropShape.Cylinder, knobCol)
        ];
    }

    public static IReadOnlyList<PropPart> BuildWebTrap(Vector2 pos)
    {
        var silk = new Vector4(0.82f, 0.88f, 0.96f, 0.85f);
        return
        [
            new(new(pos.X, 0.025f, pos.Y), new(0.90f, 0.02f, 0.06f), 0.0f, PropShape.Cube, silk),
            new(new(pos.X, 0.025f, pos.Y), new(0.90f, 0.02f, 0.06f), MathF.PI / 3f, PropShape.Cube, silk),
            new(new(pos.X, 0.025f, pos.Y), new(0.90f, 0.02f, 0.06f), -MathF.PI / 3f, PropShape.Cube, silk),
            new(new(pos.X, 0.022f, pos.Y), new(0.38f, 0.015f, 0.38f), 0f, PropShape.Cylinder, new(0.75f, 0.82f, 0.92f, 0.65f))
        ];
    }
}
