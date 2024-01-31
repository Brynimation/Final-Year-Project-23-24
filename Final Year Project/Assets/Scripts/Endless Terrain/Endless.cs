
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
public class Endless : MonoBehaviour
{
    public float renderDistance;
    public Transform viewer;
    [SerializeField] Material mat;
    [SerializeField] int numCells = 4;
    [SerializeField] Star starPrefab;
    [SerializeField] bool visualiseChunks;
    Vector3 viewerPosition;
    public int chunkSize;
    int chunksVisibleInViewDist;
    Dictionary<Vector3Int, TerrainChunk> terrainChunkDict;
    List<TerrainChunk> terrainChunkVisibleLastUpdate; 
    void Start()
    {
        chunksVisibleInViewDist = Mathf.RoundToInt(renderDistance/chunkSize);
        Debug.Log(chunksVisibleInViewDist);
        terrainChunkDict = new Dictionary<Vector3Int, TerrainChunk>();
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
                    Vector3Int viewedChunkCoord = new Vector3Int(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset, currentChunkCoordZ + zOffset);
                    //We only want to instantiate a new chunk at a coordinate if we haven't already instantiated one.
                    if (terrainChunkDict.ContainsKey(viewedChunkCoord))
                    {

                        terrainChunkDict[viewedChunkCoord].UpdateChunk(viewerPosition, renderDistance);
                        if (terrainChunkDict[viewedChunkCoord].isVisible(viewerPosition, renderDistance))
                        {
                            terrainChunkVisibleLastUpdate.Add(terrainChunkDict[viewedChunkCoord]);
                        }
                    }
                    else
                    {
                        TerrainChunk t = new TerrainChunk(viewedChunkCoord, chunkSize, mat, numCells, visualiseChunks);
                        for (int i = 0; i < t.starPositions.Count; i++) 
                        {
                            Vector3 pos = t.starPositions[i];
                            Star star = Instantiate(starPrefab, pos, Quaternion.identity);
                            star.starProperties = t.starSystems[i];
                            t.instantiatedStars.Add(star);
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

/*
public class TerrainChunk 
{

    public List<StarSystem> starSystems;
    public List<Star> instantiatedStars;
    public List<Vector3> starPositions;
    Vector3 position;
    GameObject mesh;
    Bounds bounds;
    float closestViewerDstSqrd;
    bool chunkVisible;
    int cellSize;

    void visualiseChunk(Material mat, Vector3 position, int chunkSize) 
    {
        mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Renderer r = mesh.GetComponent<Renderer>();
        mesh.gameObject.GetComponent<Collider>().enabled = false;
        r.material = mat;
        mat.color = new Color(1f, 0f, 0f, 0.25f);
        mesh.transform.position = position;
        mesh.transform.localScale = Vector3.one * chunkSize;
    }
    public TerrainChunk(Vector3Int coord, int chunkSize, Material mat, int numCells, bool visualise) 
    {
        starSystems = new List<StarSystem>();
        instantiatedStars = new List<Star>();
        starPositions = new List<Vector3>();
        cellSize = Mathf.RoundToInt(chunkSize / numCells);
        position = coord * chunkSize;
        bounds = new Bounds(position, Vector3.one * chunkSize);
        chunkVisible = visualise;
        if(visualise) visualiseChunk(mat, position, chunkSize);
        for (int x = -numCells/2; x < numCells/2; x++)
        {
            for (int y = -numCells/2; y < numCells/2; y++)
            {
                for (int z = -numCells/2; z < numCells/2; z++)
                {
                    StarSystem starSystem = new StarSystem(coord.x + x * cellSize, coord.y + y * cellSize, coord.z +  z * cellSize);

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
  /*  public void UpdateChunk(Vector3 pos, float renderDistance) 
    {
        float closestViewerDstSqrd = bounds.SqrDistance(pos); //returns the smallest squared distance between the passed point and the bounding box
        bool visible = closestViewerDstSqrd <= renderDistance * renderDistance;
        SetVisible(visible);
    }
    public void SetVisible(bool visible) 
    {
        if(chunkVisible) mesh.SetActive(visible);
        foreach (Star star in instantiatedStars)
        {
            star.gameObject.SetActive(visible);
        }



    }
    public bool isVisible(Vector3 pos, float renderDistance) 
    {
        float closestViewerDstSqrd = bounds.SqrDistance(pos);
        return closestViewerDstSqrd  <= renderDistance * renderDistance;
    }
}
*/