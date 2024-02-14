using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UIElements;

public class Rectangle 
{
    Vector2 topLeft;
    Vector2 size;

    public bool Contains(Vector2 point) 
    {
        return !(point.x < topLeft.x || point.y < topLeft.y || (point.x > topLeft.x + size.x) || (point.y > topLeft.y + size.y));
    }

    public bool Contains(Rectangle rectangle) 
    {
        return (rectangle.topLeft.x >= topLeft.x) && (rectangle.topLeft.x + rectangle.size.x < topLeft.x + size.x)
            && (rectangle.topLeft.y >= topLeft.y) && (rectangle.topLeft.y + rectangle.size.y < topLeft.y + size.y);
    }
    public bool Overlaps(Rectangle rectangle) 
    {
        return (topLeft.x < rectangle.topLeft.x + rectangle.size.x && topLeft.x + size.x >= rectangle.topLeft.x
            && topLeft.y < rectangle.topLeft.y + rectangle.size.y && topLeft.y + size.y >= rectangle.topLeft.y);
    }
}
public class StaticQuadTree : MonoBehaviour
{
    [SerializeField] Material mat;
    [SerializeField] Mesh mesh;
    [SerializeField] int numInstances = 10000;
    ComputeBuffer positionsBuffer;
    MeshProperties[] meshProperties;
    Bounds bounds;
    ComputeBuffer argsBuffer;

    private struct MeshProperties
    {
        public Matrix4x4 mat;
    }

    void Start()
    {
        positionsBuffer = new ComputeBuffer(numInstances, System.Runtime.InteropServices.Marshal.SizeOf(typeof(MeshProperties)));
        argsBuffer = new ComputeBuffer(1,  5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(new uint[] { (uint)mesh.GetIndexCount(0), (uint)numInstances, (uint)mesh.GetIndexStart(0), (uint)mesh.GetBaseVertex(0), 0u });
        Debug.Log(mesh.GetIndexCount(0));
        meshProperties = new MeshProperties[numInstances];
        for (int i = 0; i < numInstances; i++) 
        {
            Vector3 pos = new Vector3(Random.Range(0, 100), Random.Range(0, 100), 10f);
            Quaternion rotation = Quaternion.Euler(Random.Range(-180, 180), Random.Range(-180, 180), Random.Range(-180, 180));
            Vector3 scale = Vector3.one;

            meshProperties[i].mat = Matrix4x4.TRS(pos, rotation, scale);
        }
        positionsBuffer.SetData(meshProperties);
        mat.SetBuffer("_Properties", positionsBuffer);
        bounds = new Bounds(transform.position, Vector3.one * 1000);
    }

    // Update is called once per frame
    void Update()
    {
        Graphics.DrawMeshInstancedIndirect(mesh, 0, mat, bounds, argsBuffer);
        if (Input.GetKeyDown(KeyCode.Q)) 
        {
            MeshProperties[] posData = new MeshProperties[numInstances];
            positionsBuffer.GetData(posData);
            foreach (var vec in posData) 
            {
                Debug.Log(vec.mat);
            }
        }
    }
    private void OnDisable()
    {
        if (positionsBuffer != null)
        {
            positionsBuffer.Release();
            positionsBuffer = null;
        }
        if (argsBuffer != null)
        {
            argsBuffer.Release();
            argsBuffer = null;
        }
    }
}
