using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;


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

    public Star2(Vector3 centre, float a, float b, int particleCount, float angleAroundEllipse = 0f, float offset = 0f)
    {
        angularOffset %= 360f * Mathf.Deg2Rad;
        id = starCount++;
        semiMajorAxis = a;
        semiMinorAxis = b;
        theta0 = angleAroundEllipse;
        float velocityMagnitude = Mathf.Sqrt((id * 50 / (starCount - 1f)) / (float)a);
        angularVelocity = velocityMagnitude / a; 
        angularOffset = offset;
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
       /* Vector3 displacement = centre - position;
        float r = displacement.magnitude;
        Vector3 forceDir = displacement.normalized;
        Vector3 velocityDir = new Vector3(-forceDir.y, forceDir.x, 0f);*/
/*        float velocityMagnitude = Mathf.Sqrt((id * 50 / (starCount - 1f)) / (float) r);
        Vector3 orbitalVelocity = velocityDir * velocityMagnitude;
        angularVelocity = velocityMagnitude / r;*/
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

struct StarVertex 
{
    public Vector3 position;
    public Vector2 uv;
    public int id;
    public float eccentricity;
    public float theta;
    public float angleOffset;
    public StarVertex(Vector3 pos, Vector2 uv, int id, float eccentricity, float theta, float angleOffset) 
    {
        this.id = id;
        this.position = pos;
        this.uv = uv;
        this.eccentricity = eccentricity;
        this.theta = theta;
        this.angleOffset = angleOffset;
    }
}
public class ParticleDistributor : MonoBehaviour
{
    GraphicsBuffer data;
    [SerializeField] Texture2D starTexture;
    [SerializeField] Transform cameraT;
    [SerializeField] Material galaxyMaterial;
    [SerializeField] AnimationCurve distributionCurve;
    [SerializeField] int particleCount;
    [SerializeField] AnimationCurve velocityCurve;
    [SerializeField] float turnFraction = 1.618f;
    [SerializeField] float coreRadius = 2.0f;
    [SerializeField] float galaxyRadius = 10f;
    [SerializeField] float haloRadius = 20f;
    [SerializeField] float maxEccentricity = 1f;
    [SerializeField] float minEccentricity;
    [SerializeField] int offsetMultiplier;
    [SerializeField] float WeinDisplacementConstant = 2.898f * Mathf.Pow(10, -3);
    [SerializeField] Gradient visibleSpectrum;
    [SerializeField] Color centreColour;
    [SerializeField] Color edgeColour;
    [SerializeField] int numH2Regions;
    [SerializeField] float minCamDist;
    float angularOffsetIncrement;
    Vector2[] majorAndMinorAxes;
    Vector2[] angles;
    Vector2[] angularVelocities;
    Vector2[] types;
    Vector2[] temperaturesAndColours;
    Color[] colours;
    Star2[] stars;
    StarVertex[] starVertices;
    int[] indices;
    Vector3[] verts;
    MeshFilter mf;
    Mesh mesh;
    int[] testArray;

    private Color calculateColour(float temperature) 
    {
        float peakWavelength = WeinDisplacementConstant / temperature;
        return Color.black;
        
    }
    private void Start()
    {
        data = new GraphicsBuffer(GraphicsBuffer.Target.Index, particleCount, sizeof(int));
        testArray = new int[particleCount];
        mf = GetComponent<MeshFilter>();
        galaxyMaterial = GetComponent<MeshRenderer>().material;
        galaxyMaterial.SetBuffer("data", data);
        galaxyMaterial.mainTexture = starTexture;
        verts = new Vector3[particleCount];
        indices = new int[particleCount];
        angularOffsetIncrement = (1f * offsetMultiplier / (particleCount - 1f)) * 360 * Mathf.Deg2Rad;

        mesh = new Mesh();
        for (int i = 0; i < particleCount; i++)
        {
            indices[i] = i;
        }
        mesh.bounds = new Bounds(transform.position, Vector3.one * haloRadius * 100000);
        /*Each star is a vertex. Send all this data to the vertex shader.*/
        mesh.SetVertices(verts, 0, particleCount);
        mesh.SetIndices(indices, MeshTopology.Points, 0);
        galaxyMaterial.SetInt("_AngularOffsetMultiplier", offsetMultiplier);
        galaxyMaterial.SetFloat("_TimeStep", Time.fixedDeltaTime);
        galaxyMaterial.SetVector("_GalacticCentre", transform.position);
        galaxyMaterial.SetFloat("_GalacticBulgeRadius", coreRadius);
        galaxyMaterial.SetFloat("_GalacticDiskRadius", galaxyRadius);
        galaxyMaterial.SetFloat("_GalacticHaloRadius", haloRadius);
        galaxyMaterial.SetInt("_NumParticles", particleCount);
        galaxyMaterial.SetFloat("_MinEccentricity", minEccentricity);
        galaxyMaterial.SetFloat("_MaxEccentricity", maxEccentricity);
        galaxyMaterial.SetColor("_CentreColour", centreColour);
        galaxyMaterial.SetColor("_EdgeColour", edgeColour) ;
        galaxyMaterial.SetVector("_CameraPosition", cameraT.position);
        galaxyMaterial.SetVector("_CameraUp", cameraT.up);
        galaxyMaterial.SetFloat("_MinCamDist", minCamDist);

        mf.mesh = mesh;
        mf.mesh.bounds = new Bounds(transform.position, Vector3.one * haloRadius);
    }
    private void Update()
    {
        galaxyMaterial.SetFloat("_MaxEccentricity", maxEccentricity);
        galaxyMaterial.SetFloat("_MinEccentricity", minEccentricity);
        galaxyMaterial.SetFloat("_GalacticBulgeRadius", coreRadius);
        galaxyMaterial.SetFloat("_GalacticDiskRadius", galaxyRadius);
        galaxyMaterial.SetFloat("_GalacticHaloRadius", haloRadius);
        galaxyMaterial.SetInt("_AngularOffsetMultiplier", offsetMultiplier);
        galaxyMaterial.SetColor("_CentreColour", centreColour);
        galaxyMaterial.SetColor("_EdgeColour", edgeColour);
        galaxyMaterial.SetVector("_CameraPosition", cameraT.position);
        galaxyMaterial.SetFloat("_MinCamDist", minCamDist);
        data.GetData(testArray);
        foreach (int i in testArray) 
        {
            if (i == 2) Debug.Log("Got one!");
        }

    }
    void Start2()
    {
        mf = GetComponent<MeshFilter>();
        galaxyMaterial = GetComponent<MeshRenderer>().material;
        verts = new Vector3[particleCount];
        angles = new Vector2[particleCount];
        majorAndMinorAxes = new Vector2[particleCount];
        types = new Vector2[particleCount];
        stars = new Star2[particleCount];
        starVertices = new StarVertex[particleCount];
        Vector3[] uvs = new Vector3[particleCount];
        colours = new Color[particleCount];
        //NativeArray<StarVertex> starVerts = new NativeArray<StarVertex>();
        
        indices = new int[particleCount];
        angularVelocities = new Vector2[particleCount];
        angularOffsetIncrement = (1f * offsetMultiplier / (particleCount - 1f)) * 360 * Mathf.Deg2Rad;
        float currentAngularDisplacement = 0f;

        VertexAttributeDescriptor[] customVertexStreams = new[] {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, stream:0),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, stream:0),
            new VertexAttributeDescriptor(format: VertexAttributeFormat.SInt32, dimension:1, stream:0), 
            new VertexAttributeDescriptor(format: VertexAttributeFormat.Float32, dimension:1, stream:0),
            new VertexAttributeDescriptor(format: VertexAttributeFormat.Float32, dimension:1, stream:0), 
            new VertexAttributeDescriptor(format: VertexAttributeFormat.Float32,dimension:1, stream:0)
        };

        mesh = new Mesh();
        mesh.SetVertexBufferParams(particleCount * 3, customVertexStreams);

        /*Initialise stars with a random initial rotation and distance from the galactic centre
         governed by the DistributionCurve.*/

        for (int i = 0; i < particleCount; i++) 
        {
            //float theta = (i * numArms/ (float) particleCount) * 360 * Mathf.Deg2Rad * turnFraction;
            float theta = Random.Range(0, 360) * Mathf.Deg2Rad;

            float r = distributionCurve.Evaluate(i / (float)particleCount) * galaxyRadius; 
            float eccentricity = getEccentricity(r);
            float a = r;
            float b = r * eccentricity;
            currentAngularDisplacement = angularOffsetIncrement * i;
            stars[i] = new Star2(transform.position, a, b, particleCount,theta, currentAngularDisplacement);
            angles[i] = new Vector2(theta, currentAngularDisplacement);
            majorAndMinorAxes[i] = new Vector2(a, b);
            //eccentricities[i] = getEccentricity(r);
            angularVelocities[i] = new Vector2(Mathf.Sqrt((i * 50) / (float)(particleCount - 1) / r), 0f);
            colours[i] = Color.Lerp(Color.white, Color.blue * Color.white, r / galaxyRadius);
            verts[i] = stars[i].position;
            indices[i] = i;
            StarVertex star = new StarVertex(verts[i], Vector2.zero, i, getEccentricity(r), theta, currentAngularDisplacement);
            starVertices[i] = star;
            if (i % (particleCount / numH2Regions) == 0)
            {
                types[i] = new Vector2(1, 0);
            }
            else {
                types[i] = new Vector2(0, 0);
            }
        }
        /*Each star is a vertex. Send all this data to the vertex shader.*/
        mesh.SetVertices(verts, 0, particleCount);
        mesh.SetIndices(indices, MeshTopology.Points, 0);
       /* mesh.SetUVs(0, uvs);
        mesh.SetUVs(1, majorAndMinorAxes);
        mesh.SetUVs(2, angles);
        mesh.SetUVs(3, angularVelocities);
        mesh.SetUVs(4, types);
        mesh.SetColors(colours); */
        //mesh.SetVertexBufferData(starVertices, 0, 0, starVertices.Length, stream: 0);
        //mesh.vertices = verts;
        galaxyMaterial.SetFloat("_TimeStep", Time.fixedDeltaTime);
        galaxyMaterial.SetVector("_GalacticCentre", transform.position);
        galaxyMaterial.SetFloat("_GalacticBulgeRadius", coreRadius);
        galaxyMaterial.SetFloat("_GalacticDiskRadius", galaxyRadius);
        galaxyMaterial.SetFloat("_GalacticHaloRadius", haloRadius);
        galaxyMaterial.SetInt("_NumParticles", particleCount);
        galaxyMaterial.SetFloat("_MinEccentricity", minEccentricity);
        galaxyMaterial.SetFloat("_MaxEccentricity", maxEccentricity);
        galaxyMaterial.SetVector("_CameraPosition", cameraT.position);



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
    void FixedUpdate2()
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
