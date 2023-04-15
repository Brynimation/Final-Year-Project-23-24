using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NBodyDispatcher : MonoBehaviour
{
    public ComputeShader computeShader;
    public static ComputeBuffer positionsBuffer;
    public static ComputeBuffer velocitiesBuffer;
    public static ComputeBuffer massesBuffer;
    
    public static NBodyDispatcher Instance { get; private set;}

    public int numBodies;
    public int kernelIndex;
    public float timeStepMultiplier;


    public float turnFraction = 1.618f; //Golden ratio - prevents points lining up
    public float galaxyRadius;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
        positionsBuffer = new ComputeBuffer(numBodies, sizeof(float) * 3, ComputeBufferType.Default);
        velocitiesBuffer = new ComputeBuffer(numBodies, sizeof(float) * 3, ComputeBufferType.Default);
        massesBuffer = new ComputeBuffer(numBodies, sizeof(float), ComputeBufferType.Default);

        Shader.SetGlobalBuffer(Shader.PropertyToID("positions"), positionsBuffer);
        Shader.SetGlobalBuffer(Shader.PropertyToID("velocities"), velocitiesBuffer);
        Shader.SetGlobalBuffer(Shader.PropertyToID("masses"), massesBuffer);

        kernelIndex = computeShader.FindKernel("CSMain");
        //computeShader.SetBuffer(SphereGeneratorHandle, "positions", positionsBuffer);
        //computeShader.SetBuffer(SphereGeneratorHandle, "velocities", velocitiesBuffer);
        //computeShader.SetBuffer(SphereGeneratorHandle, "masses", massesBuffer);
        computeShader.SetInt("numBodies", numBodies);
        Debug.Log(Time.fixedDeltaTime * timeStepMultiplier);
        computeShader.SetFloat("timeStep", Time.fixedDeltaTime * timeStepMultiplier);

        Vector3[] posData = new Vector3[numBodies];
        Vector3[] velData = new Vector3[numBodies];
        float[] massData = new float[numBodies];

        //Distribute the points
        for(int i = 0; i < numBodies; i++) 
        {
            float r = (i / (numBodies - 1f)) * galaxyRadius;
            float theta = 2 * i * Mathf.PI * turnFraction;
            float x = r * Mathf.Cos(theta);
            float y = r * Mathf.Sin(theta);
            posData[i] = new Vector3(x, y, 0f);
            //For a vector (x, y), (y -x) is perpendicular to it. For circular motion, an object's velocity is tangential/perpendicular to the centripetal force acting on it,
            velData[i] = new Vector3(y * Time.fixedDeltaTime * timeStepMultiplier, -x * Time.fixedDeltaTime * timeStepMultiplier, 0f);
            massData[i] = 0.001f;
        }
        positionsBuffer.SetData(posData);
        velocitiesBuffer.SetData(velData);
        massesBuffer.SetData(massData);
        
    }

    // Update is called once per frame
    void Update()
    {
        computeShader.Dispatch(kernelIndex, 256, 1, 1);
    }
    private void OnDestroy()
    {
        positionsBuffer.Release();
        velocitiesBuffer.Release();
        massesBuffer.Release();
    }
}
