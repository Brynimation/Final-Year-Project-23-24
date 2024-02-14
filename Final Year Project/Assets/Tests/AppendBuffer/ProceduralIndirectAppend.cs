using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.Universal;



public class ProceduralIndirectAppend : MonoBehaviour
{
    [Header("Shaders")]
    public Material[] material;
    public ComputeShader sphereGenerator;
    public ComputeShader positionCalculator;

    [Header("Sphere Generation")]
    [Range(1, 10)]
    public int Resolution = 10;
    public int numInstances = 10000;
    public int LODSwitchDist;
    [Header("Position Calculation")]
    public Vector3 _GalacticCentre;
    public float _MinEccentricity;
    public float _MaxEccentricity;
    public float _GalacticDiskRadius;
    public float _GalacticHaloRadius;
    public float _GalacticBulgeRadius;
    public float _AngularOffsetMultiplier;
    [Header("Textures")]
    public Texture2D billboardTexture;

    private int positionGroupSizeX;
    private int SphereGeneratorHandle;
    private int positionCalculatorHandle;
    private ComputeBuffer sphereArgsBuffer;
    private ComputeBuffer billboardArgsBuffer;
    private Bounds bounds;
    Vector3[] positions;
    private ComputeBuffer _PositionsBufferLOD0;
    private ComputeBuffer _PositionsBufferLOD1;
    private ComputeBuffer _VertexBuffer;
    private GraphicsBuffer _IndexBuffer;
    private ComputeBuffer viewFrustumPlanesBuffer;

    private Vector3 prevCameraPos;
    private Quaternion prevCameraRot;

    private void SetPositionCalculatorData()
    {
        positionCalculator.SetVector("_GalacticCentre", _GalacticCentre);
        positionCalculator.SetFloat("_MinEccentricity", _MinEccentricity);
        positionCalculator.SetFloat("_MaxEccentricity", _MaxEccentricity);
        positionCalculator.SetFloat("_GalacticDiskRadius", _GalacticDiskRadius);
        positionCalculator.SetFloat("_GalacticHaloRadius", _GalacticHaloRadius);
        positionCalculator.SetFloat("_GalacticBulgeRadius", _GalacticBulgeRadius);
        positionCalculator.SetFloat("_AngularOffsetMultiplier", _AngularOffsetMultiplier);
        positionCalculator.SetInt("_NumParticles", numInstances);
        positionCalculator.SetFloat("_time", Time.time);
        positionCalculator.SetFloat("_LODSwitchDist", LODSwitchDist);
        positionCalculator.SetVector("_CameraPosition", Camera.main.transform.position);
    }
    void Start()
    {
        //Create vertex and index buffers 
        int numVertsPerInstance = Resolution * Resolution * 4 * 6; //Plane of verts made up of groups of quads. 1 plane for each of the 6 faces of a cube
        int numIndicesPerInstance = 6 * 6 * Resolution * Resolution; //indicesPerTriangle * trianglesPerQuad * 6 faces of cube * resolution^2

        _PositionsBufferLOD0 = new ComputeBuffer(numInstances, System.Runtime.InteropServices.Marshal.SizeOf(typeof(GalaxyStar)), ComputeBufferType.Append);
        _PositionsBufferLOD1 = new ComputeBuffer(numInstances, System.Runtime.InteropServices.Marshal.SizeOf(typeof(GalaxyStar)), ComputeBufferType.Append);
        _VertexBuffer = new ComputeBuffer(numVertsPerInstance * numInstances, sizeof(float) * 3, ComputeBufferType.Structured);
        _IndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, numIndicesPerInstance * numInstances, sizeof(uint));

        //Bind buffers to material
        SphereGeneratorHandle = sphereGenerator.FindKernel("CSMain");
        positionCalculatorHandle = positionCalculator.FindKernel("CSMain");
        material[0].SetBuffer("_VertexBuffer", _VertexBuffer);
        material[0].SetBuffer("_PositionsLOD0", _PositionsBufferLOD0);

        //bind relevant buffers to chunkManager computeShader.
        positionCalculator.SetBuffer(positionCalculatorHandle, "_PositionsLOD0", _PositionsBufferLOD0);
        positionCalculator.SetBuffer(positionCalculatorHandle, "_PositionsLOD1", _PositionsBufferLOD1);
        material[1].SetBuffer("_PositionsLOD1", _PositionsBufferLOD1);
        material[1].SetTexture("_MainTex", billboardTexture);

