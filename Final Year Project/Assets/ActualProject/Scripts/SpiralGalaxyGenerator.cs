using System.Collections.Generic;
using System.Collections;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Linq;
using UnityEngine.Rendering;
using Unity.VisualScripting;

//https://forum.unity.com/threads/how-do-i-add-emission-to-a-custom-fragment-shader.1313034/
struct GalaxyStar
{
    public Vector3 position;
    public Color colour;
    public float radius;
}
public class SpiralGalaxyGenerator : MonoBehaviour
{
    [Header("Shaders")]
    public Material[] material;
    public ComputeShader sphereGeneratorPrefab;
    ComputeShader sphereGenerator;
    public ComputeShader positionCalculator;
    public PlayerController player;
    public BufferManager bufferManager;
    [Header("Sphere Generation")]
    [Range(1, 10)]
    public int Resolution = 10;
    public float smallStarRadius;
    public float largeStarRadius;
    public int numInstances = 10000;
    int prevNumInstances;
    uint threadGroupSizeX;

    [Header("Position Calculation")]
    public float _TimeStep = 1.0f;
    public int LODSwitchDist;

    [Header("Textures")]
    public Texture2D billboardTexture;

    [Header("Colours")]
    public Color _EmissionColour;
    public Color _StandardColour;
    public Color _H2RegionColour;
    private int positionGroupSizeX;
    private int SphereGeneratorHandle;
    private int positionCalculatorHandle;

    private Bounds bounds;
    private Vector3 prevCameraPos;
    private Quaternion prevCameraRot;
    [Header("Buffers")]
    bool buffersSet;
    private ComputeBuffer _MainPositionBuffer;
    private ComputeBuffer _MainPositionBufferCount;
    private ComputeBuffer _PositionsBufferLODAppend0;
    private ComputeBuffer _PositionsBufferLODAppend1;
    private ComputeBuffer _PositionsBufferLODAppend2;
    private ComputeBuffer _VertexBuffer;
    private ComputeBuffer _NormalBuffer;
    private ComputeBuffer _UVBuffer;
    private GraphicsBuffer _IndexBuffer;

    private ComputeBuffer sphereArgsBuffer;
    private ComputeBuffer billboardArgsBuffer;
    private ComputeBuffer cloudArgsBuffer;
    private ComputeBuffer viewFrustumPlanesBuffer;
    private ComputeBuffer sphereGeneratorDispatchArgsBuffer;
    private ComputeBuffer positionCalculatorDispatchArgsBuffer;

    [Header("Galaxy Star Distribution")]
    private ComputeBuffer radii;
    private ComputeBuffer ids;
    private Texture2D _RadiusLookupTexture;
    private void Awake()
    {
        player = FindObjectOfType<PlayerController>();
        bufferManager = FindObjectOfType<BufferManager>();
    }

    private void SetPositionCalculatorData() 
    {
        //_GalacticCentre = transform.position;
        positionCalculator.SetFloat("_SmallStarRadius", smallStarRadius);
        positionCalculator.SetFloat("_LargeStarRadius" ,largeStarRadius);
        positionCalculator.SetFloat("_time", Time.time);
        positionCalculator.SetFloat("_LODSwitchDist", LODSwitchDist);
        positionCalculator.SetVector("_PlayerPosition", player.transform.position);
        positionCalculator.SetVector("_StandardColour", _StandardColour);
        positionCalculator.SetVector("_H2RegionColour", _H2RegionColour);

    }
    IEnumerator InitExternalResources() 
    {
        while (bufferManager.bigGalaxyProperties == null || bufferManager.bigGalaxyPropertiesCount == null || bufferManager._RadiusLookupTexture == null) 
        {
            yield return null;
        }
        _MainPositionBuffer = bufferManager.bigGalaxyProperties;
        _MainPositionBufferCount = bufferManager.bigGalaxyPropertiesCount;
        _RadiusLookupTexture = bufferManager._RadiusLookupTexture;
        positionCalculator.SetBuffer(positionCalculatorHandle, "_MainPropertiesBuffer", _MainPositionBuffer);
        positionCalculator.SetBuffer(positionCalculatorHandle, "_MainPositionBufferCount", _MainPositionBufferCount);
        positionCalculator.SetTexture(positionCalculatorHandle, "_RadiusLookupTexture", _RadiusLookupTexture);
        Debug.Log("Set!");
        buffersSet = true;
    }

