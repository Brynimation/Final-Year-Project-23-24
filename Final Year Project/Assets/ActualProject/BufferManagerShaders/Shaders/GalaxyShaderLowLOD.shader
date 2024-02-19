//ShaderLab is a Unity specific language that bridges the gap between HLSL and Unity. Everything
//defined outside of the Passes is written in ShaderLab. Everything within the passes
//is written in HLSL.

//https://www.braynzarsoft.net/viewtutorial/q16390-36-billboarding-geometry-shader - billboarding in geometry shader tutorial
//https://www.youtube.com/watch?v=gY1Mx4kkZPU&t=603s
//https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-semantics
//https://gist.github.com/fuqunaga/1a649158d69241d31b023ec9983b0164

/*In spiral galaxies the velocities of stars in the outer orbits are much faster than expected.- https://sites.ualberta.ca/~pogosyan/teaching/ASTRO_122/lect24/lecture24.html*/


Shader "Custom/GalaxyShaderLowLOD"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _CameraUp("Camera Up", vector) = (0.0,0.0,0.0)
        _BaseColour("Base Colour", Color) = (1,1,1,1)
        _MaxStarSize("Point Size", float) = 2.0
        _CameraPosition("Camera Position", vector) = (0.0,0.0,0.0)
        _EmissionMap("Emission Map", 2D) = "black"{}
        [HDR] _EmissionColour("Emission colour", Color) = (0,0,0,0)
        _Emission("Emission", Range(0, 100)) = 50
        _GridSize("GridsSize", Int) = 5
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
            //Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM

            #pragma vertex vert 
            #pragma geometry geom 
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 5.0
            #include "Assets/ActualProject/Helpers/Utility.hlsl"

            UNITY_INSTANCING_BUFFER_START(MyProps)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColour)
            UNITY_INSTANCING_BUFFER_END(MyProps)

            //https://forum.unity.com/threads/generate-random-float-between-0-and-1-in-shader.610810/
            float GenerateRandom(float2 xy)
            {
                return round(frac(sin(dot(xy, float2(12.9898, 78.233))) * 43758.5453) * 1000) ;
            }


            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            float4 _EmissionColour;
            float _Emission;
            float3 _CameraPosition;
            float _MaxStarSize;
            float _TimeStep;
            int _GridSize;
            RWStructuredBuffer<GalaxyProperties> _Properties;

            /*
                int numParticles;
    float minEccentricity;
    float maxEccentricity;
    float galacticDiskRadius;
    float galacticHaloRadius;
    float galacticBulgeRadius;
    float angularOffsetMultiplier;
            
            
            
            */
            struct GeomData
            {
                //float size : PSIZE;
                float4 positionWS : POSITION;
                float4 centreColour : COLOR;
                float4 outerColour : COLOR2;
                float fade : TEXCOORD0;
                float haloRadius : TEXCOORD1;
                float minEccentricity : TEXCOORD2;
                float maxEccentricity : TEXCOORD3;
                float diskRadius : TEXCOORD4;
                float bulgeRadius : TEXCOORD5;
                int angularOffsetMultiplier : TEXCOORD6;
                float3 forward : TEXCOORD7;
                float3 right : TEXCOORD8;
                float3 up : TEXCOORD9;
                float galaxyDensity : TEXCOORD10;
            };


                struct Interpolators
            {
                float4 positionHCS : SV_POSITION; //SV_POSITION = semantic = System Value position - pixel position
                //float size : PSIZE; //Size of each vertex.
                float4 centreColour : COLOR;
                float4 outerColour : COLOR2;
                float2 uv : TEXCOORD0;
                float fade : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float4 centreHCS : TEXCOORD3;
                float haloRadius : TEXCOORD4;
                float minEccentricity : TEXCOORD5;
                float maxEccentricity : TEXCOORD6;
                float diskRadius : TEXCOORD7;
                float bulgeRadius : TEXCOORD8;
                int angularOffsetMultiplier : TEXCOORD9;
                float galaxyDensity : TEXCOORD10;
            };

            float4 colourFromLodLevel(int lodLevel)
            {
                switch(lodLevel)
                {
                    case 0:
                        return float4(1,1,1,1);
                        break;
                    case 1:
                        return float4(1,1,0,1);
                        break;
                    case 2:
                        return float4(0,1,0,1);
                        break;
                    case 3:
                        return float4(1,0,1,1);
                        break;
                    case 4:
                        return float4(1,0,0,1);
                        break;
                    default:
                        return float4(0.5,0.5,1,1);
                        
                }
            }
            float radiusFromLodLevel(int lodLevel)
            {
                switch(lodLevel)
                {
                    case 0:
                        return 0.05;
                        break;
                    case 1:
                        return 0.1;
                        break;
                    case 2:
                        return 0.2;
                        break;
                    case 3:
                        return 0.35;
                        break;
                    case 4:
                        return 0.5;
                        break;
                    default:
                        return 0.75;
                        break;
                        
                }
            }
            /*
            
                float minEccentricity;
    float maxEccentricity;
    float galacticDiskRadius;
    float galacticHaloRadius;
    float galacticBulgeRadius;
    float angularOffsetMultiplier;
            
            struct GalaxyProperties
            {
                MeshProperties mp;
                int numParticles;
                float minEccentricity;
                float maxEccentricity;
                float galacticDiskRadius;
                float galacticHaloRadius;
                float galacticBulgeRadius;
                float angularOffsetMultiplier;
            };
            */
                
            GeomData vert(uint id : SV_INSTANCEID)
            {
                GeomData o;
                //_Matrix = CreateMatrix(_PositionsLOD1[id], float3(1.0,1.0,1.0), float3(0.0, 1.0, 0.0), id);
                //float4 posOS = mul(_Matrix, _PositionsLOD1[id]);
                GalaxyProperties gp = _Properties[id];
                MeshProperties mp = gp.mp;
                float4 posOS = mul(mp.mat, float4(0.0, 0.0, 0.0, 1.0));
                //can't use id to determine properties - let's use position
                o.positionWS = mul(unity_ObjectToWorld, posOS);
                int seed = GenerateRandom(o.positionWS.xy);
                o.centreColour = gp.centreColour;
                o.outerColour = gp.outerColour;
                o.haloRadius = gp.galacticHaloRadius;
                o.bulgeRadius = gp.galacticBulgeRadius;
                o.diskRadius = gp.galacticDiskRadius;
                o.angularOffsetMultiplier = gp.angularOffsetMultiplier;
                o.maxEccentricity = gp.maxEccentricity;
                o.minEccentricity = gp.minEccentricity;
                o.galaxyDensity = gp.galaxyDensity;
                //We need to extract the 3x3 rotation (and scale) matrix from our 4x4 trs matrix so we can properly orient our quad in the geometry shader
                float3x3 rotMat = float3x3(mp.mat[0].xyz, mp.mat[1].xyz, mp.mat[2].xyz);
                rotMat[0][0] /= o.haloRadius;
                rotMat[0][1] /= o.haloRadius;
                rotMat[0][2] /= o.haloRadius;

                rotMat[1][0] /= o.haloRadius;
                rotMat[1][1] /= o.haloRadius;
                rotMat[1][2] /= o.haloRadius;

                rotMat[2][0] /= o.haloRadius;
                rotMat[2][1] /= o.haloRadius;
                rotMat[2][2] /= o.haloRadius;
                o.forward = normalize(mul(rotMat, float3(0.0, 0.0, 1.0)));
                o.right = normalize(mul(rotMat, float3(1.0, 0.0, 0.0)));
                o.up = normalize(mul(rotMat, float3(0.0, 1.0, 0.0)));
                o.fade = mp.fade;
                //o.colour += _EmissionColour;
                //o.radius = _PositionsLOD1[id].radius;//_PositionsLOD1[id].radius;
                return o;
            }

            [maxvertexcount(4)]
            void geom(point GeomData inputs[1], inout TriangleStream<Interpolators> outputStream)
            {

                GeomData centre = inputs[0];
                
                
                float3 forward = centre.forward;

                float3 worldUp = float3(0.0f, 1.0f, 0.0f);
                float3 right = normalize(cross(forward, worldUp));
                float3 up = normalize(cross(forward, right));

                right = centre.right;
                up = centre.up;
                float3 WSPositions[4];
                float2 uvs[4];


                up *= inputs[0].haloRadius;
                right *= inputs[0].haloRadius;

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


                for (int i = 0; i < 4; i++)
                {
                    Interpolators o;
                    o.centreHCS = mul(UNITY_MATRIX_VP, centre.positionWS);
                    o.positionHCS = mul(UNITY_MATRIX_VP, float4(WSPositions[i], 1.0));
                    o.positionWS = float4(WSPositions[i], 1.0);
                    o.uv = uvs[i];
                    o.fade = centre.fade;
                    o.haloRadius = centre.haloRadius;
                    o.bulgeRadius = centre.bulgeRadius;
                    o.diskRadius = centre.diskRadius;
                    o.angularOffsetMultiplier = centre.angularOffsetMultiplier;
                    o.maxEccentricity = centre.maxEccentricity;
                    o.minEccentricity = centre.minEccentricity;
                    o.galaxyDensity = centre.galaxyDensity;
                    o.outerColour = centre.outerColour;
                    o.centreColour = centre.centreColour;
                    outputStream.Append(o);
                }
            }

     float2 calculatePosition(float theta, float angleOffset, float a, float b)
    {
        float cosTheta = cos(theta);
        float sinTheta = sin(theta);
        float cosOffset = cos(angleOffset);
        float sinOffset = sin(angleOffset);

        float xPos = a * cosTheta * cosOffset - b * sinTheta * sinOffset;
        float yPos = a * cosTheta * sinOffset + b * sinTheta * cosOffset;
        return float2(xPos, yPos);
    }
    float GetEccentricity(float r, float _GalacticBulgeRadius, float _GalacticDiskRadius, float _GalacticHaloRadius, float _MaxEccentricity, float _MinEccentricity)
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
            return lerp(_MaxEccentricity, 0.0, (r - _GalacticDiskRadius) / (_GalacticHaloRadius - _GalacticDiskRadius));
        }
        else
        {
            return 0.0;
        }
    }

    /*Generates a random, initial angle based on the id of the star. This angle is known as the true anomaly, and is a measure of how far through its orbit the orbitting body is*/
    float GetRandomAngle(float2 uv)
    {
        return radians(Hash21(uv) * 360);
    }

    float GetExpectedAngularVelocity(float r, float _GalacticBulgeRadius, float _GalaxyDensity)
    {
        float galaxyMass = (4 / 3) * PI * pow(_GalacticBulgeRadius, 3.0) * _GalaxyDensity;
        float discSpeed = sqrt(galaxyMass / pow(r, 3));
        float bulgeSpeed = 5.0; //angular velocity in the bulge is constant.
        float interpolationDist = _GalacticBulgeRadius / 2.0;
        // Calculate a smooth transition factor between bulgeSpeed and discSpeed
        float t = smoothstep(_GalacticBulgeRadius - interpolationDist, _GalacticBulgeRadius, r);

        // Calculate the interpolated speed
        float speed = lerp(bulgeSpeed, discSpeed, t);

        return speed;

        //return sqrt((i * 50)/((_NumParticles - 1) * r)); //angularVel = sqrt(G * mass within the radius r / radius^3)

        //Using Newton's form of Kepler's third law:
        //T = 4
    }

    /*Determines the angle of inclination of the elliptical orbit. Based on this article: https://beltoforion.de/en/spiral_galaxy_renderer/, by
    having the inclination of the orbit increase with the size of the semi-major axis of the orbit, we produce the desired spiral structure of the galaxy*/
    float GetAngularOffset(uint id, int _AngularOffsetMultiplier, int _NumParticles)
    {
        int multiplier = id * _AngularOffsetMultiplier;
        int finalParticle = _NumParticles - 1;
        return radians((multiplier / (float) finalParticle) * 360);
    }
    float2 GetPointOnEllipse(float2 p, int _AngularOffsetMultiplier, float _GalacticBulgeRadius, float _GalacticDiskRadius, float _GalacticHaloRadius, float _MaxEccentricity, float _MinEccentricity)
    {
        float semiMajorAxis = length(p);
        float eccentricity = GetEccentricity(semiMajorAxis, _GalacticBulgeRadius, _GalacticDiskRadius, _GalacticHaloRadius, _MaxEccentricity, _MinEccentricity);
        float semiMinorAxis = semiMajorAxis * sqrt(1.0 - pow(eccentricity, 2));
        float angularVelocity = GetExpectedAngularVelocity(semiMajorAxis, _GalacticBulgeRadius, 10.0);
        float theta = atan2(p.y, p.x) + angularVelocity * _Time * _TimeStep;
        float currentAngularOffset = lerp(0, radians(360 * _AngularOffsetMultiplier), semiMajorAxis);
        return calculatePosition(theta, currentAngularOffset, semiMajorAxis, semiMinorAxis);
    }

     float4 frag(Interpolators i) : SV_Target
    {
        clip(DiscardPixelLODCrossFade(i.positionHCS, i.fade));
        float2 p = i.uv * 2.0 - 1.0;//Convert range of uv coordinates to (-1, 1)
        float radius = 1.0;
        float2 centre = float2(0.0, 0.0);
        float dist = distance(p, centre);
        if(dist > radius) discard;
        //TO DO: Somehow make p move like one of the particles in the spiral galaxy
        //return 1.0;
        float4 col = 0.0;
        //col += Galaxy(p, 0.0, 0.0, 0.0);
        float angularOffsetMultiplier = i.angularOffsetMultiplier;
        float galacticHaloRadius = 1.0;
        float galacticBulgeRadius = i.bulgeRadius / i.haloRadius;
        float galacticDiskRadius = i.diskRadius / i.haloRadius;
        float maxEccentricity = i.maxEccentricity;
        float minEccentricity = i.minEccentricity;
        p = GetPointOnEllipse(p, angularOffsetMultiplier, galacticBulgeRadius, galacticDiskRadius, galacticHaloRadius, maxEccentricity, minEccentricity);
        col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, (p + 1.0)/2.0);
        return col * lerp(i.centreColour, i.outerColour, dist);
        /*float4 col = 0.0;

        float2 q = 0.0;
        q.x = fbm(p + _Time.y);
        q.y = fbm(p + 1.0);
                
        float2 r = 0.0;
        r.x = fbm(p + 1.0 * q + float2(1.7, 9.2) + 0.15 * _Time.y);
        r.y = fbm(p + 1.0 * q + float2(8.3, 2.8) + 0.126 * _Time.y);

        float f = fbm(p + r);

        col = float4(lerp(float3(0.101961,0.619608,0.666667),float3(0.666667,0.666667,0.498039),clamp((f*f)*4.0,0.0,1.0)), 1.0);
        col = lerp(col, float4(0.0, 0.0, 0.164706, 1.0), saturate(float4(length(q), 0.0, 1.0, 1.0)));
        col = lerp(col, float4(0.666667, 1.0, 1.0, 1.0), saturate(float4(length(r.x), 0.0, 1.0, 1.0)));
        return float4((f * f * f + 0.6 * f * f + 0.5 * f) * col);
        return col;*/
    }

         float4 fraga(Interpolators i) : SV_Target
    {
        clip(DiscardPixelLODCrossFade(i.positionHCS, i.fade));
        float2 p = i.uv * 2.0 - 1.0;//Convert range of uv coordinates to (-1, 1)
        float radius = 1.0;
        float2 centre = float2(0.0, 0.0);
        float dist = distance(p, centre);
        if(dist > radius) discard;
        //TO DO: Somehow make p move like one of the particles in the spiral galaxy
        float4 col = 0.0;
        //col += Galaxy(p, 0.0, 0.0, 0.0);
        float2 toCentre = p;
        float angle = atan2(toCentre.y, toCentre.x);
        angle += _Time.z * (1.0 - dist);
        float numArms = 2.0;
        float armSpread = 1.0;
    // Model the spiral arms
        float spiralArm = cos(numArms * angle) + armSpread;

        // Compute star brightness based on distance and proximity to spiral arms
        return smoothstep(0.0, 0.05, spiralArm - dist) * (1.0 - dist);

   }
            /*
                vec2 q = vec2(0.);
    q.x = fbm( st + 0.00*u_time);
    q.y = fbm( st + vec2(1.0));

    vec2 r = vec2(0.);
    r.x = fbm( st + 1.0*q + vec2(1.7,9.2)+ 0.15*u_time );
    r.y = fbm( st + 1.0*q + vec2(8.3,2.8)+ 0.126*u_time);

    float f = fbm(st+r);

    color = mix(vec3(0.101961,0.619608,0.666667),
                vec3(0.666667,0.666667,0.498039),
                clamp((f*f)*4.0,0.0,1.0));

    color = mix(color,
                vec3(0,0,0.164706),
                clamp(length(q),0.0,1.0));

    color = mix(color,
                vec3(0.666667,1,1),
                clamp(length(r.x),0.0,1.0));

    gl_FragColor = vec4((f*f*f+.6*f*f+.5*f)*color,1.);
            
            
            */

            float4 frag2(Interpolators i)
            {
                clip(DiscardPixelLODCrossFade(i.positionHCS, i.fade));

                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                if (baseTex.a == 0.0)discard;
                float4 colour =0.0;// i.colour;

                return baseTex * colour; //_BaseColour;
            }
            ENDHLSL
        }
    }
}

