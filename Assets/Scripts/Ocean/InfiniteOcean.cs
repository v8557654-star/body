using UnityEngine;

namespace SeaDrift.Ocean
{
    /// <summary>
    /// Бесконечный океан - тайлы океана следуют за игроком.
    /// Работает как в Sea of Thieves / Valheim.
    /// </summary>
    public class InfiniteOcean : MonoBehaviour
    {
        [Header("Настройки")]
        public Transform followTarget; // корабль
        public int gridSize = 5; // 5x5 тайлов
        public float tileSize = 100f;
        public Material oceanMaterial;
        public Mesh oceanMesh; // plane mesh

        private Transform[,] tiles;
        private Vector2Int currentCenterGrid;

        void Start()
        {
            if (followTarget == null)
            {
                var boat = FindObjectOfType<Boat.BoatController>();
                if (boat != null) followTarget = boat.transform;
            }

            if (oceanMesh == null)
            {
                oceanMesh = CreatePlaneMesh(tileSize, 50);
            }

            CreateTiles();
        }

        void Update()
        {
            if (followTarget == null) return;

            Vector2Int newCenter = WorldToGrid(followTarget.position);
            if (newCenter != currentCenterGrid)
            {
                currentCenterGrid = newCenter;
                UpdateTilePositions();
            }

            // Обновляем материал - передаем время и волны
            if (oceanMaterial != null && OceanManager.Instance != null)
            {
                oceanMaterial.SetFloat("_TimeScale", OceanManager.Instance.timeScale);
                // Если используешь шейдер с волнами - передай параметры
            }
        }

        void CreateTiles()
        {
            tiles = new Transform[gridSize, gridSize];
            currentCenterGrid = WorldToGrid(followTarget != null ? followTarget.position : Vector3.zero);

            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    GameObject tile = new GameObject($"OceanTile_{x}_{z}");
                    tile.transform.SetParent(transform);

                    var filter = tile.AddComponent<MeshFilter>();
                    filter.mesh = oceanMesh;

                    var renderer = tile.AddComponent<MeshRenderer>();
                    renderer.material = oceanMaterial;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;

                    tiles[x, z] = tile.transform;
                }
            }
            UpdateTilePositions();
        }

        void UpdateTilePositions()
        {
            int half = gridSize / 2;
            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    int gridX = currentCenterGrid.x + (x - half);
                    int gridZ = currentCenterGrid.y + (z - half);
                    Vector3 pos = new Vector3(gridX * tileSize, 0, gridZ * tileSize);
                    tiles[x, z].position = pos;
                }
            }
        }

        Vector2Int WorldToGrid(Vector3 worldPos)
        {
            int gx = Mathf.RoundToInt(worldPos.x / tileSize);
            int gz = Mathf.RoundToInt(worldPos.z / tileSize);
            return new Vector2Int(gx, gz);
        }

        Mesh CreatePlaneMesh(float size, int resolution)
        {
            Mesh mesh = new Mesh();
            mesh.name = "OceanTile";

            int vertCount = (resolution + 1) * (resolution + 1);
            Vector3[] vertices = new Vector3[vertCount];
            Vector2[] uv = new Vector2[vertCount];
            int[] triangles = new int[resolution * resolution * 6];

            float step = size / resolution;
            float half = size * 0.5f;

            int idx = 0;
            for (int z = 0; z <= resolution; z++)
            {
                for (int x = 0; x <= resolution; x++)
                {
                    vertices[idx] = new Vector3(x * step - half, 0, z * step - half);
                    uv[idx] = new Vector2((float)x / resolution, (float)z / resolution);
                    idx++;
                }
            }

            int tri = 0;
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int i = z * (resolution + 1) + x;
                    triangles[tri++] = i;
                    triangles[tri++] = i + resolution + 1;
                    triangles[tri++] = i + 1;
                    triangles[tri++] = i + 1;
                    triangles[tri++] = i + resolution + 1;
                    triangles[tri++] = i + resolution + 2;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
