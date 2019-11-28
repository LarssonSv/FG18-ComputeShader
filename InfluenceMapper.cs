#pragma warning disable 0649
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public enum MapType
{
    Navigation,
    Hazard,
    Npc,
    Object
}

public class InfluenceMapper : MonoBehaviour
{
    [Header("Map Systems:")]
    [SerializeField]public HazardSystem HazardSystem;
    [SerializeField] public NpcSystem NpcSystem;
    [SerializeField] public ObjectSystem ObjectSystem;
    [SerializeField] public NavigationSystem NavigationSystem;
    
    [Header("GPU Acceleration:")]
    [SerializeField] private ComputeShader _compute;
    [SerializeField][Tooltip("Accelerate map multiplication using GPU")] private bool useGPU = false;

    [Header("Debug:")] 
    [SerializeField] private Mesh _debugMesh;
    
    //Compute Shader Data
    private ComputeBuffer _resultBuffer;
    private ComputeBuffer _mapBuffer;
    private int _mapMultipleKernel;
    private float[] allMaps;


    public readonly Dictionary<MapType, InfluenceMap> Maps = new Dictionary<MapType, InfluenceMap>();
    private readonly List<Vector3[][]> _drawPos = new List<Vector3[][]>();
    private readonly List<float[][]> _drawMaps = new List<float[][]>();

    public BoundingBox Box;
    public static InfluenceMapper IM;


    public void Awake()
    {
        IM = this;
        Init();
    }

    public void Init()
    {
        Box = new BoundingBox(Vector3.down, 50, 10, 50); //Todo: Make this take values from MapGenerator

        ObjectSystem.Init(this);
        HazardSystem.Init(this);
        NpcSystem.Init(this);
        NavigationSystem.Init(this);
        
        _drawMaps.Add(new float[Box.ScaleX][]);

        for (int i = 0; i < _drawMaps[0].Length; i++)
        {
            _drawMaps[0][i] = new float[IM.Box.ScaleZ];
        }

        ComputeShader();

        _drawPos.Add(GetMap(MapType.Navigation).GridPosition);
        
        Invoke("NpcTest", 1f);
    }

    private void NpcTest()
    {
        NpcSystem.AddNpc(new Vector2Int(10, 10));
        NpcSystem.SetPath(1, new Vector2Int(5, 5));
    }

    //Setup our compute shader
    private void ComputeShader()
    {
        //here we convert our jagged array to a normal array, to be able to compute it inside of the GPU
        int mapSize = IM.Box.ScaleX * IM.Box.ScaleZ; 
        allMaps = new float[mapSize * Maps.Count];

        int mapCount = 0;
        int xDimension = Maps[0].Grid.Length;
        int yDimension = Maps[0].Grid[0].Length; //This only works for squares and not retagulare 
        foreach (InfluenceMap map in Maps.Values)
        {
            for (int x = 0; x < xDimension; x++)
            {
                for (int y = 0; y < yDimension; y++)
                {
                    allMaps[mapSize * mapCount + xDimension * y + x] = map.Grid[x][y];
                }
            }

            mapCount++;
        }

        _mapMultipleKernel = _compute.FindKernel("MapsMultiply");
        
        _resultBuffer = new ComputeBuffer(mapSize, 4);
        _mapBuffer = new ComputeBuffer(mapSize * 4, 4);

        _compute.SetBuffer(_mapMultipleKernel, "Result", _resultBuffer);
        _compute.SetBuffer(_mapMultipleKernel, "Maps", _mapBuffer);

        _mapBuffer.SetData(allMaps);

        _compute.SetInt("sizeX", IM.Box.ScaleX);
        _compute.SetInt("mapCount", 4);
        _compute.SetInt("mapSize", mapSize);

        _compute.Dispatch(_mapMultipleKernel, IM.Box.ScaleX, 1, IM.Box.ScaleZ);

        //With all of our buffers bound, we can simply call this line of code to multiple our maps.
        AsyncGPUReadback.Request(_resultBuffer, CheckBuffer);
    }

    //Checkbuffers is called once the compute shader is done
    private void CheckBuffer(AsyncGPUReadbackRequest obj)
    {
        allMaps = obj.GetData<float>().ToArray();
        
        int mapCount = 0;

        foreach (InfluenceMap map in Maps.Values)
        {
            for (int x = 0; x < IM.Box.ScaleX; x++)
            {
                for (int y = 0; y < IM.Box.ScaleZ; y++)
                {
                    _drawMaps[0][x][y] = allMaps[x + y * IM.Box.ScaleX];
                    //mapSize * mapCount + xDimension / y + x

                }
            }

            mapCount++;
        }
    }

    private void OnDestroy()
    {
        _mapBuffer.Dispose();
        _resultBuffer.Dispose();
    }

    private void Update()
    {
        NavigationSystem.OnUpdate();
        HazardSystem.OnUpdate();
        ObjectSystem.OnUpdate();
        NpcSystem.OnUpdate();
        CalculateMap();
    }

