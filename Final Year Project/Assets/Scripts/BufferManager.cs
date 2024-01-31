using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;
using System.Linq.Expressions;


public struct Star 
{
    public Vector3 starPosition;
    public float starRadius;
    public float starMass;
    public float starLuminosity;
    public Color starColour;
}
public struct SolarSystem
{
    public Star star;
    public int planetCount;
    public float fade;
}

public struct GalaxyProperties
{
    public MeshProperties mp;
    public int numParticles;
    public float minEccentricity;
    public float maxEccentricity;
    public float galacticDiskRadius;
    public float galacticHaloRadius;
    public float galacticBulgeRadius;
    public float angularOffsetMultiplier;
}
public struct MeshProperties
{
    public Matrix4x4 mat;
    public float scale;
    public Vector3 position;
    public Color colour;
    public float fade;
    public int lodLevel;
}
public struct ChunkIdentifier
{
    public int chunksInViewDist;
    public int chunkSize;
    public int chunkType;
    public Vector3 pos;

    public ChunkIdentifier(int chunksInViewDist, int chunkSize, int chunkType, Vector3 pos)
    {
        this.chunksInViewDist = chunksInViewDist;
        this.chunkSize = chunkSize;
        this.chunkType = chunkType;
        this.pos = pos;
    }
}

public struct TriggerChunkIdentifier 
{
    public ChunkIdentifier cid;
    public Vector3 cameraForward;
    public uint entered;
    public TriggerChunkIdentifier(ChunkIdentifier cid, Vector3 forward) 
    {
        this.cid = cid;
        this.cameraForward = forward;
        this.entered = 0u;
    }
}
public struct PlanetTerrainProperties 
{
    float roughness;
    float baseRoughness;
    float persistence;
    float minVal;
    float noiseStrength;
    Vector3 noiseCentre;
    int octaves;
}
public struct Planet
{
    Vector3 position;
    float mass;
    float radius;
    Color colour;
    float rotationSpeed;
    Vector3 rotationAxis;
    Star primaryBody;
    PlanetTerrainProperties properties;
}
public class BufferManager : MonoBehaviour
{
    public int numPositions;
    public int xSize;
    public int ySize;
    public int zSize;
    public Color _EmissionColour;
    public Color _EmissionColour2;



    //General
    public float timeStep;
    public ComputeShader positionCalculator;
    public float lodSwitchBackDist;
    public float dontSpawnRadius;
    ComputeBuffer viewFrustumPlanesBuffer;
    ComputeBuffer viewFrustumPlanesBufferAtTrigger;
    ComputeBuffer triggerBuffer;
    ComputeBuffer triggerArgsBuffer;
    ComputeBuffer chunkOffsetBuffer;
    public Material triggerMaterial;
    public Mesh sphereMesh;


    Ray[] triggerRays;
    bool drawRays;

    //Big galaxy
    public DispatcherProcedural dispatcherProcedural;
    public ComputeShader galaxyPositioner;
    public float galaxyLodSwitchDist;
    public float galaxyFadeDist;
    public Vector2 minMaxMinimumEccentricity;
    public Vector2 minMaxMaximumEccentricity;

    public Vector2 minMaxAngularOffsetMultiplier;

    public Vector2 minMaxHaloRadius;
    public Vector2 minMaxBulgeRadius;
    public Vector2 minMaxDiskRadius;

    public int[] minMaxNumParticles;

    //Solar systems
    public ComputeShader sphereGeneratorPrefab;
    public ComputeShader starSphereGenerator;
    public ComputeShader solarSystemCreator;
    public ComputeBuffer solarSystemBuffer;
    public ComputeBuffer solarSystemArgsBuffer;
    public ComputeBuffer solarSystemBufferCount;
    public ComputeBuffer solarSystemBufferCountAgain;
    public float solarSystemFadeDist;
    public float solarSystemSwitchDist;
    public Mesh starMesh;
    public Material starMaterial;
    public float cellSize = 0.1f;
    public float lowLodBorderWidth = 0.0f;
    public float highLodBorderWidth = 1.0f;
    public int starResolution;
    public float starMaxWobbleMagnitude;
    public float minLuminosity;
    public float maxLuminosity;
    public float minWavelength;
    public float maxWavelength;
    public float minRadius;
    public float maxRadius;
    public Color[] colours;
    public float[] floatColours;
    private ComputeBuffer starVertexBuffer;
    private ComputeBuffer starNormalBuffer;
    private ComputeBuffer starUVBuffer;
    private GraphicsBuffer starIndexBuffer;
    private ComputeBuffer starSphereGeneratorDispatchArgsBuffer;

