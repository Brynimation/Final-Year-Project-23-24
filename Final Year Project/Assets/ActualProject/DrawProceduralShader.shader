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
            float4x4 CreateMatrix(float3 pos, float3 dir, float3 up, uint id, int scale) {
                float3 zaxis = normalize(dir);
                float3 xaxis = normalize(cross(up, zaxis));
                float3 yaxis = cross(zaxis, xaxis);
                //float scale = GenerateRandom(id) * _MaxStarSize;
                //Transform the vertex into the object space of the currently drawn mesh using a Transform Rotation Scale matrix.
                //We add 2 to the scale to make the transition between billboard mesh and sphere mesh more seamless
                return float4x4(
                    xaxis.x * (scale + 10), yaxis.x, zaxis.x, 1,
                    xaxis.y, yaxis.y * (scale + 10), zaxis.y, 1,
                    xaxis.z, yaxis.z, zaxis.z * (scale+10), 1,
                    0, 0, 0, 1
                );
            }
            struct Attributes
            {
                uint vertexId : SV_VERTEXID;
                //float4 positionOS : POSITION;
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

            /*Interpolators vert(Attributes i)
            {
                Interpolators o;
                ThreadIdentifier ti = _PositionsLOD0[i.instanceId];
                _ModelMatrix = CreateMatrix(ti.position, float3(1.0, 1.0, 1.0), float3(0.0, 1.0, 0.0), i.instanceId, ti.radius);
                float4 vertexPosOS = float4(ti.position, 0) + mul(_ModelMatrix, _VertexBuffer[i.vertexId], 1.0);
                VertexPositionInputs positionData = GetVertexPositionInputs(vertexPosOS);
                VertexNormalInputs normalData = GetVertexNormalInputs(_NormalBuffer[i.vertexId]);
                o.colour = ti.colour + _EmissionColour;
                o.uv = uv;
                return o;
            }*/
            
           Interpolators vert(Attributes i)
            {
                Interpolators o;
                ThreadIdentifier ti = _PositionsLOD0[i.instanceId];
                _ModelMatrix = CreateMatrix(ti.position, float3(1.0, 1.0, 1.0), float3(0.0, 1.0, 0.0), i.instanceId, ti.radius);
                float4 vertexPosOS = float4(ti.position, 0) + mul(_ModelMatrix, _VertexBuffer[i.vertexId]);
                VertexPositionInputs positionData = GetVertexPositionInputs(vertexPosOS);
                VertexNormalInputs normalData = GetVertexNormalInputs(_NormalBuffer[i.vertexId]);
                float2 uv = _UVBuffer[i.vertexId];
                o.positionHCS = positionData.positionCS;
                o.colour = ti.colour + _EmissionColour;
                o.uv = uv;
                return o;
               /* Interpolators o;
                _ModelMatrix = CreateMatrix(_PositionsLOD0[i.instanceId].position, float3(1.0, 1.0, 1.0), float3(0.0, 1.0, 0.0), i.instanceId, _PositionsLOD0[i.instanceId].radius);
                float4 vertexPosOS = float4(_PositionsLOD0[i.instanceId].position, 0) +  mul(_ModelMatrix, _VertexBuffer[i.vertexId]);//float4(_Positions[i.instanceId] + _VertexBuffer[i.vertexId], 1.0);
                
                VertexPositionInputs positionData = GetVertexPositionInputs(vertexPosOS);
                VertexNormalInputs normalData = GetVertexNormalInputs(_NormalBuffer[i.vertexId]);
                float2 uv = _UVBuffer[i.vertexId];

                o.posWS = positionData.positionWS;
                o.normWS = normalData.normalWS;
                o.positionHCS = positionData.positionCS;
                //o.positionHCS = (_PositionsLOD0[i.instanceId].culled == 0) ? float4(-1000, -1000, -1000, 1) : o.positionHCS; 
                o.colour = _PositionsLOD0[i.instanceId].colour + _EmissionColour;
                o.uv = uv;
                return o; */
            }/*
            Interpolators vert(Attributes i)
            {
                Interpolators o;
                InstanceData data = _PositionsLOD0.Consume();
                _ModelMatrix = CreateMatrix(data.position, float3(1.0, 1.0, 1.0), float3(0.0, 1.0, 0.0), data.id, data.radius);
                float4 vertexPosOS = float4(data.position, 0) +  mul(_ModelMatrix, _VertexBuffer[i.vertexId]);//float4(_Positions[i.instanceId] + _VertexBuffer[i.vertexId], 1.0);
                
                VertexPositionInputs positionData = GetVertexPositionInputs(vertexPosOS);
                VertexNormalInputs normalData = GetVertexNormalInputs(_NormalBuffer[i.vertexId]);
                float2 uv = _UVBuffer[i.vertexId];

                o.posWS = positionData.positionWS;
                o.normWS = normalData.normalWS;
                o.positionHCS = positionData.positionCS;
                o.positionHCS = (data.culled == 0) ? float4(-1000, -1000, -1000, 1) : o.positionHCS; 
                o.colour = data.colour;
                o.uv = uv;
                return o;
            }
            */
            float4 frag(Interpolators i) : SV_TARGET0
            {
                float4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                return texel * i.colour;
            }


            ENDHLSL
        }
    }
}
