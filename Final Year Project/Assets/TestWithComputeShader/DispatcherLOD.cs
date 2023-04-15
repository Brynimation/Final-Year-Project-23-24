using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.HableCurve;

public class DispatcherLOD : MonoBehaviour
{
    public Texture2D billboardTexture;
    public Vector3 _GalacticCentre;
    public float _MinEccentricity;
    public float _MaxEccentricity;
    public float _GalacticDiskRadius;
    public float _GalacticHaloRadius;
    public float _GalacticBulgeRadius;
    public float _AngularOffsetMultiplier;
    public int _NumParticles;
    public float maxStarSize;
    public Vector3 _CameraPosition;
    public float LODSwitchDist;
    //public float starScale;
    ComputeBuffer _PositionsLOD0;
    ComputeBuffer _PositionsLOD1;

    public Mesh mesh;
    public Mesh mesh2;
    public Material[] material;
    public ComputeShader positionCalculator;
    public ComputeShader clearBufferShader;

    const int POSITION_BUFFER_STRIDE = sizeof(float) * 3;
    ComputeBuffer indirectArgsBufferLOD0;
    ComputeBuffer indirectArgsBufferLOD1;
    uint[] indirectArgs0 = new uint[5] { 0, 0, 0, 0, 0 };
    uint[] indirectArgs1 = new uint[4] { 0, 0, 0, 0};
    int kernelHandle;
    int groupSizeX;
    Bounds bounds;
    void Start()
    {
        Application.targetFrameRate = 300;
          //LOD0
        // _PositionsLOD0 = new ComputeBuffer(_NumParticles, POSITION_BUFFER_STRIDE, ComputeBufferType.Append);
          //LOD1
         //_PositionsLOD1 = new ComputeBuffer(_NumParticles, POSITION_BUFFER_STRIDE, ComputeBufferType.Append);
         
        _PositionsLOD0 = new ComputeBuffer(_NumParticles, POSITION_BUFFER_STRIDE, ComputeBufferType.Default);
        //LOD1
        _PositionsLOD1 = new ComputeBuffer(_NumParticles, POSITION_BUFFER_STRIDE, ComputeBufferType.Default);
        bounds = new Bounds(_GalacticCentre, Vector3.one * _GalacticBulgeRadius);
        
        //LOD0
        indirectArgsBufferLOD0 = new ComputeBuffer(1, 5 * sizeof(int), ComputeBufferType.IndirectArguments);
        indirectArgs0[0] = (uint) mesh.GetIndexCount(0); //vertex count per instance 
        Debug.Log(indirectArgs0[0]);
        indirectArgs0[1] = (uint)_NumParticles; //no instances 
        indirectArgsBufferLOD0.SetData(indirectArgs0);
        //LOD1 
        indirectArgsBufferLOD1 = new ComputeBuffer(1, 4 * sizeof(int), ComputeBufferType.IndirectArguments);
        indirectArgs1[0] = (uint)1; //vertex count per instance 
        indirectArgs1[1] = (uint)_NumParticles; //no instances 
        indirectArgsBufferLOD1.SetData(indirectArgs1);
        kernelHandle = positionCalculator.FindKernel("CSMain");
        uint threadGroupSizeX;
        positionCalculator.GetKernelThreadGroupSizes(kernelHandle, out threadGroupSizeX, out _, out _);
        groupSizeX = Mathf.CeilToInt((float)_NumParticles / threadGroupSizeX);

        //LOD0
        positionCalculator.SetBuffer(kernelHandle, "_PositionsLOD0", _PositionsLOD0);
        material[0].SetBuffer("_PositionsLOD0", _PositionsLOD0);
        //LOD1
        positionCalculator.SetBuffer(kernelHandle, "_PositionsLOD1", _PositionsLOD1);
        material[1].SetBuffer("_PositionsLOD1", _PositionsLOD1);
        material[1].SetTexture("_MainTex", billboardTexture);

        positionCalculator.SetFloat("_LODSwitchDist", LODSwitchDist);
        positionCalculator.SetVector("_GalacticCentre", _GalacticCentre);
        positionCalculator.SetFloat("_MinEccentricity", _MinEccentricity);
        positionCalculator.SetFloat("_MaxEccentricity", _MaxEccentricity);
        positionCalculator.SetFloat("_GalacticDiskRadius", _GalacticDiskRadius);
        positionCalculator.SetFloat("_GalacticHaloRadius", _GalacticHaloRadius);
        positionCalculator.SetFloat("_GalacticBulgeRadius", _GalacticBulgeRadius);
        positionCalculator.SetFloat("_AngularOffsetMultiplier", _AngularOffsetMultiplier);
        positionCalculator.SetInt("_NumParticles", _NumParticles);
    }

    // Update is called once per frame
    void Update()
    {
        material[0].SetFloat("_MaxStarSize", maxStarSize);
        material[1].SetFloat("_MaxStarSize", maxStarSize);

        positionCalculator.SetVector("_CameraPosition", Camera.main.transform.position);
        positionCalculator.SetVector("_GalacticCentre", _GalacticCentre);
        positionCalculator.SetFloat("_MinEccentricity", _MinEccentricity);
        positionCalculator.SetFloat("_MaxEccentricity", _MaxEccentricity);
        positionCalculator.SetFloat("_GalacticDiskRadius", _GalacticDiskRadius);
        positionCalculator.SetFloat("_GalacticHaloRadius", _GalacticHaloRadius);
        positionCalculator.SetFloat("_GalacticBulgeRadius", _GalacticBulgeRadius);
        positionCalculator.SetFloat("_AngularOffsetMultiplier", _AngularOffsetMultiplier);
        positionCalculator.SetInt("_NumParticles", _NumParticles);
        positionCalculator.SetFloat("_time", Time.time);
        positionCalculator.Dispatch(kernelHandle, groupSizeX, 1, 1);

        //LOD0
       //ComputeBuffer.CopyCount(_PositionsLOD0, indirectArgsBufferLOD0, sizeof(int));
        //Graphics.DrawMeshInstancedIndirect(mesh, 0, material[0], bounds, indirectArgsBufferLOD0);
        //_PositionsLOD0.SetCounterValue(0);
        //LOD1
        //ComputeBuffer.CopyCount(_PositionsLOD1, indirectArgsBufferLOD1, sizeof(int));
        //Graphics.DrawMeshInstancedIndirect(mesh2, 0, material[1], bounds, indirectArgsBufferLOD1);
        //_PositionsLOD1.SetCounterValue(0);
        Graphics.DrawProceduralIndirect(material[1], bounds, MeshTopology.Points, indirectArgsBufferLOD1);
        //Graphics.DrawMeshInstancedIndirect(mesh2, 0, material[1], bounds, indirectArgsBufferLOD1);
        //Graphics.DrawMeshInstancedIndirect(mesh[1], 0, material[1], bounds, indirectArgsBufferLOD1);
        
    }
}

