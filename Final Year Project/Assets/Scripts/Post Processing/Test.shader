Shader "PostProcessing/Test"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white"{}
        _OverlayColour("Overlay Colour", Color) = (1.0, 1.0, 1.0, 1.0)
        _Intensity("Intensity", float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        HLSLINCLUDE

        #pragma target 5.0
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        uniform float4 _OverlayColour;
        uniform float _Intensity;

        ENDHLSL
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                uint id : SV_VERTEXID;
            };

            struct Interpolators
            {
                float2 uv : TEXCOORD0;
                float4 positionHCS : SV_POSITION;
            };

            

            Interpolators vert (Attributes i)
            {
                Interpolators o;
                o.positionHCS = TransformObjectToHClip(i.positionOS);
                o.uv = i.uv;
                return o;
            }
            Interpolators vert2(Attributes i)
            {
                Interpolators o;
                float4 newPos = i.positionOS + float4(0, 0, 5 * sin(_Time.z), 1);
                o.positionHCS = TransformObjectToHClip(newPos);
                o.uv = i.uv;
                return o;
            }

            float4 frag (Interpolators i) : SV_Target
            {
               float4 colour = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _OverlayColour;
               colour.rgb *= _Intensity;
               return colour;
            }
            float4 frag2 (Interpolators i ): SV_Target
            {
                float4 colour = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                return 1 - colour;
            }
            ENDHLSL
        }
    }
}
