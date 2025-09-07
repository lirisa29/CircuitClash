using System.Collections.Generic;
using UnityEngine;

public static class AStarPathFinder
{
    private class Node
    {
        public Vector2Int pos;
        public Node parent;
        public float g;
        public float h => Vector2Int.Distance(pos, target) * (1f + Random.Range(-0.15f, 0.15f));
        public float f => g + h;

        public Node(Vector2Int pos, Node parent, float g)
        {
            this.pos = pos;
            this.parent = parent;
            this.g = g;
        }
    }

    private static Vector2Int target;
    private static TileData[,] grid;
    private static int width, height;

    public static List<Vector2Int> FindPath(TileData[,] _grid, Vector2Int start, Vector2Int end)
    {
        grid = _grid;
        width = grid.GetLength(0);
        height = grid.GetLength(1);
        target = end;

        var openSet = new List<Node>();
        var closedSet = new HashSet<Vector2Int>();
        var startNode = new Node(start, null, 0);
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            // Get lowest f score
            Node current = openSet[0];
            foreach (var n in openSet)
                if (n.f < current.f) current = n;

            if (current.pos == target)
                return ReconstructPath(current);

            openSet.Remove(current);
            closedSet.Add(current.pos);

            foreach (Vector2Int neighbour in GetNeighbours(current.pos))
            {
                if (closedSet.Contains(neighbour)) continue;

                if (!IsWalkable(neighbour)) continue;

                float tentativeG = current.g + 1;

                Node existing = openSet.Find(n => n.pos == neighbour);
                if (existing == null)
                {
                    openSet.Add(new Node(neighbour, current, tentativeG));
                }
                else if (tentativeG < existing.g)
                {
                    existing.parent = current;
                    existing.g = tentativeG;
                }
            }
        }

        return null; // No path
    }

    private static List<Vector2Int> ReconstructPath(Node node)
    {
        var path = new List<Vector2Int>();
        while (node != null)
        {
            path.Add(node.pos);
            node = node.parent;
        }
        path.Reverse();
        return path;
    }

    private static List<Vector2Int> GetNeighbours(Vector2Int pos)
    {
        List<Vector2Int> neighbours = new();

        if (pos.x > 0) neighbours.Add(new Vector2Int(pos.x - 1, pos.y));
        if (pos.x < width - 1) neighbours.Add(new Vector2Int(pos.x + 1, pos.y));
        if (pos.y > 0) neighbours.Add(new Vector2Int(pos.x, pos.y - 1));
        if (pos.y < height - 1) neighbours.Add(new Vector2Int(pos.x, pos.y + 1));

        return neighbours;
    }

    private static bool IsWalkable(Vector2Int pos)
    {
        TileData tile = grid[pos.x, pos.y];
        return tile != null && tile.isWalkable;
    }
}
