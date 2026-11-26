using Coraline.Core;
using OpenTK.Mathematics;

namespace Coraline.Rendering;

public static class KitchenAndBedroomProps
{
    public static void AddKitchenFurniture(List<PropPart> list, WorldDimension world)
    {
        var isOther = world == WorldDimension.OtherWorld;
        var wood = isOther ? new Vector4(0.26f, 0.18f, 0.36f, 1f) : new Vector4(0.48f, 0.29f, 0.18f, 1f);
        var darkWood = isOther ? new Vector4(0.16f, 0.12f, 0.26f, 1f) : new Vector4(0.30f, 0.18f, 0.12f, 1f);
        var stone = isOther ? new Vector4(0.24f, 0.26f, 0.42f, 1f) : new Vector4(0.68f, 0.66f, 0.62f, 1f);
        var iron = isOther ? new Vector4(0.18f, 0.16f, 0.28f, 1f) : new Vector4(0.20f, 0.21f, 0.24f, 1f);
        var brass = isOther ? new Vector4(0.32f, 0.94f, 0.76f, 1f) : new Vector4(0.92f, 0.72f, 0.26f, 1f);
        var glow = isOther ? new Vector4(0.20f, 0.95f, 0.72f, 1f) : new Vector4(0.98f, 0.44f, 0.14f, 1f);
        var rug = isOther ? new Vector4(0.16f, 0.20f, 0.36f, 1f) : new Vector4(0.34f, 0.28f, 0.26f, 1f);

        // Kitchen Woven Runner Rug under dining table
        list.Add(new(new(12.2f, 0.02f, 8.0f), new(3.8f, 0.03f, 3.0f), 0f, PropShape.Cube, rug, false));

        // Long Kitchen Counter + Cast-Iron Stove + Hood along top wall + Icebox on right wall
        list.Add(new(new(11.4f, 0.44f, 1.50f), new(3.2f, 0.88f, 0.80f), 0f, PropShape.Cube, darkWood));
        list.Add(new(new(11.4f, 0.92f, 1.50f), new(3.28f, 0.10f, 0.86f), 0f, PropShape.Cube, stone));
        list.Add(new(new(13.8f, 0.48f, 1.50f), new(1.25f, 0.96f, 0.84f), 0f, PropShape.Cube, iron));
        list.Add(new(new(13.8f, 1.48f, 1.45f), new(1.10f, 0.38f, 0.70f), 0f, PropShape.Cube, iron));
        list.Add(new(new(13.55f, 0.98f, 1.45f), new(0.32f, 0.06f, 0.32f), 0f, PropShape.Cylinder, glow));
        list.Add(new(new(14.05f, 0.98f, 1.45f), new(0.38f, 0.28f, 0.38f), 0f, PropShape.Cylinder, brass));
        list.Add(new(new(10.5f, 1.05f, 1.50f), new(0.45f, 0.18f, 0.45f), 0f, PropShape.Cylinder, stone));
        list.Add(new(new(15.15f, 0.78f, 2.75f), new(0.85f, 1.56f, 0.95f), 0f, PropShape.Cube, stone));

        // Central Dining Table + Tablecloth + 4 High-Back Chairs
        list.Add(new(new(12.2f, 0.68f, 8.0f), new(2.10f, 0.12f, 1.35f), 0f, PropShape.Cube, wood));
        list.Add(new(new(12.2f, 0.75f, 8.0f), new(0.55f, 0.04f, 1.38f), 0f, PropShape.Cube, stone));
        list.Add(new(new(11.3f, 0.34f, 7.45f), new(0.14f, 0.68f, 0.14f), 0f, PropShape.Cylinder, darkWood));
        list.Add(new(new(13.1f, 0.34f, 7.45f), new(0.14f, 0.68f, 0.14f), 0f, PropShape.Cylinder, darkWood));
        list.Add(new(new(11.3f, 0.34f, 8.55f), new(0.14f, 0.68f, 0.14f), 0f, PropShape.Cylinder, darkWood));
        list.Add(new(new(13.1f, 0.34f, 8.55f), new(0.14f, 0.68f, 0.14f), 0f, PropShape.Cylinder, darkWood));
        list.Add(new(new(12.2f, 0.88f, 8.0f), new(0.18f, 0.24f, 0.18f), 0f, PropShape.Cylinder, brass));
        list.Add(new(new(11.6f, 0.78f, 8.0f), new(0.34f, 0.06f, 0.34f), 0f, PropShape.Cylinder, stone));
        list.Add(new(new(12.8f, 0.78f, 8.0f), new(0.34f, 0.06f, 0.34f), 0f, PropShape.Cylinder, stone));

        AddChair(list, new(10.85f, 8.0f), MathF.PI * 0.5f, wood, darkWood);
        AddChair(list, new(13.55f, 8.0f), -MathF.PI * 0.5f, wood, darkWood);
        AddChair(list, new(12.2f, 7.05f), 0f, wood, darkWood);
        AddChair(list, new(12.2f, 8.95f), MathF.PI, wood, darkWood);

        // Pantry Cupboard, Baker's Rack, & Wine Barrels along Kitchen lower wall
        list.Add(new(new(9.8f, 0.75f, 13.4f), new(1.15f, 1.50f, 0.68f), 0f, PropShape.Cube, darkWood));
        list.Add(new(new(9.8f, 1.54f, 13.4f), new(1.24f, 0.10f, 0.75f), 0f, PropShape.Cube, wood));
        list.Add(new(new(12.3f, 0.52f, 13.4f), new(1.65f, 1.04f, 0.68f), 0f, PropShape.Cube, wood));
        list.Add(new(new(12.3f, 1.12f, 13.4f), new(1.70f, 0.10f, 0.72f), 0f, PropShape.Cube, stone));
        list.Add(new(new(12.0f, 1.26f, 13.4f), new(0.38f, 0.22f, 0.38f), 0f, PropShape.Cylinder, brass));
        list.Add(new(new(14.7f, 0.42f, 13.3f), new(0.64f, 0.84f, 0.64f), 0f, PropShape.Cylinder, wood));
        list.Add(new(new(15.2f, 0.36f, 12.6f), new(0.52f, 0.72f, 0.52f), 0f, PropShape.Cylinder, darkWood));
    }

