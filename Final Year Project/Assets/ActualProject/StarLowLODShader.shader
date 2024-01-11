//ShaderLab is a Unity specific language that bridges the gap between HLSL and Unity. Everything
//defined outside of the Passes is written in ShaderLab. Everything within the passes
//is written in HLSL.

//https://www.braynzarsoft.net/viewtutorial/q16390-36-billboarding-geometry-shader - billboarding in geometry shader tutorial
//https://www.youtube.com/watch?v=gY1Mx4kkZPU&t=603s
//https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-semantics
//https://gist.github.com/fuqunaga/1a649158d69241d31b023ec9983b0164

/*In spiral galaxies the velocities of stars in the outer orbits are much faster than expected.- https://sites.ualberta.ca/~pogosyan/teaching/ASTRO_122/lect24/lecture24.html*/


Shader "Custom/StarLowLODShader"
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
            #pragma geometry geom 
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 5.0
            #include "Assets/ActualProject/Voronoi.hlsl"

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
            uniform float _CellSize;
            uniform float4 _Colour;
            uniform float4 _BorderColour;
            uniform float _BorderWidth;

            float4 _EmissionColour;
            float _Emission;
            float3 _CameraPosition;
            RWStructuredBuffer<MeshProperties> _Properties;

            struct GeomData
            {
                //float size : PSIZE;
                float4 positionWS : POSITION;
                float4 colour : COLOR;
                float2 uv : TEXCOORD0;
                float fade : TEXCOORD1;
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
                
            GeomData vert(uint id : SV_INSTANCEID)
            {
                GeomData o;
                o.id = id;
                //_Matrix = CreateMatrix(_PositionsLOD1[id], float3(1.0,1.0,1.0), float3(0.0, 1.0, 0.0), id);
                //float4 posOS = mul(_Matrix, _PositionsLOD1[id]);
                MeshProperties mp = _Properties[id];
                float4 posOS = mul(mp.mat, float4(0.0, 0.0, 0.0, 1.0));
                //can't use id to determine properties - let's use position
                o.positionWS = mul(unity_ObjectToWorld, posOS);
                int seed = GenerateRandom(o.positionWS.xy);
                o.colour = mp.colour;
                o.radius = mp.scale;
                o.fade = mp.fade;
                //o.colour += _EmissionColour;
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


                for (int i = 0; i < 4; i++)
                {
                    Interpolators o;
                    o.centreHCS = mul(UNITY_MATRIX_VP, centre.positionWS);
                    o.positionHCS = mul(UNITY_MATRIX_VP, float4(WSPositions[i], 1.0f));
                    o.positionWS = WSPositions[i];
                    o.uv = uvs[i];
                    o.colour = centre.colour;
                    outputStream.Append(o);
                }
            }

            float4 frag(Interpolators i) : SV_Target
            {

                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                if (baseTex.a == 0.0)discard;
                float pulsatingCellSize = _CellSize;
                float2 p = i.uv * 2.0 - 1.0;
                float3 value = i.positionWS / pulsatingCellSize;

                //Make cells move by having y be time dependent
                value.y += _Time.y;


                //value.y += _Time.y;
                float3 voronoiVal = voronoiNoise3D(value, _BorderWidth);

                //fwidth(value) is the absolute value of the partial derivative of value; it measures how much value changes across the pixel's surface
	            float valueChange = length(fwidth(value)) * 0.5;
	            float isBorder = 1 - smoothstep(0.05 - valueChange, 0.05 + valueChange, voronoiVal.z); //smoothly interpolate between 0 and 1 as the distance to the border varies around 0.05
                return lerp(i.colour, _BorderColour, isBorder); //linearly interpolate between the border and cell colour based on the value of the parameter above.
            }
            ENDHLSL
        }
    }
}
