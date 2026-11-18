using System.Text.Json;
using Coraline.Core;

namespace Coraline.GameLogic;

public sealed record TileData(
    int X,
    int Y,
    string Room,
    bool RealWorldWalkable,
    bool OtherWorldWalkable
);

public sealed record GridCoord(int X, int Y);
public sealed record DoorPlacement(int X, int Y, string Room);
public sealed record EyePlacement(int Id, int X, int Y, string Room);
public sealed record GuardPlacement(int Id, string Room, List<GridCoord> Waypoints);

public sealed class LevelMap
{
    public string LevelId { get; init; } = "level1_other_house";
    public int Width { get; init; } = 24;
    public int Height { get; init; } = 16;
    public List<string> Rooms { get; init; } = [];
    public GridCoord PlayerSpawn { get; init; } = new(2, 2);
    public DoorPlacement SmallDoor { get; init; } = new(1, 7, "DrawingRoom");
    public List<EyePlacement> GhostEyes { get; init; } = [];
    public List<GuardPlacement> Guards { get; init; } = [];
    public List<TileData> Tiles { get; init; } = [];

    public bool IsWalkable(int x, int y, WorldDimension dimension)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
        {
            return false;
        }

        var tile = GetTile(x, y);
        if (tile is null)
        {
            return false;
        }

        return dimension == WorldDimension.RealWorld
            ? tile.RealWorldWalkable
            : tile.OtherWorldWalkable;
    }

    public TileData? GetTile(int x, int y)
    {
        var index = y * Width + x;
        if (index >= 0 && index < Tiles.Count && Tiles[index].X == x && Tiles[index].Y == y)
        {
            return Tiles[index];
        }

        return Tiles.FirstOrDefault(t => t.X == x && t.Y == y);
    }
}

public static class MapLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static LevelMap LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        return LoadFromJson(json);
    }

    public static LevelMap LoadFromJson(string json)
    {
        return JsonSerializer.Deserialize<LevelMap>(json, JsonOptions)
            ?? throw new InvalidDataException("Failed to deserialize 24x16 level JSON.");
    }
}
