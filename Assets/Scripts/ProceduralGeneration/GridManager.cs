using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject edgePrefab;
    public GameObject basePrefab;
    public GameObject block1Prefab;
    public GameObject block2Prefab;
    public GameObject pathPrefab;

    [Header("Grid Settings")]
    public int gridWidth = 10;
    public int gridDepth = 10;
    public float horizontalSpacing = 2f;
    public float depthSpacing = 2f;
    public float verticalSpacing = 1f;

    [Header("Stack Settings")]
    public int minStackHeight = 1;
    public int maxStackHeight = 4;
    [Range(0f, 1f)] public float stackSpawnChance = 0.7f;
    
    [Header("Defender Settings")]
    public int numberOfDefenderSpots = 10;
    public GameObject defenderSpotPrefab;
    public List<Vector2Int> defenderSpots = new List<Vector2Int>();

    public TileData[,] grid;
    
    public List<List<Vector2Int>> enemyPaths = new List<List<Vector2Int>>();

    void Start()
    {
        GenerateTileDataGrid();   // Step 1
        CarvePathWithAStar();     // Step 2
        InstantiateFromGrid();    // Step 3
        GenerateDefenderSpots(); // Step 4
    }

    // -------------------- Step 1: Generate Grid with WFC -------------------- //
    void GenerateTileDataGrid()
    {
        grid = new TileData[gridWidth, gridDepth];

        // Initialise wave function: every non-edge cell starts with all possibilities
        List<TileType> allTypes = new List<TileType> { TileType.Base, TileType.Block1, TileType.Block2, TileType.None };
        Cell[,] wave = new Cell[gridWidth, gridDepth];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridDepth; z++)
            {
                if (IsEdge(x, z))
                {
                    wave[x, z] = new Cell(new List<TileType> { TileType.Edge });
                }
                else
                {
                    wave[x, z] = new Cell(new List<TileType>(allTypes));
                }
            }
        }

        // Collapse process
        while (true)
        {
            // Find lowest entropy cell
            Cell lowest = null;
            int lowX = -1, lowZ = -1;
            int minEntropy = int.MaxValue;

            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridDepth; z++)
                {
                    if (wave[x, z].IsCollapsed) continue;
                    int entropy = wave[x, z].Possible.Count;
                    if (entropy < minEntropy)
                    {
                        minEntropy = entropy;
                        lowest = wave[x, z];
                        lowX = x; lowZ = z;
                    }
                }
            }

            // Finished collapsing
            if (lowest == null) break;

            // Collapse this cell
            TileType chosen = lowest.Possible[Random.Range(0, lowest.Possible.Count)];
            lowest.CollapseTo(chosen);

            // Propagate constraints
            PropagateConstraints(wave, lowX, lowZ);
        }

        // Convert wave function to TileData
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridDepth; z++)
            {
                TileType finalType = wave[x, z].CollapsedType;
                if (finalType == TileType.Block1 || finalType == TileType.Block2)
                {
                    int stackHeight = Random.value < stackSpawnChance ? Random.Range(minStackHeight, maxStackHeight + 1) : 1;
                    grid[x, z] = new TileData(finalType, stackHeight);
                }
                else
                {
                    grid[x, z] = new TileData(finalType);
                }
            }
        }
    }

    void PropagateConstraints(Cell[,] wave, int cx, int cz)
    {
        Queue<Vector2Int> toProcess = new Queue<Vector2Int>();
        toProcess.Enqueue(new Vector2Int(cx, cz));

        while (toProcess.Count > 0)
        {
            Vector2Int pos = toProcess.Dequeue();
            Cell cell = wave[pos.x, pos.y];

            // For each neighbour, prune invalid possibilities
            foreach (Vector2Int dir in Directions)
            {
                int nx = pos.x + dir.x;
                int nz = pos.y + dir.y;

                if (nx < 0 || nz < 0 || nx >= gridWidth || nz >= gridDepth) continue;

                Cell neighbour = wave[nx, nz];
                if (neighbour.IsCollapsed) continue;

                bool changed = neighbour.RestrictPossibilities(cell.CollapsedType, dir);
                if (changed) toProcess.Enqueue(new Vector2Int(nx, nz));
            }
        }
    }

    bool IsEdge(int x, int z)
    {
        return x == 0 || z == 0 || x == gridWidth - 1 || z == gridDepth - 1;
    }

    // Directions for propagation
    static readonly Vector2Int[] Directions =
    {
        new Vector2Int(1,0), new Vector2Int(-1,0),
        new Vector2Int(0,1), new Vector2Int(0,-1)
    };

    // -------------------- Step 2: A* Carve Paths -------------------- //
    void CarvePathWithAStar()
    {
        Vector2Int center = new Vector2Int(gridWidth / 2, gridDepth / 2);
        HashSet<string> chosenSides = new HashSet<string>();

        int pathsToGenerate = 3;
        int attempts = 0;

        while (chosenSides.Count < pathsToGenerate && attempts < 20)
        {
            string side = GetRandomSide(chosenSides);
            chosenSides.Add(side);

            Vector2Int start = GetRandomPointOnSide(side);
            List<Vector2Int> path = AStarPathFinder.FindPath(grid, start, center);

            if (path != null)
            {
                enemyPaths.Add(path);
                
                foreach (Vector2Int pos in path)
                {
                    grid[pos.x, pos.y] = new TileData(TileType.Path)
                    {
                        isWalkable = true,
                        stackHeight = 0
                    };
                }
            }

            attempts++;
        }
    }

    string GetRandomSide(HashSet<string> usedSides)
    {
        string[] allSides = { "top", "bottom", "left", "right" };
        List<string> available = new List<string>();

        foreach (string s in allSides)
            if (!usedSides.Contains(s)) available.Add(s);

        return available[Random.Range(0, available.Count)];
    }

    Vector2Int GetRandomPointOnSide(string side)
    {
        switch (side)
        {
            case "top": return new Vector2Int(Random.Range(1, gridWidth - 1), gridDepth - 1);
            case "bottom": return new Vector2Int(Random.Range(1, gridWidth - 1), 0);
            case "left": return new Vector2Int(0, Random.Range(1, gridDepth - 1));
            case "right": return new Vector2Int(gridWidth - 1, Random.Range(1, gridDepth - 1));
        }
        return Vector2Int.zero;
    }
    
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * horizontalSpacing, 1.5f, gridPos.y * depthSpacing);
    }

    // -------------------- Step 3: Instantiate -------------------- //
    void InstantiateFromGrid()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridDepth; z++)
            {
                Vector3 basePos = new Vector3(x * horizontalSpacing, 0, z * depthSpacing);
                TileData tile = grid[x, z];
                if (tile == null || tile.type == TileType.None) continue;

                switch (tile.type)
                {
                    case TileType.Edge:
                        Instantiate(edgePrefab, basePos, Quaternion.identity, transform);
                        break;
                    case TileType.Base:
                        Instantiate(basePrefab, basePos, Quaternion.identity, transform);
                        break;
                    case TileType.Block1:
                    case TileType.Block2:
                        Instantiate(basePrefab, basePos, Quaternion.identity, transform);
                        GameObject blockPrefab = tile.type == TileType.Block1 ? block1Prefab : block2Prefab;
                        for (int h = 2; h < tile.stackHeight; h++)
                        {
                            Vector3 stackPos = basePos + new Vector3(0, h * verticalSpacing, 0);
                            Instantiate(blockPrefab, stackPos, Quaternion.identity, transform);
                        }
                        break;
                    case TileType.Path:
                        Instantiate(pathPrefab, basePos, Quaternion.identity, transform);
                        break;
                }
            }
        }
    }
    
    // -------------------- Step 4: Place Defender Spots on Generated Terrain -------------------- //
    void GenerateDefenderSpots()
    {
        defenderSpots.Clear();

        List<Vector2Int> validTiles = new List<Vector2Int>();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridDepth; z++)
            {
                TileData tile = grid[x, z];
                if (tile == null || tile.type != TileType.Base) continue;

                // Check if adjacent to a Path tile
                bool nearPath = false;
                foreach (Vector2Int dir in Directions)
                {
                    int nx = x + dir.x;
                    int nz = z + dir.y;
                    if (nx < 0 || nz < 0 || nx >= gridWidth || nz >= gridDepth) continue;

                    TileData neighbour = grid[nx, nz];
                    if (neighbour != null && neighbour.type == TileType.Path)
                    {
                        nearPath = true;
                        break;
                    }
                }

                if (nearPath) validTiles.Add(new Vector2Int(x, z));
            }
        }
        
        // Get currently available defenders once (from GameManager)
        DefenderData[] defenders = GameManager.Instance.allDefenders;

        // Randomly select spots
        for (int i = 0; i < numberOfDefenderSpots && validTiles.Count > 0; i++)
        {
            int index = Random.Range(0, validTiles.Count);
            Vector2Int spot = validTiles[index];
            validTiles.RemoveAt(index);
            defenderSpots.Add(spot);

            if (defenderSpotPrefab != null)
            {
                Vector3 basePos = new Vector3(spot.x * horizontalSpacing, 0, spot.y * depthSpacing);
                Vector3 spotPos = basePos + new Vector3(0, 1.5f * verticalSpacing, 0);
                
                GameObject spotObj = Instantiate(defenderSpotPrefab, spotPos, Quaternion.identity, transform);
                
                DefenderSpot spotScript = spotObj.GetComponent<DefenderSpot>();
                
                if (spotScript != null)
                {
                    // Initialise the spot with grid coordinates + available defenders for the current wave
                    spotScript.Initialise(spot, defenders);
                }
                else
                {
                    Debug.LogWarning("DefenderSpot prefab is missing a DefenderSpot component.");
                }
            }
        }
    }
}
