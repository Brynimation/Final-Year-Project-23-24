//ShaderLab is a Unity specific language that bridges the gap between HLSL and Unity. Everything
//defined outside of the Passes is written in ShaderLab. Everything within the passes
//is written in HLSL.

//https://www.braynzarsoft.net/viewtutorial/q16390-36-billboarding-geometry-shader - billboarding in geometry shader tutorial
//https://www.youtube.com/watch?v=gY1Mx4kkZPU&t=603s
//https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-semantics

/*In spiral galaxies the velocities of stars in the outer orbits are much faster than expected.- https://sites.ualberta.ca/~pogosyan/teaching/ASTRO_122/lect24/lecture24.html*/


Shader "Custom/SpiralGalaxy3"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _CameraUp("Camera Up", vector) = (0.0,0.0,0.0)
        _BaseColour ("Base Colour", Color) = (1,1,1,1)
        _NumParticles("NumParticles", Int) = 1000
        _PointSize("Point Size", float) = 2.0
        _CameraPosition("Camera Position", vector) = (0.0,0.0,0.0)
        _TimeStep("Time Step", float) = 0.0
        _GalacticBulgeRadius("Galactic Bulge Radius", float) = 2 
        _GalacticDiskRadius("Galactic Disk Radius", float) = 5
        _GalacticHaloRadius("Galactic Halo Radius", float) = 10.0
        _AngularOffsetIncrement("Angular Offset", float) = 1.0
        _MinEccentricity("Minimum Eccentricity", float) = 0.5
        _MaxEccentricity("Maximum Eccentricity", float) = 1.0
        _CentreColour("Centre Colour", Color) = (1,1,1,1)
        _EdgeColour("Edge Colour", Color) = (0.3, 1, 0)
        _AngularOffsetMultiplier("Angular offset", Int) = 1
        _MinCamDist("Minimum Camera Distance", float) = 200
     }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        ZWrite off //contents of the depth buffer are not updated.
        Blend One One
        cull off
        LOD 100

        //Between HLSLINCLUDE and ENDHLSL, we're going to set up everything we need to use 
        //in our HLSL pass. Everything within this block will be available to all the passes we define*/
        HLSLINCLUDE
        #pragma target 5.0
        uniform RWStructuredBuffer<int> data : register(u1);
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
           float radius : TEXCOORD1;
           uint id : SV_VERTEXID;
           float4 colour : COLOR;
           
        };

        struct GeomData
        {
            float4 positionOS : POSITION;
            //float size : PSIZE;
            
            float4 colour : COLOR;
            float2 uv : TEXCOORD0;
            float4 positionWS : TEXCOORD1;
            float radius : TEXCOORD2;
            uint id : TEXCOORD3;
            uint sphere : TEXCOORD4;
        };

         struct Interpolators 
        {
            float4 positionHCS : SV_POSITION; //SV_POSITION = semantic = System Value position - pixel position
            //float size : PSIZE; //Size of each vertex.
            float4 colour : COLOR;
            float2 uv : TEXCOORD0;
            float3 positionWS : TEXCOORD1;
            float4 centreHCS : TEXCOORD2;
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
        uniform float3 _CameraUp;
        uniform float3 _CameraPosition;
        uniform float _MinEccentricity;
        uniform float _MaxEccentricity;
        uniform float4 _CentreColour;
        uniform float4 _EdgeColour;
        uniform int _AngularOffsetMultiplier;
        uniform float _MinCamDist;


        ENDHLSL
        Pass
        {
            Cull Back
            
            HLSLPROGRAM
            //pragma directives
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            //Vertex shader - Meshes are built out of vertices, which are used to construct triangles.
            //Vertex shader runs for every vertex making up a mesh. Runs in parallel on the gpu.
            //float _PointSize;
            #pragma region vertexShaderHelpers
             float3 calculatePosition(float theta, float angleOffset, float a, float b, int id)
            {
                float cosTheta = cos(theta);
                float sinTheta = sin(theta);
                float cosOffset = cos(angleOffset);
                float sinOffset = sin(angleOffset);

                float xPos = a * cosTheta * cosOffset - b * sinTheta * sinOffset + _GalacticCentre.x;
                float yPos = a * cosTheta * sinOffset + b * sinTheta * cosOffset + _GalacticCentre.y;
                float3 pos = float3(xPos, yPos, 0);
                return pos;
            }
            float GetSemiMajorAxis(float x)
            {
                return(x * x * x * _GalacticDiskRadius);
            }

            float GetEccentricity(float r)
            {
                 if (r < _GalacticBulgeRadius)
                {
                    return lerp(_MaxEccentricity, _MinEccentricity, r / _GalacticBulgeRadius);
                }
                else if (r >= _GalacticBulgeRadius && r < _GalacticDiskRadius)
                {
                    return lerp(_MinEccentricity, _MaxEccentricity, (r - _GalacticBulgeRadius) / (_GalacticDiskRadius - _GalacticBulgeRadius));
                }
                else if (r >= _GalacticDiskRadius && r < _GalacticHaloRadius)
                {
                    return lerp(_MaxEccentricity, 1.0, (r - _GalacticDiskRadius) /(_GalacticHaloRadius - _GalacticDiskRadius));
                }
                else {
                    return 1.0;
                }
            }
            float GenerateRandom(int x)
            {
                float2 p = float2(x, sqrt(x));
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            float GetRandomAngle(int id)
            {
                return radians((GenerateRandom(id) * 360));
            }

            float GetAngularVelocity(int i, float r)
            {
                return sqrt((i * 50)/((_NumParticles - 1) * r)); //angularVel = sqrt(G * mass within the radius r / radius^3)
            }

            float GetColour(float r)
            {
                float lerpPercent = r/(float)_GalacticDiskRadius;
                return lerp(_CentreColour, _EdgeColour, float4(lerpPercent, lerpPercent, lerpPercent, lerpPercent));
            }
            float GetAngularOffset(int id)
            {
                int multiplier = id * _AngularOffsetMultiplier;
                int finalParticle = _NumParticles - 1;
                return radians((multiplier/(float)finalParticle) * 360);
            }
             float3 GetPointOnEllipse(StarVertex i)
            {
                float semiMajorAxis = GetSemiMajorAxis(i.id/(float)_NumParticles);
                float eccentricity = GetEccentricity(semiMajorAxis); 
                float angularVelocity = GetAngularVelocity(i.id, semiMajorAxis);
                float semiMinorAxis = eccentricity * semiMajorAxis;   
                float currentAngularOffset = GetAngularOffset(i.id);
                float theta = GetRandomAngle(i.id) + angularVelocity * _Time.w;
                //i.colour = GetColour(semiMajorAxis);
                i.position = calculatePosition(theta, currentAngularOffset, semiMajorAxis, semiMinorAxis, i.id);
                return i.position;
            }
            #pragma endregion vertexShaderHelpers


            GeomData vert(StarVertex i)
            {
                GeomData o;
                i.colour = (i.id % 20 == 0) ? float4(1.0,0.0,0.0,1.0) : float4(1.0,1.0,1.0,1.0); //make every tenth star an H2 region 
                i.radius = (i.id % 20 == 0) ? 750 : 200;
                float3 posObjectSpace = GetPointOnEllipse(i);
                o.positionOS = float4(posObjectSpace, 1.0);
                o.positionWS = mul(unity_ObjectToWorld, posObjectSpace);
                o.sphere = (distance(_CameraPosition, o.positionWS) > _MinCamDist) ? 0 : 1;
                o.uv = i.uv;
                o.colour = i.colour;
                o.radius = i.radius;
                return o;
            }


            [maxvertexcount(4)]
            void geom(point GeomData inputs[1], inout TriangleStream<Interpolators> outputStream)
            {
                GeomData centre = inputs[0];
                
                float3 forward = -(_CameraPosition - centre.positionWS);
                forward.y = 0.0f;
                forward = normalize(forward);

                float3 up = float3(0.0f, 1.0f, 0.0f);
                float3 right = normalize(cross(forward, up));

                float3 WSPositions[4];
                float2 uvs[4];


                up.y *= inputs[0].radius/2;
                right *= inputs[0].radius/2;

                                                // We get the points by using the billboards right vector and the billboards height
                WSPositions[0] = centre.positionWS - right - up; // Get bottom left vertex
                WSPositions[1] = centre.positionWS + right - up; // Get bottom right vertex
                WSPositions[2] = centre.positionWS - right + up; // Get top left vertex
                WSPositions[3] = centre.positionWS + right + up; // Get top right vertex

                // Get billboards texture coordinates
                float2 texCoord[4];
                uvs[0] = float2(0, 0);
                uvs[1] = float2(1, 0);
                uvs[2] = float2(0, 1);
                uvs[3] = float2(1, 1);

                
                for(int i = 0; i < 4; i++)
                {
                    Interpolators o;
                    o.centreHCS = mul(UNITY_MATRIX_VP, centre.positionWS);
                    o.positionHCS = mul(UNITY_MATRIX_VP, float4(WSPositions[i], 1.0f));
                    o.positionWS = float4(WSPositions[i], 0.0f);
                    o.uv = uvs[i];
                    o.colour = centre.colour;
                    if(centre.sphere == 1) o.colour = float4(0.0,1.0,0.0,1.0);
                    outputStream.Append(o);
                }
                
                
            }

            
            //Fragment Shader
            /*In a process known as rasterisation, post vertex shader, HLSL takes all triangle pixels currently
            on screen and turns them to fragments. Our fragment shader will operate on every one of these and 
            return a colour : the final colour of those fragments.
            */
            float4 frag(Interpolators i) : SV_Target 
            {
                //Sample the main texture at the correct uv coordinate using the SAMPLE_TEXTURE_2D macro, and 
                //then passing in the main texture, its sampler and the specified uv coordinate
                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                if(baseTex.a == 0.0)discard; //No pixels that are transparent are drawn.
                float4 colour = i.colour;
                //data[0] = 1;
                
                /*float radius = 0.002;
                float dist = distance(i.positionHCS, i.centreHCS);
                clip(radius - dist);
                float4 colour = lerp(float4(1.0, 1.0, 1.0, 1.0), float4(1.0, 1.0, 1.0, 0.0), dist/radius);*/
                /*Here we blur the edges of each star*/

                return baseTex * colour; //_BaseColour;
            }
            ENDHLSL
        }
    }
}
