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
        public int lodLevel;
    }
    struct ChunkIdentifier 
    { 
        public int chunksInViewDist;
        public int chunkSize;
        public int chunkType;
        public Vector3 pos;

        public ChunkIdentifier(int chunksInViewDist, int  chunkSize, int chunkType, Vector3 pos)
        {
            this.chunksInViewDist = chunksInViewDist;
            this.chunkSize = chunkSize;
            this.chunkType = chunkType;
            this.pos = pos;
        }    
    }
    public int numPositions;
    public int xSize;
    public int ySize;
    public int zSize;
    public Color _EmissionColour;
    public Color _EmissionColour2;

    public ComputeShader positionCalculator;

    //Big galaxy
    public DispatcherProcedural dispatcherProcedural;
    public ComputeShader galaxyPositioner;
    public float galaxyLodSwitchDist;

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

    public Material material3;
    ComputeBuffer positionsBuffer3;
    ComputeBuffer argsBuffer3;

    public Material material4;
    ComputeBuffer positionsBuffer4;
    ComputeBuffer argsBuffer4;

    public Material material5;
    ComputeBuffer positionsBuffer5;
    ComputeBuffer argsBuffer5;

    ComputeBuffer debugPosBuffer;
    ComputeBuffer dispatchBuffer;
    ComputeBuffer chunksBuffer;
    ComputeBuffer chunksBufferPrevFrame;
    ChunkIdentifier[] chunksVisible;

    ComputeBuffer mainProperties;
    ComputeBuffer mainPropertiesCount;
    public Material skyboxMat;

    ThreadIdentifier[] positions;
    int mainKernelIndex;
    int galaxyPositionerIndex;

    Bounds bounds;
    void Start()
    {
        //RenderSettings.skybox = skyboxMat;
        //material = new Material(shader);
        //material2 = new Material(shader);
        chunksVisibleInViewDist = Mathf.RoundToInt(renderDistance / chunkSize);
        chunksVisible = new ChunkIdentifier[1] { new ChunkIdentifier(chunksVisibleInViewDist, chunkSize, 4, Vector3.one * -1) };
        mainKernelIndex = positionCalculator.FindKernel("CSMain");
        galaxyPositionerIndex = galaxyPositioner.FindKernel("CSMain");

        positionsBuffer = new ComputeBuffer((int)Mathf.Pow(chunksVisibleInViewDist * 8 + 1, 3), System.Runtime.InteropServices.Marshal.SizeOf(typeof(MeshProperties)), ComputeBufferType.Append);
        argsBuffer = new ComputeBuffer(1, sizeof(uint) * 4, ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(new uint[] { (uint)1, (uint)Mathf.Pow(chunksVisibleInViewDist * 8 + 1, 3), 0u, 0u });

        positionsBuffer2 = new ComputeBuffer((int)Mathf.Pow(chunksVisibleInViewDist * 8 + 1, 3), System.Runtime.InteropServices.Marshal.SizeOf(typeof(MeshProperties)), ComputeBufferType.Append);
        argsBuffer2 = new ComputeBuffer(1, sizeof(uint) * 4, ComputeBufferType.IndirectArguments);
        argsBuffer2.SetData(new uint[] { (uint)1, (uint)Mathf.Pow(chunksVisibleInViewDist * 8 + 1, 3), 0u, 0u });

        positionsBuffer3 = new ComputeBuffer((int)Mathf.Pow(chunksVisibleInViewDist * 8 + 1, 3), System.Runtime.InteropServices.Marshal.SizeOf(typeof(MeshProperties)), ComputeBufferType.Append);
        argsBuffer3 = new ComputeBuffer(1, sizeof(uint) * 4, ComputeBufferType.IndirectArguments);
        argsBuffer3.SetData(new uint[] { (uint)1, (uint)Mathf.Pow(chunksVisibleInViewDist * 8 + 1, 3), 0u, 0u });

        positionsBuffer4 = new ComputeBuffer((int)Mathf.Pow(chunksVisibleInViewDist * 8 + 1, 3), System.Runtime.InteropServices.Marshal.SizeOf(typeof(MeshProperties)), ComputeBufferType.Append);
        argsBuffer4 = new ComputeBuffer(1, sizeof(uint) * 4, ComputeBufferType.IndirectArguments);
        argsBuffer4.SetData(new uint[] { (uint)1, (uint)Mathf.Pow(chunksVisibleInViewDist * 8 + 1, 3), 0u, 0u });

        positionsBuffer5 = new ComputeBuffer((int)Mathf.Pow(chunksVisibleInViewDist * 8 + 1, 3), System.Runtime.InteropServices.Marshal.SizeOf(typeof(MeshProperties)), ComputeBufferType.Append);
        argsBuffer5 = new ComputeBuffer(1, sizeof(uint) * 4, ComputeBufferType.IndirectArguments);
        argsBuffer5.SetData(new uint[] { (uint)1, (uint)Mathf.Pow(chunksVisibleInViewDist * 8 + 1, 3), 0u, 0u });

        mainProperties = new ComputeBuffer(1, sizeof(float) * 3, ComputeBufferType.Append);
        mainPropertiesCount = new ComputeBuffer(1, sizeof(uint));
        chunksBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ChunkIdentifier)), ComputeBufferType.Structured);
        chunksBuffer.SetData(chunksVisible);
        chunksBufferPrevFrame = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ChunkIdentifier)), ComputeBufferType.Structured);
        chunksBufferPrevFrame.SetData(chunksVisible);
        debugPosBuffer = new ComputeBuffer((int)Mathf.Pow(chunksVisibleInViewDist * 8 + 1, 3), System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector3Int)), ComputeBufferType.Append);
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
        positionCalculator.SetBuffer(mainKernelIndex, "_Properties", positionsBuffer);
        positionCalculator.SetBuffer(mainKernelIndex, "_Properties2", positionsBuffer2);
        positionCalculator.SetBuffer(mainKernelIndex, "_Properties3", positionsBuffer3);
        positionCalculator.SetBuffer(mainKernelIndex, "_Properties4", positionsBuffer4);
        positionCalculator.SetBuffer(mainKernelIndex, "_Properties5", positionsBuffer5);

        positionCalculator.SetBuffer(mainKernelIndex, "_ChunksBuffer", chunksBuffer);
        positionCalculator.SetBuffer(mainKernelIndex, "_ChunksBufferPrevFrame",chunksBufferPrevFrame);
        positionCalculator.SetBuffer(mainKernelIndex, "_DispatchBuffer", dispatchBuffer);

        positionCalculator.SetInt("chunkSize", chunkSize);
        positionCalculator.SetInt("renderDistance", renderDistance);
        positionCalculator.SetFloat("lodSwitchDist", lodSwitchDist);
        positionCalculator.SetFloat("galaxySwitchDist", galaxyLodSwitchDist);
        positionCalculator.SetInt("chunksVisibleInViewDist", chunksVisibleInViewDist);
        positionCalculator.SetVector("playerPosition", playerPosition.position);

        galaxyPositioner.SetBuffer(galaxyPositionerIndex, "_ChunksBuffer", chunksBuffer);
        galaxyPositioner.SetBuffer(galaxyPositionerIndex, "_MainProperties", mainProperties);
        galaxyPositioner.SetFloat("lodSwitchDist", galaxyLodSwitchDist);
        galaxyPositioner.SetVector("playerPosition", playerPosition.position);
        dispatcherProcedural._MainPositionBuffer = mainProperties;
        dispatcherProcedural._MainPositionBufferCount = mainPropertiesCount;

        material.SetBuffer("_Properties", positionsBuffer);
        material2.SetBuffer("_Properties", positionsBuffer2);
        material3.SetBuffer("_Properties", positionsBuffer3);
        material4.SetBuffer("_Properties", positionsBuffer4);
        material5.SetBuffer("_Properties", positionsBuffer5);

        bounds = new Bounds(Vector3.zero, new Vector3(1000000, 1000000, 1000000));
        material.SetColor("_EmissionColour", _EmissionColour);
        material2.SetColor("_EmissionColour", _EmissionColour2);
    }

    // Update is called once per frame
    void Update()
    {
        positionsBuffer.SetCounterValue(0);
        positionsBuffer2.SetCounterValue(0);
        positionsBuffer3.SetCounterValue(0);
        positionsBuffer4.SetCounterValue(0);
        positionsBuffer5.SetCounterValue(0);
        mainProperties.SetCounterValue(0);

        debugPosBuffer.SetCounterValue(0);
        positionCalculator.SetVector("playerPosition", playerPosition.position);
        positionCalculator.DispatchIndirect(mainKernelIndex, dispatchBuffer);

        galaxyPositioner.SetVector("playerPosition", playerPosition.position);
        galaxyPositioner.DispatchIndirect(galaxyPositionerIndex, dispatchBuffer);

        ComputeBuffer.CopyCount(mainProperties, mainPropertiesCount, 0);

        ComputeBuffer.CopyCount(positionsBuffer, argsBuffer, sizeof(uint));
        Graphics.DrawProceduralIndirect(material, bounds, MeshTopology.Points, argsBuffer);

        ComputeBuffer.CopyCount(positionsBuffer2, argsBuffer2, sizeof(uint));
        Graphics.DrawProceduralIndirect(material2, bounds, MeshTopology.Points, argsBuffer2);

        ComputeBuffer.CopyCount(positionsBuffer3, argsBuffer3, sizeof(uint));
        Graphics.DrawProceduralIndirect(material3, bounds, MeshTopology.Points, argsBuffer3);

        ComputeBuffer.CopyCount(positionsBuffer4, argsBuffer4, sizeof(uint));
        Graphics.DrawProceduralIndirect(material4, bounds, MeshTopology.Points, argsBuffer4);

        ComputeBuffer.CopyCount(positionsBuffer5, argsBuffer5, sizeof(uint));
        Graphics.DrawProceduralIndirect(material5, bounds, MeshTopology.Points, argsBuffer5);

        MeshProperties[] mp = new MeshProperties[(int)Mathf.Pow(chunksVisibleInViewDist * 8 + 1, 3)];
        if (Input.GetKeyDown(KeyCode.Q)) 
        {
            positionsBuffer.GetData(mp);
            foreach(var p in mp) 
            {
                Debug.Log(p.mat);
            }
        }
        MeshProperties[] mp2 = new MeshProperties[(int)Mathf.Pow(chunksVisibleInViewDist * 8 + 1, 3)];
        if (Input.GetKeyDown(KeyCode.T))
        {
            positionsBuffer2.GetData(mp2);
            foreach (var p in mp)
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
        Vector3Int[] pos = new Vector3Int[(int)Mathf.Pow(chunksVisibleInViewDist * 8 + 1, 3)];
        if (Input.GetKeyDown(KeyCode.X))
        {
            debugPosBuffer.GetData(pos);
            foreach (var p in pos)
            {
                Debug.Log(p);
            }
        }
        ChunkIdentifier[] chunks = new ChunkIdentifier[1];
        if (Input.GetKeyDown(KeyCode.B))
        {
            chunksBuffer.GetData(chunks);
            foreach (var p in chunks)
            {
                Debug.Log(p.pos);
                Debug.Log(p.chunksInViewDist);
                Debug.Log(p.chunkSize);
                Debug.Log(p.chunkType);
            }
        }
        Vector3[] mainPos = new Vector3[1];
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log(mainPos.Length);
            mainProperties.GetData(mainPos);
            foreach (var p in mainPos)
            {
                Debug.Log(p);
            }
        }

    }
}
