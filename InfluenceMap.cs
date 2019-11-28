using UnityEngine;

public class InfluenceMap
{
    public MapType Type;
    public LayerMask Mask;
    public float DefaultValue;
    public float[][] Grid;
    public Vector3[][] GridPosition; 

    public InfluenceMap(MapType type, BoundingBox box, float minValue,
        float maxValue, float defaultValue)
    {
        Type = type;
        DefaultValue = defaultValue;
        Grid = new float[box.ScaleX][];
        GridPosition = new Vector3[box.ScaleX][];

        for (int i = 0; i < box.ScaleX; i++)
        {
            Grid[i] = new float[box.ScaleX];
            GridPosition[i] = new Vector3[box.ScaleZ];
        }

        for (int x = 0; x < box.ScaleX; x++)
        {
            for (int z = 0; z < box.ScaleZ; z++)
            {
                Grid[x][z] = DefaultValue;

                if (type == MapType.Navigation)
                    CalculateNavData(x, z);
            }
        }
    }
    
    public InfluenceMap(MapType type, BoundingBox box, float minValue,
        float maxValue, float defaultValue, LayerMask mask)
    {
        Type = type;
        DefaultValue = defaultValue;
        Mask = mask;
        Grid = new float[box.ScaleX][];
        GridPosition = new Vector3[box.ScaleX][];

        for (int i = 0; i < box.ScaleX; i++)
        {
            Grid[i] = new float[box.ScaleX];
            GridPosition[i] = new Vector3[box.ScaleZ];
        }

        for (int x = 0; x < box.ScaleX; x++)
        {
            for (int z = 0; z < box.ScaleZ; z++)
            {
                Grid[x][z] = DefaultValue;

                if (type == MapType.Navigation)
                    CalculateNavData(x, z);

            }
        }
    }

    private void CalculateNavData(int x, int z)
    {
        RaycastHit hit;
        Vector3 rayOrigin = new Vector3(InfluenceMapper.IM.Box.Min.x + x + 0.5f, InfluenceMapper.IM.Box.Max.y, InfluenceMapper.IM.Box.Min.z + z + 0.5f);
        if (Physics.BoxCast(rayOrigin, new Vector3(0.5f, 0.5f, 0.5f), Vector3.down, out hit, Quaternion.identity, InfluenceMapper.IM.Box.ScaleY, Mask))
        {
            RaycastHit centerHit;
                Physics.Raycast(rayOrigin, Vector3.down, out centerHit, Mathf.Infinity, Mask);
                GridPosition[x][z] = centerHit.point;
                Grid[x][z] = 1f;
            
        }
    }

    public float[][] Multiply(float[][] b)
    {
        //Here we multiple our values of the grid in our class with b sent in.
        float[][] newGrid = new float[InfluenceMapper.IM.Box.ScaleX][];
        for (int x = 0; x < InfluenceMapper.IM.Box.ScaleX; x++)
        {
            newGrid[x] = new float[InfluenceMapper.IM.Box.ScaleZ];
            for (int z = 0; z < InfluenceMapper.IM.Box.ScaleZ; z++)
            {
                newGrid[x][z] = this.Grid[x][z] * b[x][z];
            }
        }

        return newGrid;
    }

    public void Reset()
    {
        for (int x = 0; x < InfluenceMapper.IM.Box.ScaleX; x++)
        {
            for (int z = 0; z < InfluenceMapper.IM.Box.ScaleZ; z++)
            {
                Grid[x][z] = DefaultValue;
            }
        }
    }
}