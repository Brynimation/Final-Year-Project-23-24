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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
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
                float4x4 modelMatrix = GenerateTRSMatrix(planetData.position, planetData.radius); //Create TRS matrix

                //We want to rotate the vertices so that the planet spins about its axis.
                float4 worldPos = mul(modelMatrix, float4(_VertexBuffer[i.vertexId], 1.0));
                worldPos.xyz -= planetData.position;
                float3x3 rotationMatrix = rotateAroundAxis(planetData.rotationAxis, planetData.rotationSpeed);
                worldPos.xyz = mul(worldPos.xyz, rotationMatrix);

                float4 vertexPosOS = float4(worldPos.xyz + planetData.position, 1.0);


                VertexPositionInputs positionData = GetVertexPositionInputs(vertexPosOS); //compute world space and clip space position
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