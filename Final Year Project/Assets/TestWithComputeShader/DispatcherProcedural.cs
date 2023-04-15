using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DispatcherProcedural : MonoBehaviour
{
    [Header("Shaders")]
    public Material material;
    public ComputeShader sphereGenerator;
    public ComputeShader positionCalculator;

    [Header("Sphere Generation")]
    [Range(1, 10)]
    public int Resolution = 10;
    public int numInstances = 10000;
    [Header("Position Calculation")]
    public Vector3 _GalacticCentre;
    public float _MinEccentricity;
    public float _MaxEccentricity;
    public float _GalacticDiskRadius;
    public float _GalacticHaloRadius;
    public float _GalacticBulgeRadius;
    public float _AngularOffsetMultiplier;


    private int SphereGeneratorHandle;
    private int positionCalculatorHandle;
    private ComputeBuffer mArgBuffer;
    private Bounds bounds;
    Vector3[] positions;
    private ComputeBuffer _PositionsBuffer;
    private ComputeBuffer _VertexBuffer;
    private GraphicsBuffer _IndexBuffer;

    
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
    }
    void Start()
    {
        //Create vertex and index buffers 
        int numVertsPerInstance = Resolution * Resolution * 4 * 6; //Plane of verts made up of groups of quads. 1 plane for each of the 6 faces of a cube
        int numIndicesPerInstance = 6 * 6 * Resolution * Resolution; //indicesPerTriangle * trianglesPerQuad * 6 faces of cube * resolution^2

        _PositionsBuffer = new ComputeBuffer(numInstances, sizeof(float) * 3, ComputeBufferType.Structured);
        _VertexBuffer = new ComputeBuffer(numVertsPerInstance * numInstances, sizeof(float) * 3, ComputeBufferType.Structured);
        _IndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, numIndicesPerInstance * numInstances, sizeof(uint));

        //Bind buffers to material
        SphereGeneratorHandle = sphereGenerator.FindKernel("CSMain");
        material.SetBuffer("_VertexBuffer", _VertexBuffer);
        material.SetBuffer("_Positions", _PositionsBuffer);

        //bind relevant buffers to positionCalculator computeShader.
        positionCalculator.SetBuffer(SphereGeneratorHandle, "_Positions", _PositionsBuffer);
        uint threadGroupSizeX;
        positionCalculator.GetKernelThreadGroupSizes(positionCalculatorHandle, out threadGroupSizeX, out _, out _);

        //Bind relevant buffers to Sphere Generator compute shader and set variables needed to generate the spheres.
        sphereGenerator.SetBuffer(SphereGeneratorHandle, "_VertexBuffer", _VertexBuffer);
        sphereGenerator.SetBuffer(SphereGeneratorHandle, "_Positions", _PositionsBuffer);
        sphereGenerator.SetBuffer(SphereGeneratorHandle, "_IndexBuffer", _IndexBuffer);
        sphereGenerator.SetInt("_Resolution", Resolution);
        sphereGenerator.SetInt("_NumVertsPerInstance", numVertsPerInstance);


        //Additional arguments to DrawProceduralIndirect: bounds and the arguments buffer
        bounds = new Bounds(Vector3.zero, new Vector3(10000, 10000, 10000));
        
        mArgBuffer = new ComputeBuffer(1, sizeof(int) * 5, ComputeBufferType.IndirectArguments);
        // index-0 : index count per instance
        // index-1 : instance count
        // index-2 : start vertex location
        // index-3 : start instance location
        mArgBuffer.SetData(new int[] { numIndicesPerInstance, numInstances, 0, 0, 0 });
    }


    private void Update()
    {
        int numRows = Resolution;
        positionCalculator.Dispatch(positionCalculatorHandle, 1, 1, 1);
        sphereGenerator.Dispatch(SphereGeneratorHandle, Resolution, Resolution, 1);
        sphereGenerator.SetInt("_Resolution", Resolution);
        SetPositionCalculatorData();
        Graphics.DrawProceduralIndirect(material, bounds, MeshTopology.Triangles, _IndexBuffer, mArgBuffer);
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Vector3[] data = new Vector3[numInstances];
            _PositionsBuffer.GetData(data);
            Debug.Log("length: " +data.Length);
            foreach (Vector3 pos in data) 
            {
                Debug.Log(pos);
            }
        }
    }

    private void OnDestroy()
    {
        if (_VertexBuffer != null)
        {
            _VertexBuffer.Release();
            _VertexBuffer = null;
        }

        if (mArgBuffer != null)
        {
            mArgBuffer.Release();
            mArgBuffer = null;
        }

        if (_IndexBuffer != null)
        {
            _IndexBuffer.Release();
            _IndexBuffer = null;
        }
    }
}
