using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UniGenerator : MonoBehaviour
{
    public static GalaxyProperties currentGalaxyProperties;
    public Mesh galaxyMesh;
    public Material galaxyMaterial;
    public int width;
    public int height;
    public int depth;

    public Vector2Int minMaxDisc;
    public Vector2Int minMaxBulge;
    public Vector2Int minMaxAngularOffset;
    public Vector2Int minMaxStarCount;

    public int numGalaxiesX;
    public int numGalaxiesY;
    public int numGalaxiesZ;

    int numGroupsX;
    int numGroupsY;
    int numGroupsZ;
    public ComputeBuffer positionsBuffer;
    public ComputeBuffer indirectArgsBuffer;
    public ComputeShader galaxyPositioner;

    Vector3[] positionData;

    private int kernelId;
    private uint[] indirectArgs = new uint[5] {0,0,0,0,0};
    private Bounds bounds;

    void Awake() 
    {
        currentGalaxyProperties = new GalaxyProperties(transform.position, minMaxDisc, minMaxBulge, minMaxAngularOffset, minMaxStarCount);
        indirectArgs[0] = galaxyMesh.GetIndexCount(0);
        //positionData = new Vector3[numGalaxiesX * numGalaxiesY * numGalaxiesZ];
        indirectArgs[1] = (uint) (numGalaxiesX * numGalaxiesY * numGalaxiesZ);
        positionsBuffer = new ComputeBuffer(numGalaxiesX * numGalaxiesY * numGalaxiesZ, sizeof(float) * 3);
        indirectArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
        indirectArgsBuffer.SetData(indirectArgs);
        kernelId = galaxyPositioner.FindKernel("CSMain2");
    }
    void Start()
    {
        uint groupSizeX;
        uint groupSizeY;
        uint groupSizeZ;
        galaxyPositioner.GetKernelThreadGroupSizes(kernelId, out groupSizeX, out groupSizeY, out groupSizeZ);
        Debug.Log($"{groupSizeX}, {groupSizeY}, {groupSizeZ}");
        numGroupsX = Mathf.CeilToInt((float)numGalaxiesX /(float) groupSizeX);
        numGroupsY = Mathf.CeilToInt((float)numGalaxiesY / (float)groupSizeY);
        numGroupsZ = Mathf.CeilToInt((float)numGalaxiesZ / (float)groupSizeZ);
        galaxyPositioner.SetInt("_NumGalaxiesX", numGalaxiesX);
        galaxyPositioner.SetInt("_NumGalaxiesY", numGalaxiesY);
        galaxyPositioner.SetInt("_NumGalaxiesZ", numGalaxiesZ);
        galaxyPositioner.SetFloat("_UniverseWidth", width);
        galaxyPositioner.SetFloat("_UniverseHeight", height);
        galaxyPositioner.SetFloat("_Time", Time.time);
        galaxyPositioner.SetFloat("_UniverseDepth", depth);
        bounds = new Bounds(transform.position, new Vector3(width * 100, height * 100, depth * 100));
        galaxyPositioner.SetBuffer(kernelId, "_Positions", positionsBuffer);
        galaxyMaterial.SetBuffer("_Positions", positionsBuffer);
        galaxyPositioner.Dispatch(kernelId, numGroupsX, numGroupsY, numGroupsZ);
        

    }

    // Update is called once per frame
    void Update()
    {
        //galaxyPositioner.Dispatch(kernelId, numGroupsX, numGroupsY, numGroupsZ);
        galaxyPositioner.SetFloat("_Time", Time.time);
        Graphics.DrawMeshInstancedIndirect(galaxyMesh, 0, galaxyMaterial, bounds, indirectArgsBuffer);

    }
}
