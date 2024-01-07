using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SolarSystemCreator : MonoBehaviour
{

    public struct SolarSystem
    {
        public Vector3 starPosition;
        public float starRadius;
        public SolarSystem(Vector3 pos, float rad) 
        {
            starPosition = pos;
            starRadius = rad;
        }
    
    }
    public Mesh starMesh;
    public int numStars;
    public ComputeBuffer solarSystemBuffer;
    public ComputeBuffer solarSystemArgsBuffer;
    public SolarSystem[] solarSystems;
    public Material starMaterial;
    public Bounds bounds;

    void Start()
    {
        solarSystems = new SolarSystem[numStars];
        solarSystemBuffer = new ComputeBuffer(numStars, System.Runtime.InteropServices.Marshal.SizeOf(typeof(SolarSystem)), ComputeBufferType.Structured);
        solarSystemArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
        solarSystemArgsBuffer.SetData(new uint[] { (uint)starMesh.GetIndexCount(0), (uint) numStars, 0u, 0u, 0u });
        for (int i = 0; i < numStars; i++) 
        {
            solarSystems[i] = new SolarSystem(new Vector3(Random.Range(0, 100), Random.Range(0, 100), Random.Range(0, 100)), Random.Range(0, 10));
        }
        solarSystemBuffer.SetData(solarSystems);
        starMaterial.SetBuffer("_SolarSystems", solarSystemBuffer);
        bounds = new Bounds(Vector3.zero, Vector3.one * 10000);
    }

    // Update is called once per frame
    void Update()
    {
        Graphics.DrawMeshInstancedIndirect(starMesh, 0, starMaterial, bounds, solarSystemArgsBuffer);

        SolarSystem[] ss = new SolarSystem[numStars];
        if (Input.GetKeyDown(KeyCode.Q))
        {
            solarSystemBuffer.GetData(ss);
            foreach (var p in ss)
            {
                Debug.Log(p.starPosition);
            }
        }
    }
}
