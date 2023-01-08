using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class Star2 
{
    public Vector3 position;
    public static int starCount;
    int id;
    int pointsOnOrbit;
    bool orbitFinished;
    public float theta0;
    float angularVelocity;
    float angularOffset;
    float semiMajorAxis;
    float semiMinorAxis;
    public static float G = 6.67f * (float) Mathf.Pow(10, -3);
    public Vector3[] orbitPositions;

    public Star2(Vector3 centre, float semiMajorAxis, float semiMinorAxis, int particleCount, int resolution, float angleAroundEllipse = 0f, float angularOffset = 0f)
    {
        angularOffset %= 360f * Mathf.Deg2Rad;
        id = starCount++;
        this.semiMajorAxis = semiMajorAxis;
        this.semiMinorAxis = semiMinorAxis;
        theta0 = angleAroundEllipse;
        this.angularOffset = angularOffset;
        //position = new Vector3(semiMajorAxis * Mathf.Sin(theta0), semiMinorAxis * Mathf.Cos(theta0)) + centre;
        //position = getRotatedPoint(position, Quaternion.Euler(0f, 0f, angularOffset));
        position = calcPosition(centre, semiMajorAxis, semiMinorAxis, angleAroundEllipse, angularOffset);
        //DrawOrbit(centre, semiMajorAxis, semiMinorAxis,particleCount,resolution);
    }

    private void DrawOrbit(Vector3 centre, float semiMajorAxis, float semiMinorAxis, int particleCount, LineRenderer lr, int resolution) 
    {
        float angle = 0f;
        Vector3[] points = new Vector3[resolution];
        if (id % 25 == 0) 
        {
            for (int i = 0; i < resolution; i++)
            {
                Vector3 pos = GetPointOnEllipse(centre, starCount, Time.fixedDeltaTime);//calcPosition(centre, semiMajorAxis, semiMinorAxis, angle, angularOffset);
                points[i] = pos;
                lr.SetPosition((id/25) * resolution + i, pos);
            }
        }

        
    }
    private Vector3 calcPosition(Vector3 centre, float semiMajorAxis, float semiMinorAxis, float theta = 0f, float angularOffset = 0f) 
    {
        float cosTheta = Mathf.Cos(theta);
        float sinTheta = Mathf.Sin(theta);
        float cosOffset = Mathf.Cos(angularOffset);
        float sinOffset = Mathf.Sin(angularOffset);
        return new Vector3(semiMajorAxis * cosTheta * cosOffset - semiMinorAxis * sinTheta * sinOffset + centre.x,
            semiMajorAxis * cosTheta * sinOffset + semiMinorAxis * sinTheta * cosOffset + centre.y, 0f);
    }
    public Vector3 GetPointOnEllipse(Vector3 centre, int starCount, float timeStep) 
    {
        theta0 = (theta0 > 360f * Mathf.Deg2Rad) ? theta0 - 360f * Mathf.Deg2Rad : theta0; //
        Vector3 displacement = centre - position;
        float r = displacement.magnitude;
        Vector3 forceDir = displacement.normalized;
        Vector3 velocityDir = new Vector3(-forceDir.y, forceDir.x, 0f);
        float velocityMagnitude = Mathf.Sqrt((id * 50 / (starCount - 1f)) / (float) r);
        Vector3 orbitalVelocity = velocityDir * velocityMagnitude;
        angularVelocity = velocityMagnitude / r;
        theta0 += (angularVelocity * timeStep * 1f);
        //position = new Vector3(semiMajorAxis * Mathf.Sin(theta0), semiMinorAxis * Mathf.Cos(theta0)) + centre;
        //position = getRotatedPoint(position, Quaternion.Euler(0f, 0f, angularOffset));
        position = calcPosition(centre, semiMajorAxis, semiMinorAxis, theta0, angularOffset);

     
        return position;
    }
    private Vector3 getRotatedPoint(Vector3 centre, Quaternion rot)
    {
        Vector3 dir = position - centre;
        dir = rot * dir;
        return position + dir;
    }
}
public class ParticleDistributor : MonoBehaviour
{
    [SerializeField] AnimationCurve distributionCurve;
    [SerializeField] int particleCount;
    [SerializeField] AnimationCurve velocityCurve;
    [SerializeField] int numArms;
    [SerializeField] float turnFraction = 1.618f;
    [SerializeField] float coreRadius = 2.0f;
    [SerializeField] float galaxyRadius = 10f;
    [SerializeField] float haloRadius = 20f;
    [SerializeField] float maxEccentricity = 1f;
    [SerializeField] float minEccentricity;
    [SerializeField] float offsetMultiplier;
    [SerializeField] Material lineMat;
    [SerializeField] int resolution = 20;
    LineRenderer lineRenderer;
    float angularOffsetIncrement;
    Star2[] stars;
    int[] indices;
    Vector3[] verts;
    MeshFilter mf;
    Mesh mesh;
    void Start()
    {
        mf = GetComponent<MeshFilter>();
        verts = new Vector3[particleCount];
        stars = new Star2[particleCount];
        Vector3[] uvs = new Vector3[particleCount];
        indices = new int[particleCount];
        angularOffsetIncrement = (1f * offsetMultiplier / (particleCount - 1f)) * 360 * Mathf.Deg2Rad;
        float currentAngularDisplacement = 0f;
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = lineRenderer.endWidth = 0.01f;
        lineRenderer.positionCount = particleCount * resolution;
        for (int i = 0; i < particleCount; i++) 
        {
            //float theta = (i * numArms/ (float) particleCount) * 360 * Mathf.Deg2Rad * turnFraction;
            float theta = Random.Range(0, 360) * Mathf.Deg2Rad;
            float r = distributionCurve.Evaluate(i / (float)particleCount) * galaxyRadius;
            float eccentricity = getEccentricity(r);
            float a = r;
            float b = r * eccentricity;

            stars[i] = new Star2(transform.position, a, b, particleCount, resolution, theta, currentAngularDisplacement);
            
            verts[i] = stars[i].position;
            indices[i] = i;
            currentAngularDisplacement = angularOffsetIncrement * i;
            
        }
        mesh = new Mesh();
        mesh.vertices = verts;
        mesh.SetIndices(indices, MeshTopology.Points, 0);
        mf.mesh = mesh;
        
    }
    private float getEccentricity(float radius) 
    {
        if (radius < coreRadius)
        {
            return Mathf.Lerp(maxEccentricity, minEccentricity, radius / coreRadius);
        }
        else if (radius >= coreRadius && radius < galaxyRadius)
        {
            return Mathf.Lerp(minEccentricity, maxEccentricity, (radius - coreRadius) / (galaxyRadius - coreRadius));
        }
        else if (radius >= galaxyRadius && radius < haloRadius)
        {
            return Mathf.Lerp(maxEccentricity, 1f, (radius - galaxyRadius) /(haloRadius - galaxyRadius));
        }
        else {
            return 1f;
        }
    }

