using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class GameManager : MonoBehaviour
{
    public float renderDistance;
    public float lodSwitchDist1 = 2.0f;
    public float lodSwitchDist2 = 15.0f;
    public int chunkSize;
    public Vector3Int playerChunkCoord;
    public Transform viewer;
    public GameObject sphere;
    public ComputeShader positionsCalculator;
    public Texture2D randomTexture;

    private int positionsCalculatorIndex;
    private List<MeshProperties> chunkPositions;
    private int chunksVisibleInViewDist;

    //LOD sprite
    private ComputeBuffer positionsBuffer;
    private ComputeBuffer argsBuffer;
    public Mesh chunkMesh;
    public Material chunkMaterial;

    //LOD mini galaxies
    private ComputeBuffer positionsBuffer2;
    private ComputeBuffer argsBuffer2;
    public Mesh chunkMesh2;
    public Material chunkMaterial2;
    public int starCount = 5000;
    public bool testing = false;
    private int[] indices;
    private Vector3[] verts;

    //LOD galaxy
    public DispatcherProcedural dispatcherProcedural;
    private ComputeBuffer galacticCentreBuffer;
    private ComputeBuffer positionsBuffer3;
    private ComputeBuffer positionsBuffer4;
    private ComputeBuffer _VertexBuffer;
    private ComputeBuffer _NormalBuffer;
    private ComputeBuffer _UVBuffer;
    private GraphicsBuffer _IndexBuffer;
    public Material particleMaterial;
    public Material starMaterial;
    public ComputeShader positionCalculator;


    private ComputeBuffer viewFrustumPlanesBuffer;
    private Vector3 prevCameraPos;
    private Quaternion prevCameraRot;
    private int numThreadGroupsX;
    private int numThreadGroupsY;
    private int numThreadGroupsZ;

    List<GameObject> go;
    Vector3 startingPos;
    private struct MeshProperties
    {
        public Matrix4x4 mat;
    }
    void Start()
    {
        indices = new int[starCount];
        for (int i = 0; i < starCount; i++) indices[i] = i;
        verts = new Vector3[starCount];
        if (!testing) 
        {
            chunkMesh2 = new Mesh();
            chunkMesh2.SetVertices(verts);
            chunkMesh2.SetIndices(indices, MeshTopology.Points, 0);
            chunkMesh2.GetIndices(0);

            chunkMaterial2.SetInt("_NumParticles", starCount);
        }


        positionsCalculatorIndex = positionsCalculator.FindKernel("CSMain");
        chunksVisibleInViewDist = Mathf.RoundToInt(renderDistance / chunkSize);
        chunkPositions = new List<MeshProperties>();

        positionsBuffer = new ComputeBuffer((int) Mathf.Pow(chunksVisibleInViewDist + 1, 3), System.Runtime.InteropServices.Marshal.SizeOf(typeof(MeshProperties)), ComputeBufferType.Append);
        positionsBuffer2 = new ComputeBuffer((int)Mathf.Pow(chunksVisibleInViewDist + 1, 3), System.Runtime.InteropServices.Marshal.SizeOf(typeof(MeshProperties)), ComputeBufferType.Append);
        galacticCentreBuffer = new ComputeBuffer(10000, sizeof(float) * 3, ComputeBufferType.Append);

        viewFrustumPlanesBuffer = new ComputeBuffer(6, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Plane)));
        
        argsBuffer = new ComputeBuffer(1, sizeof(uint) * 4, ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(new uint[] { 1, (uint) Mathf.Pow(chunksVisibleInViewDist, 3), 0u, 0u});

        argsBuffer2 = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
        argsBuffer2.SetData(new uint[] { chunkMesh2.GetIndexCount(0), (uint)Mathf.Pow(chunksVisibleInViewDist, 3), 0u, 0u, 0u });

        chunkMaterial.SetBuffer("_Properties", positionsBuffer);

        chunkMaterial2.SetBuffer("_Properties", positionsBuffer2);

        positionsCalculator.SetBuffer(positionsCalculatorIndex, "_Properties", positionsBuffer);
        positionsCalculator.SetBuffer(positionsCalculatorIndex, "_GalacticCentreBuffer", galacticCentreBuffer);
        positionsCalculator.SetBuffer(positionsCalculatorIndex, "_Properties2", positionsBuffer2);
        positionsCalculator.SetTexture(positionsCalculatorIndex, "_Texture", randomTexture);
        positionsCalculator.SetBuffer(positionsCalculatorIndex, "_ViewFrustumPlanesBuffer", viewFrustumPlanesBuffer);

        uint xGroups, yGroups, zGroups;
        positionsCalculator.GetKernelThreadGroupSizes(positionsCalculatorIndex, out xGroups, out yGroups, out zGroups);
        numThreadGroupsX = Mathf.CeilToInt((float) chunksVisibleInViewDist / xGroups);
        numThreadGroupsY = Mathf.CeilToInt((float)chunksVisibleInViewDist / yGroups);
        numThreadGroupsZ = Mathf.CeilToInt((float)chunksVisibleInViewDist / zGroups);

        startingPos = viewer.position;
        GenerateStars();

    }

    /*
     float renderDistance;
int chunkSize;
float3 playerPosition;
int3 playerChunkCoord;
int chunksVisibleInViewDst;
StructuredBuffer<Plane> _ViewFrustumPlanes;
     */
    void GenerateStars() 
    {
        positionsBuffer.SetCounterValue(0);
        positionsBuffer2.SetCounterValue(0);
        galacticCentreBuffer.SetCounterValue(0);

        positionsCalculator.SetFloat("lodSwitchDist1", lodSwitchDist1);
        positionsCalculator.SetFloat("lodSwitchDist2", lodSwitchDist2);
        positionsCalculator.SetFloat("renderDistance", renderDistance);
        positionsCalculator.SetVector("playerPosition", viewer.position);
        positionsCalculator.SetInt("chunksVisibleInViewDist", chunksVisibleInViewDist);
        positionsCalculator.SetInt("chunkSize", chunkSize);
        positionsCalculator.Dispatch(positionsCalculatorIndex, numThreadGroupsX, numThreadGroupsY, numThreadGroupsZ);
        if (prevCameraPos != Camera.main.transform.position || prevCameraRot != Camera.main.transform.rotation)
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            viewFrustumPlanesBuffer.SetData(planes);
        }
        ComputeBuffer.CopyCount(positionsBuffer, argsBuffer, sizeof(uint));
        ComputeBuffer.CopyCount(positionsBuffer2, argsBuffer2, sizeof(uint));
        //Graphics.DrawMeshInstancedIndirect(chunkMesh, 0, chunkMaterial, new Bounds(startingPos, Vector3.one * renderDistance*renderDistance), argsBuffer);
        prevCameraPos = Camera.main.transform.position;
        prevCameraRot = Camera.main.transform.rotation;
        if (Input.GetKeyDown(KeyCode.Q))
        {
            int[] args = new int[5];
            argsBuffer.GetData(args);
            for (int i = 0; i < args.Length; i++)
            {
                Debug.Log($"Billboard args: {i}.) {args[i]}");
            }
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            int[] args = new int[5];
            argsBuffer2.GetData(args);
            for (int i = 0; i < args.Length; i++)
            {
                Debug.Log($"Billboard args 2: {i}.) {args[i]}");
            }
        }
        if (Input.GetKeyDown(KeyCode.F)) 
        {
            List<Vector3> verts = new List<Vector3>();
            chunkMesh2.GetVertices(verts);
            foreach (var vert in verts) 
            {
                Debug.Log(vert);
            }
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            int[] inds = chunkMesh2.GetIndices(0);
            foreach (var i in inds)
            {
                Debug.Log(i);
            }
        }
    }
    void GenerateStars2()
    {
        
        chunkPositions.Clear();
        positionsBuffer.SetCounterValue(0);
        Vector3 viewerPosition = viewer.position;
        //_ViewFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);
        int currentChunkCoordZ = Mathf.RoundToInt(viewerPosition.z / chunkSize);
        playerChunkCoord = new Vector3Int(currentChunkCoordX, currentChunkCoordY, currentChunkCoordZ);
        for (int xOffset = -chunksVisibleInViewDist / 2; xOffset <= chunksVisibleInViewDist / 2; xOffset++) 
        {
            for (int yOffset = -chunksVisibleInViewDist / 2; yOffset <= chunksVisibleInViewDist / 2; yOffset++) 
            {
                for (int zOffset = -chunksVisibleInViewDist / 2; zOffset <= chunksVisibleInViewDist / 2; zOffset++) 
                {
                    Vector3Int viewedChunkCoord = new Vector3Int(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset, currentChunkCoordZ + zOffset);
                    int seed = viewedChunkCoord.x + viewedChunkCoord.y * chunksVisibleInViewDist + viewedChunkCoord.z * chunksVisibleInViewDist * chunksVisibleInViewDist;
                    UnityEngine.Random.InitState(seed);
                    if (UnityEngine.Random.Range(0, 20) == 0) 
                    {
                        Vector3 actualPosition = viewedChunkCoord * chunkSize;
                        if (IntersectSphere(actualPosition, 2, GeometryUtility.CalculateFrustumPlanes(Camera.main)) != 0)
                        {
                            Quaternion rotation = Quaternion.Euler(0, 0, 0);
                            Vector3 scale = Vector3.one;
                            chunkPositions.Add(new MeshProperties { mat = Matrix4x4.TRS(actualPosition, rotation, scale) });
                        }

                    }
                    /*StarSystem starSystem = new StarSystem(viewedChunkCoord.x, viewedChunkCoord.y, viewedChunkCoord.z);
                    if (starSystem.starExists) 
                    {
                        Vector3 actualCoord = (Vector3) viewedChunkCoord * chunkSize;
                        chunkPositions.Add(actualCoord);
                    }*/
                }
            }
        }
        positionsBuffer.SetData(chunkPositions);
        ComputeBuffer.CopyCount(positionsBuffer, argsBuffer, sizeof(uint));
        if (Input.GetKeyDown(KeyCode.Q))
        {
            int[] args = new int[5];
            argsBuffer.GetData(args);
            Debug.Log($"Chunk posns: {chunkPositions.Count}");
            for (int i = 0; i < args.Length; i++)
            {
                Debug.Log($"Billboard args: {i}.) {args[i]}");
            }
        }
        //Graphics.DrawMeshInstancedIndirect(chunkMesh, 0, chunkMaterial, new Bounds(viewer.position, Vector3.one * renderDistance), argsBuffer);
        //foreach (GameObject g in go) Destroy(g);
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(startingPos, Vector3.one * renderDistance * renderDistance);
    }
    private void Update()
    {
        //startingPos = Vector3.zero;
        GenerateStars();
        /*Vector3 viewerPosition = viewer.position;
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);
        int currentChunkCoordZ = Mathf.RoundToInt(viewerPosition.z / chunkSize);
        playerChunkCoord = new Vector3Int(currentChunkCoordX, currentChunkCoordY, currentChunkCoordZ);*/
        Bounds instancingBounds = new Bounds(Vector3.zero, Vector3.one * renderDistance * renderDistance);
        if (Vector3.Distance(viewer.position, startingPos) >= renderDistance * renderDistance) 
        {
            //viewer.position = Vector3.zero;
            Debug.Log("changing!");
        }
        //Bounds instancingBounds = new Bounds(startingPos - viewer.position, Vector3.one * renderDistance * renderDistance);
        // Offset the bounds by the viewer's position relative to the startingPos.
        //instancingBounds.center += startingPos - viewer.position;

        //Graphics.DrawMeshInstancedIndirect(chunkMesh, 0, chunkMaterial, instancingBounds, argsBuffer);
        Graphics.DrawProceduralIndirect(chunkMaterial, instancingBounds, MeshTopology.Points, argsBuffer);
        Graphics.DrawMeshInstancedIndirect(chunkMesh2, 0, chunkMaterial2, instancingBounds, argsBuffer2);
        //Graphics.DrawMeshInstancedIndirect(chunkMesh, 0, chunkMaterial, new Bounds(startingPos, Vector3.one * renderDistance*renderDistance), argsBuffer);
    }

    uint IntersectSphere(float3 centre, float radius, Plane[] viewFrustumPlanes)
    {

        for (uint i = 0; i < 6; i++)
        {
            Plane plane = viewFrustumPlanes[i];
            float normalDotCentre = Vector3.Dot(plane.normal, centre);
            float cullDist = plane.distance;
            if (normalDotCentre + cullDist + radius <= 0)
            {
                return 0;
            }
        }
        return 1;
    }
}
