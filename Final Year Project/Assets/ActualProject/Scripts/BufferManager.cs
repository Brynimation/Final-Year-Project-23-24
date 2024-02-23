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
    public int angularOffsetMultiplier;
    public float maxHaloRadius;
    public float galaxyDensity;
    public Color centreColour;
    public Color outerColour;
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

public struct PlanetTerrainColours 
{
    Color[] colours;
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
    PlanetTerrainColours colours;
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
    public Color _EmissionColour;
    public Color _EmissionColour2;

    //General
    [Header("General Properties")]
    public ComputeShader chunkManager;
    public float timeStep;
    public float lodSwitchBackDist;
    public float dontSpawnRadius;
    public Material triggerMaterial;
    public Mesh sphereMesh;
    public int chunkSize;
    public int renderDistance;
    public float lodSwitchDist;
    int chunksVisibleInViewDist;
    ComputeBuffer viewFrustumPlanesBuffer;
    ComputeBuffer viewFrustumPlanesBufferAtTrigger;
    ComputeBuffer triggerBuffer;
    ComputeBuffer triggerArgsBuffer;
    ComputeBuffer chunkOffsetBuffer;


    Ray[] triggerRays;
    bool drawRays;

    //Big galaxy
    [Header("Galaxy Properties")]
    public ComputeShader galaxyPositioner;
    public float galaxyLodSwitchDist;
    public float galaxyFadeDist;
    public Vector2 minMaxMinimumEccentricity;
    public Vector2 minMaxMaximumEccentricity;
    public Vector2 minMaxAngularOffsetMultiplier;
    public Vector2 minMaxHaloRadius;
    public Vector2 minMaxBulgeRadius;
    public Vector2 minMaxDiskRadius;
    public Vector2 minMaxGalaxyDensity;

    public Color[] galaxyCentreColours;
    public Color[] galaxyOuterColours;

    float[] galaxyCentreColourFloats;
    float[] galaxyOuterColourFloats;
    public int[] minMaxNumParticles;

    [Header("Galaxy Star Distribution")]
    //Good params: I0 = 0.4, maxHaloRadius = 4, kappa = 0.01 (produces nice galaxies)
    public CDF cdf;
    public float cdfBulgeRadius;
    public float centralIntensity = 0.4f;
    public float differenceThreshold = 0.1f;
    public float kappa = 0.01f;
    public float cdfScaleLength;
    public Texture2D _RadiusLookupTexture;

    ComputeBuffer radii;

    //Solar systems
    [Header("Solar System Properties")]
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
    [Header("Planet Properties")]
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

    //Planet colours
    [Header("Planet Colours")]
    public Color[] oceanColours;
    public Color[] groundColours;
    public Color[] mountainColours;
    public Color[] mountainTopColours;

    float[] floatOceanColours;
    float[] floatGroundColours;
    float[] floatMountainColours;
    float[] floatMountainTopColours;

    public Material lowLODSolarSystemMaterial;
    ComputeBuffer lowLODSolarSystemPositions;
    ComputeBuffer lowLODSolarSystemArgsBuffer;

    public Material lowLODGalaxyMaterial;
    ComputeBuffer lowLODGalaxyPositions;
    ComputeBuffer lowLODGalaxyArgsBuffer;

    public Material galacticClusterMaterial;
    ComputeBuffer galacticClusterPositionsBuffer;
    ComputeBuffer galacticClusterArgsBuffer;

    ComputeBuffer debugPosBuffer;
    ComputeBuffer dispatchBuffer;
    ComputeBuffer chunksBuffer;
    ComputeBuffer chunksBufferPrevFrame;
    ChunkIdentifier[] chunksVisible;

    public ComputeBuffer bigGalaxyProperties;
    public ComputeBuffer bigGalaxyPropertiesCount;

    int mainKernelIndex;
    int galaxyPositionerIndex;
    int solarSystemCreatorIndex;
    int starSphereGeneratorIndex;
    int planetSphereGeneratorIndex;

    Bounds bounds;

