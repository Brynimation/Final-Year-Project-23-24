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

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                uint instanceId : SV_INSTANCEID;
            };

            struct Interpolators
            {
                float2 uv : TEXCOORD0;
                float4 positionHCS : SV_POSITION;
            };

            

            Interpolators vert (Attributes i)
            {
                Interpolators o;
                float3 curObjectPos = _Planets[i.instanceId].position;
                float4 vertexPosOS = mul(GenerateTRSMatrix(curObjectPos,  _Planets[i.instanceId].radius), float4(i.positionOS.xyz, 1.0));
                VertexPositionInputs positionData = GetVertexPositionInputs(vertexPosOS); 
                o.uv = i.uv;
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