    void Start()
    {
        numInstances = bufferManager.minMaxNumParticles[1];
        //---Debugging---
        radii = new ComputeBuffer(numInstances, 2 * sizeof(float), ComputeBufferType.Append);
        ids = new ComputeBuffer(numInstances, sizeof(uint), ComputeBufferType.Structured);
        //--Debugging---




        //Create vertex and index buffers 
        sphereGenerator = Instantiate(sphereGeneratorPrefab);
        int numVertsPerInstance = Resolution * Resolution * 4 * 6; //Plane of verts made up of groups of quads. 1 plane for each of the 6 faces of a cube
        int numIndicesPerInstance = 6 * 6 * Resolution * Resolution; //indicesPerTriangle * trianglesPerQuad * 6 faces of cube * resolution^2

        _PositionsBufferLODAppend0 = new ComputeBuffer(numInstances, System.Runtime.InteropServices.Marshal.SizeOf(typeof(GalaxyStar)), ComputeBufferType.Append);
        _PositionsBufferLODAppend1 = new ComputeBuffer(numInstances, System.Runtime.InteropServices.Marshal.SizeOf(typeof(GalaxyStar)), ComputeBufferType.Append);
        _PositionsBufferLODAppend2 = new ComputeBuffer(numInstances, System.Runtime.InteropServices.Marshal.SizeOf(typeof(GalaxyStar)), ComputeBufferType.Append);
        _VertexBuffer = new ComputeBuffer(numVertsPerInstance * numInstances, sizeof(float) * 3, ComputeBufferType.Structured);
        _NormalBuffer = new ComputeBuffer(numVertsPerInstance * numInstances, sizeof(float) * 3, ComputeBufferType.Structured);
        _UVBuffer = new ComputeBuffer(numVertsPerInstance * numInstances, sizeof(float) * 2, ComputeBufferType.Structured);
        _IndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, numIndicesPerInstance * numInstances, sizeof(uint));
        viewFrustumPlanesBuffer = new ComputeBuffer(6, sizeof(float) * 4, ComputeBufferType.Structured);
        //Bind buffers to material
        SphereGeneratorHandle = sphereGenerator.FindKernel("CSMain");
        positionCalculatorHandle = positionCalculator.FindKernel("CSMain");
        material[0].SetBuffer("_VertexBuffer", _VertexBuffer);
        material[0].SetBuffer("_NormalBuffer", _NormalBuffer);
        material[0].SetBuffer("_UVBuffer", _UVBuffer);
        material[0].SetColor("_EmissionColour", _EmissionColour);
        material[0].SetBuffer("_PositionsLOD0", _PositionsBufferLODAppend0);

        //bind relevant buffers to position calculator computeShader.
        positionCalculator.SetBuffer(positionCalculatorHandle, "_ViewFrustumPlanes", viewFrustumPlanesBuffer);
        StartCoroutine(InitExternalResources());
        positionCalculator.SetBuffer(positionCalculatorHandle, "_Radii", radii);
        positionCalculator.SetBuffer(positionCalculatorHandle, "_Ids", ids);
        positionCalculator.SetBuffer(positionCalculatorHandle, "_PositionsLODAppend0", _PositionsBufferLODAppend0);
        positionCalculator.SetBuffer(positionCalculatorHandle, "_PositionsLODAppend1", _PositionsBufferLODAppend1);
        positionCalculator.SetBuffer(positionCalculatorHandle, "_PositionsLODAppend2", _PositionsBufferLODAppend2);


        material[1].SetBuffer("_PositionsLOD1", _PositionsBufferLODAppend1);
        material[1].SetColor("_EmissionColour", _EmissionColour);
        material[1].SetTexture("_MainTex", billboardTexture);

        material[2].SetBuffer("_Positions", _PositionsBufferLODAppend2);

        //calculate thread group sizes 
        positionCalculator.GetKernelThreadGroupSizes(positionCalculatorHandle, out threadGroupSizeX, out _, out _);
        positionGroupSizeX = Mathf.CeilToInt((float)numInstances / (float)threadGroupSizeX);