    //planets
    public ComputeShader planetSphereGenerator;
    public ComputeBuffer planetsBuffer;
    public ComputeBuffer planetsArgsBuffer;
    public Transform playerPosition;
    public Material planetMaterial;
    public Mesh planetMesh;
    public int planetResolution;
    private ComputeBuffer planetVertexBuffer;
    private ComputeBuffer planetNormalBuffer;
    private ComputeBuffer planetUVBuffer;
    private GraphicsBuffer planetIndexBuffer;
    private ComputeBuffer planetSphereArgsBuffer;
    private ComputeBuffer planetSphereGeneratorDispatchArgsBuffer;

    public int chunkSize;
    public int renderDistance;
    public float lodSwitchDist;

    int chunksVisibleInViewDist;

    public Shader shader;
    public Material material;
    ComputeBuffer positionsBuffer;
    ComputeBuffer argsBuffer;

    public Material material2;
    ComputeBuffer positionsBuffer2;
    ComputeBuffer argsBuffer2;

    public Material material3;
    ComputeBuffer positionsBuffer3;
    ComputeBuffer argsBuffer3;

    public Material material4;
    ComputeBuffer positionsBuffer4;
    ComputeBuffer argsBuffer4;

    public Material material5;
    ComputeBuffer positionsBuffer5;
    ComputeBuffer argsBuffer5;

    ComputeBuffer debugPosBuffer;
    ComputeBuffer dispatchBuffer;
    ComputeBuffer chunksBuffer;
    ComputeBuffer chunksBufferPrevFrame;
    ChunkIdentifier[] chunksVisible;

    ComputeBuffer mainProperties;
    ComputeBuffer mainPropertiesCount;
    public Material skyboxMat;

    ThreadIdentifier[] positions;
    int mainKernelIndex;
    int galaxyPositionerIndex;
    int solarSystemCreatorIndex;
    int starSphereGeneratorIndex;
    int planetSphereGeneratorIndex;

    Bounds bounds;
    void Start()
    {
        triggerRays = new Ray[2];
        floatColours =  colours.SelectMany(c => new float[] { c.r, c.g, c.b, c.a }).ToArray();  //needed to pass to shader
        starSphereGenerator = Instantiate(sphereGeneratorPrefab);
        planetSphereGenerator = Instantiate(sphereGeneratorPrefab);
        //RenderSettings.skybox = skyboxMat;
        //material = new Material(shader);
        //material2 = new Material(shader);
        chunksVisibleInViewDist = Mathf.RoundToInt(renderDistance / chunkSize);
        chunksVisible = new ChunkIdentifier[1] { new ChunkIdentifier(chunksVisibleInViewDist, chunkSize, 4, Vector3.one * -1) };
        mainKernelIndex = positionCalculator.FindKernel("CSMainNew");
        galaxyPositionerIndex = galaxyPositioner.FindKernel("CSMain");
        solarSystemCreatorIndex = solarSystemCreator.FindKernel("CSMain");
        starSphereGeneratorIndex = starSphereGenerator.FindKernel("CSMain");
        planetSphereGeneratorIndex = planetSphereGenerator.FindKernel("CSMain");

        int maxInstanceCount = (int)Mathf.Pow(chunksVisibleInViewDist * 8 + 1, 3);

        int numVertsPerStar = starResolution * starResolution * 4 * 6; //Plane of verts made up of groups of quads. 1 plane for each of the 6 faces of a cube
        int numIndicesPerStar = 6 * 6 * starResolution * starResolution; //indicesPerTriangle * trianglesPerQuad * 6 faces of cube * resolution^2

        int numVertsPerPlanet = planetResolution * planetResolution * 4 * 6; //Plane of verts made up of groups of quads. 1 plane for each of the 6 faces of a cube
        int numIndicesPerPlanet = 6 * 6 * planetResolution * planetResolution; //indicesPerTriangle * trianglesPerQuad * 6 faces of cube * resolution^2

        positionsBuffer = new ComputeBuffer(maxInstanceCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(MeshProperties)), ComputeBufferType.Append);
        argsBuffer = new ComputeBuffer(1, sizeof(uint) * 4, ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(new uint[] { (uint)1, (uint)maxInstanceCount, 0u, 0u });

        positionsBuffer2 = new ComputeBuffer(maxInstanceCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(MeshProperties)), ComputeBufferType.Append);
        argsBuffer2 = new ComputeBuffer(1, sizeof(uint) * 4, ComputeBufferType.IndirectArguments);
        argsBuffer2.SetData(new uint[] { (uint)1, (uint)maxInstanceCount, 0u, 0u });

