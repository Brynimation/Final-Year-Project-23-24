using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarCreatorRenderer : MonoBehaviour
{
    // Start is called before the first frame update
    public float starSize = 1.0f;
    public Material material;
    public Mesh mesh;
    const int maxInstances = 1023;
    Matrix4x4[][] transformList;
    int numBodies;


    
   
    void Start()
    {
        numBodies = NBodyDispatcher.Instance.numBodies;
        transformList = new Matrix4x4[numBodies / maxInstances][];
        MeshFilter mf = GetComponent<MeshFilter>();
        mesh = new Mesh();
        Vector3[] vertices = new Vector3[4];

        vertices[0] = new Vector3(-starSize, -starSize, 0);
        vertices[1] = new Vector3(starSize, -starSize, 0);
        vertices[2] = new Vector3(-starSize, starSize, 0);
        vertices[3] = new Vector3(starSize, starSize, 0);

        mesh.vertices = vertices;

        int[] tri = new int[6];

        tri[0] = 0;
        tri[1] = 2;
        tri[2] = 1;

        tri[3] = 2;
        tri[4] = 3;
        tri[5] = 1;

        mesh.triangles = tri;
        mf.mesh = mesh;

        for (int i = 0; i < numBodies / maxInstances; i++) 
        {
            int instances = maxInstances;
            if (i == (numBodies / maxInstances) - 1) 
            {
                instances = numBodies % maxInstances;
            }
            transformList[i] = new Matrix4x4[instances];

            for (int j = 0; j < instances; j++) 
            {
                Matrix4x4 mat = new Matrix4x4();
                mat.SetTRS(Vector3.zero, Quaternion.Euler(0f, 0f, 0f), Vector3.one);
                transformList[i][j] = mat;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < numBodies / maxInstances; i++)
        {
            int instances = maxInstances;
            if (i == numBodies / maxInstances - 1) 
            {
                instances = numBodies % maxInstances;
            }
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            Debug.Log(i * maxInstances);
            mpb.SetInt("offsetVal", i * maxInstances);
            if (i < (numBodies / maxInstances) / 2)
            {
                mpb.SetColor("_BaseColour", new Color(0.45f, 0.5f, 0.75f, 0.5f));
            }
            else {
                mpb.SetColor("_BaseColour", new Color(0.9f, 0.4f, 0.3f, 0.5f));
            }
            Graphics.DrawMeshInstanced(mesh, 0, material, transformList[i], instances, mpb);
        }
    }
}
