Shader "Custom/PlanetShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {

        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #include "Assets/ActualProject/Utility.hlsl"
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

            struct Attributes
            {
                uint instanceId : SV_INSTANCEID;
                uint vertexId : SV_VERTEXID;
            };

            struct Interpolators
            {
                float2 uv : TEXCOORD0;
                float4 positionHCS : SV_POSITION;
                float3 normWS : TEXCOORD2;
            };

            
            Interpolators vert (Attributes i)
            {
                Interpolators o;
                Planet planetData = _Planets[i.instanceId];
                PlanetTerrainProperties properties = planetData.properties;
                float4x4 modelMatrix = GenerateTRSMatrix(planetData.position, planetData.radius); //Create TRS matrix

                /*
                //wobble
                float noiseValue = pNoise(vertexPosOS.xyz);
                float dist = distance(systemData.starPosition, playerPosition);
                float maxWobbleMagnitude = _WobbleMagnitude * systemData.starRadius / 2.0; //modulate wobble amount by star radius
                float wobbleMagnitude = lerp(0.0, maxWobbleMagnitude, systemData.fade);
                vertexPosOS.xyz +=_NormalBuffer[i.vertexId] * wobbleMagnitude * sin(_Time.w * noiseValue);
                */

                //Use perlin noise to perturb the height of this vertex so the planet is no longer perfectly spherical.
                //pNoise returns a value in range [-1, 1], so we compress this to [0, 1]
                float3 vertexPosUnitSphere = _VertexBuffer[i.vertexId];
                float noiseValue = fractalBrownianMotion(vertexPosUnitSphere, properties);
                vertexPosUnitSphere += _NormalBuffer[i.vertexId] * noiseValue;

                //Transform vertex position in vertex buffer of the unit sphere centred at (0,0,0) to the object space of the planet
                float4 vertexPosOS = mul(modelMatrix, float4(vertexPosUnitSphere, 1.0));

                //Rotate the vertices so that the planet spins about its axis.
                vertexPosOS.xyz -= planetData.position;
                float3x3 rotationMatrix = rotateAroundAxis(planetData.rotationAxis, planetData.rotationSpeed);
                vertexPosOS.xyz = mul(vertexPosOS.xyz, rotationMatrix);
                float4 rotatedVertexPos = float4(vertexPosOS.xyz + planetData.position, 1.0);


                VertexPositionInputs positionData = GetVertexPositionInputs(rotatedVertexPos); //compute world space and clip space position
                VertexNormalInputs normalData = GetVertexNormalInputs(_NormalBuffer[i.vertexId]);
                o.positionHCS = positionData.positionCS;

                float2 uv = _UVBuffer[i.vertexId];
                o.uv = uv;
                o.normWS = normalData.normalWS;
                o.positionHCS = positionData.positionCS;
                return o;

            }

            float4 frag (Interpolators i) : SV_Target
            {
                //return float4(1.0, 0.0, 0.0, 1.0);
                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);;
                return baseTex;
            }
            ENDHLSL
        }
    }
}