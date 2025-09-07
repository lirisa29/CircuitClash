using System.Collections.Generic;
using UnityEngine;

// -------------------- WFC Helper Classes -------------------- //
public class Cell
{
    public List<TileType> Possible;
    public bool IsCollapsed => Possible.Count == 1;
    public TileType CollapsedType => IsCollapsed ? Possible[0] : TileType.None;

    public Cell(List<TileType> possibilities)
    {
        Possible = possibilities;
    }

    public void CollapseTo(TileType type)
    {
        Possible.Clear();
        Possible.Add(type);
    }

    // Restrict possibilities of neighbour given this cell’s collapsed type
    public bool RestrictPossibilities(TileType neighbourOf, Vector2Int dir)
    {
        // Simple ruleset: Block1 next to Block1/Block2/Base, Block2 next to Block1/Block2/Base, Base can touch anything.
        // Extendable if stricter rules needed.
        List<TileType> allowed = new List<TileType>();
        switch (neighbourOf)
        {
            case TileType.Block1:
            case TileType.Block2:
                allowed.Add(TileType.Block1);
                allowed.Add(TileType.Block2);
                allowed.Add(TileType.Base);
                allowed.Add(TileType.None);
                break;
            case TileType.Base:
                allowed.Add(TileType.Block1);
                allowed.Add(TileType.Block2);
                allowed.Add(TileType.Base);
                allowed.Add(TileType.None);
                break;
            case TileType.Edge:
                allowed.Add(TileType.Base);
                allowed.Add(TileType.None);
                break;
            case TileType.None:
                allowed.Add(TileType.Base);
                allowed.Add(TileType.Block1);
                allowed.Add(TileType.Block2);
                allowed.Add(TileType.None);
                break;
        }

        int before = Possible.Count;
        Possible.RemoveAll(t => !allowed.Contains(t));
        return Possible.Count != before;
    }
}
