using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.Universal;

//https://forum.unity.com/threads/how-do-i-add-emission-to-a-custom-fragment-shader.1313034/
public struct InstanceData
{
    Vector3 position;
    Color colour;
    float radius;
    uint culled;
};
public class DispatcherProcedural : MonoBehaviour
{
    [Header("Shaders")]
    public Material[] material;
    public ComputeShader sphereGenerator;
    public ComputeShader positionCalculator;
    [Header("Sphere Generation")]
    [Range(1, 10)]
    public int Resolution = 10;
    public int numInstances = 10000;
    int prevNumInstances;
    uint threadGroupSizeX;
    public int LODSwitchDist;
    [Header("Position Calculation")]
    public Vector3 _GalacticCentre;
    public float _TimeStep = 1.0f;
    public float _MinEccentricity;
    public float _MaxEccentricity;
    public float _GalacticDiskRadius;
    public float _GalacticHaloRadius;
    public float _GalacticBulgeRadius;
    public float _AngularOffsetMultiplier;
    [Header("Textures")]
    public Texture2D billboardTexture;

    [Header("Colours")]
    public Color _EmissionColour;
    public Color _StandardColour;
    public Color _H2RegionColour;
    private int positionGroupSizeX;
    int sphereGeneratorGroupSizeX;
    private int SphereGeneratorHandle;
    private int positionCalculatorHandle;

    private Bounds bounds;
    private Vector3 prevCameraPos;
    private Quaternion prevCameraRot;
    Vector3[] positions;
    [Header("Buffers")]
    private ComputeBuffer _PositionsBufferLOD0;
    private ComputeBuffer _PositionsBufferLOD1;
    private ComputeBuffer _PositionsBufferLODAppend0;
    private ComputeBuffer _PositionsBufferLODAppend1;
    private ComputeBuffer _VertexBuffer;
    private ComputeBuffer _NormalBuffer;
    private ComputeBuffer _UVBuffer;
    private GraphicsBuffer _IndexBuffer;

