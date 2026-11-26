using Coraline.Core;
using OpenTK.Mathematics;

namespace Coraline.Rendering;

public enum PropShape { Cube, Cylinder }

public readonly record struct PropPart(
    Vector3 Position,
    Vector3 Scale,
    float RotationYRad,
    PropShape Shape,
    Vector4 Color,
    bool CastOutline = true
);

public static class PropPlacementLayout
{
    public static readonly Vector2[] WebTrapPositions = [new(6.5f, 10.5f), new(11.5f, 11.5f), new(19.5f, 7.5f)];

    public static IReadOnlyList<PropPart> BuildRoomFurniture(WorldDimension world)
    {
        var list = new List<PropPart>();
        AddDrawingRoomFurniture(list, world);
        KitchenAndBedroomProps.AddKitchenFurniture(list, world);
        KitchenAndBedroomProps.AddBedroomFurniture(list, world);
        return list;
    }

    public static IReadOnlyList<PropPart> BuildSmallDoorArch(Vector2 doorPos, bool unlocked, WorldDimension world)
    {
        var isOther = world == WorldDimension.OtherWorld;
        var stone = isOther ? new Vector4(0.24f, 0.20f, 0.38f, 1f) : new Vector4(0.38f, 0.32f, 0.26f, 1f);
        var wood = isOther ? new Vector4(0.18f, 0.12f, 0.28f, 1f) : new Vector4(0.28f, 0.16f, 0.10f, 1f);
        var knob = unlocked ? new Vector4(0.18f, 0.98f, 0.65f, 1f) : new Vector4(0.88f, 0.72f, 0.24f, 1f);

        return
        [
            new(new(doorPos.X, 0.65f, doorPos.Y - 0.52f), new(0.24f, 1.30f, 0.22f), 0f, PropShape.Cube, stone),
            new(new(doorPos.X, 0.65f, doorPos.Y + 0.52f), new(0.24f, 1.30f, 0.22f), 0f, PropShape.Cube, stone),
            new(new(doorPos.X, 1.35f, doorPos.Y), new(0.28f, 0.24f, 1.28f), 0f, PropShape.Cube, stone),
            new(new(doorPos.X, 1.52f, doorPos.Y), new(0.24f, 0.16f, 0.90f), 0f, PropShape.Cube, wood),
            new(new(doorPos.X + 0.18f, 0.58f, doorPos.Y + 0.24f), new(0.16f, 0.16f, 0.16f), 0f, PropShape.Cylinder, knob)
        ];
    }

    public static IReadOnlyList<PropPart> BuildWebTrap(Vector2 pos)
    {
        var silk = new Vector4(0.84f, 0.90f, 0.98f, 0.90f);
        var parts = new List<PropPart>();
        for (var i = 0; i < 4; i++)
            parts.Add(new(new(pos.X, 0.025f, pos.Y), new(1.15f, 0.02f, 0.045f), i * MathF.PI / 4f, PropShape.Cube, silk, false));
        parts.Add(new(new(pos.X, 0.022f, pos.Y), new(0.55f, 0.015f, 0.55f), 0f, PropShape.Cylinder, new(0.78f, 0.85f, 0.95f, 0.7f), false));
        parts.Add(new(new(pos.X, 0.028f, pos.Y), new(0.22f, 0.020f, 0.22f), 0f, PropShape.Cylinder, silk, false));
        return parts;
    }

    public static void AddPottedPlant(List<PropPart> list, Vector2 pos, Vector4 potCol, Vector4 rimCol, Vector4 leafCol)
    {
        var darkLeaf = new Vector4(leafCol.X * 0.72f, leafCol.Y * 0.78f, leafCol.Z * 0.75f, 1f);
        list.Add(new(new(pos.X, 0.24f, pos.Y), new(0.46f, 0.48f, 0.46f), 0f, PropShape.Cylinder, potCol));
        list.Add(new(new(pos.X, 0.49f, pos.Y), new(0.52f, 0.08f, 0.52f), 0f, PropShape.Cylinder, rimCol));
        for (var i = 0; i < 6; i++)
        {
            var angle = i * MathF.PI / 3f;
            var dx = MathF.Cos(angle) * 0.16f;
            var dz = MathF.Sin(angle) * 0.16f;
            list.Add(new(new(pos.X + dx, 0.68f + (i % 2) * 0.08f, pos.Y + dz), new(0.36f, 0.10f, 0.14f), angle, PropShape.Cube, i % 2 == 0 ? leafCol : darkLeaf));
        }
        list.Add(new(new(pos.X, 0.88f, pos.Y), new(0.18f, 0.28f, 0.18f), 0.4f, PropShape.Cube, leafCol));
    }

