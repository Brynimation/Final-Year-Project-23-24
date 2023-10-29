//ShaderLab is a Unity specific language that bridges the gap between HLSL and Unity. Everything
//defined outside of the Passes is written in ShaderLab. Everything within the passes
//is written in HLSL.

//https://www.braynzarsoft.net/viewtutorial/q16390-36-billboarding-geometry-shader - billboarding in geometry shader tutorial
//https://www.youtube.com/watch?v=gY1Mx4kkZPU&t=603s
//https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-semantics
//https://gist.github.com/fuqunaga/1a649158d69241d31b023ec9983b0164

/*In spiral galaxies the velocities of stars in the outer orbits are much faster than expected.- https://sites.ualberta.ca/~pogosyan/teaching/ASTRO_122/lect24/lecture24.html*/


Shader "Custom/InstancingLOD2"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _CameraUp("Camera Up", vector) = (0.0,0.0,0.0)
        _BaseColour ("Base Colour", Color) = (1,1,1,1)
        _CameraPosition("Camera Position", vector) = (0.0,0.0,0.0)
        _EmissionMap("Emission Map", 2D) = "black"{}
        [HDR] _EmissionColour("Emission colour", Color) = (1.0,1.0,1.0,1.0)
        _StandardColour("Standard Colour", Color) = (1.0, 0.0, 0.0, 1.0)
        _H2RegionColour("H2 Region Colour", Color) = (1.0, 1.0, 1.0, 1.0)
        _Emission("Emission", Range(0, 100)) = 50
        _GalacticHaloRadius("Galactic Halo Radius", float) = 1.0
        _GalacticDiskRadius("Galactic Disk Radius", float) = 0.15
        _GalacticBulgeRadius("Galactic Bulge Radius", float) = 0.05
        _MaxEccentricity("Max Eccentricity", float) = 1.0 
        _MinEccentricity("Min Eccentricity", float) = 0.5
        _NumParticles("Num Particles", int) = 1000
        _AngularOffsetMultiplier("Angular offset multiplier", int) = 2
        _TimeStep("Time step", float) = 0.05

      }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }
		Cull Off
		//ZTest Always
		ZWrite Off
        LOD 100
        Pass
        {

            HLSLPROGRAM
            #pragma vertex vert 
            #pragma geometry geom 
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 5.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            UNITY_INSTANCING_BUFFER_START(MyProps)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColour)
            UNITY_INSTANCING_BUFFER_END(MyProps)


            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            float4 _EmissionColour;
            float4 _StandardColour;
            float4 _H2RegionColour;
            float _Emission;
            float3 _CameraPosition;
            float _GalacticHaloRadius;
            float _GalacticDiskRadius;
            float _GalacticBulgeRadius;
            float _MaxEccentricity;
            float _MinEccentricity;
            float _TimeStep;
            int _NumParticles;
            int _AngularOffsetMultiplier;

            struct MeshProperties
            {
                float4x4 mat;
            };
            RWStructuredBuffer<MeshProperties> _Properties;

            struct Attributes
            {
                uint vertexId : SV_VERTEXID;
                uint instanceId : SV_INSTANCEID;
            };
            struct GeomData
            {
                //float size : PSIZE;
                float4 colour : COLOR;
                float4 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float radius : TEXCOORD2;
                uint id : TEXCOORD3;
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
            float3 calculatePosition(float theta, float angleOffset, float a, float b, int id)
            {
                float cosTheta = cos(theta);
                float sinTheta = sin(theta);
                float cosOffset = cos(angleOffset);
                float sinOffset = sin(angleOffset);

                float xPos = a * cosTheta * cosOffset - b * sinTheta * sinOffset;
                float yPos = a * cosTheta * sinOffset + b * sinTheta * cosOffset;
                float zPos = 0.0;
                float3 pos = float3(xPos, yPos, zPos);
                return pos;
            }
            float GetSemiMajorAxis(float x)
            {
                return(x * x * x * _GalacticHaloRadius);
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
            /*Generates a random, initial angle based on the id of the star. This angle is known as the true anomaly, and is a measure of how far through its orbit the orbitting body is*/
            float GetRandomAngle(uint id)
            {
                return radians((GenerateRandom(id) * 360));
            }

            float GetExpectedAngularVelocity(uint i, float r)
            {
                //Due to Newton's shell theorem: for a test particle contained within a sphere of constant density, gravitational force (and hence acceleration)
                //increases linearly with distance from the centre (at 0) to the surface of the sphere (at a maximum).  Beyond the sphere, we use Newton's 
                //universal law of gravitation to show that orbital velocity is proportion to 1/sqrt(distance) from the centre of mass, and hence, angular velocity is proportional to 1/distance^(3/2).
                //Here, we make the simplifying assumption that the nuclear bulge is a sphere of constant density that houses the mass of the entire galaxy.
                //We do the lerping so that the transition from the bulge to the disc is more seamless
                float discSpeed = sqrt((_NumParticles * 50)/ pow(r, 3)) * 500;
                float constant =  ((float)discSpeed / pow(_GalacticBulgeRadius, 2));//angular velocity = sqrt(4/3 * G * pi * r^2 * sphereDensity); - This constant is needed so that, when r = bulge radius, the speed of the particle in the disc and in the bulge is the same, making the transition smoother
                float bulgeSpeed = sqrt(constant * pow(r, 2));
                if(r < _GalacticBulgeRadius)
                {
                    return bulgeSpeed;
                }

                //For a keplerian system (where a body follows a circular orbit around some centre of mass), angular velocity = sqrt(G * COM / r^3)
                return discSpeed;
                //return sqrt((i * 50)/((_NumParticles - 1) * r)); //angularVel = sqrt(G * mass within the radius r / radius^3)
    
                //Using Newton's form of Kepler's third law:
                //T = 4
            }
            float GetAngularOffset(uint id)
            {
                int multiplier = id * _AngularOffsetMultiplier;
                int finalParticle = _NumParticles - 1;
                return radians((multiplier/(float)finalParticle) * 360);
            }
            float3 GetPointOnEllipse(uint id)
            {
                float semiMajorAxis = GetSemiMajorAxis(id/(float)_NumParticles);
                float eccentricity = GetEccentricity(semiMajorAxis); 
                float angularVelocity = GetExpectedAngularVelocity(id, semiMajorAxis);
                float semiMinorAxis = eccentricity * semiMajorAxis;   
                float currentAngularOffset = GetAngularOffset(id);
                float theta = GetRandomAngle(id) + angularVelocity * _Time * _TimeStep;
                return calculatePosition(theta, currentAngularOffset, semiMajorAxis, semiMinorAxis, id);
            }

            GeomData vert(Attributes i)
            {
                GeomData o;
                o.id = i.vertexId;
                //_Matrix = CreateMatrix(_PositionsLOD1[id], float3(1.0,1.0,1.0), float3(0.0, 1.0, 0.0), id);
                //float4 posOS = mul(_Matrix, _PositionsLOD1[id]);
                MeshProperties mp = _Properties[i.instanceId];
                float3 pos = GetPointOnEllipse(i.vertexId);
                float4 posOS = mul(mp.mat, float4(pos, 1.0));
                o.positionWS = mul(unity_ObjectToWorld, posOS);
                o.radius = (i.vertexId % 100) == 0 ?  0.01 : 0.005;
                o.colour = (i.vertexId % 100) == 0 ? _H2RegionColour : _StandardColour;
                o.colour += _EmissionColour;
                //o.radius = _PositionsLOD1[id].radius;//_PositionsLOD1[id].radius;
                return o;
            }

            [maxvertexcount(4)]
            void geom(point GeomData inputs[1], inout TriangleStream<Interpolators> outputStream)
            {

                GeomData centre = inputs[0];
                //if(_PositionsLOD1[centre.id].culled == 0) return;
                float3 forward = centre.positionWS - GetCameraPositionWS();
                //forward.y ;
                forward = normalize(forward);

                float3 worldUp = float3(0.0f, 1.0f, 0.0f);
                float3 right = normalize(cross(forward, worldUp));
                float3 up = normalize(cross(forward, right));

                float3 WSPositions[4];
                float2 uvs[4];


                up *= inputs[0].radius;
                right *= inputs[0].radius;

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
                    o.positionWS = float4(WSPositions[i], 1.0f);
                    o.uv = uvs[i];
                    o.colour = centre.colour;
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

                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                if(baseTex.a == 0.0)discard;
                float4 colour = i.colour;

                return baseTex * colour; //_BaseColour;
            }
            ENDHLSL
        }
    }
}
