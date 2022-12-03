using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ConvertIdsToCircularPixels : MonoBehaviour
{
    // [SerializeField] Material material;
    // // void Start()
    // {
    //     int width = colormap.width;
    //     int height = colormap.height;
    //     Vector4[] pixels = new Vector4[width * height];
    //     for (int y = 0; y < height; y++)
    //     {
    //         for (int x = 0; x < width; x++)
    //         {
    //             Color c = colormap.GetPixel(x, y);
    //             pixels[y+width*x] = new Vector4(c.r, c.g, c.b, c.a);
    //         }
    //     }
    //     int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector4));
    //     buffer = new ComputeBuffer (width * height, stride, ComputeBufferType.Default);
    //     buffer.SetData (pixels);
    //     material.SetBuffer ("pixels", buffer);
    //     material.SetInt("resolution", width);
    //     GetComponent<Renderer>().material = material;
    // }
 
}

