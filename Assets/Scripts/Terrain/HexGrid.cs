using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[ExecuteInEditMode]
public class HexGrid : MonoBehaviour
{
    public static HexGrid Singleton { get; private set; }

    public const float OUTER_RADIUS = 1f;

    public static readonly float INNER_RADIUS = OUTER_RADIUS * Mathf.Sqrt(3) / 2f;

    public const float ELEVATION_STEP = 0.1f;

    public const int CHUNK_SIZE_X = 5;
    public const int CHUNK_SIZE_Z = 5;



    private int cellCountX;
    private int cellCountZ;

    [Header("Structural Variables")]
    [Range(1, 20)]
    public int chunkCountX = 4;

    [Range(1, 20)]
    public int chunkCountZ = 3;

    [SerializeField]
    private Transform chunkPrefab = default;

    [SerializeField]
    private HexCell cellPrefab = default;

    private GameObject playerBase = default;

    [Header("Terrain Data")]
    private Transform[] chunks = default;

    [SerializeField]
    private Vector2[] chunkBuildable = default;

    [HideInInspector]
    public HexCell[] cells;

    [ReadOnly, SerializeField]
    private HexCell[] _spawnableEdgeCells = default;

    [Header("Scenery Variables")]
    [SerializeField]
    private GameObject[] occupyingSceneryObjects = default;

    [SerializeField]
    private GameObject[] nonOccupyingSceneryObjects = default;

    [SerializeField]
    private bool mountainBorder = default;

    [SerializeField]
    private bool treeBorder = default;

    [Header("Noise")]
    [SerializeField]
    private Texture2D noise = default;

    [Header("Noise Scale")]
    [SerializeField]
    private float noiseScale = 0.003f;

    // Custom inspector variables
    [Header("Cell Variables")]
    [SerializeField]
    private HexCellType[] cellTypes = default;

    [SerializeField]
    private HexCellType[] buildableCellTypes = default;

    private HexCellType tallestCellType = default;

    [Header("Savekey")]
    [SerializeField]
    private GameObject[] towerPrefabs = default;

    private IDictionary<string, GameObject> nameToGameObject;
    private IDictionary<GameObject, string> gameObjectToName;

    public HexCell[] SpawnableEdgeCells { get => _spawnableEdgeCells; private set => _spawnableEdgeCells = value; }

    void Awake()
    {
        #region Singleton boilerplate

        if (Singleton != null)
        {
            if (Singleton != this)
            {
                Debug.LogWarning($"There's more than one {Singleton.GetType()} in the scene!");
                Destroy(gameObject);
            }

            return;
        }

        Singleton = this;

        #endregion Singleton boilerplate

        playerBase = GameObject.FindGameObjectWithTag("Base");

        // Code from https://stackoverflow.com/a/3188804
        tallestCellType = cellTypes.Aggregate((t1, t2) => t1.elevation > t2.elevation ? t1 : t2);

        // Calculate borders for the terrain
        cellCountX = chunkCountX * CHUNK_SIZE_X;
        cellCountZ = chunkCountZ * CHUNK_SIZE_Z;

        SpawnableEdgeCells = GetSpawnableEdgeCells();

        nameToGameObject = new Dictionary<string, GameObject>();
        gameObjectToName = new Dictionary<GameObject, string>();

        IEnumerable<GameObject> allGameObjectArrays = towerPrefabs.Concat(occupyingSceneryObjects).Concat(nonOccupyingSceneryObjects);
        foreach (GameObject obj in allGameObjectArrays)
        {
            string gameObjectName = $"{obj.name}(Clone)";
            nameToGameObject.Add(gameObjectName, obj);
            gameObjectToName.Add(obj, gameObjectName);
        }
    }

    /// <summary>
    /// Rebuild the terrain from scratch
    /// </summary>
    public void RebuildTerrain()
    {
        // Calculate borders
        cellCountX = chunkCountX * CHUNK_SIZE_X;
        cellCountZ = chunkCountZ * CHUNK_SIZE_Z;

        // Create the terrain
        CreateChunks();
        CreateCells();

        PlacePlayerBase();

        PlaceSceneryObjects();
    }

    private void PlacePlayerBase()
    {
        // Place the base in the middle of the terrain
        Vector3 firstCellPos = cells[0].transform.position;
        Vector3 lastCellPos = cells[cells.Length - 1].transform.position;
        Vector3 basePos = (lastCellPos - firstCellPos) / 2f;
        HexCell centerCell = GetCell(basePos);

        playerBase.transform.position = centerCell.transform.position;
        centerCell.OccupyingObject = playerBase;
    }

