//ShaderLab is a Unity specific language that bridges the gap between HLSL and Unity. Everything
//defined outside of the Passes is written in ShaderLab. Everything within the passes
//is written in HLSL.
//https://www.youtube.com/watch?v=gY1Mx4kkZPU&t=603s
//https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-semantics

/*In spiral galaxies the velocities of stars in the outer orbits are much faster than expected.- https://sites.ualberta.ca/~pogosyan/teaching/ASTRO_122/lect24/lecture24.html*/


Shader "Custom/GPUInstancingShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BaseColour ("Base Colour", Color) = (0.45, 0.25, 0.01, 1.0)
        //_NumVertices("NumVertices", Integer) = 1000
     }
    SubShader
    {
		Tags {"Queue" = "Transparent" "RenderType" = "Transparent" }
		LOD 100

		Cull Off
		ZWrite Off
		Blend One One

        Pass
        {
            
            HLSLPROGRAM
            //pragma directives
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);


            struct VertexInput
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0; 
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f 
            {
                float4 position : SV_POSITION; //SV_POSITION = semantic = System Value position - pixel position
                float4 vertexPos : VERTEX;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            uniform StructuredBuffer<float3> positions : register(t1);
            uniform int offsetVal;

            UNITY_INSTANCING_BUFFER_START(GPUInstancedProps)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColour)
            UNITY_INSTANCING_BUFFER_END(GPUInstancedProps)



            v2f vert(VertexInput i)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(i); //Makes the instanceID accessible to shader functions.
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                o.position = TransformObjectToHClip(i.position);//
                o.uv = i.uv;
                #ifdef UNITY_INSTANCING_ENABLED
                o.position += float4(positions[unity_InstanceID + offsetVal], 0.0);
                #endif
                o.vertexPos = i.position;
                return o;
            }
         
            float4 frag(v2f i) : SV_Target 
            {
                //Sample the main texture at the correct uv coordinate using the SAMPLE_TEXTURE_2D macro, and 
                //then passing in the main texture, its sampler and the specified uv coordinate
                UNITY_SETUP_INSTANCE_ID(i);
                //float dist = length(i.vertexPos);
                //float multiplier = min((0.1/pow(dist, 100.0)), 1);
                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                return baseTex * UNITY_ACCESS_INSTANCED_PROP(GPUInstancedProps, _BaseColour);// * multiplier;
            }
            ENDHLSL
        }
    }
}
