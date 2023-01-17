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
           float2 type : TEXCOORD4;
           float4 colour : COLOR;
        };

        struct GeomData
        {
            float4 positionOS : POSITION;
            //float size : PSIZE;
            float4 colour : COLOR;
            float2 uv : TEXCOORD0;
            float3 positionWS : TEXCOORD1;
        };

         struct Interpolators 
        {
            float4 positionHCS : SV_POSITION; //SV_POSITION = semantic = System Value position - pixel position
            //float size : PSIZE; //Size of each vertex.
            float4 colour : COLOR;
            float2 uv : TEXCOORD0;
            float3 positionWS : TEXCOORD1;
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
            #pragma geometry geom
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
            GeomData vert(StarVertex i)
            {
                GeomData o;
                float size = 2.0;
                float3 posObjectSpace = GetPointOnEllipse(i);
                /*if(i.type.x == 1.0)
                {
                   i.majorAndMinorAxes.x += _GalacticDiskRadius;
                   float3 pos2ObjectSpace = GetPointOnEllipse(i);
                   float dst = distance(posObjectSpace, pos2ObjectSpace);
                   size = (_GalacticDiskRadius - dst);
                   i.colour *= float4(1.0, 0.0, 0.0, 0.0);
                }*/

                o.positionOS = float4(posObjectSpace, 1.0);
                o.positionWS = mul(unity_ObjectToWorld, posObjectSpace);
                o.uv = i.uv;
                //o.size = size;
                o.colour = i.colour;
                return o;
            }

            [maxvertexcount(24)]
            void geom(point GeomData inputs[1], inout TriangleStream<Interpolators> outputstream)
            {
        
                const int numPoints = 8;
                float angleIncrement = radians(360)/(float) numPoints;

                float3 centreOS = inputs[0].positionOS;
                float3 centreWS = inputs[0].positionWS;
                float2 centreUV = inputs[0].uv;


                Interpolators centre;
                centre.positionWS = centreWS;
                centre.uv = centreUV;
                centre.positionHCS = TransformObjectToHClip(centreOS);
                centre.colour = inputs[0].colour;
                Interpolators positions[numPoints];
                for(int i = 0; i < numPoints; i++)
                {
                    Interpolators o;
                    float angle = angleIncrement * i;
                    float r = 2.0;
                    float x = r * cos(angle);
                    float y = r * sin(angle);
                    float3 os = centreOS + float3(x,y,0);
                    o.positionWS = mul(unity_ObjectToWorld, os);//centreWS + os;
                    o.positionHCS = TransformObjectToHClip(os);
                    o.uv = centreUV;
                    o.colour = inputs[0].colour;

                    positions[i] = o;
                }
                for(int i = 0; i < numPoints; i++)
                {
                    outputstream.Append(positions[i]);
                    outputstream.Append(positions[(i + 1)%numPoints]);
                    outputstream.Append(centre);
                    
                    outputstream.RestartStrip();
                }
                
            }

            
            //Fragment Shader
            /*In a process known as rasterisation, post vertex shader, HLSL takes all triangle pixels currently
            on screen and turns them to fragments. Our fragment shader will operate on every one of these and 
            return a colour : the final colour of those fragments.
            */
            float4 frag(GeomData i) : SV_Target 
            {
                //Sample the main texture at the correct uv coordinate using the SAMPLE_TEXTURE_2D macro, and 
                //then passing in the main texture, its sampler and the specified uv coordinate
                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                /*Here we blur the edges of each star*/

                return baseTex * i.colour; //_BaseColour;
            }
            ENDHLSL
        }
    }
}
