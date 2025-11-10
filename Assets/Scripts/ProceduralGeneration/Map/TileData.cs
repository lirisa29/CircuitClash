public enum TileType
{
    None,
    Edge,
    Base,
    Block1,
    Block2,
    Path
}

public class TileData
{
    public TileType type;
    public int stackHeight;
    public bool isWalkable = true;

    public TileData(TileType type, int stackHeight = 0)
    {
        this.type = type;
        this.stackHeight = stackHeight;
        this.isWalkable = type != TileType.Edge;
    }
}