        uint threadGroupSizeX;
        positionCalculator.GetKernelThreadGroupSizes(positionCalculatorHandle, out threadGroupSizeX, out _, out _);
        positionGroupSizeX = Mathf.CeilToInt(numInstances / threadGroupSizeX);
        //Bind relevant buffers to Sphere Generator compute shader and set variables needed to generate the spheres.
        sphereGenerator.SetBuffer(SphereGeneratorHandle, "_VertexBuffer", _VertexBuffer);
        sphereGenerator.SetBuffer(SphereGeneratorHandle, "_Positions", _PositionsBufferLOD0);
        sphereGenerator.SetBuffer(SphereGeneratorHandle, "_IndexBuffer", _IndexBuffer);
        sphereGenerator.SetInt("_Resolution", Resolution);
        sphereGenerator.SetInt("_NumVertsPerInstance", numVertsPerInstance);

        viewFrustumPlanesBuffer = new ComputeBuffer(6, sizeof(float) * 4, ComputeBufferType.Structured);
        positionCalculator.SetBuffer(positionCalculatorHandle, "_ViewFrustumPlanes", viewFrustumPlanesBuffer);
        
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        Debug.Log(planes);
        Debug.Log(viewFrustumPlanesBuffer);
        viewFrustumPlanesBuffer.SetData(planes);

        //Additional arguments to DrawProceduralIndirect: bounds and the arguments buffer
        bounds = new Bounds(Vector3.zero, new Vector3(10000, 10000, 10000));

        sphereArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
        billboardArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 4, ComputeBufferType.IndirectArguments);
        // index 0 : index count per instance
        // index 1 : instance count
        // index 2 : start vertex location
        // index 3 : start instance location
        // index 4 : start instance location
        sphereArgsBuffer.SetData(new uint[] { (uint)numIndicesPerInstance, (uint)numInstances, 0u, 0u, 0u });
        billboardArgsBuffer.SetData(new uint[] { (uint)1, (uint)numInstances, 0u, 0u });
    }


    private void Update()
    {
        _PositionsBufferLOD0.SetCounterValue(0);
        _PositionsBufferLOD1.SetCounterValue(0);
        if (prevCameraPos != Camera.main.transform.position || prevCameraRot != Camera.main.transform.rotation)
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            Debug.Log(planes);
            Debug.Log(viewFrustumPlanesBuffer);
            viewFrustumPlanesBuffer.SetData(planes);
        }
        int numRows = Resolution;
        positionCalculator.Dispatch(positionCalculatorHandle, positionGroupSizeX, 1, 1);
        sphereGenerator.Dispatch(SphereGeneratorHandle, Resolution, Resolution, 1);
        sphereGenerator.SetInt("_Resolution", Resolution);
        SetPositionCalculatorData();
        Graphics.DrawProceduralIndirect(material[0], bounds, MeshTopology.Triangles, _IndexBuffer, sphereArgsBuffer);//Spheres
        Graphics.DrawProceduralIndirect(material[1], bounds, MeshTopology.Points, billboardArgsBuffer);
        if (Input.GetKeyDown(KeyCode.P))
        {
            GalaxyStar[] data = new GalaxyStar[numInstances];
            _PositionsBufferLOD0.GetData(data);
            Debug.Log("length: " + data.Length);
            foreach (GalaxyStar pos in data)
            {
                Debug.Log(pos.position);
            }
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            GalaxyStar[] data = new GalaxyStar[numInstances];
            _PositionsBufferLOD1.GetData(data);
            Debug.Log("length: " + data.Length);
            foreach (GalaxyStar pos in data)
            {
                Debug.Log(pos.position);
            }
            uint[] billboardThing = new uint[4];
            billboardArgsBuffer.GetData(billboardThing);
            for (int i = 0; i < 4; i++) 
            {
                Debug.Log($"{i} + {billboardThing[i]}");
            }

        }
        ComputeBuffer.CopyCount(_PositionsBufferLOD0, sphereArgsBuffer, sizeof(uint));
        ComputeBuffer.CopyCount(_PositionsBufferLOD1, billboardArgsBuffer, sizeof(uint));
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
