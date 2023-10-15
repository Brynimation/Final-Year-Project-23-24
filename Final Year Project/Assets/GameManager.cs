using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public float renderDistance;
    public int chunkSize;
    public Vector3Int playerChunkCoord;
    public Transform viewer;
    public Mesh chunkMesh;
    public Material chunkMaterial;
    public GameObject sphere;
    public ComputeShader positionsCalculator;
    public Texture2D randomTexture;

    private int positionsCalculatorIndex;
    private List<MeshProperties> chunkPositions;
    private int chunksVisibleInViewDist;
    private ComputeBuffer positionsBuffer;
    private ComputeBuffer argsBuffer;
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
        positionsCalculatorIndex = positionsCalculator.FindKernel("CSMain");
        chunksVisibleInViewDist = Mathf.RoundToInt(renderDistance / chunkSize);
        chunkPositions = new List<MeshProperties>();
        positionsBuffer = new ComputeBuffer((int) Mathf.Pow(chunksVisibleInViewDist + 1, 3), System.Runtime.InteropServices.Marshal.SizeOf(typeof(MeshProperties)), ComputeBufferType.Append);
        viewFrustumPlanesBuffer = new ComputeBuffer(6, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Plane)));
        argsBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(new uint[] { chunkMesh.GetIndexCount(0), (uint) Mathf.Pow(chunksVisibleInViewDist, 3), 0u, 0u, 0u });
        chunkMaterial.SetBuffer("_Properties", positionsBuffer);
        positionsCalculator.SetBuffer(positionsCalculatorIndex, "_Properties", positionsBuffer);
        positionsCalculator.SetTexture(positionsCalculatorIndex, "_Texture", randomTexture);
        positionsCalculator.SetBuffer(positionsCalculatorIndex, "_ViewFrustumPlanesBuffer", viewFrustumPlanesBuffer);
        startingPos = viewer.position;
        uint xGroups, yGroups, zGroups;
        positionsCalculator.GetKernelThreadGroupSizes(positionsCalculatorIndex, out xGroups, out yGroups, out zGroups);
        numThreadGroupsX = Mathf.CeilToInt((float) chunksVisibleInViewDist / xGroups);
        numThreadGroupsY = Mathf.CeilToInt((float)chunksVisibleInViewDist / yGroups);
        numThreadGroupsZ = Mathf.CeilToInt((float)chunksVisibleInViewDist / zGroups);
        Debug.Log($"{numThreadGroupsX}, {numThreadGroupsY}, {numThreadGroupsZ}");
        go = new List<GameObject>();
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
        Graphics.DrawMeshInstancedIndirect(chunkMesh, 0, chunkMaterial, new Bounds(Vector3.zero, Vector3.one * renderDistance*renderDistance), argsBuffer);
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
            Color[] colours = randomTexture.GetPixels();
            foreach (var col in colours)
            {
                Debug.Log(col);
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
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one * renderDistance * renderDistance);
    }
    private void Update()
    {
        GenerateStars();
        /*Vector3 viewerPosition = viewer.position;
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);
        int currentChunkCoordZ = Mathf.RoundToInt(viewerPosition.z / chunkSize);
        playerChunkCoord = new Vector3Int(currentChunkCoordX, currentChunkCoordY, currentChunkCoordZ);*/
        if (Vector3.Distance(viewer.position, startingPos) >= renderDistance / 2) 
        {
            //startingPos = viewer.position;
        }
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
