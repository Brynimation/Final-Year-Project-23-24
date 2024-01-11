#define _G 1.0

struct SolarSystem
{
    float3 starPosition;
    float starRadius;
    float starMass;
    float4 starColour;
    int planetCount;  
    float fade;
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
    float fade;
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

float3 Hash13(float value){
    return float3(
        Hash1(value + 3.9812),
        Hash1(value + 7.1536),
        Hash1(value + 5.7241)
    );
}

float Hash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p+45.32);
    return frac(p.x * p.y);
}
float2 Hash22(float2 value){
    return float2(
        Hash21(value + float2(12.989, 78.233)),
        Hash21(value + float2(39.346, 11.135))
    );
}
//https://www.ronja-tutorials.com/post/024-white-noise/
float Hash31(float3 value, float3 dotDir = float3(12.9898, 78.233, 37.719))
{
    float3 val = sin(value);
    return frac(sin(dot(val, dotDir)) * 143758.5453);
}


float3 Hash33(float3 value){
    return float3(
        Hash31(value, float3(12.989, 78.233, 37.719)),
        Hash31(value, float3(39.346, 11.135, 83.155)),
        Hash31(value, float3(73.156, 52.235, 09.151))
    );
}


MeshProperties GenerateMeshProperties(float3 position, float scale, int lodLevel, float4 colour, float fade)
{
    MeshProperties mp = (MeshProperties)0;
    mp.mat = GenerateTRSMatrix(position, scale);
    mp.scale = scale;
    mp.position = position;
    mp.colour = colour;
    mp.lodLevel = lodLevel;
    mp.fade = fade;
    return mp;
}

MeshProperties GenerateMeshProperties(float3 position, float3 rotation, float scale, int lodLevel, float4 colour, float fade)
{
    MeshProperties mp = (MeshProperties)0;
    mp.mat = GenerateTRSMatrix(position, rotation, scale);
    mp.scale = scale;
    mp.position = position;
    mp.colour = colour;
    mp.lodLevel = lodLevel;
    mp.fade = fade;
    return mp;
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

float CrossFade(float3 playerPosition, float3 worldPos, float _StartFadeOutDist, float _StartFadeInDist, float _FadeDist)
{
    float dist = distance(playerPosition, worldPos);
    float fade = 0.0;
    if (dist <= _StartFadeOutDist - _FadeDist)
    {
        fade = 0.0;
    }
    else if (dist <= _StartFadeOutDist)
    {
        fade = lerp(1.0, 0.0, dist / (_StartFadeOutDist - _FadeDist));
    }
    else if (dist <= _StartFadeInDist - _FadeDist)
    {
        fade = 1.0;
    }
    else if (dist <= _StartFadeInDist)
    {
        fade = lerp(1.0, 0.0, (dist - (_StartFadeInDist - _FadeDist)) / _FadeDist);
    }
    else
    {
        fade = 0.0;
    }
    return fade;
}

//https://gist.github.com/oscnord/35cbe399853b338e281aaf6221d9a29b

/*
hash based 3d value noise
function taken from https://www.shadertoy.com/view/XslGRr
Created by inigo quilez - iq/2013
License Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.

https://creativecommons.org/licenses/by-nc-sa/3.0/deed.en#ref-appropriate-credit
*/
// ported from GLSL to HLSL
float hash( float n ) {
    return frac(sin(n)*43758.5453);
}
     
float pNoise( float3 x ) {
    // The noise function returns a value in the range -1.0f -> 1.0f
    float3 p = floor(x);
    float3 f = frac(x);
     
    f = f*f*(3.0-2.0*f);
    float n = p.x + p.y*57.0 + 113.0*p.z;
     
    return lerp(lerp(lerp( hash(n+0.0), hash(n+1.0),f.x),
            lerp( hash(n+57.0), hash(n+58.0),f.x),f.y),
            lerp(lerp( hash(n+113.0), hash(n+114.0),f.x),
            lerp( hash(n+170.0), hash(n+171.0),f.x),f.y),f.z);
}