        positionsBuffer3 = new ComputeBuffer(maxInstanceCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(MeshProperties)), ComputeBufferType.Append);
        argsBuffer3 = new ComputeBuffer(1, sizeof(uint) * 4, ComputeBufferType.IndirectArguments);
        argsBuffer3.SetData(new uint[] { (uint)1, (uint)maxInstanceCount, 0u, 0u });

        solarSystemBuffer = new ComputeBuffer(maxInstanceCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(SolarSystem)), ComputeBufferType.Append);
        solarSystemBufferCount = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Structured);
        solarSystemBufferCountAgain = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Structured);
        solarSystemArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
        solarSystemArgsBuffer.SetData(new uint[] { (uint)numIndicesPerStar, (uint)maxInstanceCount, 0u, 0u, 0u });

        //Create star sphere generation buffers
        starVertexBuffer = new ComputeBuffer(maxInstanceCount * numVertsPerStar, sizeof(float) * 3, ComputeBufferType.Structured);
        starNormalBuffer = new ComputeBuffer(maxInstanceCount * numVertsPerStar, sizeof(float) * 3, ComputeBufferType.Structured);
        starUVBuffer = new ComputeBuffer(maxInstanceCount * numVertsPerStar, sizeof(float) * 2, ComputeBufferType.Structured);
        starIndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, numIndicesPerStar * maxInstanceCount, sizeof(uint));
        starSphereGeneratorDispatchArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 3, ComputeBufferType.IndirectArguments);
        starSphereGeneratorDispatchArgsBuffer.SetData(new uint[] { (uint)starResolution, 1u, 1u });

        planetsBuffer = new ComputeBuffer(maxInstanceCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Planet)), ComputeBufferType.Append);
        planetsArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
        planetsArgsBuffer.SetData(new uint[] { (uint)numIndicesPerPlanet, (uint)maxInstanceCount, 0u, 0u, 0u });

        //Create planet sphere generation buffers
        planetVertexBuffer = new ComputeBuffer(maxInstanceCount * numVertsPerPlanet, sizeof(float) * 3, ComputeBufferType.Structured);
        planetNormalBuffer = new ComputeBuffer(maxInstanceCount * numVertsPerPlanet, sizeof(float) * 3, ComputeBufferType.Structured);
        planetUVBuffer = new ComputeBuffer(maxInstanceCount * numVertsPerPlanet, sizeof(float) * 2, ComputeBufferType.Structured);
        planetIndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, numIndicesPerPlanet * maxInstanceCount, sizeof(uint));
        planetSphereGeneratorDispatchArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 3, ComputeBufferType.IndirectArguments);
        planetSphereGeneratorDispatchArgsBuffer.SetData(new uint[] { (uint)planetResolution, 1u, 1u });

        positionsBuffer4 = new ComputeBuffer(maxInstanceCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(GalaxyProperties)), ComputeBufferType.Append);
        argsBuffer4 = new ComputeBuffer(1, sizeof(uint) * 4, ComputeBufferType.IndirectArguments);
        argsBuffer4.SetData(new uint[] { (uint)1, (uint)maxInstanceCount, 0u, 0u });

        positionsBuffer5 = new ComputeBuffer(maxInstanceCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(MeshProperties)), ComputeBufferType.Append);
        argsBuffer5 = new ComputeBuffer(1, sizeof(uint) * 4, ComputeBufferType.IndirectArguments);
        argsBuffer5.SetData(new uint[] { (uint)1, (uint)maxInstanceCount, 0u, 0u });

        mainProperties = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(GalaxyProperties)), ComputeBufferType.Append);
        mainPropertiesCount = new ComputeBuffer(1, sizeof(uint));

        triggerArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
        triggerArgsBuffer.SetData(new uint[] { sphereMesh.GetIndexCount(0), 100u, 0u, 0u, 0u });
        chunksBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ChunkIdentifier)), ComputeBufferType.Structured);
        chunksBuffer.SetData(chunksVisible);
        chunksBufferPrevFrame = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ChunkIdentifier)), ComputeBufferType.Structured);
        triggerBuffer = new ComputeBuffer(2, System.Runtime.InteropServices.Marshal.SizeOf(typeof(TriggerChunkIdentifier)), ComputeBufferType.Structured);
        triggerBuffer.SetData(Enumerable.Repeat(new TriggerChunkIdentifier(chunksVisible[0], Camera.main.transform.forward), 2).ToArray());
        viewFrustumPlanesBuffer = new ComputeBuffer(6, sizeof(float) * 4, ComputeBufferType.Structured);
        viewFrustumPlanesBufferAtTrigger = new ComputeBuffer(6, sizeof(float) * 4, ComputeBufferType.Structured);

        chunkOffsetBuffer = new ComputeBuffer(1, sizeof(float) * 3, ComputeBufferType.Structured);
        chunksBufferPrevFrame.SetData(chunksVisible);
        debugPosBuffer = new ComputeBuffer(maxInstanceCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector3Int)), ComputeBufferType.Append);
        dispatchBuffer = new ComputeBuffer(3, sizeof(uint), ComputeBufferType.IndirectArguments);
        dispatchBuffer.SetData(new uint[3] { 1u, 1u, 1u });

        starMaterial.SetBuffer("_SolarSystems", solarSystemBuffer);
        starMaterial.SetBuffer("_VertexBuffer", starVertexBuffer);
        starMaterial.SetBuffer("_NormalBuffer", starNormalBuffer);
        starMaterial.SetBuffer("_UVBuffer", starUVBuffer);
        starMaterial.SetFloat("solarSystemSwitchDist", solarSystemSwitchDist);
        starMaterial.SetVector("playerPosition", playerPosition.position);
        starMaterial.SetFloat("minDist", (float)(solarSystemSwitchDist / 3.0f));
        starMaterial.SetFloat("_WobbleMagnitude", starMaxWobbleMagnitude);

        starSphereGenerator.SetBuffer(starSphereGeneratorIndex, "_VertexBuffer", starVertexBuffer);
        starSphereGenerator.SetBuffer(starSphereGeneratorIndex, "_NormalBuffer", starNormalBuffer);
        starSphereGenerator.SetBuffer(starSphereGeneratorIndex, "_UVBuffer", starUVBuffer);
        starSphereGenerator.SetBuffer(starSphereGeneratorIndex, "_IndexBuffer", starIndexBuffer);

        planetMaterial.SetBuffer("_Planets", planetsBuffer);
        planetMaterial.SetBuffer("_VertexBuffer", planetVertexBuffer);
        planetMaterial.SetBuffer("_NormalBuffer", planetNormalBuffer);
        planetMaterial.SetBuffer("_UVBuffer", planetUVBuffer);

        planetSphereGenerator.SetBuffer(planetSphereGeneratorIndex, "_VertexBuffer", planetVertexBuffer);
        planetSphereGenerator.SetBuffer(planetSphereGeneratorIndex, "_NormalBuffer", planetNormalBuffer);
        planetSphereGenerator.SetBuffer(planetSphereGeneratorIndex, "_UVBuffer", planetUVBuffer);
        planetSphereGenerator.SetBuffer(planetSphereGeneratorIndex, "_IndexBuffer", planetIndexBuffer);

        //positionCalculator.SetBuffer(mainKernelIndex, "_Properties", positionsBuffer);
        //positionCalculator.SetBuffer(mainKernelIndex, "_Properties2", positionsBuffer2);
        //positionCalculator.SetBuffer(mainKernelIndex, "_Properties3", positionsBuffer3);
        //positionCalculator.SetBuffer(mainKernelIndex, "_Properties4", positionsBuffer4);
        positionCalculator.SetBuffer(mainKernelIndex, "_Properties5", positionsBuffer5);
        positionCalculator.SetBuffer(mainKernelIndex, "_ChunkOffset", chunkOffsetBuffer);

        positionCalculator.SetBuffer(mainKernelIndex, "_ChunksBuffer", chunksBuffer);
        positionCalculator.SetBuffer(mainKernelIndex, "_ChunksBufferPrevFrame", chunksBufferPrevFrame);
        positionCalculator.SetBuffer(mainKernelIndex, "_TriggerBuffer", triggerBuffer);
        positionCalculator.SetBuffer(mainKernelIndex, "_ViewFrustumPlanes", viewFrustumPlanesBuffer);
        positionCalculator.SetBuffer(mainKernelIndex, "_ViewFrustumPlanesAtTrigger", viewFrustumPlanesBufferAtTrigger);
        positionCalculator.SetBuffer(mainKernelIndex, "_DispatchBuffer", dispatchBuffer);

        positionCalculator.SetInt("chunkSize", chunkSize);
        positionCalculator.SetFloat("dontSpawnRadius", dontSpawnRadius);
        positionCalculator.SetInt("renderDistance", renderDistance);
        positionCalculator.SetFloat("lodSwitchDist", lodSwitchDist);
        positionCalculator.SetFloat("galaxySwitchDist", galaxyLodSwitchDist);
        positionCalculator.SetFloat("solarSystemSwitchDist", solarSystemSwitchDist);
        positionCalculator.SetInt("chunksVisibleInViewDist", chunksVisibleInViewDist);
        positionCalculator.SetVector("playerPosition", playerPosition.position);
        positionCalculator.SetVector("playerRight", playerPosition.right);
        positionCalculator.SetVector("playerUp", playerPosition.up);
        positionCalculator.SetVector("playerForward", playerPosition.forward);
        positionCalculator.SetFloat("galaxyFadeDist", galaxyFadeDist);
        positionCalculator.SetFloats("colours", floatColours);
        positionCalculator.SetVector("minMaxMinimumEccentricity", minMaxMinimumEccentricity);
        positionCalculator.SetVector("minMaxMaximumEccentricity", minMaxMaximumEccentricity);
        positionCalculator.SetVector("minMaxAngularOffsetMultiplier", minMaxAngularOffsetMultiplier);
        positionCalculator.SetVector("minMaxHaloRadius", minMaxHaloRadius);
        positionCalculator.SetVector("minMaxBulgeRadius", minMaxBulgeRadius);
        positionCalculator.SetVector("minMaxDiskRadius", minMaxDiskRadius);
        positionCalculator.SetInts("minMaxNumParticles", minMaxNumParticles);

        galaxyPositioner.SetBuffer(galaxyPositionerIndex, "_ChunksBuffer", chunksBuffer);
        galaxyPositioner.SetBuffer(galaxyPositionerIndex, "_TriggerBuffer", triggerBuffer);
        galaxyPositioner.SetBuffer(galaxyPositionerIndex, "_ViewFrustumPlanes", viewFrustumPlanesBuffer);
        galaxyPositioner.SetBuffer(galaxyPositionerIndex, "_ViewFrustumPlanesAtTrigger", viewFrustumPlanesBufferAtTrigger);
        galaxyPositioner.SetBuffer(galaxyPositionerIndex, "_MainProperties", mainProperties);
        galaxyPositioner.SetBuffer(galaxyPositionerIndex, "_Properties4", positionsBuffer4);
        galaxyPositioner.SetFloat("lodSwitchDist", galaxyLodSwitchDist);
        galaxyPositioner.SetFloat("dontSpawnRadius", dontSpawnRadius);
        galaxyPositioner.SetFloat("galaxyFadeDist", galaxyFadeDist);
        galaxyPositioner.SetVector("playerPosition", playerPosition.position);
        galaxyPositioner.SetVector("minMaxMinimumEccentricity", minMaxMinimumEccentricity);
        galaxyPositioner.SetVector("minMaxMaximumEccentricity", minMaxMaximumEccentricity);
        galaxyPositioner.SetVector("minMaxAngularOffsetMultiplier", minMaxAngularOffsetMultiplier);
        galaxyPositioner.SetVector("minMaxHaloRadius", minMaxHaloRadius);
        galaxyPositioner.SetVector("minMaxBulgeRadius", minMaxBulgeRadius);
        galaxyPositioner.SetVector("minMaxDiskRadius", minMaxDiskRadius);
        galaxyPositioner.SetInts("minMaxNumParticles", minMaxNumParticles);
        /*float2 minMaxMinimumEccentricity;
        float2 minMaxMaximumEccentricity;

        float2 minMaxAngularOffsetMultiplier;

        float2 minMaxHaloRadius;
        float2 minMaxBulgeRadius;
        float2 minMaxDiskRadius;

        int2 minMaxNumParticles;*/

        dispatcherProcedural._MainPositionBuffer = mainProperties;
        dispatcherProcedural._MainPositionBufferCount = mainPropertiesCount;

        solarSystemCreator.SetFloat("solarSystemSwitchDist", solarSystemSwitchDist);
        solarSystemCreator.SetVector("playerPosition", playerPosition.position);
        solarSystemCreator.SetFloat("time", Time.time);
        solarSystemCreator.SetFloat("timeStep", timeStep);
        solarSystemCreator.SetFloats("colours", floatColours);
        solarSystemCreator.SetBuffer(solarSystemCreatorIndex, "_ChunksBuffer", chunksBuffer);
        solarSystemCreator.SetBuffer(solarSystemCreatorIndex, "_Properties3", positionsBuffer3);
        solarSystemCreator.SetBuffer(solarSystemCreatorIndex, "_SolarSystemCount", solarSystemBufferCount);
        solarSystemCreator.SetBuffer(solarSystemCreatorIndex, "_SolarSystems", solarSystemBuffer);
        solarSystemCreator.SetBuffer(solarSystemCreatorIndex, "_Planets", planetsBuffer);
        solarSystemCreator.SetBuffer(solarSystemCreatorIndex, "_ViewFrustumPlanes", viewFrustumPlanesBuffer);
        solarSystemCreator.SetBuffer(solarSystemCreatorIndex, "_ViewFrustumPlanesAtTrigger", viewFrustumPlanesBufferAtTrigger);
        solarSystemCreator.SetBuffer(solarSystemCreatorIndex, "_TriggerBuffer", triggerBuffer);
        solarSystemCreator.SetFloat("dontSpawnRadius", dontSpawnRadius);

        material.SetBuffer("_Properties", positionsBuffer);
        material2.SetBuffer("_Properties", positionsBuffer2);
        material3.SetBuffer("_Properties", positionsBuffer3);
        material4.SetBuffer("_Properties", positionsBuffer4);
        material5.SetBuffer("_Properties", positionsBuffer5);

        bounds = new Bounds(Vector3.zero, new Vector3(1000000, 1000000, 1000000));
        material.SetColor("_EmissionColour", _EmissionColour);
        material2.SetColor("_EmissionColour", _EmissionColour2);
        material3.SetFloat("_CellSize", cellSize);
        starMaterial.SetFloat("_CellSize", cellSize);

        material4.SetFloat("_TimeStep", timeStep);

        triggerMaterial.SetBuffer("_ChunksBuffer", chunksBuffer);
        triggerMaterial.SetBuffer("_TriggerBuffer", triggerBuffer);
    }

    // Update is called once per frame
    void Update()
    {
        //positionsBuffer.SetCounterValue(0);
        //positionsBuffer2.SetCounterValue(0);
        positionsBuffer3.SetCounterValue(0);
        positionsBuffer4.SetCounterValue(0);
        positionsBuffer5.SetCounterValue(0);
        solarSystemBuffer.SetCounterValue(0);
        planetsBuffer.SetCounterValue(0);
        mainProperties.SetCounterValue(0);

        debugPosBuffer.SetCounterValue(0);

        Vector3 camForward = Camera.main.transform.forward;
        //Debug.Log(camForward);
        positionCalculator.SetVector("playerPosition", playerPosition.position);
        positionCalculator.SetVector("cameraForward", camForward);
        positionCalculator.SetFloat("lodSwitchBackDist", lodSwitchBackDist);
        positionCalculator.SetFloat("minWavelength", minWavelength);
        positionCalculator.SetFloat("maxWavelength", maxWavelength);
        positionCalculator.SetFloat("minLuminosity", minLuminosity);
        positionCalculator.SetFloat("maxLuminosity", maxLuminosity);
        positionCalculator.SetFloat("minRadius", minRadius);
        positionCalculator.SetFloat("maxRadius", maxRadius);
        positionCalculator.SetBool("goBack", Input.GetKeyDown(KeyCode.K));
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        viewFrustumPlanesBuffer.SetData(planes);
        positionCalculator.DispatchIndirect(mainKernelIndex, dispatchBuffer);

        galaxyPositioner.SetVector("playerPosition", playerPosition.position);
        galaxyPositioner.SetVector("cameraForward", camForward);
        galaxyPositioner.DispatchIndirect(galaxyPositionerIndex, dispatchBuffer);

        solarSystemCreator.SetVector("playerPosition", playerPosition.position);
        solarSystemCreator.SetFloat("fadeDist", solarSystemFadeDist);
        solarSystemCreator.SetFloat("time", Time.time);
        solarSystemCreator.SetFloat("timeStep", timeStep);
        solarSystemCreator.SetFloat("minWavelength", minWavelength);
        solarSystemCreator.SetFloat("maxWavelength", maxWavelength);
        solarSystemCreator.SetFloat("minLuminosity", minLuminosity);
        solarSystemCreator.SetFloat("maxLuminosity", maxLuminosity);
        solarSystemCreator.SetFloat("minRadius", minRadius);
        solarSystemCreator.SetFloat("maxRadius", maxRadius);

        solarSystemCreator.DispatchIndirect(solarSystemCreatorIndex, dispatchBuffer);
        starMaterial.SetVector("playerPosition", playerPosition.position);
        starMaterial.SetFloat("_WobbleMagnitude", starMaxWobbleMagnitude);

        planetSphereGenerator.SetInt("_Resolution", planetResolution);
        starSphereGenerator.SetInt("_Resolution", starResolution);

        planetSphereGenerator.DispatchIndirect(planetSphereGeneratorIndex, planetSphereGeneratorDispatchArgsBuffer);
        starSphereGenerator.DispatchIndirect(starSphereGeneratorIndex, starSphereGeneratorDispatchArgsBuffer);

        material3.SetFloat("_CellSize", cellSize);
        starMaterial.SetFloat("_CellSize", cellSize);

        material3.SetFloat("_BorderWidth", lowLodBorderWidth);
        starMaterial.SetFloat("_BorderWidth", highLodBorderWidth);

        material4.SetFloat("_TimeStep", timeStep);

        ComputeBuffer.CopyCount(mainProperties, mainPropertiesCount, 0);
        ComputeBuffer.CopyCount(positionsBuffer3, solarSystemBufferCount, 0);
        ComputeBuffer.CopyCount(solarSystemBuffer, solarSystemBufferCountAgain, 0);

        ComputeBuffer.CopyCount(positionsBuffer, argsBuffer, sizeof(uint));
        Graphics.DrawProceduralIndirect(material, bounds, MeshTopology.Points, argsBuffer);

        ComputeBuffer.CopyCount(positionsBuffer2, argsBuffer2, sizeof(uint));
        Graphics.DrawProceduralIndirect(material2, bounds, MeshTopology.Points, argsBuffer2);

        ComputeBuffer.CopyCount(positionsBuffer3, argsBuffer3, sizeof(uint));
        Graphics.DrawProceduralIndirect(material3, bounds, MeshTopology.Points, argsBuffer3);

        ComputeBuffer.CopyCount(positionsBuffer4, argsBuffer4, sizeof(uint));
        Graphics.DrawProceduralIndirect(material4, bounds, MeshTopology.Points, argsBuffer4);

        ComputeBuffer.CopyCount(positionsBuffer5, argsBuffer5, sizeof(uint));
        Graphics.DrawProceduralIndirect(material5, bounds, MeshTopology.Points, argsBuffer5);

        ComputeBuffer.CopyCount(solarSystemBuffer, solarSystemArgsBuffer, sizeof(uint));
        //ComputeBuffer.CopyCount(solarSystemBuffer, starSphereArgsBuffer, sizeof(uint));

        ComputeBuffer.CopyCount(planetsBuffer, planetsArgsBuffer, sizeof(uint));
        //ComputeBuffer.CopyCount(planetsBuffer, planetSphereArgsBuffer, sizeof(uint));

        //Graphics.DrawMeshInstancedIndirect(starMesh, 0, starMaterial, bounds, solarSystemArgsBuffer);
        //Graphics.DrawMeshInstancedIndirect(planetMesh, 0, planetMaterial, bounds, planetsArgsBuffer);
        Graphics.DrawProceduralIndirect(starMaterial, bounds, MeshTopology.Triangles, starIndexBuffer, solarSystemArgsBuffer);//Spheres
        Graphics.DrawProceduralIndirect(planetMaterial, bounds, MeshTopology.Triangles, planetIndexBuffer, planetsArgsBuffer);//Spheres
        Graphics.DrawMeshInstancedIndirect(sphereMesh, 0, triggerMaterial, bounds, triggerArgsBuffer);

        Vector3[] offset = new Vector3[1];
        if (Input.GetKeyDown(KeyCode.C)) 
        {
            //Debug.Log(Camera.main.transform.forward);
            chunkOffsetBuffer.GetData(offset);
            Debug.Log(offset[0]);
        }

        MeshProperties[] mp = new MeshProperties[(int)Mathf.Pow(chunksVisibleInViewDist * 8 + 1, 3)];
        if (Input.GetKeyDown(KeyCode.Q))
        {
            positionsBuffer3.GetData(mp);
            foreach (var p in mp)
            {
                Debug.Log(p.mat);
            }
        }
        Plane[] plns = new Plane[6];
        if (Input.GetKeyDown(KeyCode.T)) 
        {
            viewFrustumPlanesBufferAtTrigger.GetData(plns);
            foreach (var pl in plns) 
            {
                Debug.Log(pl);
            }
        }
        int[] ss = new int[5];
        if (Input.GetKeyDown(KeyCode.O))
        {
            planetsArgsBuffer.GetData(ss);
            foreach (var p in ss)
            {
                Debug.Log(p);
            }
        }
        MeshProperties[] mp2 = new MeshProperties[(int)Mathf.Pow(chunksVisibleInViewDist * 8 + 1, 3)];
        if (Input.GetKeyDown(KeyCode.T))
        {
            positionsBuffer2.GetData(mp2);
            foreach (var p in mp)
            {
                Debug.Log(p.mat);
            }
        }
        int[] args = new int[3];
        if (Input.GetKeyDown(KeyCode.R))
        {
            dispatchBuffer.GetData(args);
            foreach (int x in args)
            {
                Debug.Log(x);
            }
        }
        ChunkIdentifier[] chunk = new ChunkIdentifier[1];
        if (Input.GetKeyDown(KeyCode.X))
        {
            chunksBuffer.GetData(chunk);
            foreach (var c in chunk)
            {
                Debug.Log(c.pos);
                Debug.Log(c.chunkType);
                Debug.Log(c.chunkSize);
            }
        }
        TriggerChunkIdentifier[]chunks = new TriggerChunkIdentifier[2];
        if (Input.GetKeyDown(KeyCode.B))
        {
            triggerBuffer.GetData(chunks);
            for (int i = 0; i < chunks.Length; i++) 
            {
                TriggerChunkIdentifier t = chunks[i];
                Debug.Log("type: "+ t.cid.chunkType);
                Debug.Log("pos: "+ t.cid.pos);
                Debug.Log("forward: "+ t.cameraForward);
                triggerRays[i] = new Ray(t.cid.pos, t.cameraForward * 10000.0f);
                drawRays = true;
            }
        }

    }

    private void OnDrawGizmos()
    {
        if (!drawRays) return;
        for (int i = 0; i < triggerRays.Length; i++) 
        {
            Gizmos.color = new Color(i, i, i, 1.0f);
            Gizmos.DrawRay(triggerRays[i]);
        }
    }
    private void ReleaseBuffer(ComputeBuffer buffer) 
    {
        if(buffer != null) 
        {
            buffer.Release();
            buffer = null;
        }
    }
    private void ReleaseBuffer(GraphicsBuffer buffer) 
    {
        if (buffer != null) 
        {
            buffer.Release();
            buffer = null;
        }
    }
    private void OnDestroy()
    {
        ReleaseBuffer(positionsBuffer);
        ReleaseBuffer(positionsBuffer2);
        ReleaseBuffer(positionsBuffer3);
        ReleaseBuffer(positionsBuffer4);
        ReleaseBuffer(positionsBuffer5);

        ReleaseBuffer(debugPosBuffer);
        ReleaseBuffer(argsBuffer); 
        ReleaseBuffer(argsBuffer2);
        ReleaseBuffer(argsBuffer3);
        ReleaseBuffer(argsBuffer4); 
        ReleaseBuffer(argsBuffer5);

        ReleaseBuffer(starVertexBuffer);
        ReleaseBuffer(starIndexBuffer);
        ReleaseBuffer(starNormalBuffer);
        ReleaseBuffer(starUVBuffer);
        ReleaseBuffer(solarSystemBuffer);
        ReleaseBuffer(solarSystemArgsBuffer);
        ReleaseBuffer(solarSystemBufferCount);
        ReleaseBuffer(solarSystemBufferCountAgain);

        ReleaseBuffer(planetVertexBuffer);
        ReleaseBuffer(planetIndexBuffer);
        ReleaseBuffer(planetNormalBuffer);
        ReleaseBuffer(planetUVBuffer);
        ReleaseBuffer(planetsArgsBuffer);
    }
}
