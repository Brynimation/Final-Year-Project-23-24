Shader "Custom/InstancingShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Colour("Colour", Color) = (1.0, 1.0, 1.0, 1.0)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        Pass
        {
            ZWrite off
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0
            #pragma multi_compile_instancing

            UNITY_INSTANCING_BUFFER_START(MyProps)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Colour)
            UNITY_INSTANCING_BUFFER_END(MyProps)
            TEXTURE2D(_MainTex);
            struct MeshProperties
            {
                float4x4 mat;
            };
            RWStructuredBuffer<MeshProperties> _Properties;
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                uint instanceId : SV_InstanceID;
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Interpolators
            {
                float2 uv : TEXCOORD0;
                float4 positionHCS : SV_POSITION;
            };

            
            Interpolators vert (Attributes i)
            {
                Interpolators o;
                float4 posOS = mul(_Properties[i.instanceId].mat, i.positionOS);
                o.positionHCS = TransformObjectToHClip(posOS);
                o.uv = i.uv;
                return o;
            }

            float4 frag (Interpolators i) : SV_Target
            {
                // sample the texture
                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);;
                //return baseTex;
                return _Colour;
            }
            ENDHLSL
        }
    }
}