    private float[] ColourToFloatArray(Color[] colours) 
    {
        return colours.SelectMany(c => new float[] { c.r, c.g, c.b, c.a }).ToArray();
    }
        void Start()
    {
        triggerRays = new Ray[2];
        floatColours = ColourToFloatArray(colours);  //needed to pass to shader

        floatOceanColours = ColourToFloatArray(oceanColours);
        floatGroundColours = ColourToFloatArray(groundColours);
        floatMountainColours = ColourToFloatArray(mountainColours);
        floatMountainTopColours = ColourToFloatArray(mountainTopColours);

        galaxyCentreColourFloats = ColourToFloatArray(galaxyCentreColours);
        galaxyOuterColourFloats = ColourToFloatArray(galaxyOuterColours);

        starSphereGenerator = Instantiate(sphereGeneratorPrefab);
        planetSphereGenerator = Instantiate(sphereGeneratorPrefab);

        chunksVisibleInViewDist = Mathf.RoundToInt(renderDistance / chunkSize);
        chunksVisible = new ChunkIdentifier[1] { new ChunkIdentifier(chunksVisibleInViewDist, chunkSize, 4, Vector3.one * -1) };
        mainKernelIndex = chunkManager.FindKernel("CSMainNew");
        galaxyPositionerIndex = galaxyPositioner.FindKernel("CSMain");
        solarSystemCreatorIndex = solarSystemCreator.FindKernel("CSMain");
        starSphereGeneratorIndex = starSphereGenerator.FindKernel("CSMain");
        planetSphereGeneratorIndex = planetSphereGenerator.FindKernel("CSMain");

        int maxInstanceCount = (int)Mathf.Pow(chunksVisibleInViewDist * 8 + 1, 3);

        int numVertsPerStar = starResolution * starResolution * 4 * 6; //Plane of verts made up of groups of quads. 1 plane for each of the 6 faces of a cube
        int numIndicesPerStar = 6 * 6 * starResolution * starResolution; //indicesPerTriangle * trianglesPerQuad * 6 faces of cube * resolution^2

        int numVertsPerPlanet = planetResolution * planetResolution * 4 * 6; //Plane of verts made up of groups of quads. 1 plane for each of the 6 faces of a cube
        int numIndicesPerPlanet = 6 * 6 * planetResolution * planetResolution; //indicesPerTriangle * trianglesPerQuad * 6 faces of cube * resolution^2

        radii = new ComputeBuffer(1, sizeof(float), ComputeBufferType.Structured); //debugging

        //Low LOD solar system buffer - only needs to store the properties of the stars as planets cannot be viewed from this distance
        lowLODSolarSystemPositions = new ComputeBuffer(maxInstanceCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(MeshProperties)), ComputeBufferType.Append);
        lowLODSolarSystemArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 4, ComputeBufferType.IndirectArguments);
        lowLODSolarSystemArgsBuffer.SetData(new uint[] { (uint)1, (uint)maxInstanceCount, 0u, 0u });

        //High LOD solar system buffer
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

        lowLODGalaxyPositions = new ComputeBuffer(maxInstanceCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(GalaxyProperties)), ComputeBufferType.Append);
        lowLODGalaxyArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 4, ComputeBufferType.IndirectArguments);
        lowLODGalaxyArgsBuffer.SetData(new uint[] { (uint)1, (uint)maxInstanceCount, 0u, 0u });

        bigGalaxyProperties = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(GalaxyProperties)), ComputeBufferType.Append);
        bigGalaxyPropertiesCount = new ComputeBuffer(1, sizeof(uint));

        galacticClusterPositionsBuffer = new ComputeBuffer(maxInstanceCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(MeshProperties)), ComputeBufferType.Append);
        galacticClusterArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 4, ComputeBufferType.IndirectArguments);
        galacticClusterArgsBuffer.SetData(new uint[] { (uint)1, (uint)maxInstanceCount, 0u, 0u });

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

        //Setting up the inverse cdf lookup table
        float maxHaloRadius = minMaxHaloRadius.y;
        cdfBulgeRadius = maxHaloRadius / 4f;
        cdfScaleLength = maxHaloRadius / 2f;
        cdf = new CDF(0.0f, maxHaloRadius, cdfBulgeRadius, 1000, 100, centralIntensity, kappa, differenceThreshold, cdfScaleLength);
        _RadiusLookupTexture = new Texture2D(100, 1, TextureFormat.RGBAFloat, false); //RGBAFloat accepts HDR values - lets us encode radii > 1 within our texture.
        _RadiusLookupTexture.filterMode = FilterMode.Bilinear;
        _RadiusLookupTexture.wrapMode = TextureWrapMode.Clamp;
        _RadiusLookupTexture.SetPixels(cdf.GenerateInverseCDFLookUpArray().Select(radius => new Color(radius, 0.0f, 0.0f, 0.0f)).ToArray<Color>());
        _RadiusLookupTexture.Apply();

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

        chunkManager.SetBuffer(mainKernelIndex, "_Properties5", galacticClusterPositionsBuffer);
        chunkManager.SetBuffer(mainKernelIndex, "_ChunkOffset", chunkOffsetBuffer);

        chunkManager.SetBuffer(mainKernelIndex, "_ChunksBuffer", chunksBuffer);
        chunkManager.SetBuffer(mainKernelIndex, "_ChunksBufferPrevFrame", chunksBufferPrevFrame);
        chunkManager.SetBuffer(mainKernelIndex, "_TriggerBuffer", triggerBuffer);
        chunkManager.SetBuffer(mainKernelIndex, "_ViewFrustumPlanes", viewFrustumPlanesBuffer);
        chunkManager.SetBuffer(mainKernelIndex, "_ViewFrustumPlanesAtTrigger", viewFrustumPlanesBufferAtTrigger);
        chunkManager.SetBuffer(mainKernelIndex, "_DispatchBuffer", dispatchBuffer);

        chunkManager.SetInt("chunkSize", chunkSize);
        chunkManager.SetFloat("dontSpawnRadius", dontSpawnRadius);
        chunkManager.SetInt("renderDistance", renderDistance);
        chunkManager.SetFloat("lodSwitchDist", lodSwitchDist);
        chunkManager.SetFloat("galaxySwitchDist", galaxyLodSwitchDist);
        chunkManager.SetFloat("solarSystemSwitchDist", solarSystemSwitchDist);
        chunkManager.SetInt("chunksVisibleInViewDist", chunksVisibleInViewDist);
        chunkManager.SetVector("playerPosition", playerPosition.position);
        chunkManager.SetVector("playerRight", playerPosition.right);
        chunkManager.SetVector("playerUp", playerPosition.up);
        chunkManager.SetVector("playerForward", playerPosition.forward);
        chunkManager.SetFloat("galaxyFadeDist", galaxyFadeDist);
        chunkManager.SetFloats("colours", floatColours);
        chunkManager.SetVector("minMaxMinimumEccentricity", minMaxMinimumEccentricity);
        chunkManager.SetVector("minMaxMaximumEccentricity", minMaxMaximumEccentricity);
        chunkManager.SetVector("minMaxAngularOffsetMultiplier", minMaxAngularOffsetMultiplier);
        chunkManager.SetVector("minMaxHaloRadius", minMaxHaloRadius);
        chunkManager.SetVector("minMaxBulgeRadius", minMaxBulgeRadius);
        chunkManager.SetVector("minMaxDiskRadius", minMaxDiskRadius);
        chunkManager.SetInts("minMaxNumParticles", minMaxNumParticles);

        galaxyPositioner.SetBuffer(galaxyPositionerIndex, "_ChunksBuffer", chunksBuffer);
        galaxyPositioner.SetBuffer(galaxyPositionerIndex, "_TriggerBuffer", triggerBuffer);
        galaxyPositioner.SetBuffer(galaxyPositionerIndex, "_ViewFrustumPlanes", viewFrustumPlanesBuffer);
        galaxyPositioner.SetBuffer(galaxyPositionerIndex, "_ViewFrustumPlanesAtTrigger", viewFrustumPlanesBufferAtTrigger);
        galaxyPositioner.SetBuffer(galaxyPositionerIndex, "_MainProperties", bigGalaxyProperties);
        galaxyPositioner.SetBuffer(galaxyPositionerIndex, "_Properties4", lowLODGalaxyPositions);
        galaxyPositioner.SetBuffer(galaxyPositionerIndex, "_Radii", radii);
        galaxyPositioner.SetTexture(galaxyPositionerIndex, "_RadiusLookupTexture", _RadiusLookupTexture);
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
        galaxyPositioner.SetVector("minMaxDensity", minMaxGalaxyDensity);
        galaxyPositioner.SetFloats("centreColours", galaxyCentreColourFloats);
        galaxyPositioner.SetFloats("outerColours", galaxyOuterColourFloats);


        solarSystemCreator.SetFloat("solarSystemSwitchDist", solarSystemSwitchDist);
        solarSystemCreator.SetVector("playerPosition", playerPosition.position);
        solarSystemCreator.SetFloat("time", Time.time);
        solarSystemCreator.SetFloat("timeStep", timeStep);
        solarSystemCreator.SetFloats("colours", floatColours);
        solarSystemCreator.SetFloats("oceanColours", floatOceanColours);
        solarSystemCreator.SetFloats("groundColours", floatGroundColours);
        solarSystemCreator.SetFloats("mountainColours", floatMountainColours);
        solarSystemCreator.SetFloats("mountainTopColours", floatMountainTopColours);
        solarSystemCreator.SetBuffer(solarSystemCreatorIndex, "_ChunksBuffer", chunksBuffer);
        solarSystemCreator.SetBuffer(solarSystemCreatorIndex, "_Properties3", lowLODSolarSystemPositions);
        solarSystemCreator.SetBuffer(solarSystemCreatorIndex, "_SolarSystemCount", solarSystemBufferCount);
        solarSystemCreator.SetBuffer(solarSystemCreatorIndex, "_SolarSystems", solarSystemBuffer);
        solarSystemCreator.SetBuffer(solarSystemCreatorIndex, "_Planets", planetsBuffer);
        solarSystemCreator.SetBuffer(solarSystemCreatorIndex, "_ViewFrustumPlanes", viewFrustumPlanesBuffer);
        solarSystemCreator.SetBuffer(solarSystemCreatorIndex, "_ViewFrustumPlanesAtTrigger", viewFrustumPlanesBufferAtTrigger);
        solarSystemCreator.SetBuffer(solarSystemCreatorIndex, "_TriggerBuffer", triggerBuffer);
        solarSystemCreator.SetFloat("dontSpawnRadius", dontSpawnRadius);

        lowLODSolarSystemMaterial.SetBuffer("_Properties", lowLODSolarSystemPositions);
        lowLODGalaxyMaterial.SetBuffer("_Properties", lowLODGalaxyPositions);
        galacticClusterMaterial.SetBuffer("_Properties", galacticClusterPositionsBuffer);

        bounds = new Bounds(Vector3.zero, new Vector3(1000000, 1000000, 1000000));
        lowLODSolarSystemMaterial.SetFloat("_CellSize", cellSize);
        starMaterial.SetFloat("_CellSize", cellSize);

        lowLODGalaxyMaterial.SetFloat("_TimeStep", timeStep);

        triggerMaterial.SetBuffer("_ChunksBuffer", chunksBuffer);
        triggerMaterial.SetBuffer("_TriggerBuffer", triggerBuffer);



    }

    // Update is called once per frame
    void Update()
    {
        //positionsBuffer.SetCounterValue(0);
        //positionsBuffer2.SetCounterValue(0);

        if (Input.GetKeyDown(KeyCode.V)) 
        {
            float[] rads = new float[1];
            radii.GetData(rads);
            foreach (float rad in rads) 
            {
                Debug.Log($"radius: {rad}");
            }
        }
        lowLODSolarSystemPositions.SetCounterValue(0);
        lowLODGalaxyPositions.SetCounterValue(0);
        galacticClusterPositionsBuffer.SetCounterValue(0);
        solarSystemBuffer.SetCounterValue(0);
        planetsBuffer.SetCounterValue(0);
        bigGalaxyProperties.SetCounterValue(0);

        debugPosBuffer.SetCounterValue(0);

        Vector3 camForward = Camera.main.transform.forward;
        //Debug.Log(camForward);
        chunkManager.SetVector("playerPosition", playerPosition.position);
        chunkManager.SetVector("cameraForward", camForward);
        chunkManager.SetFloat("lodSwitchBackDist", lodSwitchBackDist);
        chunkManager.SetFloat("minWavelength", minWavelength);
        chunkManager.SetFloat("maxWavelength", maxWavelength);
        chunkManager.SetFloat("minLuminosity", minLuminosity);
        chunkManager.SetFloat("maxLuminosity", maxLuminosity);
        chunkManager.SetFloat("minRadius", minRadius);
        chunkManager.SetFloat("maxRadius", maxRadius);
        chunkManager.SetBool("goBack", Input.GetKeyDown(KeyCode.K));
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        viewFrustumPlanesBuffer.SetData(planes);
        chunkManager.DispatchIndirect(mainKernelIndex, dispatchBuffer);

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

        lowLODSolarSystemMaterial.SetFloat("_CellSize", cellSize);
        starMaterial.SetFloat("_CellSize", cellSize);

        lowLODSolarSystemMaterial.SetFloat("_BorderWidth", lowLodBorderWidth);
        starMaterial.SetFloat("_BorderWidth", highLodBorderWidth);

        lowLODGalaxyMaterial.SetFloat("_TimeStep", timeStep);

        ComputeBuffer.CopyCount(bigGalaxyProperties, bigGalaxyPropertiesCount, 0);
        ComputeBuffer.CopyCount(lowLODSolarSystemPositions, solarSystemBufferCount, 0);
        ComputeBuffer.CopyCount(solarSystemBuffer, solarSystemBufferCountAgain, 0);

        ComputeBuffer.CopyCount(lowLODSolarSystemPositions, lowLODSolarSystemArgsBuffer, sizeof(uint));
        Graphics.DrawProceduralIndirect(lowLODSolarSystemMaterial, bounds, MeshTopology.Points, lowLODSolarSystemArgsBuffer);

        ComputeBuffer.CopyCount(lowLODGalaxyPositions, lowLODGalaxyArgsBuffer, sizeof(uint));
        Graphics.DrawProceduralIndirect(lowLODGalaxyMaterial, bounds, MeshTopology.Points, lowLODGalaxyArgsBuffer);

        ComputeBuffer.CopyCount(galacticClusterPositionsBuffer, galacticClusterArgsBuffer, sizeof(uint));
        Graphics.DrawProceduralIndirect(galacticClusterMaterial, bounds, MeshTopology.Points, galacticClusterArgsBuffer);

        ComputeBuffer.CopyCount(solarSystemBuffer, solarSystemArgsBuffer, sizeof(uint));
        //ComputeBuffer.CopyCount(solarSystemBuffer, starSphereArgsBuffer, sizeof(uint));

        ComputeBuffer.CopyCount(planetsBuffer, planetsArgsBuffer, sizeof(uint));
        //ComputeBuffer.CopyCount(planetsBuffer, planetSphereArgsBuffer, sizeof(uint));

        //Graphics.DrawMeshInstancedIndirect(starMesh, 0, starMaterial, bounds, solarSystemArgsBuffer);
        //Graphics.DrawMeshInstancedIndirect(planetMesh, 0, planetMaterial, bounds, planetsArgsBuffer);
        Graphics.DrawProceduralIndirect(starMaterial, bounds, MeshTopology.Triangles, starIndexBuffer, solarSystemArgsBuffer);//Spheres
        Graphics.DrawProceduralIndirect(planetMaterial, bounds, MeshTopology.Triangles, planetIndexBuffer, planetsArgsBuffer);//Spheres
        Graphics.DrawMeshInstancedIndirect(sphereMesh, 0, triggerMaterial, bounds, triggerArgsBuffer);

        int[] planetArgs = new int[5];
        int[] solarSystemArgs = new int[5];
        int[] clusterArgs = new int[4];
        int[] lowGalaxyArgs = new int[4];
        int[] lowSolarArgs = new int[4];
        int[] bigGalaxyArgs = new int[1];

        //verify that gpu view frustum culling works
        if (Input.GetKeyDown(KeyCode.K)) 
        {
            planetsArgsBuffer.GetData(planetArgs);
            solarSystemArgsBuffer.GetData(solarSystemArgs);
            galacticClusterArgsBuffer.GetData(clusterArgs);
            lowLODGalaxyArgsBuffer.GetData(lowGalaxyArgs);
            lowLODSolarSystemArgsBuffer.GetData(lowSolarArgs);
            bigGalaxyPropertiesCount.GetData(bigGalaxyArgs);

            Debug.Log($"planets: {planetArgs[1]}, low lod solars {lowSolarArgs[1]}, high lod solars {solarSystemArgs[1]}, low lod galaxies {lowGalaxyArgs[1]}, high lod galaxies {bigGalaxyArgs[0]}, clusters {clusterArgs[1]}");
        }

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
            lowLODSolarSystemPositions.GetData(mp);
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
        ReleaseBuffer(lowLODSolarSystemPositions);
        ReleaseBuffer(lowLODGalaxyPositions);
        ReleaseBuffer(galacticClusterPositionsBuffer);

        ReleaseBuffer(debugPosBuffer);
        ReleaseBuffer(lowLODSolarSystemArgsBuffer);
        ReleaseBuffer(lowLODGalaxyArgsBuffer); 
        ReleaseBuffer(galacticClusterArgsBuffer);

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
