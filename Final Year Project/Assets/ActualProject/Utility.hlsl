#define _G 1.0

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
    float4 colour;
    float rotationSpeed;
    float3 rotationAxis;
};

struct MeshProperties
{
    float4x4 mat;
    float scale;
    float3 position;
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

float4x4 GenerateScaleMatrix(float3 scale)
{
    float4x4 mat = 
    {
        scale.x, 0.0, 0.0, 0.0,
        0.0, scale.y, 0.0, 0.0,
        0.0, 0.0, scale.z, 0.0,
        0.0, 0.0,   0.0,   1.0
    };
    return mat;
}
float4x4 GenerateTranslationMatrix(float3 translation)
{
    float4x4 mat = 
    {
        1.0, 0.0, 0.0, translation.x,
        0.0, 1.0, 0.0, translation.y,
        0.0, 0.0, 1.0, translation.z,
        0.0, 0.0, 0.0, 1.0
    };
    return mat;
}
float4x4 GenerateRotationMatrix(float3 r)
{
    float4x4 xRot = 
    {
        1.0, 0.0, 0.0, 0.0,
        0.0, cos(r.x), - sin(r.x), 0.0,
        0.0, sin(r.x), cos(r.x), 0.0,
        0.0, 0.0, 0.0, 1.0
    };
    float4x4 yRot = 
    {
        cos(r.y), 0.0, sin(r.y), 0.0,
        0.0, 1.0, 0.0, 0.0,
        -sin(r.y), 0.0, cos(r.y), 0.0,
        0.0, 0.0, 0.0, 1.0
    };
    float4x4 zRot = 
    {
        cos(r.z), -sin(r.z), 0.0, 0.0,
        sin(r.z), cos(r.z), 0.0, 0.0,
        0.0, 0.0, 1.0, 0.0,
        0.0, 0.0, 0.0, 1.0
    };
    return mul(zRot, mul(yRot, xRot));
}
float4x4 GenerateTRSMatrix(float3 position, float3 rotation, float3 scale)
{
    float4x4 translationM = GenerateTranslationMatrix(position);
    float4x4 scaleM = GenerateScaleMatrix(scale);
    float4x4 rotationM = GenerateRotationMatrix(rotation);
    return mul(translationM, mul(rotationM, scaleM));
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
    mp.position = position;
    mp.colour = colour;
    mp.lodLevel = lodLevel;
    return mp;
}

MeshProperties GenerateMeshProperties(float3 position, float3 rotation, float scale, int lodLevel, float4 colour)
{
    MeshProperties mp = (MeshProperties)0;
    mp.mat = GenerateTRSMatrix(position, rotation, scale);
    mp.scale = scale;
    mp.position = position;
    mp.colour = colour;
    mp.lodLevel = lodLevel;
    return mp;
}

float Hash1(float seed)
{
    // Create a bit pattern from the seed
    float bitPattern = sin(seed) * 43758.5453;
    
    // Fract() returns the fractional part of the float, 
    // giving us a pseudo-random result between 0 and 1
    return frac(bitPattern);
}

float3 Hash3(float3 position)
{
    return float3(Hash1(position.x), Hash1(position.y), Hash1(position.z));
}
float4x4 MakeRotationMatrix(float3 axis, float angle)
{
    float theta = radians(angle); // Convert to radians
    float cosTheta = cos(theta);
    float sinTheta = sin(theta);
    float3x3 rotation;

    // Calculate rotation matrix (assuming axis is normalized)
    // You can optimize this by pre-calculating for specific axes if needed
    rotation[0] = cosTheta + axis.x * axis.x * (1 - cosTheta);
    rotation[0].y = axis.x * axis.y * (1 - cosTheta) - axis.z * sinTheta;
    rotation[0].z = axis.x * axis.z * (1 - cosTheta) + axis.y * sinTheta;

    rotation[1].x = axis.y * axis.x * (1 - cosTheta) + axis.z * sinTheta;
    rotation[1].y = cosTheta + axis.y * axis.y * (1 - cosTheta);
    rotation[1].z = axis.y * axis.z * (1 - cosTheta) - axis.x * sinTheta;

    rotation[2].x = axis.z * axis.x * (1 - cosTheta) - axis.y * sinTheta;
    rotation[2].y = axis.z * axis.y * (1 - cosTheta) + axis.x * sinTheta;
    rotation[2].z = cosTheta + axis.z * axis.z * (1 - cosTheta);

    return float4x4(rotation[0], 0, rotation[1], 0, rotation[2], 0, 0, 0, 0, 1);
}

float3x3 rotateAroundAxis(float3 axis, float angle)
{
    axis = normalize(axis);
    float s = sin(angle);
    float c = cos(angle);
    float oc = 1.0 - c;

    return float3x3(
        oc * axis.x * axis.x + c, oc * axis.x * axis.y - axis.z * s, oc * axis.z * axis.x + axis.y * s,
        oc * axis.x * axis.y + axis.z * s, oc * axis.y * axis.y + c, oc * axis.y * axis.z - axis.x * s,
        oc * axis.z * axis.x - axis.y * s, oc * axis.y * axis.z + axis.x * s, oc * axis.z * axis.z + c
    );
}