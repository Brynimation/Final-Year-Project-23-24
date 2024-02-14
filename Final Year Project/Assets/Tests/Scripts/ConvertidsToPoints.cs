using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConvertidsToPoints : MonoBehaviour
{
    
    [SerializeField] GameObject quadToRenderTo;
    [SerializeField] int numPoints;

    ComputeBuffer indexBuffer; // we'll fill this compute buffer with many ids.
    private int stride = sizeof(int);
    int[] indices;

    public void  fillBuffer()
    {
    
        indices = new int[numPoints];

        for(int i = 0; i < numPoints; i++){
            indices[i] = i;
        }
    }
}