    private ComputeBuffer sphereArgsBuffer;
    private ComputeBuffer billboardArgsBuffer;
    private ComputeBuffer viewFrustumPlanesBuffer;
    private ComputeBuffer sphereGeneratorDispatchArgsBuffer;
    private ComputeBuffer positionCalculatorDispatchArgsBuffer;
    private void Awake()
    {
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_GalacticCentre, _GalacticHaloRadius);
    }

    private void SetPositionCalculatorData()
    {
        positionCalculator.SetVector("_GalacticCentre", _GalacticCentre);
        _MinEccentricity = UniGenerator.currentGalaxyProperties.minEccentricity;
        positionCalculator.SetFloat("_MinEccentricity", _MinEccentricity);
        _MaxEccentricity = UniGenerator.currentGalaxyProperties.maxEccentricity;
        positionCalculator.SetFloat("_MaxEccentricity", _MaxEccentricity);
        _GalacticDiskRadius = UniGenerator.currentGalaxyProperties.galacticDiskRadius;
        positionCalculator.SetFloat("_GalacticDiskRadius", _GalacticDiskRadius);
        positionCalculator.SetFloat("_GalacticHaloRadius", _GalacticHaloRadius);
        _GalacticBulgeRadius = UniGenerator.currentGalaxyProperties.galacticBulgeRadius;
        positionCalculator.SetFloat("_GalacticBulgeRadius", _GalacticBulgeRadius);
        _AngularOffsetMultiplier = UniGenerator.currentGalaxyProperties.angularOffsetMultiplier;
        positionCalculator.SetFloat("_AngularOffsetMultiplier",_AngularOffsetMultiplier);
        numInstances = UniGenerator.currentGalaxyProperties.starCount;
        positionCalculator.SetInt("_NumParticles", numInstances);
        positionCalculator.SetFloat("_time", Time.time);
        positionCalculator.SetFloat("_LODSwitchDist", LODSwitchDist);
        positionCalculator.SetVector("_CameraPosition", Camera.main.transform.position);
        positionCalculator.SetFloat("_TimeStep", _TimeStep);
        positionCalculator.SetVector("_StandardColour", _StandardColour);
        positionCalculator.SetVector("_H2RegionColour", _H2RegionColour);

    }
    private void SetPositionCalculatorData2() 
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
        positionCalculator.SetFloat("_TimeStep", _TimeStep);
        positionCalculator.SetVector("_StandardColour", _StandardColour);
        positionCalculator.SetVector("_H2RegionColour", _H2RegionColour);
        
    }

    void Start()
    {
        //Create vertex and index buffers 
        Application.targetFrameRate = 300;
        int numVertsPerInstance = Resolution * Resolution * 4 * 6; //Plane of verts made up of groups of quads. 1 plane for each of the 6 faces of a cube
        int numIndicesPerInstance = 6 * 6 * Resolution * Resolution; //indicesPerTriangle * trianglesPerQuad * 6 faces of cube * resolution^2

        int starCount = UniGenerator.currentGalaxyProperties == null ? numInstances : UniGenerator.currentGalaxyProperties.starCount;

        _PositionsBufferLOD0 = new ComputeBuffer(numInstances, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ThreadIdentifier)), ComputeBufferType.Structured);
        _PositionsBufferLOD1 = new ComputeBuffer(numInstances, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ThreadIdentifier)), ComputeBufferType.Structured);
        _PositionsBufferLODAppend0 = new ComputeBuffer(numInstances, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ThreadIdentifier)), ComputeBufferType.Append);
        _PositionsBufferLODAppend1 = new ComputeBuffer(numInstances, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ThreadIdentifier)), ComputeBufferType.Append);
        _VertexBuffer = new ComputeBuffer(numVertsPerInstance * starCount, sizeof(float) * 3, ComputeBufferType.Structured);
        _NormalBuffer = new ComputeBuffer(numVertsPerInstance * starCount, sizeof(float) * 3, ComputeBufferType.Structured);
        _UVBuffer = new ComputeBuffer(numVertsPerInstance * starCount, sizeof(float) * 2, ComputeBufferType.Structured);
        _IndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, numIndicesPerInstance * starCount, sizeof(uint));
        viewFrustumPlanesBuffer = new ComputeBuffer(6, sizeof(float) * 4, ComputeBufferType.Structured);
        //Bind buffers to material
        SphereGeneratorHandle = sphereGenerator.FindKernel("CSMain");
        positionCalculatorHandle = positionCalculator.FindKernel("CSMain");
        material[0].SetBuffer("_VertexBuffer", _VertexBuffer);
        material[0].SetBuffer("_NormalBuffer", _NormalBuffer);
        material[0].SetBuffer("_UVBuffer", _UVBuffer);
        material[0].SetColor("_EmissionColour", _EmissionColour);
        material[0].SetBuffer("_PositionsLOD0", _PositionsBufferLODAppend0);

        //bind relevant buffers to positionCalculator computeShader.
        positionCalculator.SetBuffer(positionCalculatorHandle, "_ViewFrustumPlanes", viewFrustumPlanesBuffer);
        positionCalculator.SetBuffer(positionCalculatorHandle, "_PositionsLOD0", _PositionsBufferLOD0);
        positionCalculator.SetBuffer(positionCalculatorHandle, "_PositionsLOD1", _PositionsBufferLOD1);
        positionCalculator.SetBuffer(positionCalculatorHandle, "_PositionsLODAppend0", _PositionsBufferLODAppend0);
        positionCalculator.SetBuffer(positionCalculatorHandle, "_PositionsLODAppend1", _PositionsBufferLODAppend1);

        material[1].SetBuffer("_PositionsLOD1", _PositionsBufferLODAppend1);
        material[1].SetColor("_EmissionColour", _EmissionColour);
        material[1].SetTexture("_MainTex", billboardTexture);

        //calculate thread group sizes 
        positionCalculator.GetKernelThreadGroupSizes(positionCalculatorHandle, out threadGroupSizeX, out _, out _);
        positionGroupSizeX = Mathf.CeilToInt((float)numInstances / (float)threadGroupSizeX);


        uint threadGroupGeneratorX;
        //sphereGenerator.GetKernelThreadGroupSizes(SphereGeneratorHandle, out threadGroupGeneratorX, out _, out _);
        //sphereGeneratorGroupSizeX = Mathf.CeilToInt(numVertsPerInstance / threadGroupGeneratorX);
        //Bind relevant buffers to Sphere Generator compute shader and set variables needed to generate the spheres.
        sphereGenerator.SetBuffer(SphereGeneratorHandle, "_VertexBuffer", _VertexBuffer);
        sphereGenerator.SetBuffer(SphereGeneratorHandle, "_NormalBuffer", _NormalBuffer);
        sphereGenerator.SetBuffer(SphereGeneratorHandle, "_UVBuffer", _UVBuffer);
        sphereGenerator.SetBuffer(SphereGeneratorHandle, "_Positions", _PositionsBufferLODAppend0);
        sphereGenerator.SetBuffer(SphereGeneratorHandle, "_IndexBuffer", _IndexBuffer);
        sphereGenerator.SetInt("_Resolution", Resolution);
        sphereGenerator.SetInt("_NumVertsPerInstance", numVertsPerInstance);


        //Additional arguments to DrawProceduralIndirect: bounds and the arguments buffer
        bounds = new Bounds(Vector3.zero, new Vector3(_GalacticHaloRadius, _GalacticHaloRadius, _GalacticHaloRadius));

        sphereArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
        billboardArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 4, ComputeBufferType.IndirectArguments);
        sphereGeneratorDispatchArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 3,ComputeBufferType.IndirectArguments);
        positionCalculatorDispatchArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 3, ComputeBufferType.IndirectArguments);
        
        // index 0 : index count per instance
        // index 1 : instance count
        // index 2 : start vertex location
        // index 3 : start instance location
        // index 4 : start instance location
        sphereArgsBuffer.SetData(new uint[] { (uint)numIndicesPerInstance, (uint)numInstances, 0u, 0u, 0u });
        billboardArgsBuffer.SetData(new uint[] { (uint)1, (uint)numInstances, 0u, 0u });
        sphereGeneratorDispatchArgsBuffer.SetData(new uint[] { 10u, 10u, 1u });
        positionCalculatorDispatchArgsBuffer.SetData(new uint[] { (uint)positionGroupSizeX, 1u, 1u });

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        Debug.Log(viewFrustumPlanesBuffer);
        viewFrustumPlanesBuffer.SetData(planes);
        prevCameraPos = Camera.main.transform.position;
        prevCameraRot = Camera.main.transform.rotation;
    }
    void Start2()
    {
        //Create vertex and index buffers 
        int numVertsPerInstance = Resolution * Resolution * 4 * 6; //Plane of verts made up of groups of quads. 1 plane for each of the 6 faces of a cube
        int numIndicesPerInstance = 6 * 6 * Resolution * Resolution; //indicesPerTriangle * trianglesPerQuad * 6 faces of cube * resolution^2


        _PositionsBufferLOD0 = new ComputeBuffer(numInstances, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ThreadIdentifier)), ComputeBufferType.Structured);
        _PositionsBufferLOD1 = new ComputeBuffer(numInstances, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ThreadIdentifier)), ComputeBufferType.Structured);
        _PositionsBufferLODAppend0 = new ComputeBuffer(numInstances, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ThreadIdentifier)), ComputeBufferType.Append);
        _PositionsBufferLODAppend1 = new ComputeBuffer(numInstances, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ThreadIdentifier)), ComputeBufferType.Append);
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
        material[0].SetBuffer("_PositionsLOD0", _PositionsBufferLOD0);

        //bind relevant buffers to positionCalculator computeShader.
        positionCalculator.SetBuffer(positionCalculatorHandle, "_ViewFrustumPlanes", viewFrustumPlanesBuffer);
        positionCalculator.SetBuffer(positionCalculatorHandle, "_PositionsLOD0", _PositionsBufferLOD0);
        positionCalculator.SetBuffer(positionCalculatorHandle, "_PositionsLOD1", _PositionsBufferLOD1);
        positionCalculator.SetBuffer(positionCalculatorHandle, "_PositionsLODAppend0", _PositionsBufferLODAppend0);
        positionCalculator.SetBuffer(positionCalculatorHandle, "_PositionsLODAppend1", _PositionsBufferLODAppend1);
        material[1].SetBuffer("_PositionsLOD1", _PositionsBufferLODAppend1);
        material[1].SetColor("_EmissionColour", _EmissionColour);
        material[1].SetTexture("_MainTex", billboardTexture);

        //calculate thread group sizes 

        uint threadGroupSizeX;
        positionCalculator.GetKernelThreadGroupSizes(positionCalculatorHandle, out threadGroupSizeX, out _, out _);
        positionGroupSizeX = Mathf.CeilToInt((float)numInstances /(float) threadGroupSizeX);

        //sphereGenerator.GetKernelThreadGroupSizes(SphereGeneratorHandle, out threadGroupGeneratorX, out _, out _);
        //sphereGeneratorGroupSizeX = Mathf.CeilToInt(numVertsPerInstance / threadGroupGeneratorX);
        //Bind relevant buffers to Sphere Generator compute shader and set variables needed to generate the spheres.
        sphereGenerator.SetBuffer(SphereGeneratorHandle, "_VertexBuffer", _VertexBuffer);
        sphereGenerator.SetBuffer(SphereGeneratorHandle, "_NormalBuffer", _NormalBuffer);
        sphereGenerator.SetBuffer(SphereGeneratorHandle, "_UVBuffer", _UVBuffer);
        sphereGenerator.SetBuffer(SphereGeneratorHandle, "_Positions", _PositionsBufferLODAppend0); 
        sphereGenerator.SetBuffer(SphereGeneratorHandle, "_IndexBuffer", _IndexBuffer);
        sphereGenerator.SetInt("_Resolution", Resolution);
        sphereGenerator.SetInt("_NumVertsPerInstance", numVertsPerInstance);


        //Additional arguments to DrawProceduralIndirect: bounds and the arguments buffer
        bounds = new Bounds(Vector3.zero, new Vector3(_GalacticHaloRadius, _GalacticHaloRadius, _GalacticHaloRadius));
        
        sphereArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
        billboardArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 4, ComputeBufferType.IndirectArguments);
        // index 0 : index count per instance
        // index 1 : instance count
        // index 2 : start vertex location
        // index 3 : start instance location
        // index 4 : start instance location
        sphereArgsBuffer.SetData(new uint[] { (uint) numIndicesPerInstance, (uint) numInstances, 0u, 0u, 0u });
        billboardArgsBuffer.SetData(new uint[] { (uint)1, (uint)numInstances, 0u, 0u });

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        viewFrustumPlanesBuffer.SetData(planes);
        prevCameraPos = Camera.main.transform.position;
        prevCameraRot = Camera.main.transform.rotation;
    }

    private void Update()
    {
        if (numInstances != prevNumInstances) 
        {
            positionGroupSizeX = Mathf.CeilToInt((float)numInstances / (float)threadGroupSizeX);
            positionCalculatorDispatchArgsBuffer.SetData(new uint[] { (uint)positionGroupSizeX, 1u, 1u });
        }
        _PositionsBufferLODAppend0.SetCounterValue(0);
        _PositionsBufferLODAppend1.SetCounterValue(0);
        material[1].SetColor("_EmissionColour", _EmissionColour);
        //_PositionsBufferLOD0.SetCounterValue(0);
        //_PositionsBufferLOD1.SetCounterValue(0);
        //only upload view frustum plane data to gpu if it has changed.
        if (prevCameraPos != Camera.main.transform.position || prevCameraRot != Camera.main.transform.rotation)
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            Debug.Log(planes);
            Debug.Log(viewFrustumPlanesBuffer);
            viewFrustumPlanesBuffer.SetData(planes);
        }

        int numRows = Resolution;
        positionCalculator.DispatchIndirect(positionCalculatorHandle, positionCalculatorDispatchArgsBuffer);
        //sphereGenerator.Dispatch(SphereGeneratorHandle, 10, 10, 1);
        sphereGenerator.DispatchIndirect(SphereGeneratorHandle, sphereGeneratorDispatchArgsBuffer);
        sphereGenerator.SetInt("_Resolution", Resolution);
        if (UniGenerator.currentGalaxyProperties != null)
        {
            SetPositionCalculatorData();
        }
        else
        {
            SetPositionCalculatorData2();
        }

        ComputeBuffer.CopyCount(_PositionsBufferLODAppend0, sphereArgsBuffer, sizeof(uint));
        ComputeBuffer.CopyCount(_PositionsBufferLODAppend1, billboardArgsBuffer, sizeof(uint));

        if (Input.GetKeyDown(KeyCode.Space)) 
        {
            int[] args = new int[4];
            billboardArgsBuffer.GetData(args);
            for(int i = 0; i < args.Length; i++) 
            {
                Debug.Log($"Billboard args: {i}.) {args[i]}");
            }
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            int[] args = new int[5];
            sphereArgsBuffer.GetData(args);
            for (int i = 0; i < args.Length; i++)
            {
                Debug.Log($"Sphere args{i}.) {args[i]}");
            }
        }
        Graphics.DrawProceduralIndirect(material[0], bounds, MeshTopology.Triangles, _IndexBuffer, sphereArgsBuffer);//Spheres
        Graphics.DrawProceduralIndirect(material[1], bounds, MeshTopology.Points, billboardArgsBuffer);

        prevCameraPos = Camera.main.transform.position;
        prevCameraRot = Camera.main.transform.rotation;
        prevNumInstances = numInstances;
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