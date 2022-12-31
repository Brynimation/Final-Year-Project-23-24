
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Endless : MonoBehaviour
{
    public float renderDistance;
    public Transform viewer;
    Vector2 viewerPosition;

    public int chunkSize;
    int chunksVisibleInViewDist;
    Dictionary<Vector2, TerrainChunk> terrainChunkDict;
    List<TerrainChunk> terrainChunkVisibleLastUpdate; 
    void Start()
    {
        chunksVisibleInViewDist = Mathf.RoundToInt(renderDistance/chunkSize);
        terrainChunkDict = new Dictionary<Vector2, TerrainChunk>();
        terrainChunkVisibleLastUpdate = new List<TerrainChunk>();
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.transform.position.x, viewer.transform.position.z);
        UpdateVisibleChunks();
    }
    void UpdateVisibleChunks() 
    {
        for (int i = 0; i < terrainChunkVisibleLastUpdate.Count; i++) 
        {
            terrainChunkVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunkVisibleLastUpdate.Clear();
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x/ chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -chunksVisibleInViewDist; yOffset < chunksVisibleInViewDist; yOffset++) 
        {
            for (int xOffset = -chunksVisibleInViewDist; xOffset < chunksVisibleInViewDist; xOffset++) 
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
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
                    TerrainChunk t = new TerrainChunk(viewedChunkCoord, chunkSize);
                    terrainChunkDict.Add(viewedChunkCoord, t);
                }
                
            }
        }
    }
}
public class TerrainChunk 
{
    Vector2 position;
    GameObject mesh;
    Bounds bounds;
    public TerrainChunk(Vector2 coord, int size) 
    {
        position = coord * size;
        bounds = new Bounds(position, Vector2.one * size);
        Vector3 position3D = new Vector3(position.x, 0f, position.y);
        mesh = GameObject.CreatePrimitive(PrimitiveType.Plane);
        mesh.transform.position = position3D;
        mesh.transform.localScale = Vector3.one * size / 10f;
    }

    /*Find the point on its perimeter that's the closest to the viewer's position. 
     It will then find the distance between that point and the viewer. If this is 
    less than the render distance, the mesh will be enabled. Otherwise, it'll be 
    disabled.*/
    public void UpdateChunk(Vector2 pos, float renderDistance) 
    {
        float closestViewerDst = Mathf.Sqrt(bounds.SqrDistance(pos)); //returns the smallest squared distance between the passed point and the bounding box
        bool visible = closestViewerDst <= renderDistance;
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
