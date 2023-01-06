using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleDistributor : MonoBehaviour
{
    [SerializeField] AnimationCurve distributionCurve;
    [SerializeField] int particleCount;
    [SerializeField] AnimationCurve velocityCurve;
    [SerializeField] int numArms;
    [SerializeField] float turnFraction = 1.618f;
    [SerializeField] float discRadius = 2.0f;
    [SerializeField] float galaxyRadius = 10f;
    int[] indices;
    Vector3[] verts;
    MeshFilter mf;
    MeshRenderer mr;
    Mesh mesh;
    void Start()
    {
        mf = GetComponent<MeshFilter>();
        mr = GetComponent<MeshRenderer>();
        verts = new Vector3[particleCount];
        Vector3[] uvs = new Vector3[particleCount];
        indices = new int[particleCount];
        for (int i = 0; i < particleCount; i++) 
        {
            //float theta = (i * numArms/ (float) particleCount) * 360 * Mathf.Deg2Rad * turnFraction;
            float theta = Random.Range(0, 360) * Mathf.Deg2Rad;
            float r = distributionCurve.Evaluate(i / (float)particleCount) * galaxyRadius;
            float x = r * Mathf.Cos(theta);
            float y = r * Mathf.Sin(theta);
            verts[i] = new Vector3(x, y, 0f);
            indices[i] = i;
        }
        mesh = new Mesh();
        mesh.vertices = verts;
        mesh.SetIndices(indices, MeshTopology.Points, 0);
        mf.mesh = mesh;
        
    }

    // Update is called once per frame
    float MassDisc(float r)
    {
        float d = discRadius  ; //thickness of the disk    // Dicke der Scheibe
        float centreDensity = 1;  //Density at the centre
        float rH = discRadius;   //Radius at which the centre has fallen to half // Radius auf dem die Dichte um die Hälfte gefallen ist
        return (float)centreDensity * (float)Mathf.Exp(-r / rH) * (r * r) * Mathf.PI * d;
    }
    void Update()
    {

        for(int i = 0; i < verts.Length; i++) 
        {
            Vector3 vert = verts[i];
            Vector3 forceDir = this.transform.position - vert.normalized;
            Vector3 velocityDir = new Vector3(-forceDir.y, forceDir.x, 0f);
            //float velocityMagnitude = Mathf.Sqrt((i / (float)particleCount) / (((distributionCurve.Evaluate(i / (float)particleCount) * galaxyRadius))));
            float r = distributionCurve.Evaluate(i / (float)particleCount);
            float velocityMagnitude = Mathf.Sqrt(MassDisc(r));
            vert += (velocityDir * Time.deltaTime * velocityMagnitude);
            verts[i] = vert;

        }
        mesh = new Mesh();
        mesh.vertices = verts;
        mesh.SetIndices(indices, MeshTopology.Points, 0);
        mf.mesh = mesh;
    }
}
