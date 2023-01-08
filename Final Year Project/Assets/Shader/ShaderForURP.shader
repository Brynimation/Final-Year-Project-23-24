//ShaderLab is a Unity specific language that bridges the gap between HLSL and Unity. Everything
//defined outside of the Passes is written in ShaderLab. Everything within the passes
//is written in HLSL.
//https://www.youtube.com/watch?v=gY1Mx4kkZPU&t=603s
//https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-semantics

/*In spiral galaxies the velocities of stars in the outer orbits are much faster than expected.- https://sites.ualberta.ca/~pogosyan/teaching/ASTRO_122/lect24/lecture24.html*/


Shader "Custom/ShaderForURP"
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
        CBUFFER_START(UnityPerMaterial) //The UnityPerMaterial parameter ensures these properties are consistent among passes.
        //By defining all of our properties within a buffer, we make our material compatible with SRP Batcher, which makes rendering
        //happen faster.
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

        ENDHLSL

        Pass
        {
            
            HLSLPROGRAM
            //pragma directives
            #pragma vertex vert
            #pragma fragment frag
            //Vertex shader - Meshes are built out of vertices, which are used to construct triangles.
            //Vertex shader runs for every vertex making up a mesh. Runs in parallel on the gpu.
            //float _PointSize;
            v2f vert1(VertexInput i)
            {
                v2f o; //What our function will output
                //We need to transform the vertices from object space, where each vertex is positioned
                //relative to the object's centre, to clip space, where each vertex is positioned relative
                //to the 2D screen coordinates.'
                /*Rotate vertex more from the centre to create a spiral*/
                float dist = length(i.position);
                float theta = atan2(i.position.x, i.position.y);
                float theta2 = theta + dist;
                //x = rcostheta2, y = rcostheta2
                //float speed = (dist == 0.0) ? 1000 : (float) 1.0/dist; 
                float x = dist * cos(theta2);
                float y = dist * sin(theta2);
                float3 newCoord = float3(x, y, i.position.z);
                o.position = TransformObjectToHClip(newCoord);
                o.uv = i.uv;
                o.size = 4.0;
                return o;
            }
            /*https://gist.github.com/keijiro/ee7bc388272548396870*/
            float nrand(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            v2f vert(VertexInput i)
            {
                v2f o;
                float3 newCoord = i.position;
                if(length(i.position) - _GalacticDiskRadius/2.0 > 0.001f){
                    newCoord = normalize(i.position) * _GalacticDiskRadius/2.0;
                }
                /*Now we've got a bunch of points on the surface of a circle.*/
                float theta = atan2(newCoord.x, newCoord.y);
                //float d = nrand(i.uv) * _GalacticDiskRadius;
                float d = smoothstep(0, 1, i.index/10000.0) * _GalacticDiskRadius;
                newCoord.x = d * cos(theta);
                newCoord.y = d * sin(theta);
                //now we redefine the radius of each point
                o.position = TransformObjectToHClip(newCoord);
                o.uv = i.uv;
                o.size = 4.0;
                return o;
            }

            v2f vert2(VertexInput i)
            {

                float maxArmOffset = 0.5f;
                int numArms = 5;
                float armSeparationDistance = 2 * PI / numArms;
                v2f o;
                float dist = i.index * i.index;
                float angle = i.index;
                float armOffset = i.index / 5000.0;
                armOffset -= maxArmOffset/2.0;
                armOffset = max(armOffset, maxArmOffset);
                armOffset = armOffset * 1/(dist);
                /*Here we constrain it so that only certain angles are possible. This creates the 
                density waves of our galaxy. We add the offset to widen the arms*/
                angle = (int)(angle/armSeparationDistance) * armSeparationDistance +armOffset;

                

                float x = dist * cos(angle);
                float y = dist * sin(angle);
                float3 newCoord = float3(x, y, 0);
                o.position = TransformObjectToHClip(newCoord);
                o.uv = i.uv;
                o.size = 4.0;
                return o;
            }
            v2f vert3(VertexInput i)
            {
                v2f o;
                float dist = exp(-length(i.position)) * 100;
                float theta = atan2(i.position.x, i.position.y);
                float theta2 = theta + dist;
                //x = rcostheta2, y = rcostheta2
                //float speed = (dist == 0.0) ? 1000 : (float) 1.0/dist; 
                float x = dist * cos(theta2);
                float y = dist * sin(theta2);
                float3 newCoord = float3(x, y, i.position.z);
                o.position = TransformObjectToHClip(newCoord);
                o.uv = i.uv;
                o.size = 4.0;
                return o;
            }
            //Fragment Shader
            /*In a process known as rasterisation, post vertex shader, HLSL takes all triangle pixels currently
            on screen and turns them to fragments. Our fragment shader will operate on every one of these and 
            return a colour : the final colour of those fragments.
            */
            float4 frag(v2f i) : SV_Target 
            {
                //Sample the main texture at the correct uv coordinate using the SAMPLE_TEXTURE_2D macro, and 
                //then passing in the main texture, its sampler and the specified uv coordinate
                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                return baseTex * _BaseColour;
            }
            ENDHLSL
        }
    }
}
