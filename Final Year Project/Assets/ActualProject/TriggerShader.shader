//ShaderLab is a Unity specific language that bridges the gap between HLSL and Unity. Everything
//defined outside of the Passes is written in ShaderLab. Everything within the passes
//is written in HLSL.

//https://www.braynzarsoft.net/viewtutorial/q16390-36-billboarding-geometry-shader - billboarding in geometry shader tutorial
//https://www.youtube.com/watch?v=gY1Mx4kkZPU&t=603s
//https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-semantics
//https://gist.github.com/fuqunaga/1a649158d69241d31b023ec9983b0164

/*In spiral galaxies the velocities of stars in the outer orbits are much faster than expected.- https://sites.ualberta.ca/~pogosyan/teaching/ASTRO_122/lect24/lecture24.html*/


Shader "Custom/TriggerShader"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _CameraUp("Camera Up", vector) = (0.0,0.0,0.0)
        _CameraPosition("Camera Position", vector) = (0.0,0.0,0.0)
        _EmissionMap("Emission Map", 2D) = "black"{}
        [HDR] _EmissionColour("Emission colour", Color) = (0,0,0,0)
        _Emission("Emission", Range(0, 100)) = 50
        _CellSize("CellSize", Float) = 1.0
        _Colour("Colour", Color) = (1.0, 0.0, 0.0, 1.0)
        _BorderColour("BorderColour", Color) = (1.0, 0.2, 0.2, 1.0)
        _BorderWidth("BorderWidth", Float) = 0.1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }
        Cull Off
        //ZTest Always
        ZWrite On
        LOD 100
        Pass
        {

            HLSLPROGRAM
            #pragma vertex vert 
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 5.0
            #include "Assets/ActualProject/Utility.hlsl"

            UNITY_INSTANCING_BUFFER_START(MyProps)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColour)
            UNITY_INSTANCING_BUFFER_END(MyProps)

            //https://forum.unity.com/threads/generate-random-float-between-0-and-1-in-shader.610810/
            float GenerateRandom(float2 xy)
            {
                return round(frac(sin(dot(xy, float2(12.9898, 78.233))) * 43758.5453) * 1000);
            }


            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);
            uniform float _CellSize;
            uniform float4 _Colour;
            uniform float4 _BorderColour;
            uniform float _BorderWidth;

            float4 _EmissionColour;
            float _Emission;
            float3 _CameraPosition;
            RWStructuredBuffer<TriggerChunkIdentifier> _TriggerBuffer;
            RWStructuredBuffer<ChunkIdentifier> _ChunksBuffer;

            struct Attributes
            {
                float4 vertexPosOS : POSITION;
                uint instanceId : SV_INSTANCEID;
            };

            struct Interpolators
            {
                float4 positionHCS : SV_POSITION; //SV_POSITION = semantic = System Value position - pixel position
                //float size : PSIZE; //Size of each vertex.
                float4 colour : COLOR;
            };
                
            Interpolators vert(Attributes i)
            {
                Interpolators o;
                int chunkIndex = ChunkTypeToIndex(4);
                float3 pos;
                if(chunkIndex == -1)
                {
                    pos = -10000.0;
                }else{
                    pos = _TriggerBuffer[chunkIndex].cid.pos;
                }
                o.positionHCS = TransformObjectToHClip(pos + i.vertexPosOS);
                o.colour = float4(1.0, 0.0, 0.0, 1.0);
                o.colour *= 1000.0;
                return o;
            }

            float4 frag(Interpolators i) : SV_Target
            {
                return i.colour;
            }
            ENDHLSL
        }
    }
}
