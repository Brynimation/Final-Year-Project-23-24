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
        _BaseColour ("Base Colour", Color) = (1,1,1,1)
        //_NumVertices("NumVertices", Integer) = 1000
        _PointSize("Point Size", float) = 2.0
        _CameraPosition("Camera Position", vector) = (0.0,0.0,0.0)
        _CurTime("Time", float) = 0.0
        _GalacticBulgeRadius("Galactic Bulge Radius", float) = 10.0
        _GalacticDiskRadius("Galactic Disk Radius", float) = 30.0
        _GalacticHaloRadius("Galactic Halo Radius", float) = 50.0
     }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        //Between HLSLINCLUDE and ENDHLSL, we're going to set up everything we need to use 
        //in our HLSL pass. Everything within this block will be available to all the passes we define*/
        HLSLINCLUDE
        static const float PI = 3.14159265f;
        static const float angleStep = 180;
        static const float DegreeToRadians = 1 / PI;
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        //we need to include the properties our shader is going to use between the cbuffer start and 
        //cbuffer end Tags
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColour;
        CBUFFER_END

        //Textures don't need to go within the cbuffer
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        /*Shaders usually consist of a vertex shader and a fragment shader.
         Shader graph handles the receipt of certain input from the mesh for the user, such
         as vertex positions and UV coordinates. The properties of our shader are also passed as input.
         The vertex shader operates on every vertex of the mesh that the shader is attached to. It
         will output a 2D screenspace position for each of these vertices. We must first create a
         struct that is passed as input to our VertexShader*/

        

        struct VertexInput
        {
            float4 position : POSITION;
            float2 uv : TEXCOORD0; 

            uint index : SV_VertexId;
        };

        /*A semantic is a string attached to a shader input or output that conveys information 
        about the intended use of a parameter. Semantics are required on all variables passed 
        between shader stages.
        
        points to points 
        nothing to points

        points to quads!

        discrete indices - hashfunction to convert to spiral
        random sample of space - c
        */

        /*Output by our vertex shader, passed to our fragment shader.*/
        struct v2f 
        {
            float4 position : SV_POSITION; //SV_POSITION = semantic = System Value position - pixel position
            float size : PSIZE; //Size of each vertex.
            float2 uv : TEXCOORD0;
        };
        float _GalacticDiskRadius;
        float _GalacticBulgeRadius;
        float _GalacticHaloRadius;
        float _PointSize;
        float _CurTime;
        float3 _CameraPosition;
        uniform RWStructuredBuffer<float3> positions : register(u1);
        int offset;

        ENDHLSL

        Pass
        {
            
            HLSLPROGRAM
            //pragma directives
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            v2f vert(VertexInput i)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(i); //Makes the instanceID accessible to shader functions.
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                o.position = TransformObjectToHClip(i.vertex);//
                o.uv = i.uv;
                #ifdef UNITY_INSTANCING_ENABLED
                    o.position += float4(positions[unity_InstanceID + offset], 0.0f);
                #endif
                o.vertex = i.vertex;
                return o;
            }
         
            float4 frag(v2f i) : SV_Target 
            {
                //Sample the main texture at the correct uv coordinate using the SAMPLE_TEXTURE_2D macro, and 
                //then passing in the main texture, its sampler and the specified uv coordinate
                UNITY_SETUP_INSTANCE_ID(i);
                float dist = distance(i.vertex, float(0.0, 0.0, 0.0));
                float multiplier = min((0.1/pow(dist, 100.0)), 1);
                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                return baseTex * _BaseColour * multiplier;
            }
            ENDHLSL
        }
    }
}