        uint threadGroupGeneratorX;
        sphereGenerator.SetBuffer(SphereGeneratorHandle, "_VertexBuffer", _VertexBuffer);
        sphereGenerator.SetBuffer(SphereGeneratorHandle, "_NormalBuffer", _NormalBuffer);
        sphereGenerator.SetBuffer(SphereGeneratorHandle, "_UVBuffer", _UVBuffer);
        sphereGenerator.SetBuffer(SphereGeneratorHandle, "_Positions", _PositionsBufferLODAppend0);
        sphereGenerator.SetBuffer(SphereGeneratorHandle, "_IndexBuffer", _IndexBuffer);
        sphereGenerator.SetInt("_Resolution", Resolution);
        sphereGenerator.SetInt("_NumVertsPerInstance", numVertsPerInstance);


        //Additional arguments to DrawProceduralIndirect: bounds and the arguments buffer
        bounds = new Bounds(Vector3.zero, Vector3.one * 100000);

        sphereArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
        billboardArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 4, ComputeBufferType.IndirectArguments);
        cloudArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 4, ComputeBufferType.IndirectArguments);
        sphereGeneratorDispatchArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 3,ComputeBufferType.IndirectArguments);
        positionCalculatorDispatchArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 3, ComputeBufferType.IndirectArguments);

        // index 0 : index count per instance
        // index 1 : instance count
        // index 2 : start vertex location
        // index 3 : start instance location
        // index 4 : start instance location
        sphereArgsBuffer.SetData(new uint[] { (uint)numIndicesPerInstance, (uint)numInstances, 0u, 0u, 0u });
        billboardArgsBuffer.SetData(new uint[] { (uint)1, (uint)numInstances, 0u, 0u });
        cloudArgsBuffer.SetData(new uint[] { (uint)1, (uint)numInstances, 0u, 0u });

        sphereGeneratorDispatchArgsBuffer.SetData(new uint[] { (uint) Resolution, 1u, 1u });
        positionCalculatorDispatchArgsBuffer.SetData(new uint[] { (uint)positionGroupSizeX, 1u, 1u });

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        viewFrustumPlanesBuffer.SetData(planes);
        prevCameraPos = Camera.main.transform.position;
        prevCameraRot = Camera.main.transform.rotation;
    }
    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.I)) 
        {
            Vector2[] rads = new Vector2[numInstances];
            radii.GetData(rads);
            Vector2 minRad = new Vector2(float.MaxValue, 1.0f);
            Vector2 maxRad = new Vector2(float.MinValue, 0.0f);

            Vector2 minRadX = new Vector2(float.MaxValue, 0.0f);
            Vector2 maxRadX = new Vector2(float.MinValue, 1.0f);
            foreach (Vector2 rad in rads) 
            {
                Debug.Log(rad);
                minRadX = Mathf.Min(rad.x, minRadX.x) == rad.x ? rad : minRadX;
                maxRadX = Mathf.Max(rad.x, maxRadX.x) == rad.x ? rad : maxRadX;
                if (minRad.y > rad.y) 
                {
                    minRad = rad;
                }
                else if (minRad.y == rad.y && minRad.x >= rad.x) 
                {
                    minRad = rad;
                }
                if (maxRad.y < rad.y)
                {
                    maxRad = rad;
                }
                else if (maxRad.y == rad.y && maxRad.x <= rad.x)
                {
                    maxRad = rad;
                }
                if (rad.y == 1.0) Debug.Log(rad);
            }
            Debug.Log($"minrad : {minRad}, maxRad: {maxRad}");
            Debug.Log($"minradX : {minRadX}, maxRadX: {maxRadX}");
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            Color[] colors = _RadiusLookupTexture.GetPixels();
            int width = _RadiusLookupTexture.width;
            int height = _RadiusLookupTexture.height;

            Debug.Log($"Texture width is {width}, texture height is {height}");
            for (int i = 0; i < colors.Length; i++)
            {
                // Calculate the UV coordinates
                float u = (i % width) / (float)width;
                float v = (i / width) / (float)height;

                Debug.Log($"Value at coordinate ({u}, {v}) is: {colors[i]}");
            }
        }

        if (Input.GetKeyDown(KeyCode.H)) 
        {
            int[] idArray = new int[numInstances];
            int minId = int.MaxValue;
            int maxId = int.MinValue;
            ids.GetData(idArray);
            foreach (int x in idArray) 
            {
                Debug.Log($"id: {x}");
                minId = Mathf.Min(minId, x);
                maxId = Mathf.Max(maxId, x);
            }
            Debug.Log($"minId: {minId}, maxId: {maxId}, numInstances: {numInstances}");
        }
        radii.SetCounterValue(0);

        _PositionsBufferLODAppend0.SetCounterValue(0);
        _PositionsBufferLODAppend1.SetCounterValue(0);
        _PositionsBufferLODAppend2.SetCounterValue(0);
        material[1].SetColor("_EmissionColour", _EmissionColour);

        //only upload view frustum plane data to gpu if it has changed.
        if (prevCameraPos != Camera.main.transform.position || prevCameraRot != Camera.main.transform.rotation)
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            viewFrustumPlanesBuffer.SetData(planes);
        }

        int numRows = Resolution;
        if (buffersSet) 
        {
            positionCalculator.DispatchIndirect(positionCalculatorHandle, positionCalculatorDispatchArgsBuffer);
            //sphereGenerator.Dispatch(SphereGeneratorHandle, 10, 10, 1);
            sphereGenerator.DispatchIndirect(SphereGeneratorHandle, sphereGeneratorDispatchArgsBuffer);
            sphereGenerator.SetInt("_Resolution", Resolution);
        }

        SetPositionCalculatorData();


        ComputeBuffer.CopyCount(_PositionsBufferLODAppend0, sphereArgsBuffer, sizeof(uint));
        ComputeBuffer.CopyCount(_PositionsBufferLODAppend1, billboardArgsBuffer, sizeof(uint));
        ComputeBuffer.CopyCount(_PositionsBufferLODAppend2, cloudArgsBuffer, sizeof(uint));


        Graphics.DrawProceduralIndirect(material[0], bounds, MeshTopology.Triangles, _IndexBuffer, sphereArgsBuffer);//Spheres
        Graphics.DrawProceduralIndirect(material[1], bounds, MeshTopology.Points, billboardArgsBuffer);
        Graphics.DrawProceduralIndirect(material[2], bounds, MeshTopology.Points, cloudArgsBuffer);

        prevCameraPos = Camera.main.transform.position;
        prevCameraRot = Camera.main.transform.rotation;
        prevNumInstances = numInstances;
        positionCalculator.SetFloat("_TimeStep", bufferManager.timeStep);
    }
    private void Update2()
    {
        material[1].SetColor("_EmissionColour", _EmissionColour);
        //_PositionsBufferLOD0.SetCounterValue(0);
        //_PositionsBufferLOD1.SetCounterValue(0);
        if (prevCameraPos != Camera.main.transform.position || prevCameraRot != Camera.main.transform.rotation) 
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            viewFrustumPlanesBuffer.SetData(planes);
        }

        int numRows = Resolution;
        positionCalculator.Dispatch(positionCalculatorHandle, positionGroupSizeX, 1, 1);
        sphereGenerator.Dispatch(SphereGeneratorHandle, 10, 10, 1);
        sphereGenerator.SetInt("_Resolution", Resolution);
        SetPositionCalculatorData();
        Graphics.DrawProceduralIndirect(material[0], bounds, MeshTopology.Triangles, _IndexBuffer, sphereArgsBuffer);//Spheres
        Graphics.DrawProceduralIndirect(material[1], bounds, MeshTopology.Points, billboardArgsBuffer);
        prevCameraPos = Camera.main.transform.position;
        prevCameraRot = Camera.main.transform.rotation;
    }

    private void OnDestroy()
    {
        if (_VertexBuffer != null)
        {
            _VertexBuffer.Release();
            _VertexBuffer = null;
        }

        if (sphereArgsBuffer != null)
        {
            sphereArgsBuffer.Release();
            sphereArgsBuffer = null;
        }

        if (_IndexBuffer != null)
        {
            _IndexBuffer.Release();
            _IndexBuffer = null;
        }
    }
}
