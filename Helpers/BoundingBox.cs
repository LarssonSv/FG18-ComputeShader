using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoundingBox
{
    public Vector3 Min;
    public Vector3 Max;
    public int ScaleX;
    public int ScaleY;
    public int ScaleZ;
    
    
    public BoundingBox(Vector3 pos, int xScale, int yScale, int zScale)
    {
      Min = pos;
      Max = pos;
      
      //Expand
      Max.x += xScale;
      Max.y += yScale;
      Max.z += zScale;

      ScaleX = xScale;
      ScaleY = yScale;
      ScaleZ = zScale;
    }

    public float GetSize()
    {
        return ScaleX * ScaleZ;
    }
    
}
