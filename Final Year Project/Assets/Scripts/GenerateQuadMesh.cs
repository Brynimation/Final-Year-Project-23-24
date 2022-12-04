using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateQuadMesh : MonoBehaviour
{

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    public int xResolution;
    public int yResolution;
    public Material material;
    public Vector3 cameraPos;

    public int xSize;
    public int ySize;
    int triangleIndex;
    int vertexIndex;
    int indexVal;

    Vector3[] vertices;
    int[] triangles;
    int[] indices;
    Vector2[] uvs; 

    void Start()
    {
        meshRenderer = this.gameObject.AddComponent<MeshRenderer>();
        meshFilter = this.gameObject.AddComponent<MeshFilter>();
        GeneratePoints();
        //GeneratePlane();
    }

    void GeneratePoints()
    {
        uvs = new Vector2[xResolution * yResolution];
        vertices = new Vector3[xResolution * yResolution];
        indices = new int[xResolution * yResolution];
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        for(int x = 0; x < xResolution; x++){
            for(int y = 0; y < yResolution; y++)
            {
                uvs[vertexIndex] = new Vector3((x/(float)xResolution), y/(float)yResolution);
                vertices[vertexIndex] = new Vector3((x * xSize)/xResolution, (y * ySize)/yResolution, 0f);
                indices[vertexIndex] = vertexIndex;
                vertexIndex++;
            }
        }
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.SetIndices(indices, MeshTopology.Points, 0);
        mesh.RecalculateBounds();
        meshFilter.mesh = mesh;
        meshRenderer.sharedMaterial = material;
    }

    void GeneratePlane()
    {
        uvs = new Vector2[xResolution * yResolution];
        vertices = new Vector3[xResolution * yResolution];
        triangles = new int[xResolution * yResolution * 6];
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        for(int x = 0; x < xResolution; x++){
            for(int y = 0; y < yResolution; y++)
            {
                uvs[vertexIndex] = new Vector3((x/(float)xResolution), y/(float)yResolution);
                vertices[vertexIndex] = new Vector3((x * xSize)/xResolution, (y * ySize)/yResolution, 0f);
                if(x < xResolution - 1 && y < yResolution - 1)
                {
                    addTriangle(vertexIndex, vertexIndex + 1, vertexIndex + xResolution + 1); // triangles must be wound clockwise
                    addTriangle(vertexIndex, vertexIndex + xResolution + 1, vertexIndex + xResolution);
                }
                vertexIndex++;
            }
        }
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
        meshRenderer.material = material;
    }
    private void Update()
    {
        cameraPos = Camera.main.transform.position;
        Debug.Log(cameraPos);
        material.SetVector("_CameraPosition", cameraPos);
    }

    public void addTriangle(int a, int b, int c){
        triangles[triangleIndex] = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;
        triangleIndex +=3;

    }
    
    
}
