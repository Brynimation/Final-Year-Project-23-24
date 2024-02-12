#define _G 1.0 //Universal Gravitational Constant - 6.67 * 10 - 11
#define _sigma 1.0/(4.0 * PI)//Stefan-Boltzmann Constant - 5.67 * 10^-8, calculated when the sun has luminosity, radius and temperature = 1
#define _k 508.0 //Wien's displacement constant, 5.898 * 10-3, multiplied by 10^9 and divided by the sun's surface temperature * 5 in kelvin
#define _SPECULAR_COLOR 
#define _SPECGLOSSMAP
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

struct MeshProperties
{
    float4x4 mat;
    float scale;
    float3 position;
    float4 colour;
    float fade;
    int lodLevel;
};

struct Star
{
    float3 starPosition;
    float starRadius;
    float starMass;
    float starLuminosity;
    float4 starColour;
};

struct SolarSystem
{
    Star star;
    int planetCount;  
    float fade;
};

struct PlanetTerrainColours
{
    float4 colours[4];
};
struct PlanetTerrainProperties
{
    float roughness;
    float baseRoughness;
    float persistence;
    float minVal;
    float noiseStrength;
    float3 noiseCentre;
    int octaves;
    PlanetTerrainColours colours;
};
struct Planet {
    float3 position;
    float mass;
    float radius;
    float4 colour;
    float rotationSpeed;
    float3 rotationAxis;
    Star primaryBody;
    PlanetTerrainProperties properties;
};

