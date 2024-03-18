Shader "Custom/PlanetShader"
{
/*
struct PlanetTerrainProperties
{
    float roughness;
    float baseRoughness;
    float persistence;
    float minVal;
    float noiseStrength;
    float3 noiseCentre;
    int octaves;
};

*/
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Roughness("Roughness", Float) = 1.0
        _BaseRoughness("Base Roughness", Float) = 1.0
        _Persistence("Persistence", Float) = 1.0
        _MinVal("Min Val", Float) = 1.0
        _NoiseStrength("Noise Strength", Float) = 1.0 
        _NoiseCentre("Noise Centre", vector) = (0.0, 0.0, 0.0)
        _Octaves("Octaves", int) = 1
        _Testing("Testing", int) = 0
        _AmbientLight("Ambient Colour", Color) = (0.1, 0.0, 0.1, 1.0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #include "Assets/ActualProject/Helpers/Utility.hlsl"
            #pragma target 5.0
            #pragma vertex vert 
            #pragma fragment frag
            #pragma multi_compile_instancing


            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            StructuredBuffer<Planet> _Planets;

            StructuredBuffer<float3> _VertexBuffer;
            StructuredBuffer<float3> _NormalBuffer;
            StructuredBuffer<float2> _UVBuffer;

            uniform float _Roughness;
            uniform float _BaseRoughness;
            uniform float _Persistence;
            uniform float _MinVal;
            uniform float _NoiseStrength;
            uniform float3 _NoiseCentre;
            uniform float _Octaves;
            uniform float4 _AmbientLight;
            uniform bool _Testing;
            struct Attributes
            {
                uint instanceId : SV_INSTANCEID;
                uint vertexId : SV_VERTEXID;
            };

            struct Interpolators
            {
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 colour : COLOR;
                float4 positionHCS : SV_POSITION;
                float3 normWS : TEXCOORD2;
                float3 lightPosWS : TEXCOORD3;
                float4 lightColour : TEXCOORD4;
                float lightRadius : TEXCOORD5;
            };

            
            Interpolators vert (Attributes i)
            {
                Interpolators o;
                Planet planetData = _Planets[i.instanceId];
                PlanetTerrainProperties planetProperties;
                if(_Testing == 1)
                {
                    planetProperties = (PlanetTerrainProperties)0;
                    planetProperties.baseRoughness = _BaseRoughness;
                    planetProperties.roughness = _Roughness;
                    planetProperties.persistence = _Persistence;
                    planetProperties.octaves = _Octaves;
                    planetProperties.minVal = _MinVal;
                    planetProperties.noiseCentre = _NoiseCentre;
                    planetProperties.noiseStrength = _NoiseStrength;
                }else{
                    planetProperties = planetData.properties;
                }
                
                float4x4 modelMatrix = GenerateTRSMatrix(planetData.position, planetData.radius); //Create TRS matrix

                //Use perlin noise to perturb the height of this vertex so the planet is no longer perfectly spherical.
                //pNoise returns a value in range [-1, 1], so we compress this to [0, 1]
                float3 vertexPosUnitSphere = _VertexBuffer[i.vertexId];
                float3 normalOS = _NormalBuffer[i.vertexId];
                float noiseValue = fractalBrownianMotion(vertexPosUnitSphere, planetProperties);
                float maxHeightValue = fractalBrownianMotion(vertexPosUnitSphere, planetProperties, 1u);
                vertexPosUnitSphere += normalOS * noiseValue;

                //Transform vertex position in vertex buffer of the unit sphere centred at (0,0,0) to the object space of the planet
                float4 vertexPosOS = mul(modelMatrix, float4(vertexPosUnitSphere, 1.0));

                //Rotate the vertices so that the planet spins about its axis.
                vertexPosOS.xyz -= planetData.position;
                float3x3 rotationMatrix = rotateAroundAxis(planetData.rotationAxis, planetData.rotationSpeed);
                vertexPosOS.xyz = mul(vertexPosOS.xyz, rotationMatrix);
                normalOS = mul(normalOS, rotationMatrix);
                float4 rotatedVertexPos = float4(vertexPosOS.xyz + planetData.position, 1.0);


                VertexPositionInputs positionData = GetVertexPositionInputs(rotatedVertexPos); //compute world space and clip space position
                VertexNormalInputs normalData = GetVertexNormalInputs(normalOS);
                o.positionHCS = positionData.positionCS;

                float2 uv = _UVBuffer[i.vertexId];
                o.uv = uv;
                o.positionWS = positionData.positionWS;
                o.lightPosWS = planetData.primaryBody.starPosition;
                o.lightColour = lerp(planetData.primaryBody.starColour, float4(1.0, 1.0, 1.0, 1.0), 0.7); //light should be mostly white, with a slight hint of the colour of the star itself
                o.lightRadius = planetData.primaryBody.starRadius;
                o.normWS = normalData.normalWS;
                o.positionHCS = positionData.positionCS;

                float interpolator = noiseValue/maxHeightValue;
                o.colour = InterpolateColours(planetProperties.colours.colours, interpolator);
                return o;

            }


            float4 frag (Interpolators i) : SV_Target
            {
                //return float4(1.0, 0.0, 0.0, 1.0)
                /*Unlike the other bodies in the simulation that are emissive, the planets need to be shaded
                For a given planet, we will only consider the light incident on it from the star that it is orbitting
                */
                //Basic Phong shading, where the star the planet is orbitting is treated as a uniformly coloured point light
                float3 hitToLight = i.lightPosWS - i.positionWS;
                float3 lightDir = normalize(hitToLight);
                float3 viewDirection = normalize(i.positionWS - GetCameraPositionWS());
                float diffuseTerm = max(0.0, dot(lightDir, i.normWS));
                float3 reflectedDir = reflect(viewDirection, i.normWS);
                float specularTerm = pow(max(0.0, dot(lightDir, reflectedDir)), 2.0);
                float4 light = i.colour * _AmbientLight +  i.lightColour * (diffuseTerm * i.colour + specularTerm * float4(0.25, 0.25, 0.25, 1.0));
                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                return light;
                
            }
            ENDHLSL
        }
    }
}