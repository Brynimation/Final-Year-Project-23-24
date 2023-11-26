Shader "Custom/DrawProceduralShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _EmissionMap("Emission Map", 2D) = "black"{}
        [HDR] _EmissionColour("Emission colour", Color) = (0,0,0,0)
        _Emission("Emission", Range(0, 100)) = 50
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        pass
        {
            cull Off
            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #pragma target 5.0
            #pragma vertex vert 
            #pragma fragment frag
            #pragma multi_compile_instancing


            struct ThreadIdentifier
            {
                float3 position;
                float4 colour;
                float radius;
                uint id;
            };

            struct SphereVertex
            {
                float3 position;
                float2 uv;
                float3 normal;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);   

            uniform float4 _EmissionColour;
            RWStructuredBuffer<ThreadIdentifier> _PositionsLOD0;
            StructuredBuffer<float3> _VertexBuffer;
            StructuredBuffer<float3> _NormalBuffer;
            StructuredBuffer<float2> _UVBuffer;
            float4x4 _ModelMatrix;

            float GenerateRandom(int x)
            {
                float2 p = float2(x, sqrt(x));
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }
            float4x4 CreateMatrix(float3 pos, int scale) {
                return float4x4(
                    scale, 0.0, 0.0, pos.x,
                    0.0, scale, 0.0, pos.y,
                    0.0, 0.0, scale, pos.z,
                    0.0, 0.0, 0.0, 1.0
                );
            }
            struct Attributes
            {
                uint vertexId : SV_VERTEXID;
                uint instanceId : SV_INSTANCEID;
            };

            struct Interpolators
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 posWS : TEXCOORD1;
                float3 normWS : TEXCOORD2;
                float4 colour : COLOR;
            };

      
           Interpolators vert(Attributes i)
            {
                Interpolators o;
                ThreadIdentifier ti = _PositionsLOD0[i.instanceId]; //Sphere centre position 
                _ModelMatrix = CreateMatrix(ti.position, ti.radius); //Create TRS matrix
                float4 vertexPosOS = mul(_ModelMatrix, float4(_VertexBuffer[i.vertexId], 1.0)); //transform to object space of sphere 

                VertexPositionInputs positionData = GetVertexPositionInputs(vertexPosOS); //compute world space and clip space position
                VertexNormalInputs normalData = GetVertexNormalInputs(_NormalBuffer[i.vertexId]);
                o.posWS = positionData.positionWS;
                o.normWS = normalData.normalWS;
                o.positionHCS = positionData.positionCS;

                float2 uv = _UVBuffer[i.vertexId];
                o.colour = ti.colour + _EmissionColour;
                o.uv = uv;
                return o;
            }

            float4 frag(Interpolators i) : SV_TARGET0
            {
                float4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                return texel * i.colour;
            }


            ENDHLSL
        }
    }
}
