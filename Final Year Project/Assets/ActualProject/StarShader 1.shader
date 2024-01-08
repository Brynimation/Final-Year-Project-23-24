Shader "Custom/StarShader"
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
            StructuredBuffer<SolarSystem> _SolarSystems;
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
                float4 mainColour : COLOR;
            };

            
            Interpolators vert (Attributes i)
            {
                Interpolators o;
                SolarSystem systemData = _SolarSystems[i.instanceId];
                float4x4 modelMatrix = GenerateTRSMatrix(systemData.starPosition, systemData.starRadius); //Create TRS matrix

                float4 vertexPosOS = mul(modelMatrix, float4(_VertexBuffer[i.vertexId], 1.0));
                VertexPositionInputs positionData = GetVertexPositionInputs(vertexPosOS); //compute world space and clip space position
                VertexNormalInputs normalData = GetVertexNormalInputs(_NormalBuffer[i.vertexId]);
                o.positionHCS = positionData.positionCS;

                float2 uv = _UVBuffer[i.vertexId];
                o.uv = uv;
                o.normWS = normalData.normalWS;
                o.positionHCS = positionData.positionCS;
                o.mainColour = systemData.starColour;
                return o;

            }

            float4 frag (Interpolators i) : SV_Target
            {
                //return float4(1.0, 0.0, 0.0, 1.0);
                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                return baseTex * i.mainColour;
            }
            ENDHLSL
        }
    }
}