    public static void AddBedroomFurniture(List<PropPart> list, WorldDimension world)
    {
        var isOther = world == WorldDimension.OtherWorld;
        var wood = isOther ? new Vector4(0.24f, 0.16f, 0.36f, 1f) : new Vector4(0.42f, 0.24f, 0.16f, 1f);
        var darkWood = isOther ? new Vector4(0.15f, 0.10f, 0.25f, 1f) : new Vector4(0.26f, 0.14f, 0.10f, 1f);
        var quilt = isOther ? new Vector4(0.44f, 0.16f, 0.48f, 1f) : new Vector4(0.68f, 0.16f, 0.24f, 1f);
        var linen = isOther ? new Vector4(0.68f, 0.74f, 0.88f, 1f) : new Vector4(0.92f, 0.89f, 0.84f, 1f);
        var brass = isOther ? new Vector4(0.25f, 0.95f, 0.72f, 1f) : new Vector4(0.95f, 0.78f, 0.30f, 1f);
        var mirror = isOther ? new Vector4(0.30f, 0.88f, 0.92f, 1f) : new Vector4(0.66f, 0.80f, 0.88f, 1f);
        var plant = isOther ? new Vector4(0.18f, 0.62f, 0.58f, 1f) : new Vector4(0.20f, 0.52f, 0.24f, 1f);

        // Four-Poster Gothic Canopy Bed + Twin Nightstands + Toy Chest
        list.Add(new(new(19.6f, 0.28f, 13.15f), new(2.15f, 0.42f, 1.50f), 0f, PropShape.Cube, darkWood));
        list.Add(new(new(19.4f, 0.54f, 13.15f), new(1.65f, 0.16f, 1.38f), 0f, PropShape.Cube, quilt));
        list.Add(new(new(20.38f, 0.56f, 13.15f), new(0.40f, 0.18f, 1.36f), 0f, PropShape.Cube, linen));
        list.Add(new(new(20.38f, 0.68f, 12.80f), new(0.32f, 0.14f, 0.52f), 0f, PropShape.Cube, linen));
        list.Add(new(new(20.38f, 0.68f, 13.50f), new(0.32f, 0.14f, 0.52f), 0f, PropShape.Cube, linen));
        list.Add(new(new(20.68f, 0.72f, 13.15f), new(0.18f, 1.15f, 1.54f), 0f, PropShape.Cube, darkWood));
        list.Add(new(new(18.58f, 0.78f, 12.45f), new(0.14f, 1.56f, 0.14f), 0f, PropShape.Cylinder, darkWood));
        list.Add(new(new(18.58f, 0.78f, 13.85f), new(0.14f, 1.56f, 0.14f), 0f, PropShape.Cylinder, darkWood));
        list.Add(new(new(20.62f, 0.78f, 12.45f), new(0.14f, 1.56f, 0.14f), 0f, PropShape.Cylinder, darkWood));
        list.Add(new(new(20.62f, 0.78f, 13.85f), new(0.14f, 1.56f, 0.14f), 0f, PropShape.Cylinder, darkWood));
        list.Add(new(new(19.6f, 1.56f, 13.15f), new(2.18f, 0.10f, 1.54f), 0f, PropShape.Cube, quilt));
        list.Add(new(new(17.95f, 0.34f, 13.35f), new(0.62f, 0.68f, 0.62f), 0f, PropShape.Cube, wood));
        list.Add(new(new(17.95f, 0.80f, 13.35f), new(0.16f, 0.24f, 0.16f), 0f, PropShape.Cylinder, brass));
        list.Add(new(new(17.95f, 0.98f, 13.35f), new(0.36f, 0.24f, 0.36f), 0f, PropShape.Cylinder, linen));
        list.Add(new(new(17.85f, 0.28f, 11.45f), new(0.75f, 0.56f, 1.10f), 0f, PropShape.Cube, wood));

        // Mirrored Vanity Dresser, Wardrobe & Potted Plant
        list.Add(new(new(18.2f, 0.42f, 1.55f), new(1.35f, 0.84f, 0.65f), 0f, PropShape.Cube, wood));
        list.Add(new(new(18.2f, 1.24f, 1.38f), new(0.95f, 0.82f, 0.12f), 0f, PropShape.Cube, darkWood));
        list.Add(new(new(18.2f, 1.24f, 1.46f), new(0.78f, 0.66f, 0.06f), 0f, PropShape.Cube, mirror));
        list.Add(new(new(18.2f, 0.22f, 2.25f), new(0.45f, 0.44f, 0.45f), 0f, PropShape.Cylinder, quilt));
        list.Add(new(new(21.1f, 0.80f, 1.55f), new(1.25f, 1.60f, 0.72f), 0f, PropShape.Cube, darkWood));
        list.Add(new(new(21.1f, 1.64f, 1.55f), new(1.34f, 0.12f, 0.80f), 0f, PropShape.Cube, wood));

        // Bedroom Center: Tea Table, Plush Reading Chair, Pink Palace Dollhouse Table & Potted Fern
        list.Add(new(new(19.3f, 0.58f, 6.2f), new(0.95f, 0.10f, 0.95f), 0f, PropShape.Cylinder, wood));
        list.Add(new(new(19.3f, 0.29f, 6.2f), new(0.24f, 0.58f, 0.24f), 0f, PropShape.Cylinder, darkWood));
        list.Add(new(new(19.3f, 0.72f, 6.2f), new(0.24f, 0.22f, 0.24f), 0f, PropShape.Cylinder, brass));
        AddChair(list, new(18.0f, 7.6f), 0.75f, quilt, darkWood);
        list.Add(new(new(21.9f, 0.38f, 7.4f), new(0.90f, 0.76f, 1.15f), 0f, PropShape.Cube, wood));
        list.Add(new(new(21.9f, 1.02f, 7.4f), new(0.60f, 0.52f, 0.78f), 0f, PropShape.Cube, linen));
        list.Add(new(new(21.9f, 1.38f, 7.4f), new(0.68f, 0.24f, 0.86f), 0f, PropShape.Cube, quilt));
        list.Add(new(new(22.15f, 0.28f, 10.4f), new(0.45f, 0.56f, 0.45f), 0f, PropShape.Cylinder, wood));
        list.Add(new(new(22.15f, 0.78f, 10.4f), new(0.72f, 0.62f, 0.72f), 0f, PropShape.Cylinder, plant));
    }

    private static void AddChair(List<PropPart> list, Vector2 pos, float rotY, Vector4 seatCol, Vector4 frameCol)
    {
        var backOffset = new Vector2(-MathF.Sin(rotY), -MathF.Cos(rotY)) * 0.20f;
        list.Add(new(new(pos.X, 0.42f, pos.Y), new(0.48f, 0.10f, 0.48f), rotY, PropShape.Cube, seatCol));
        list.Add(new(new(pos.X, 0.21f, pos.Y), new(0.36f, 0.42f, 0.36f), rotY, PropShape.Cylinder, frameCol));
        list.Add(new(new(pos.X + backOffset.X, 0.74f, pos.Y + backOffset.Y), new(0.48f, 0.58f, 0.10f), rotY, PropShape.Cube, frameCol));
    }
}