    // Update is called once per frame
    float MassDisc(float r)
    {
        
        float d = coreRadius  ; //thickness of the disk    // Dicke der Scheibe
        float centreDensity = 1;  //Density at the centre
        float rH = coreRadius;   //Radius at which the centre has fallen to half // Radius auf dem die Dichte um die Hälfte gefallen ist
        return (float)centreDensity * (float)Mathf.Exp(-r / rH) * (r * r) * Mathf.PI * d;
    }
    void FixedUpdate()
    {
        for (int i = 0; i < stars.Length; i++) 
        {
            stars[i].GetPointOnEllipse(transform.position, particleCount, Time.fixedDeltaTime);
            verts[i] = stars[i].position;
        }

        /*for(int i = 0; i < verts.Length; i++) 
        {
            Vector3 vert = verts[i];
            Vector3 displacement = this.transform.position - vert;
            Vector3 forceDir = displacement.normalized;
            Vector3 velocityDir = new Vector3(-forceDir.y, forceDir.x, 0f);
            //float velocityMagnitude = Mathf.Sqrt((i / (float)particleCount) / (((distributionCurve.Evaluate(i / (float)particleCount) * galaxyRadius))));
            float dist = displacement.magnitude;
            float semiMajorAxis = galaxyRadius * 5;
            float velocityMagnitude = Mathf.Sqrt(particleCount * (2f / dist - 1f/semiMajorAxis));
            //float r = distributionCurve.Evaluate(i / (float)particleCount);
            //float velocityMagnitude = Mathf.Sqrt(MassDisc(r));
            vert += (velocityDir * Time.deltaTime * velocityMagnitude);
            verts[i] = vert;

        }*/
        mesh = new Mesh();
        mesh.vertices = verts;
        mesh.SetIndices(indices, MeshTopology.Points, 0);
        mf.mesh = mesh;
    }
}
