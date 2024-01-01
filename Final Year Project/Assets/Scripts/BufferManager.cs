using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class BufferManager : MonoBehaviour
{
    private struct MeshProperties
    {
        public Matrix4x4 mat;
    }
    public int numPositions;
    public int xSize;
    public int ySize;
    public int zSize;
    public Color _EmissionColour;
    public Color _EmissionColour2;
    public ComputeShader positionCalculator;
    public Transform playerPosition;
    public int chunkSize;
    public int renderDistance;
    public float lodSwitchDist;

    int chunksVisibleInViewDist;

    public Shader shader;
    public Material material;
    ComputeBuffer positionsBuffer;
    ComputeBuffer argsBuffer;

    public Material material2;
    ComputeBuffer positionsBuffer2;
    ComputeBuffer argsBuffer2;

    ComputeBuffer debugPosBuffer;
    ComputeBuffer dispatchBuffer;
    ComputeBuffer chunksBuffer;
    int[] chunksVisible;

    ThreadIdentifier[] positions;
    int kernelIndex;
    int dispatchIndex;

    Bounds bounds;
    void Start()
    {
        //material = new Material(shader);
        //material2 = new Material(shader);
        chunksVisibleInViewDist = Mathf.RoundToInt(renderDistance / chunkSize);
        chunksVisible = new int[1] { chunksVisibleInViewDist };
        kernelIndex = positionCalculator.FindKernel("CSMain");

        positionsBuffer = new ComputeBuffer((int)Mathf.Pow(chunksVisibleInViewDist + 1, 3), System.Runtime.InteropServices.Marshal.SizeOf(typeof(MeshProperties)), ComputeBufferType.Append);
        argsBuffer = new ComputeBuffer(1, sizeof(uint) * 4, ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(new uint[] { (uint)1, (uint)Mathf.Pow(chunksVisibleInViewDist + 1, 3), 0u, 0u });

        positionsBuffer2 = new ComputeBuffer((int)Mathf.Pow(chunksVisibleInViewDist + 1, 3), System.Runtime.InteropServices.Marshal.SizeOf(typeof(MeshProperties)), ComputeBufferType.Append);
        argsBuffer2 = new ComputeBuffer(1, sizeof(uint) * 4, ComputeBufferType.IndirectArguments);
        argsBuffer2.SetData(new uint[] { (uint)1, (uint)Mathf.Pow(chunksVisibleInViewDist + 1, 3), 0u, 0u });

        chunksBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Structured);
        chunksBuffer.SetData(chunksVisible);
        debugPosBuffer = new ComputeBuffer((int)Mathf.Pow(chunksVisibleInViewDist + 1, 3), System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector3Int)), ComputeBufferType.Append);
        dispatchBuffer = new ComputeBuffer(3, sizeof(uint), ComputeBufferType.IndirectArguments);
        dispatchBuffer.SetData(new uint[3] { 1u, 1u, 1u });
        /*
        for(uint i = 0; i < numPositions; i++) 
        {
            Vector3 pos = new Vector3(UnityEngine.Random.Range(-xSize / 2, xSize / 2), UnityEngine.Random.Range(-ySize / 2, ySize / 2), UnityEngine.Random.Range(-zSize / 2, zSize / 2));
            Color col = Color.red;
            float rad = UnityEngine.Random.Range(0.5f, 4f);
            positions[i] = new ThreadIdentifier() {
                position = pos,
                colour = col,
                radius = rad,
                id = i
            };
            
        }*/
        positionCalculator.SetBuffer(kernelIndex, "_Properties", positionsBuffer);
        positionCalculator.SetBuffer(kernelIndex, "_Properties2", positionsBuffer2);
        positionCalculator.SetBuffer(kernelIndex, "_ChunksBuffer", chunksBuffer);
        positionCalculator.SetBuffer(kernelIndex, "_Positions", debugPosBuffer);
        positionCalculator.SetBuffer(kernelIndex, "_DispatchBuffer", dispatchBuffer);
        positionCalculator.SetInt("chunkSize", chunkSize);
        positionCalculator.SetInt("renderDistance", renderDistance);
        positionCalculator.SetFloat("lodSwitchDist", lodSwitchDist);
        positionCalculator.SetInt("chunksVisibleInViewDist", chunksVisibleInViewDist);
        positionCalculator.SetVector("playerPosition", playerPosition.position);

        material.SetBuffer("_Properties", positionsBuffer);
        material2.SetBuffer("_Properties", positionsBuffer2);

        bounds = new Bounds(Vector3.zero, new Vector3(1000000, 1000000, 1000000));
        material.SetColor("_EmissionColour", _EmissionColour);
        material2.SetColor("_EmissionColour", _EmissionColour2);
    }

    // Update is called once per frame
    void Update()
    {
        positionsBuffer.SetCounterValue(0);
        positionsBuffer2.SetCounterValue(0);
        debugPosBuffer.SetCounterValue(0);
        positionCalculator.SetVector("playerPosition", playerPosition.position);
        positionCalculator.DispatchIndirect(kernelIndex, dispatchBuffer);

        ComputeBuffer.CopyCount(positionsBuffer, argsBuffer, sizeof(uint));
        Graphics.DrawProceduralIndirect(material, bounds, MeshTopology.Points, argsBuffer);

        ComputeBuffer.CopyCount(positionsBuffer2, argsBuffer2, sizeof(uint));
        Graphics.DrawProceduralIndirect(material2, bounds, MeshTopology.Points, argsBuffer2);

        MeshProperties[] mp = new MeshProperties[(int)Mathf.Pow(chunksVisibleInViewDist + 1, 3)];
        if (Input.GetKeyDown(KeyCode.Q)) 
        {
            positionsBuffer.GetData(mp);
            foreach(var p in mp) 
            {
                Debug.Log(p.mat);
            }
        }
        int[] args = new int[3];
        if (Input.GetKeyDown(KeyCode.R)) 
        {
            dispatchBuffer.GetData(args);
            foreach (int x in args) 
            {
                Debug.Log(x);
            }
        }
        Vector3Int[] pos = new Vector3Int[(int)Mathf.Pow(chunksVisibleInViewDist + 1, 3)];
        if (Input.GetKeyDown(KeyCode.X))
        {
            debugPosBuffer.GetData(pos);
            foreach (var p in pos)
            {
                Debug.Log(p);
            }
        }
        int[] chunks = new int[1];
        if (Input.GetKeyDown(KeyCode.B))
        {
            chunksBuffer.GetData(chunks);
            foreach (var p in chunks)
            {
                Debug.Log(p);
            }
        }

    }
}
