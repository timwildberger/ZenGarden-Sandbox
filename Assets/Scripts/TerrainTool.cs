using System.Linq;


using UnityEngine;
using System;

public sealed class TerrainTool : MonoBehaviour
{
    

    private Camera cam;

    public int brushSize;
    private int brushWidth;
    private int brushHeight;

    private Terrain _targetTerrain;
    [Header("Height of the terrain: 0 - 100")]
    private float terrainHeight;
    private float _terrainHeight;

    public TerrainModificationAction modificationAction;
    public enum TerrainModificationAction
    {
        Flatten,
        FlattenAll,
        Sample,
        SampleAverage,
        Bresenham,
    }
    private float _sampledHeight;

    private void Start()
    {
        cam = Camera.main;
        cam.rect = new Rect(0, 0, 4000, 4000);
        _targetTerrain = Terrain.activeTerrain;

        brushWidth = brushSize;
        brushHeight = brushSize;

        terrainHeight = brushSize / 10;
        _terrainHeight = terrainHeight / 100;

        FlattenAllTerrain(_terrainHeight);
    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hit))
            {
                if (hit.transform.TryGetComponent(out Terrain terrain)) _targetTerrain = terrain;

                switch (modificationAction)
                {
                    case TerrainModificationAction.Flatten:

                        FlattenTerrain(hit.point, _sampledHeight, brushWidth, brushHeight);

                        break;

                    case TerrainModificationAction.FlattenAll:

                        FlattenAllTerrain(_terrainHeight);

                        break;

                    case TerrainModificationAction.Sample:

                        _sampledHeight = SampleHeight(hit.point);

                        break;

                    case TerrainModificationAction.SampleAverage:

                        _sampledHeight = SampleAverageHeight(hit.point, brushWidth, brushHeight);

                        break;
                    case TerrainModificationAction.Bresenham:

                        Bresenhams(hit.point, brushWidth, brushHeight);

                        break;
                }
            }
        }
    }

    private TerrainData GetTerrainData() => _targetTerrain.terrainData;

    private int GetHeightmapResolution() => GetTerrainData().heightmapResolution;

    private Vector3 GetTerrainSize() => GetTerrainData().size;

    /// <summary>
    /// Translates worldPosition to position in heightmap.
    /// Returns Vector3 heightmap position.
    /// </summary>
    /// <param name="worldPosition"></param>
    /// <returns></returns>
    public Vector3 WorldToTerrainPosition(Vector3 worldPosition)
    {
        var terrainPosition = worldPosition - _targetTerrain.GetPosition();

        var terrainSize = GetTerrainSize();

        var heightmapResolution = GetHeightmapResolution();

        terrainPosition = new Vector3(terrainPosition.x / terrainSize.x, terrainPosition.y / terrainSize.y, terrainPosition.z / terrainSize.z);

        return new Vector3(terrainPosition.x * heightmapResolution, 0, terrainPosition.z * heightmapResolution);
    }

    /// <summary>
    /// Returns bottom left coordinate of brush area.
    /// Coordinate system is the heightmap.
    /// 
    /// </summary>
    /// <param name="worldPosition"></param>
    /// <param name="brushWidth"></param>
    /// <param name="brushHeight"></param>
    /// <returns></returns>

    public Vector2Int GetBrushPosition(Vector3 worldPosition, int brushWidth, int brushHeight)
    {
        var terrainPosition = WorldToTerrainPosition(worldPosition);

        var heightmapResolution = GetHeightmapResolution();

        return new Vector2Int((int)Mathf.Clamp(terrainPosition.x - brushWidth / 2.0f, 0.0f, heightmapResolution), (int)Mathf.Clamp(terrainPosition.z - brushHeight / 2.0f, 0.0f, heightmapResolution));
    }

    public Vector2Int GetSafeBrushSize(int brushX, int brushY, int brushWidth, int brushHeight)
    {
        var heightmapResolution = GetHeightmapResolution();

        while (heightmapResolution - (brushX + brushWidth) < 0) brushWidth--;

        while (heightmapResolution - (brushY + brushHeight) < 0) brushHeight--;

        return new Vector2Int(brushWidth, brushHeight);
    }

    public void FlattenTerrain(Vector3 worldPosition, float height, int brushWidth, int brushHeight)
    {
        var brushPosition = GetBrushPosition(worldPosition, brushWidth, brushHeight);

        var brushSize = GetSafeBrushSize(brushPosition.x, brushPosition.y, brushWidth, brushHeight);

        var terrainData = GetTerrainData();

        var heights = terrainData.GetHeights(brushPosition.x, brushPosition.y, brushSize.x, brushSize.y);

        for (var y = 0; y < brushSize.y; y++)
        {
            for (var x = 0; x < brushSize.x; x++)
            {
                heights[y, x] = height;
            }
        }

        terrainData.SetHeights(brushPosition.x, brushPosition.y, heights);
    }

    public void FlattenAllTerrain(float height)
    {

        var terrainData = GetTerrainData();
        var size = terrainData.heightmapResolution;
        // y is height

        var heights = terrainData.GetHeights(0, 0, size, size);

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                heights[y, x] = height;
            }
        }
        terrainData.SetHeights(0, 0, heights);

    }
    /// <summary>
    /// Calculates the height value on a positon in the heightmap.
    /// </summary>
    /// <param name="worldPosition"></param>
    /// <returns></returns>
    public float SampleHeight(Vector3 worldPosition)
    {
        var terrainPosition = WorldToTerrainPosition(worldPosition);
        //Debug.Log(GetTerrainData().GetInterpolatedHeight((int)terrainPosition.x, (int)terrainPosition.z));
        return GetTerrainData().GetInterpolatedHeight((int)terrainPosition.x, (int)terrainPosition.z);
    }

    public float SampleAverageHeight(Vector3 worldPosition, int brushWidth, int brushHeight)
    {
        var brushPosition = GetBrushPosition(worldPosition, brushWidth, brushHeight);

        var brushSize = GetSafeBrushSize(brushPosition.x, brushPosition.y, brushWidth, brushHeight);

        var heights2D = GetTerrainData().GetHeights(brushPosition.x, brushPosition.y, brushSize.x, brushSize.y);

        var heights = new float[heights2D.Length];

        var i = 0;

        for (int y = 0; y <= heights2D.GetUpperBound(0); y++)
        {
            for (int x = 0; x <= heights2D.GetUpperBound(1); x++)
            {
                heights[i++] = heights2D[y, x];
            }
        }

        return heights.Average();
    }

    private void Bresenhams(Vector3 worldPosition, int brushWidth, int brushHeight) {

        var brushPosition = GetBrushPosition(worldPosition, brushWidth, brushHeight);

        var brushSize = GetSafeBrushSize(brushPosition.x, brushPosition.y, brushWidth, brushHeight);

        var terrainData = GetTerrainData();

        var heights = terrainData.GetHeights(brushPosition.x, brushPosition.y, brushSize.x, brushSize.y);


        int r = brushSize.x / 2;
        Vector2 calc = new(0, 0);

        float rr = r * r;
        int x, y;
        float distanceStrength;

        Vector2 test_max = new Vector3(1, 1);
        float distanceStrength_base = EaseInOutCubic(test_max.magnitude / brushSize.x);

        if ((Input.GetAxis("Mouse X") != 0) || (Input.GetAxis("Mouse Y") != 0))
        {
            for (y = 0; y < r; y++)
            {
                for (x = 0; x < r; x++)
                {
                    if ((x * x) + (y * y) <= rr) //Check whether points are in circle
                    {
                        // Calculate Point
                        calc.Set(x, y);

                        distanceStrength = EaseInOutCubic(calc.magnitude / brushSize.x);
                        if(distanceStrength == 0)
                        {
                            distanceStrength = distanceStrength_base;
                        }



                        //distanceStrength = easeInOutBack((calc.magnitude) / brushSize.x);

                        // 0.5 is average height
                        // Repeat calculation für each quadrant of the standard circle
                        if (!(heights[r + y, r + x] <= distanceStrength))
                        {
                            heights[r + y, r + x] = distanceStrength;
                        }
                        if (!(heights[r + y, r - x] <= distanceStrength))
                        {
                            heights[r + y, r - x] = distanceStrength;
                        }
                        if (!(heights[r - y, r - x] <= distanceStrength))
                        {
                            heights[r - y, r - x] = distanceStrength;
                        }
                        if (!(heights[r - y, r + x] <= distanceStrength))
                        {
                            heights[r - y, r + x] = distanceStrength;
                        }
                    }
                }
            }
        terrainData.SetHeights(brushPosition.x, brushPosition.y, heights);
    }
}

    private float EaseInOutCubic(float t)
    {
            float sqt = t * t;
            return sqt / (2.0f * (sqt - t) + 1.0f);
    }


    public static float EaseInOutBack(float t)
    {
        if (t < 0.5) return EaseInBack(t * 2) / 2;
        return 1 - EaseInBack((1 - t) * 2) / 2;
    }

    public static float EaseInBack(float t)
    {
        float s = 1.70158f;
        return t * t * ((s + 1) * t - s);
    }


}