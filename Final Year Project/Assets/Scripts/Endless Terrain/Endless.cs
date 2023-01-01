
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Endless : MonoBehaviour
{
    public float renderDistance;
    public Transform viewer;
    [SerializeField] Material mat;
    [SerializeField] int numCells = 16;
    [SerializeField] GameObject starPrefab;
    Vector3 viewerPosition;
    public int chunkSize;
    int chunksVisibleInViewDist;
    Dictionary<Vector3, TerrainChunk> terrainChunkDict;
    List<TerrainChunk> terrainChunkVisibleLastUpdate; 
    void Start()
    {
        chunksVisibleInViewDist = Mathf.RoundToInt(renderDistance/chunkSize);
        Debug.Log(chunksVisibleInViewDist);
        terrainChunkDict = new Dictionary<Vector3, TerrainChunk>();
        terrainChunkVisibleLastUpdate = new List<TerrainChunk>();
    }

    private void Update()
    {
        viewerPosition = viewer.position;
        UpdateVisibleChunks();
    }
    void UpdateVisibleChunks() 
    {
        for (int i = 0; i < terrainChunkVisibleLastUpdate.Count; i++) 
        {
            terrainChunkVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunkVisibleLastUpdate.Clear();
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);
        int currentChunkCoordZ = Mathf.RoundToInt(viewerPosition.z / chunkSize);
        for (int zOffset = -chunksVisibleInViewDist; zOffset <= chunksVisibleInViewDist; zOffset++) 
        {
            for (int yOffset = -chunksVisibleInViewDist; yOffset <= chunksVisibleInViewDist; yOffset++)
            {
                for (int xOffset = -chunksVisibleInViewDist; xOffset <= chunksVisibleInViewDist; xOffset++)
                {
                    Vector3 viewedChunkCoord = new Vector3(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset, currentChunkCoordZ + zOffset);
                    //We only want to instantiate a new chunk at a coordinate if we haven't already instantiated one.
                    if (terrainChunkDict.ContainsKey(viewedChunkCoord))
                    {

                        terrainChunkDict[viewedChunkCoord].UpdateChunk(viewerPosition, renderDistance);
                        if (terrainChunkDict[viewedChunkCoord].isVisible())
                        {
                            terrainChunkVisibleLastUpdate.Add(terrainChunkDict[viewedChunkCoord]);
                        }
                    }
                    else
                    {
                        TerrainChunk t = new TerrainChunk(viewedChunkCoord, chunkSize, mat, numCells);
                        for (int i = 0; i < t.starPositions.Count; i++) 
                        {
                            Vector3 pos = t.starPositions[i];
                            GameObject star = Instantiate(starPrefab, pos, Quaternion.identity);
                            star.transform.localScale = Vector3.one * t.starSystems[i].starRadius;
                            star.GetComponent<Renderer>().material.color = t.starSystems[i].starColour;
                        }
                        terrainChunkDict.Add(viewedChunkCoord, t);
                    }

                }
            }
        }

    }
}

/*Split Each chunk up into cells*/

public class TerrainChunk 
{
    Vector3 position;
    GameObject mesh;
    Bounds bounds;
    public List<StarSystem> starSystems;
    public List<Vector3> starPositions;
    int cellSize;
    public TerrainChunk(Vector3 coord, int chunkSize, Material mat, int numCells) 
    {
        starSystems = new List<StarSystem>();
        starPositions = new List<Vector3>();
        cellSize = Mathf.RoundToInt(chunkSize / numCells);
        position = coord * chunkSize;
        bounds = new Bounds(position, Vector3.one * chunkSize);
        Vector3 position3D = new Vector3(position.x, position.y, position.z);
        mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Renderer r = mesh.GetComponent<Renderer>();
        mesh.gameObject.GetComponent<Collider>().enabled = false;
        r.material = mat;
        mat.color = new Color(1f, 0f, 0f, 0.25f);
        mesh.transform.position = position3D;
        mesh.transform.localScale = Vector3.one * chunkSize;

        for (int x = 0; x < numCells; x++)
        {
            for (int y = 0; y < numCells; y++)
            {
                for (int z = 0; z < numCells; z++)
                {
                    StarSystem starSystem = new StarSystem(x * cellSize, y * cellSize, z * cellSize);

                    if (starSystem.starExists) 
                    {
                        starSystems.Add(starSystem);
                        Vector3 pos = new Vector3(position.x + x*cellSize, position.y + y * cellSize, position.z + z*cellSize);
                        starPositions.Add(pos);
                    }
                }
            }
        }
    }

    /*Find the point on its perimeter that's the closest to the viewer's position. 
     It will then find the distance between that point and the viewer. If this is 
    less than the render distance, the mesh will be enabled. Otherwise, it'll be 
    disabled.*/
    public void UpdateChunk(Vector3 pos, float renderDistance) 
    {
        float closestViewerDstSqrd = bounds.SqrDistance(pos); //returns the smallest squared distance between the passed point and the bounding box
        bool visible = closestViewerDstSqrd <= renderDistance * renderDistance;
        SetVisible(visible);
    }
    public void SetVisible(bool visible) 
    {
        mesh.SetActive(visible);
    }
    public bool isVisible() 
    {
        return mesh.activeSelf;
    }
}
