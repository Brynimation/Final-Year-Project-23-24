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
    public ComputeShader computeShader;
    
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
        kernelHandle = computeShader.FindKernel("CSMain");
        if (mesh != null) 
        {
            indirectArgs[0] = mesh.GetIndexCount(0);
            indirectArgs[1] = (uint) _NumParticles;
        }
        indirectArgsBuffer.SetData(indirectArgs);
        uint threadGroupSizeX;
        computeShader.GetKernelThreadGroupSizes(kernelHandle, out threadGroupSizeX, out _, out _);
        Debug.Log(_NumParticles);
        groupSizeX = Mathf.CeilToInt((float)_NumParticles / threadGroupSizeX);
        Debug.Log(threadGroupSizeX);
        computeShader.SetBuffer(kernelHandle, "_Positions", _Positions);
        material.SetBuffer("_Positions", _Positions);
        material.SetFloat("_MaxStarSize", maxStarSize);
        computeShader.SetVector("_GalacticCentre", _GalacticCentre);
        computeShader.SetFloat("_MinEccentricity", _MinEccentricity);
        computeShader.SetFloat("_MaxEccentricity", _MaxEccentricity);
        computeShader.SetFloat("_GalacticDiskRadius", _GalacticDiskRadius);
        computeShader.SetFloat("_GalacticHaloRadius", _GalacticHaloRadius);
        computeShader.SetFloat("_GalacticBulgeRadius", _GalacticBulgeRadius);
        computeShader.SetFloat("_AngularOffsetMultiplier", _AngularOffsetMultiplier);
        computeShader.SetInt("_NumParticles", _NumParticles);
    }

    // Update is called once per frame
    void Update()
    {
        computeShader.SetVector("_GalacticCentre", _GalacticCentre);
        computeShader.SetFloat("_MinEccentricity", _MinEccentricity);
        computeShader.SetFloat("_MaxEccentricity", _MaxEccentricity);
        computeShader.SetFloat("_GalacticDiskRadius", _GalacticDiskRadius);
        computeShader.SetFloat("_GalacticHaloRadius", _GalacticHaloRadius);
        computeShader.SetFloat("_GalacticBulgeRadius", _GalacticBulgeRadius);
        computeShader.SetFloat("_AngularOffsetMultiplier", _AngularOffsetMultiplier);
        computeShader.SetInt("_NumParticles", _NumParticles);
        computeShader.SetFloat("_time", Time.time);
        computeShader.Dispatch(kernelHandle, groupSizeX, 1, 1);
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, indirectArgsBuffer);
    }
}
