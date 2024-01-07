#define PI 3.14159265

struct SolarSystem
{
    float3 starPosition;
    float starRadius;
    float starMass;
    float4 starColour;
    int planetCount;  
};

struct Planet {
    float3 position;
    float mass;
    float radius;
};

struct MeshProperties
{
    float4x4 mat;
    float scale;
    float4 colour;
    int lodLevel;
};

struct Plane
{
    float3 normal;
    float distance;
};

struct InstanceData
{
    float3 position;
    float4 colour;
    float radius;
    uint culled;
};

struct ThreadIdentifier
{
    float3 position;
    float4 colour;
    float radius;
    uint id;
};

struct ChunkIdentifier 
{ 
    int chunksInViewDist;
    int chunkSize;
    int chunkType;
    float3 pos;
};

float4x4 GenerateTRSMatrix(float3 position, float scale)
{
    float4x4 mat = 
    {
        scale, 0.0, 0.0, position.x,
        0.0, scale, 0.0, position.y,
        0.0, 0.0, scale, position.z,
        0.0, 0.0, 0.0, 1.0
    };
    return mat;
}

float Hash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p+45.32);
    return frac(p.x * p.y);
}
MeshProperties GenerateMeshProperties(float3 position, float scale, int lodLevel, float4 colour)
{
    MeshProperties mp = (MeshProperties)0;
    mp.mat = GenerateTRSMatrix(position, scale);
    mp.scale = scale;
    mp.colour = colour;
    mp.lodLevel = lodLevel;
    return mp;
}

