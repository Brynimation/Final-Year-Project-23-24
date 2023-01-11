//ShaderLab is a Unity specific language that bridges the gap between HLSL and Unity. Everything
//defined outside of the Passes is written in ShaderLab. Everything within the passes
//is written in HLSL.
//https://www.youtube.com/watch?v=gY1Mx4kkZPU&t=603s
//https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-semantics

/*In spiral galaxies the velocities of stars in the outer orbits are much faster than expected.- https://sites.ualberta.ca/~pogosyan/teaching/ASTRO_122/lect24/lecture24.html*/


Shader "Custom/SpiralGalaxy"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BaseColour ("Base Colour", Color) = (1,1,1,1)
        _NumParticles("NumParticles", Int) = 1000
        _PointSize("Point Size", float) = 2.0
        _CameraPosition("Camera Position", vector) = (0.0,0.0,0.0)
        _TimeStep("Time Step", float) = 0.0
        _GalacticBulgeRadius("Galactic Bulge Radius", float) = 2
        _GalacticDiskRadius("Galactic Disk Radius", float) = 5
        _GalacticHaloRadius("Galactic Halo Radius", float) = 10.0
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
        //static const float DegToRad = 1 / PI;
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


        struct StarVertex
        {
           float3 position : POSITION;
           float2 uv : TEXCOORD0;
           uint id : SV_VERTEXID;
           float2 majorAndMinorAxes : TEXCOORD1;
           float2 angles : TEXCOORD2;
           float2 angularVelocity: TEXCOORD3;
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
        uniform int _NumParticles;
        uniform float3 _GalacticCentre;
        uniform float _GalacticDiskRadius;
        uniform float _GalacticBulgeRadius;
        uniform float _GalacticHaloRadius;
        uniform float _PointSize;
        uniform float _TimeStep;
        uniform float3 _CameraPosition;

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
             float3 calculatePosition(StarVertex i)
            {
                float theta = i.angles.x;
                float angleOffset = i.angles.y;
                float a = i.majorAndMinorAxes.x;
                float b = i.majorAndMinorAxes.y;
                float cosTheta = cos(theta);
                float sinTheta = sin(theta);
                float cosOffset = cos(angleOffset);
                float sinOffset = sin(angleOffset);

                float xPos = a * cosTheta * cosOffset - b * sinTheta * sinOffset + _GalacticCentre.x;
                float yPos = a * cosTheta * sinOffset + b * sinTheta * cosOffset + _GalacticCentre.y;
                return float3(xPos, yPos, 0);
            }
             float3 GetPointOnEllipse(StarVertex i)
            {
                i.angles.x = (i.angles.x > radians(360)) ? i.angles.x - radians(360) : i.angles.x;
                i.angles.x += (i.angularVelocity * _Time);
                //i.theta = i.theta + 10.0;
                i.position = calculatePosition(i);
                return i.position;
            }
      /*     

    public Vector3 GetPointOnEllipse(Vector3 centre, int starCount, float timeStep) 
    {
        theta0 = (theta0 > 360f * Mathf.Deg2Rad) ? theta0 - 360f * Mathf.Deg2Rad : theta0; //
        Vector3 displacement = centre - position;
        float r = displacement.magnitude;
        Vector3 forceDir = displacement.normalized;
        Vector3 velocityDir = new Vector3(-forceDir.y, forceDir.x, 0f);
        float velocityMagnitude = Mathf.Sqrt((id * 50 / (starCount - 1f)) / (float) r);
        Vector3 orbitalVelocity = velocityDir * velocityMagnitude;
        angularVelocity = velocityMagnitude / r;
        theta0 += (angularVelocity * timeStep * 1f);
        //position = new Vector3(semiMajorAxis * Mathf.Sin(theta0), semiMinorAxis * Mathf.Cos(theta0)) + centre;
        //position = getRotatedPoint(position, Quaternion.Euler(0f, 0f, angularOffset));
        position = calcPosition(centre, semiMajorAxis, semiMinorAxis, theta0, angularOffset);

     
        return position;
    }*/
            v2f vert(StarVertex i)
            {
                v2f o;
                o.position = TransformObjectToHClip(GetPointOnEllipse(i));
                o.uv = i.uv;
                o.size = 2.0;
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
