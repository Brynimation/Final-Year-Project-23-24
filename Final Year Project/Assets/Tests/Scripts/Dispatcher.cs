using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.HableCurve;

public class Dispatcher : MonoBehaviour
{

    public Vector3 _GalacticCentre;
    public float _MinEccentricity;
    public float _MaxEccentricity;
    public float _GalacticDiskRadius;
    public float _GalacticHaloRadius;
    public float _GalacticBulgeRadius;
    public float _AngularOffsetMultiplier;
    public int _NumParticles;
    public float maxStarSize;
    ComputeBuffer _Positions;

    public Mesh mesh;
    public Material material;
    public ComputeShader positionCalculator;
    
    const int POSITION_BUFFER_STRIDE = sizeof(float) * 3;
    ComputeBuffer indirectArgsBuffer;
    uint[] indirectArgs = new uint[5] { 0, 0, 0, 0, 0 };
    int kernelHandle;
    int groupSizeX;
    Bounds bounds;
    void Start()
    {
        
        _Positions = new ComputeBuffer(_NumParticles, POSITION_BUFFER_STRIDE);
         bounds = new Bounds(_GalacticCentre, Vector3.one * _GalacticHaloRadius);
        indirectArgsBuffer = new ComputeBuffer(1, 5* sizeof(int), ComputeBufferType.IndirectArguments);
        kernelHandle = positionCalculator.FindKernel("CSMain");
        if (mesh != null) 
        {
            indirectArgs[0] = mesh.GetIndexCount(0);
            indirectArgs[1] = (uint) _NumParticles;
        }
        indirectArgsBuffer.SetData(indirectArgs);
        uint threadGroupSizeX;
        positionCalculator.GetKernelThreadGroupSizes(kernelHandle, out threadGroupSizeX, out _, out _);
        Debug.Log(_NumParticles);
        groupSizeX = Mathf.CeilToInt((float)_NumParticles / threadGroupSizeX);
        Debug.Log(threadGroupSizeX);
        positionCalculator.SetBuffer(kernelHandle, "_Positions", _Positions);
        material.SetBuffer("_Positions", _Positions);
        material.SetFloat("_MaxStarSize", maxStarSize);
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
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, indirectArgsBuffer);
    }
}