struct GalaxyProperties
{
    MeshProperties mp;
    int numParticles;
    float minEccentricity;
    float maxEccentricity;
    float galacticDiskRadius;
    float galacticHaloRadius;
    float galacticBulgeRadius;
    int angularOffsetMultiplier;
    float maxHaloRadius;
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

struct GalaxyStar
{
    float3 position;
    float4 colour;
    float radius;
};

struct ChunkIdentifier 
{ 
    int chunksInViewDist; //Number of chunks rendered within the player's view radius
    int chunkSize;  //Distance between adjacent chunks
    int chunkType; //Represents whether the chunk will hold a galactic cluster, galaxy or solar system
    float3 pos; //The position of the chunk most recently entered by the player
};

struct TriggerChunkIdentifier
{
    ChunkIdentifier cid; //Holds the properties of the chunk that caused the change in scale
    float3 cameraForward; //The direction of the camera's local z-axis at the instant the player enters the chunk
    uint entered; //1 if the player just entered a lower scale, 0 if they just left it
};

int ChunkTypeToIndex(int ChunkType)
{
    //chunk type 4 = 0
    //chunk type 3 = 1
    //chunk type 2 = 2
    switch(ChunkType)
    {
        case 3:
            return 0;
            break;
        case 2:
            return 1;
            break;
        default:
            break;
            return -1;
    }
    return -1;
}
float ChunkTypeToDistanceMultiplier(int chunkType)
{

    switch(chunkType)
    {
        case 4:
            return 1.5;
            break;
        case 3:
            return 1.2;
            break;
        case 2:
            return 0.5;
            break;
        default:
            return 1.0;
            break;
    }
    return 1.0;

}
//Abundances of different
//luminosities from: https://astrobackyard.com/types-of-stars/ (adapted to only include main sequence stars)
float weightedRandomSample(float r, float typeM = 0.01, float typeK = 0.1, float typeG = 1.0, float typeF= 20.0, float typeAB = 1000.0, float typeO = 10000.0) {
    if (r < 0.85) return typeM;
    else if (r < 0.93) return typeK;
    else if (r < 0.97) return typeG;
    else if (r < 0.99) return typeF;
    else if (r < 0.999) return typeAB;
    else if (r < 0.999999) return typeO;
    else return typeO;
}

float weightedRandomSampleRadius(float r) {
    return weightedRandomSample(r, 0.3, 0.8, 1.0, 1.3, 5.0, 10.0);
}

float CalculateStarSurfaceTemperature(float luminosity, float radius)
{
    //Use Stefan-Boltzmann law
    return pow((luminosity / (pow(radius, 2.0) * _sigma * 4.0 * PI)), 0.25);
}
float CalculatePeakWavelength(float surfaceTemperature)
{
    //Use Wien's Displacement law
    return _k / surfaceTemperature;
}
float4 ColourFromWavelength(float wavelength, float minWavelength, float maxWavelength)
{
    float interpolator = max(wavelength - minWavelength, 0.0) / (maxWavelength - minWavelength);
    float solColourInterpolator = max(_k - minWavelength, 0.0) / (maxWavelength - minWavelength);
    
    if(interpolator < solColourInterpolator)
    {
        return lerp(float4(0.0, 0.0, 1.0, 1.0), float4(1.0, 0.5, 0.5, 1.0), interpolator * 1.0/solColourInterpolator);
    }
    return lerp(float4(1.0, 0.5, 0.5, 1.0), float4(1.0, 0.0, 0.0, 0.0), (interpolator - solColourInterpolator) * 1.0/solColourInterpolator);

}
float4 ColourFromWavelength(float waveLength, float4 blue, float4 blueishWhite, float4 white, float4 yellowWhite, float4 yellowOrange, float4 orangeRed)
{
    if(waveLength < 96)
    {
        return blue;
    }else if(waveLength >= 96 && waveLength < 145)
    {
        return blueishWhite;
    }else if(waveLength >= 145 && waveLength < 341)
    {
        return white;
    }else if(waveLength >= 341 && waveLength < 446)
    {
        return yellowWhite;
    }else if(waveLength >= 446 && waveLength < 508)
    {
        return yellowOrange;
    }else if(waveLength >= 508){
        return orangeRed;
    }
    return orangeRed;
}

float4 ColourFromLuminosity(float luminosity, float radius, float minWavelength, float maxWavelength)
{
    float temp = CalculateStarSurfaceTemperature(luminosity, radius);
    float wavelength = CalculatePeakWavelength(temp);
    return ColourFromWavelength(wavelength, minWavelength, maxWavelength);
}


float4 ColourFromLuminosity(float luminosity, float radius, float4 blue, float4 blueishWhite, float4 white, float4 yellowWhite, float4 yellowOrange, float4 orangeRed)
{
    float temp = CalculateStarSurfaceTemperature(luminosity, radius);
    if(temp > 7.01)// Type O
    {
        return blue;
    }else if(temp > 1.5 && temp <= 7.01) //type B and A
    {
        return blueishWhite;
    }else if(temp > 1.14 && temp <= 1.5) //Type F
    {
        return white;
    }else if(temp > 1.00 && temp <= 1.14) //Type G
    {
        return yellowWhite;
    }else if(temp > 0.56 && temp <= 1.00) //Type K
    {
        return yellowOrange;
    }else{ //tyoe M
        return orangeRed;
    }
   /* float wavelength = CalculatePeakWavelength(temp);
    return ColourFromWavelength(wavelength, blue, blueishWhite, white, yellowWhite, yellowOrange, orangeRed);
    O - 7.0T, 10R, 10000000L 
    B -  3.5T, 5R, 100000L
    A - 1.5T, 1.7R, 2000L
    F - 1.14T, 1.3R, 400L
    G - 1T, 1R, 100L
    K - 0.79T, 0.7R, 20L
    M - 0.56T, 0.2R, L
    
    */
}

float CalculatePlanetAngularVelocity(float dist, float starMass, float planetMass)
{
    //m1rw^2 = Gm1m2/r^2 
    //w = sqrt(Gm2 / r^3)
    float angularVelSqrd = _G * starMass / pow(dist, 3.0);
    float angularVelocity = sqrt(angularVelSqrd);
    return angularVelocity;
}
float calculateSphereMass(float sphereRadius, float sphereDensity)
{
    return (4.0 / 3.0) * PI * pow(sphereRadius, 3.0) * sphereDensity;
}
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

float3 RotationFromPosition(float3 position)
{
   float3 val = float3(
        dot(position, float3(1.0, 2.0, 0.5)),
        dot(position, float3(0.2, 0.1, 2.0)),
        dot(position, float3(2.5, 1.2, 3.3))
    
);
    return val * PI * 2.0;

}
float2x2 RotationMatrix(float angle)
{
    float2x2 mat = 
    {
        cos(angle), -sin(angle),
        sin(angle), cos(angle)
    };
    return mat;
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

GalaxyProperties GenerateGalaxyProperties(MeshProperties mp, float minEccentricity, float maxEccentricity, float haloRadius, float bulgeRadius, float diskRadius, float angularOffsetMultiplier, int numParticles, float maxHaloRadius)
{
    GalaxyProperties gp = (GalaxyProperties)0;
    gp.mp = mp;
    gp.minEccentricity = minEccentricity;
    gp.maxEccentricity = maxEccentricity;
    gp.galacticHaloRadius = haloRadius;
    gp.galacticBulgeRadius = bulgeRadius;
    gp.galacticDiskRadius = diskRadius;
    gp.angularOffsetMultiplier = angularOffsetMultiplier;
    gp.numParticles = numParticles;
    gp.maxHaloRadius = maxHaloRadius;
    return gp;
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
        fade = (dist - (_StartFadeOutDist - _FadeDist)) / _FadeDist;
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

float DiscardPixelLODCrossFade(float4 posHCS, float fade)
{
    float dither = InterleavedGradientNoise(posHCS, fade);
    return fade - dither;
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

float pNoise2( float2 uv) {
    // The noise function returns a value in the range -1.0f -> 1.0f
    float2 p = floor(uv);
    float2 f = frac(uv);
     
    f = f*f*(3.0-2.0*f);
    float n = p.x + p.y*57.0;
     
    return lerp(lerp( hash(n+0.0), hash(n+1.0),f.x),
            lerp( hash(n+57.0), hash(n+58.0),f.x),f.y),
            lerp(lerp( hash(n+113.0), hash(n+114.0),f.x),
            lerp( hash(n+170.0), hash(n+171.0),f.x),f.y);
}

//Resource on Fractal Brownian Motion: https://thebookofshaders.com/13/
/*By adding different iterations of noise (octaves), where we successively increment the frequencies in regular steps (lacunarity) and decrease the amplitude (gain) 
of the noise, we can obtain a finer granularity in the noise and get more fine detail. This is known as Fractal Brownian Motion (fBM or fractal noise)

//properties
const int octaves = 1;
float lacunarity = 2.0;
float gain = 0.5;

//Initial Values
for(int i = 0; i < octaves; i++)
{
    y += amplitude * noise(frequency * x);
    frequency *= lacunarity;
    amplitude *= gain;
}

With each octave added, the curve gets more detailed. There is also self similarity; if you zoom in on the curve, a smaller part looks similar to the whole thing, and each
section looks essentially the same as any other section. This is a property of mathematical fractals that we are simulating in our loop. Our loop would produce a true 
mathematical fractal should it be allowed to run indefinitely.
*/
float noise(float2 uv)
{
    float2 i = floor(uv);
    float2 f = frac(uv);

    //Four corners of the 2d tile
    float a = Hash21(i);
    float b = Hash21(i + float2(1.0, -1.0));
    float c = Hash21(i + float2(-1.0, 1.0));
    float d = Hash21(i + float2(1.0, 1.0));
    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(a, b, u.x) * (c - a) * u.y * (1.0 - u.x) * (d - b) * u.x * u.y;
}

float fbm (float2 uv) {
    // Initial values
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 2.0;
    //
    const int octaves = 4;
    float lacunarity = 2.0;
    float gain = 0.5;

    //Initial Values
    for(int i = 0; i < octaves; i++)
    {
        value += amplitude * pNoise2(uv);
        frequency *= lacunarity;
        amplitude *= gain;
    }
    return value;
}

//https://www.youtube.com/watch?v=uY9PAcNMu8s&list=PLFt_AvWsXl0cONs3T0By4puYy6GM22ko8&index=3
//https://thebookofshaders.com/13/
float fractalBrownianMotion(float3 pos, PlanetTerrainProperties properties, uint getMaxNoise = 0u)
{
    float noiseVal = 0.0;
    float amplitude = 1.0;
    float frequency = properties.baseRoughness;
    float lacunarity = properties.roughness;
    float persistence = properties.persistence;
    float3 centre = properties.noiseCentre;
    float minVal = properties.minVal;
    int numOctaves = properties.octaves;
    for (int i = 0; i < numOctaves; i++)
    {
        float noise = (getMaxNoise == 0u) ? pNoise(pos * frequency + centre) : 1.0;
        float v = 0.5 * (noise + 1.0);
        noiseVal += v * amplitude;
        frequency *= lacunarity;
        amplitude *= persistence;

    }
    noiseVal = max(0.0, noiseVal - minVal);
    return noiseVal * properties.noiseStrength;
}


float4 InterpolateColours(float4 colours[4], float t)
{
    if (t <= 0)
        return colours[0];
    if(t >= 0.99)
        return colours[3];
    float segmentWidth = (float) 1 / (4 - 1);
    int index = floor(t / segmentWidth);
    float localT = (t - index * segmentWidth) / segmentWidth;
    return lerp(colours[index], colours[index + 1], localT);
    /*
    if t<= 0:
        return values[0]
    if t >= 1:
        return values[n-1]

    // Determine the segment
    segmentWidth = 1 / (n - 1)
    i = floor(t / segmentWidth)
    localT = (t - i * segmentWidth) / segmentWidth

    // Perform linear interpolation on the segment
    return (1 - localT) * values[i] + localT * values[i + 1] */
}

float4 Galaxy(float2 uv, float a1, float a2, float cut) {
    

    float seed = Hash21(uv);
        
    float4 col = 0.0;
    float3 dustCol = float3(0.3, 0.6, 1.0);

    float alpha = 0.0;
    int numStars = 15;
        
    float ringWidth = lerp(15.0, 25.0, seed);
    float twist = lerp(0.3, 2.0, frac(seed * 10.0));
    float flip = 1.0;
    float t = _Time.y;
    float z;
    float r;
    float ell;
    float n;
    float d;
    float sL;
    float sN;
    float i;

    if(cut==0.0) twist = 1.0;
        
    for(i=0.0; i<1.0; i+=1.0/40.0) {

        flip *= -1.0;
        z = lerp(.06, 0., i)*flip*frac(sin(i*563.2)*673.2);
        r = lerp(.1, 1., i);
        
        float2 st = mul(RotationMatrix(i*6.2832*twist + _Time.y), uv);
        st.x *= lerp(1.0, 0.5, i);

        ell = exp(-0.5*abs(dot(st,st)-r)*ringWidth);
        float3 dust = pNoise(float3(st, uv.x));
        float3 dL = pow(ell * dust/r, 1.0);


        float2 id = floor(st);
        float n = Hash21(id);
        d = length(st); 

        sL = smoothstep(0.5, 0.0, d)*pow(dL.r,2.0)*0.2/d;
           
        sN = sL;
        //sL *= sin(n*784.+T)*.5+.5;
        //sL += sN*smoothstep(0.9999,1.0, sin(n*784.0 +_Time.x *0.05))*10.;
        col.rgb += dL*dustCol;// * 100.0;

        col.a += dL.r*dL.g*dL.b;

        if(i>3./numStars)
        col += sL;//* lerp(float3(0.5+sin(n*100.0)*0.5, 0.5, 1.0), v(1), n);
    }

    col.xyz = col.xyz/40.0;
    return col;
}