    public void CalculateMap()
    {
        //This is the new call for our GPU combinder
        if(useGPU)
            AsyncGPUReadback.Request(_resultBuffer, CheckBuffer);
        else
        {
            //This is the old CPU based map combinder, with a multiple extenstion added to our map class.
            _drawMaps[0] = GetMap(MapType.Navigation)
                .Multiply(GetMap(MapType.Object).Multiply(GetMap(MapType.Hazard).Multiply(GetMap(MapType.Npc).Grid)));
        }

    }

    #region Debug

    private void OnDrawGizmos()
    {
//Draw Map
        Gizmos.color = new Color(0, 1, 0, 0.5F);
        foreach (float[][] drawMap in _drawMaps)
        {
            for (int x = 0; x < Box.ScaleX; x++)
            {
                for (int z = 0; z < Box.ScaleZ; z++)
                {
                    if (drawMap[x][z] > 0.1f)
                    {
                        Gizmos.color = new Color(0, 1, 0, drawMap[x][z] /2f);
                        Gizmos.DrawMesh(_debugMesh, _drawPos[0][x][z] + new Vector3(0, 0.1f, 0),
                            Quaternion.Euler(90, 0, 0));
                    }
                }
            }
        }

        if (Box == null)
            return;

//Draw Box
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(Box.Min, Box.Min + new Vector3(Box.ScaleX, 0f, 0f));
        Gizmos.DrawLine(Box.Min + new Vector3(Box.ScaleX, 0f, 0f),
            Box.Min + new Vector3(Box.ScaleX, 0f, Box.ScaleZ));
        Gizmos.DrawLine(Box.Min, Box.Min + new Vector3(0, 0, Box.ScaleZ));
        Gizmos.DrawLine(Box.Min + new Vector3(0, 0, Box.ScaleZ),
            Box.Min + new Vector3(Box.ScaleX, 0, Box.ScaleZ));
        Gizmos.DrawLine(Box.Max, Box.Max - new Vector3(Box.ScaleX, 0f, 0f));
        Gizmos.DrawLine(Box.Max - new Vector3(Box.ScaleX, 0f, 0f),
            Box.Max - new Vector3(Box.ScaleX, 0f, Box.ScaleZ));
        Gizmos.DrawLine(Box.Max, Box.Max - new Vector3(0, 0, Box.ScaleZ));
        Gizmos.DrawLine(Box.Max - new Vector3(0, 0, Box.ScaleZ),
            Box.Max - new Vector3(Box.ScaleX, 0, Box.ScaleZ));
        Gizmos.DrawLine(Box.Min, Box.Max - new Vector3(Box.ScaleX, 0, Box.ScaleZ));
        Gizmos.DrawLine(Box.Min + new Vector3(Box.ScaleX, 0, 0), Box.Max - new Vector3(0, 0, Box.ScaleZ));
        Gizmos.DrawLine(Box.Min + new Vector3(0, 0, Box.ScaleZ), Box.Max - new Vector3(Box.ScaleX, 0, 0));
        Gizmos.DrawLine(Box.Min + new Vector3(Box.ScaleX, 0, Box.ScaleZ), Box.Max);
    }
    
    private void DrawRayCasts()
    {
        for (int x = 0; x < Box.ScaleX; x++)
        {
            for (int z = 0; z < Box.ScaleZ; z++)
            {
                Vector3 rayOrigin = new Vector3(Box.Min.x + x + 0.5f, Box.Max.y, Box.Min.z + z + 0.5f);

                ExtDebug.DrawBoxCastBox(rayOrigin, new Vector3(0.5f, 0.5f, 0.5f), Quaternion.identity, Vector3.down,
                    10f,
                    Color.blue);
            }
        }
    }

    #endregion

    #region DrawToMap

    public void DrawCircleOnMap(MapType type, int x, int z, int radius, float value)
    {
        InfluenceMap map = GetMap(type);
        for (int i = radius;
            i >= 0;
            i--)
        {
            float angle = 0;
            while (angle < 360)
            {
                float newX = x + i * Mathf.Cos(angle);
                float newZ = z + i * Mathf.Sin(angle);

                if (newX > 0 && newX < Box.ScaleX - 1 && newZ > 0 && newZ < Box.ScaleZ - 1)
                    map.Grid[Mathf.RoundToInt(newX)][Mathf.RoundToInt(newZ)] = value * i;
                angle += 10f;
            }
        }
    }

    public void DrawOnMap(MapType type, Vector2Int pos, float value)
    {
        GetMap(type).Grid[pos.x][pos.y] = value;
    }

    #endregion

    #region Helpers

    public float[][] GetDrawMap()
    {
        return _drawMaps[0];
    }
    
    public Vector2Int WorldToGrid(Vector3 pos)
    {
        Vector3 localPos = pos - Box.Min;
        return new Vector2Int(Mathf.FloorToInt(localPos.x), Mathf.FloorToInt(localPos.z));
    }

    public Vector3 GridToWorld(Vector2Int pos)
    {
        return _drawPos[0][pos.x][pos.y];
    }

    public void ResetMap(MapType type)
    {
        GetMap(type).Reset();
    }

    private InfluenceMap GetMap(MapType type)
    {
        if (Maps.ContainsKey(type))
            return Maps[type];


        Debug.Log("Could not find map!");
        return null;
    }

    #endregion

}