    private static void AddDrawingRoomFurniture(List<PropPart> list, WorldDimension world)
    {
        var isOther = world == WorldDimension.OtherWorld;
        var wood = isOther ? new Vector4(0.28f, 0.18f, 0.38f, 1f) : new Vector4(0.46f, 0.27f, 0.17f, 1f);
        var darkWood = isOther ? new Vector4(0.18f, 0.12f, 0.28f, 1f) : new Vector4(0.24f, 0.14f, 0.09f, 1f);
        var velvet = isOther ? new Vector4(0.42f, 0.16f, 0.44f, 1f) : new Vector4(0.66f, 0.18f, 0.24f, 1f);
        var brass = isOther ? new Vector4(0.42f, 0.92f, 0.80f, 1f) : new Vector4(0.92f, 0.74f, 0.26f, 1f);
        var stone = isOther ? new Vector4(0.24f, 0.24f, 0.40f, 1f) : new Vector4(0.56f, 0.53f, 0.48f, 1f);
        var ember = isOther ? new Vector4(0.22f, 0.95f, 0.72f, 1f) : new Vector4(1.00f, 0.48f, 0.12f, 1f);
        var rug = isOther ? new Vector4(0.22f, 0.14f, 0.34f, 1f) : new Vector4(0.48f, 0.16f, 0.20f, 1f);
        var leaf = isOther ? new Vector4(0.18f, 0.72f, 0.62f, 1f) : new Vector4(0.22f, 0.58f, 0.26f, 1f);
        var ivory = isOther ? new Vector4(0.70f, 0.76f, 0.92f, 1f) : new Vector4(0.94f, 0.92f, 0.86f, 1f);
        var ebony = new Vector4(0.09f, 0.09f, 0.12f, 1f);

        // Bordered Parlor Rugs
        list.Add(new(new(4.4f, 0.018f, 3.8f), new(3.55f, 0.02f, 2.95f), 0f, PropShape.Cube, brass, false));
        list.Add(new(new(4.4f, 0.025f, 3.8f), new(3.35f, 0.025f, 2.75f), 0f, PropShape.Cube, rug, false));
        list.Add(new(new(4.8f, 0.018f, 10.4f), new(3.75f, 0.02f, 3.55f), 0f, PropShape.Cube, brass, false));
        list.Add(new(new(4.8f, 0.025f, 10.4f), new(3.55f, 0.025f, 3.35f), 0f, PropShape.Cube, new(rug.X * 0.88f, rug.Y * 0.95f, rug.Z * 1.1f, 1f), false));

        // Fireplace, Mantle, Logs, Glowing Hearth & Portrait Above
        list.Add(new(new(4.4f, 0.08f, 1.55f), new(2.1f, 0.16f, 0.95f), 0f, PropShape.Cube, stone));
        list.Add(new(new(3.6f, 0.65f, 1.45f), new(0.42f, 1.15f, 0.68f), 0f, PropShape.Cube, stone));
        list.Add(new(new(5.2f, 0.65f, 1.45f), new(0.42f, 1.15f, 0.68f), 0f, PropShape.Cube, stone));
        list.Add(new(new(4.4f, 1.28f, 1.45f), new(2.25f, 0.22f, 0.78f), 0f, PropShape.Cube, darkWood));
        list.Add(new(new(4.4f, 0.30f, 1.42f), new(0.95f, 0.42f, 0.45f), 0f, PropShape.Cube, ember));
        list.Add(new(new(3.7f, 1.52f, 1.45f), new(0.14f, 0.28f, 0.14f), 0f, PropShape.Cylinder, brass));
        list.Add(new(new(5.1f, 1.52f, 1.45f), new(0.14f, 0.28f, 0.14f), 0f, PropShape.Cylinder, brass));

        // Tufted Sofa (cushions, arms, backrest) + Coffee Table + Armchair
        list.Add(new(new(4.4f, 0.28f, 4.75f), new(2.2f, 0.32f, 0.85f), 0f, PropShape.Cube, velvet));
        list.Add(new(new(4.4f, 0.62f, 5.08f), new(2.2f, 0.56f, 0.24f), 0f, PropShape.Cube, darkWood));
        list.Add(new(new(3.38f, 0.48f, 4.75f), new(0.24f, 0.44f, 0.88f), 0f, PropShape.Cube, velvet));
        list.Add(new(new(5.42f, 0.48f, 4.75f), new(0.24f, 0.44f, 0.88f), 0f, PropShape.Cube, velvet));
        list.Add(new(new(3.9f, 0.48f, 4.70f), new(0.75f, 0.12f, 0.58f), 0f, PropShape.Cube, new(velvet.X * 1.15f, velvet.Y * 1.15f, velvet.Z * 1.15f, 1f)));
        list.Add(new(new(4.9f, 0.48f, 4.70f), new(0.75f, 0.12f, 0.58f), 0f, PropShape.Cube, new(velvet.X * 1.15f, velvet.Y * 1.15f, velvet.Z * 1.15f, 1f)));
        list.Add(new(new(4.4f, 0.38f, 3.25f), new(1.25f, 0.10f, 0.80f), 0f, PropShape.Cube, wood));
        list.Add(new(new(3.9f, 0.19f, 2.95f), new(0.12f, 0.38f, 0.12f), 0f, PropShape.Cylinder, darkWood));
        list.Add(new(new(4.9f, 0.19f, 2.95f), new(0.12f, 0.38f, 0.12f), 0f, PropShape.Cylinder, darkWood));
        list.Add(new(new(3.9f, 0.19f, 3.55f), new(0.12f, 0.38f, 0.12f), 0f, PropShape.Cylinder, darkWood));
        list.Add(new(new(4.9f, 0.19f, 3.55f), new(0.12f, 0.38f, 0.12f), 0f, PropShape.Cylinder, darkWood));
        list.Add(new(new(4.25f, 0.48f, 3.25f), new(0.32f, 0.08f, 0.24f), 0.25f, PropShape.Cube, ivory));
        list.Add(new(new(4.65f, 0.56f, 3.25f), new(0.12f, 0.24f, 0.12f), 0f, PropShape.Cylinder, ember));
        list.Add(new(new(2.25f, 0.30f, 3.35f), new(0.85f, 0.38f, 0.85f), 0.35f, PropShape.Cube, velvet));
        list.Add(new(new(2.00f, 0.62f, 3.35f), new(0.24f, 0.56f, 0.85f), 0.35f, PropShape.Cube, darkWood));

        // Detailed Bookcase (shelves + varied book spines)
        list.Add(new(new(6.85f, 0.78f, 1.50f), new(1.25f, 1.56f, 0.48f), 0f, PropShape.Cube, darkWood));
        for (var b = 0; b < 5; b++)
        {
            var bx = 6.38f + b * 0.23f;
            var bookCol = (b % 3) switch { 0 => velvet, 1 => brass, _ => new Vector4(0.22f, 0.48f, 0.58f, 1f) };
            list.Add(new(new(bx, 0.52f, 1.66f), new(0.16f, 0.30f, 0.28f), 0f, PropShape.Cube, bookCol));
            list.Add(new(new(bx, 1.04f, 1.66f), new(0.16f, 0.28f, 0.28f), 0f, PropShape.Cube, (b % 2 == 0) ? ivory : velvet));
        }

        // Grand Piano (ebony body, ivory keys, black sharp keys, music stand + pages, bench, brass pedals)
        list.Add(new(new(5.8f, 0.72f, 9.8f), new(1.45f, 0.34f, 1.72f), 0f, PropShape.Cube, ebony));
        list.Add(new(new(5.04f, 0.74f, 9.8f), new(0.28f, 0.10f, 1.44f), 0f, PropShape.Cube, ivory));
        for (var k = 0; k < 5; k++)
            list.Add(new(new(5.10f, 0.80f, 9.22f + k * 0.28f), new(0.14f, 0.05f, 0.10f), 0f, PropShape.Cube, ebony));
        list.Add(new(new(5.32f, 1.04f, 9.8f), new(0.08f, 0.28f, 0.56f), 0f, PropShape.Cube, darkWood));
        list.Add(new(new(5.26f, 1.06f, 9.8f), new(0.04f, 0.22f, 0.44f), 0f, PropShape.Cube, ivory));
        list.Add(new(new(5.3f, 0.32f, 9.2f), new(0.14f, 0.64f, 0.14f), 0f, PropShape.Cylinder, ebony));
        list.Add(new(new(6.3f, 0.32f, 9.2f), new(0.14f, 0.64f, 0.14f), 0f, PropShape.Cylinder, ebony));
        list.Add(new(new(6.3f, 0.32f, 10.5f), new(0.14f, 0.64f, 0.14f), 0f, PropShape.Cylinder, ebony));
        list.Add(new(new(5.22f, 0.08f, 9.8f), new(0.12f, 0.10f, 0.36f), 0f, PropShape.Cube, brass));
        list.Add(new(new(4.35f, 0.38f, 9.8f), new(0.55f, 0.14f, 1.10f), 0f, PropShape.Cube, velvet));
        list.Add(new(new(4.35f, 0.18f, 9.8f), new(0.45f, 0.32f, 0.95f), 0f, PropShape.Cube, ebony));

        // Writing Desk, Clock (dial + crown) & Lush Potted Fern
        list.Add(new(new(2.35f, 0.68f, 11.4f), new(1.35f, 0.12f, 0.78f), 0f, PropShape.Cube, wood));
        list.Add(new(new(1.80f, 0.34f, 11.4f), new(0.26f, 0.68f, 0.72f), 0f, PropShape.Cube, darkWood));
        list.Add(new(new(2.90f, 0.34f, 11.4f), new(0.26f, 0.68f, 0.72f), 0f, PropShape.Cube, darkWood));
        list.Add(new(new(2.05f, 0.88f, 11.35f), new(0.18f, 0.32f, 0.18f), 0f, PropShape.Cylinder, brass));
        list.Add(new(new(2.55f, 0.78f, 11.40f), new(0.32f, 0.06f, 0.26f), -0.2f, PropShape.Cube, ivory));
        list.Add(new(new(2.35f, 0.42f, 10.55f), new(0.55f, 0.45f, 0.55f), 0f, PropShape.Cube, velvet));
        list.Add(new(new(1.55f, 0.68f, 9.30f), new(0.14f, 1.36f, 0.14f), 0f, PropShape.Cylinder, darkWood));
        list.Add(new(new(1.55f, 0.08f, 9.30f), new(0.42f, 0.16f, 0.42f), 0f, PropShape.Cylinder, darkWood));
        list.Add(new(new(1.85f, 0.80f, 13.45f), new(0.75f, 1.60f, 0.72f), 0f, PropShape.Cube, darkWood));
        list.Add(new(new(1.85f, 1.66f, 13.45f), new(0.84f, 0.14f, 0.80f), 0f, PropShape.Cube, wood));
        list.Add(new(new(1.85f, 1.25f, 13.06f), new(0.42f, 0.42f, 0.10f), 0f, PropShape.Cylinder, ivory));
        list.Add(new(new(1.85f, 0.70f, 13.06f), new(0.20f, 0.45f, 0.08f), 0f, PropShape.Cube, brass));
        AddPottedPlant(list, new(7.15f, 13.45f), wood, brass, leaf);
    }
}