    // Create the chunks to better organize the cell data
    private void CreateChunks()
    {
        // Remove previous chunks
        while (transform.childCount > 0)
            DestroyImmediate(transform.GetChild(0).gameObject);

        chunks = new Transform[chunkCountX * chunkCountZ];

        for (int z = 0, i = 0; z < chunkCountZ; z++)
        {
            for (int x = 0; x < chunkCountX; x++)
            {
                Transform chunk = chunks[i++] = Instantiate(chunkPrefab);
                chunk.transform.SetParent(transform);
                chunk.name = $"Chunk ({x}, {z})";
                chunk.transform.position -= new Vector3(
                    INNER_RADIUS * cellCountX,
                    0,
                    OUTER_RADIUS * cellCountZ * 1.5f / 2f
                );
            }
        }
    }

    // Creates an array of all cells and fills it out with individual cells
    private void CreateCells()
    {
        cells = new HexCell[cellCountZ * cellCountX];

        for (int z = 0, i = 0; z < cellCountZ; z++)
        {
            for (int x = 0; x < cellCountX; x++)
                CreateCell(x, z, i++);
        }
    }

    // Returns the HexCell at a given position
    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        position -= cells[0].transform.position;
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;

        return cells[index];
    }

    private void CreateCell(int x, int z, int i)
    {
        // Determine the position of the cell
        Vector3 position;
        position.x = (x + z * 0.5f - z / 2) * (INNER_RADIUS * 2f);
        position.y = 0f;
        position.z = z * (OUTER_RADIUS * 1.5f);

        // Instantiate and set up the cell
        HexCell cell = cells[i] = Instantiate(cellPrefab);
        cell.transform.localPosition = position;

        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);

        // Assign the neighbors of the cell
        if (x > 0)
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);

        if (z > 0)
        {
            if ((z & 1) == 0)
            {
                cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
                if (x > 0)
                    cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
            } else
            {
                cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
                if (x < cellCountX - 1)
                    cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
            }
        }

        // Add cell to correlating chunk
        int chunkX = x / CHUNK_SIZE_X;
        int chunkZ = z / CHUNK_SIZE_Z;

        cell.transform.SetParent(chunks[chunkX + chunkZ * chunkCountX]);

        //No buildable chunk set, assumes every chunk should be buildable
        if (chunkBuildable.Length == 0) {
            SetTypeForCell(cell);
            return;
        }

        foreach (Vector2 chunk in chunkBuildable)
        {
            if (chunk.x == chunkX && chunk.y == chunkZ)
            {
                SetTypeForCell(cell, true);
                return;
            }
        }

        SetTypeForCell(cell, false);
        
    }

    /// <summary>
    /// Adjust a cells transform and material information based off the elevation integer.
    /// </summary>
    /// <param name="cell">The target cell</param>
    private void SetTypeForCell(HexCell cell)
    {
        float noiseValue = SampleNoise(cell.transform.localPosition, noise).y; // will be between 0.0 and 1.0 (both inclusive)
        int cellTypeIndex = Mathf.FloorToInt(noiseValue * cellTypes.Length * 0.99f); // will be between 0 and the last index of `cellTypes`
        cell.CellType = cellTypes[cellTypeIndex];
    }

    /// <summary>
    /// Adjust a cells transform and material information based off the elevation integer and if the chunk is allowed to be built on.
    /// </summary>
    /// <param name="cell">The target cell</param>
    private void SetTypeForCell(HexCell cell, bool isBuildable)
    {
        float noiseValue = HexMetrics.SampleNoise(cell.transform.localPosition, noise).y; // will be between 0.0 and 1.0 (both inclusive)

        if (!isBuildable)
        {
            int cellTypeIndex = Mathf.FloorToInt(noiseValue * cellTypes.Length * 0.99f); // will be between 0 and the last index of `cellTypes`
            cell.CellType = cellTypes[cellTypeIndex];
            return;
        }

        int buildableCellTypeIndex = Mathf.FloorToInt(noiseValue * buildableCellTypes.Length * 0.99f); // will be between 0 and the last index of `BuildableCellTypes`
        cell.CellType = cellTypes[buildableCellTypeIndex];

    }

    /// <returns>
    /// An array containing the cells on the edge of the map (not including the "mountain" edge) that enemies can spawn on.
    /// </returns>
    public HexCell[] GetSpawnableEdgeCells()
    {
        List<HexCell> edgeCells = new List<HexCell>();
        // Start at the corner that is the farthest towards -X and -Z (not including the "mountain" cells)
        Vector2Int currentPos = new Vector2Int(1, 1);

        void SetEdgeCellsAlongEdge(int outerEdgeCellCount, Vector2Int edgeDir)
        {
            for (int _ = 0; _ < outerEdgeCellCount - 3; _++)
            {
                HexCell cell = cells[currentPos.x + currentPos.y * cellCountX];
                // Don't add the "mountain" cells, as enemies shouldn't spawn on them
                if (cell.CellType.elevation < tallestCellType.elevation)
                    edgeCells.Add(cell);
                currentPos += edgeDir;
            }
        }

        // Traverse the cells along the -Z edge
        SetEdgeCellsAlongEdge(cellCountX, new Vector2Int(1, 0));
        // Traverse the cells along the +X edge
        SetEdgeCellsAlongEdge(cellCountX, new Vector2Int(0, 1));
        // Traverse the cells along the +Z edge
        SetEdgeCellsAlongEdge(cellCountX, new Vector2Int(-1, 0));
        // Traverse the cells along the -X edge
        SetEdgeCellsAlongEdge(cellCountX, new Vector2Int(0, -1));

        return edgeCells.ToArray();
    }

    /// <summary>
    /// Creates the map decorations
    /// </summary>
    private void PlaceSceneryObjects()
    {
        if (occupyingSceneryObjects.Length == 0)
        {
            Debug.LogError("No scenery objects have been assigned!");
            return;
        }

        PlaceSceneryOnMapEdge();
        PlaceSceneryOnPlayArea(occupyingSceneryObjects, true);
        PlaceSceneryOnPlayArea(nonOccupyingSceneryObjects, false);
    }

    private void PlaceSceneryOnMapEdge()
    {
        // Adding a wall around the map
        foreach (HexCell cell in SpawnableEdgeCells)
        {
            if (mountainBorder)
                cell.CellType = tallestCellType;

            if (treeBorder)
            {
                GameObject sceneryObj = cell.InstantiatePrefabOnCell(occupyingSceneryObjects[0]);
                float yRotation = Random.Range(0f, 360f);
                sceneryObj.transform.Rotate(0, yRotation, 0);
            }
        }
    }

    private void PlaceSceneryOnPlayArea(GameObject[] sceneryObjects, bool occupying)
    {
        foreach (HexCell cell in cells)
        {
            float elevation = cell.CellType.elevation;
            if (Random.Range(0, 100) > 10
                || elevation < 0
                || elevation >= tallestCellType.elevation
                || cell.IsOccupied)
                continue;

            if (chunkBuildable.Length > 0)
            {
                int chunkX = cell.coordinates.X / HexMetrics.CHUNK_SIZE_X;
                int chunkZ = cell.coordinates.Z / HexMetrics.CHUNK_SIZE_Z;
                foreach (Vector2 chunk in chunkBuildable)
                {
                    if (chunk.x == chunkX && chunk.y == chunkZ)
                    {
                        if (occupying){ continue; }

                        int sceneryIndexBuildable = Random.Range(0, sceneryObjects.Length);
                        GameObject sceneryObjectBuildable = cell.InstantiatePrefabOnCell(sceneryObjects[sceneryIndexBuildable]);
                        float yRotationBuildable = Random.Range(0f, 360f);
                        sceneryObjectBuildable.transform.Rotate(0, yRotationBuildable, 0);
                        cell.OccupyingObject = null;
                        continue;
                    }
                }
            }

            int sceneryIndex = Random.Range(0, sceneryObjects.Length);
            GameObject sceneryObject = cell.InstantiatePrefabOnCell(sceneryObjects[sceneryIndex]);
            float yRotation = Random.Range(0f, 360f);
            sceneryObject.transform.Rotate(0, yRotation, 0);

            if (!occupying)
                cell.OccupyingObject = null;
        }
    }

    private Vector4 SampleNoise(Vector3 position, Texture2D noiseSource)
    {
        return noiseSource.GetPixelBilinear(
            position.x * noiseScale,
            position.z * noiseScale
        );
    }

    public void SaveTerrain()
    {
        SaveSystem.SaveTerrain(this);
    }

    public void LoadTerrain()
    {
        TerrainData data = SaveSystem.LoadTerrain();
        if (data == null)
            return;

        chunkCountX = data.xChunks;
        chunkCountZ = data.zChunks;

        // Create a basic terrain
        CreateChunks();
        CreateCells();

        PlacePlayerBase();

        // Update all cells according to stored data
        for (int i = 0; i < cells.Length; i++)
        {
            HexCellData hexCellData = data.hexCells[i];
            cells[i].CellType = hexCellData.cellType;

            if (hexCellData.occupier == "null" || !nameToGameObject.ContainsKey(hexCellData.occupier))
                continue;

            GameObject sceneryObject = cells[i].InstantiatePrefabOnCell(nameToGameObject[hexCellData.occupier]);
            if (sceneryObject.GetComponent<TowerLogic>() is TowerLogic towerLogic)
            {
                Quaternion rotation = towerLogic.RotationAxis.rotation;
                towerLogic.RotationAxis.rotation = rotation * Quaternion.Euler(0f, hexCellData.occupierRotation, 0f);
            } else
            {
                sceneryObject.transform.rotation = Quaternion.Euler(0f, hexCellData.occupierRotation, 0f);
            }
        }
    }
}
