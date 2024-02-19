
//Star field shader tutorial: https://www.youtube.com/watch?v=dhuigO4A7RY&list=PLGmrMu-IwbguU_nY2egTFmlg691DN7uE5&index=32

Shader "Custom/UniverseShaderStarField"
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
        _NumStarLayers("Num Star Layers", Int) = 5
        _GridSize("Grid Size", Int) = 5
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
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
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
                uniform float _NumStarLayers;
                uniform float _GridSize;
                RWStructuredBuffer<MeshProperties> _Properties;

                struct GeomData
                {
                    //float size : PSIZE;
                    float4 positionWS : POSITION;
                    float4 colour : COLOR;
                    float2 uv : TEXCOORD0;
                    float radius : TEXCOORD2;
                    uint id : TEXCOORD3;
                    float3 forward: TEXCOORD4;
                    float3 right : TEXCOORD5;
                    float3 up : TEXCOORD6;
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
                    o.forward = normalize(GetCameraPositionWS() - o.positionWS);
                    float3 worldUp = float3(0.0f, 1.0f, 0.0f);
                    o.right = normalize(cross(o.forward, worldUp));
                    o.up = normalize(cross(o.forward, o.right));
                    return o;
                }

                [maxvertexcount(4)]
                void geom(point GeomData inputs[1], inout TriangleStream<Interpolators> outputStream)
                {

                    GeomData centre = inputs[0];
                    //if(_PositionsLOD1[centre.id].culled == 0) return;
                    float3 forward = centre.forward;
                    float3 right = centre.right;
                    float3 up = centre.up;

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
                        o.positionWS = float4(WSPositions[i], 1.0f);
                        o.uv = uvs[i];
                        o.colour = centre.colour;
                        outputStream.Append(o);
                    }
                }
                 float2x2 Rotate(float angle)
                {
                    float s = sin(angle);
                    float c = cos(angle);
                    return float2x2(c, -s, s, c);
                }

                float bellShapeSineFunction(float x) {
                    return  sin(PI * x);
                }

                float DrawStar(float2 uv, float flare)
                {
                    float4 col = float4(0.0, 0.0, 0.0, 0.0);
                    float2 p = uv; 

                    //Create a "light" at the centre of the texture
                    float d = length(p);
                    float m = 0.01/d;
                    col += m;

                    //create horizontal and vertical lens flare
                    float rays = max(0.0, 1.0 - abs(p.x * p.y * 1000.0)) * flare;

                    //Create 45 degree rotation of lens flarer
                    float2 pRot = mul(Rotate(PI/4.0), p);
                    float rays2 = max(0.0, 1.0 - abs(pRot.x * pRot.y * 1000.0)) * 0.3 * flare;
                    col += (rays + rays2);

                    //fade the brightness out from the centre of the cell
                    col *= smoothstep(0.5, 0.2, d);
                    return col;
                }

                float4 DrawStarLayer(float2 p, int gridSize)
                {
                    float4 col = float4(0.0, 0.0, 0.0, 1.0);
                    p *= gridSize;
                    float2 gv = frac(p) - 0.5; //fractional component of uv - creates a repeating grid over our texture. Number of repetitions is determined by len(p)
                
                    //We want to create a random offset for each star within its cell of the grid. So that the boundaries between cells are not obvious, we need to account for the contribution of the 3x3 subgrid of neighbouring cells
                    float2 id = floor(p);

                    for(int y = -1; y <= 1; y++)
                    {
                        for(int x = -1; x <= 1; x++)
                        {
                            float2 offs = float2(x, y); 
                            float random = Hash21(id + offs);
                            float size = frac(random * 126.34);
                            float flare = smoothstep(0.5, 1.0, size) * 0.5;
                            float star = DrawStar(gv - offs -  float2(random, frac(random * 34.0)) + 0.5, flare);
                            float4 colour = lerp(float4(1.0, 0.0, 0.0, 1.0), float4(0.0, 0.0, 1.0, 1.0), random);//float4(tanh(float3(0.2, 0.0, 0.95) * frac(random * 2353.34) * 2 * PI), 1.0);
                            col += star * size * colour * (sin(_Time.y * 0.3 * random * 2.0 * PI) *0.5 + 1.0);
                        }
                    }
                    //col += Hash21(id);
                    return col;
                }

                float4 frag (Interpolators i) : SV_Target
                {
                    // sample the texture
                    float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                    if(baseTex.a == 0) discard;
                    float4 col = (0.0, 0.0, 0.0, 0.0);
                    float t = _Time.y * 0.1;
                    float2 p = i.uv * 2.0 - 1.0;//Convert range of uv coordinates to (-1, 1)
                    p = mul(Rotate(t), p);

                    for(float i = 0; i < 1.0; i += 1.0/float(_NumStarLayers))
                    {
                        float depth = frac(i + t);
                        float scale = lerp(2.0, 0.5, depth);
                        float fade = bellShapeSineFunction(depth);
                        col += DrawStarLayer(p * scale + i * 453.2, _GridSize) * fade;
                    }

                    return col;
                }
                ENDHLSL
            }
        }
}
