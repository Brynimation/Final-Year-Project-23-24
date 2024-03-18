//Star field shader tutorial: https://www.youtube.com/watch?v=dhuigO4A7RY&list=PLGmrMu-IwbguU_nY2egTFmlg691DN7uE5&index=32

Shader "Custom/Skybox"
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
        _StarSpeed("Star Speed", Float) = 0.5
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
                #pragma fragment frag
                #pragma multi_compile_instancing
                #pragma target 5.0
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Assets/ActualProject/Helpers/Utility.hlsl"

                UNITY_INSTANCING_BUFFER_START(MyProps)
                    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColour)
                UNITY_INSTANCING_BUFFER_END(MyProps)

                TEXTURE2D(_MainTex);
                SAMPLER(sampler_MainTex);
                TEXTURE2D(_EmissionMap);
                SAMPLER(sampler_EmissionMap);

                float4 _EmissionColour;
                float _Emission;
                float3 _CameraPosition;
                float _MaxStarSize;
                uniform float _NumStarLayers;
                uniform float _StarSpeed;
                uniform float _GridSize;
                RWStructuredBuffer<MeshProperties> _Properties;

                struct Attributes
                {
                    float4 positionOS : POSITION;
                    float2 uv : TEXCOORD0;
                };
                 struct Interpolators
                {
                    float4 positionHCS : SV_POSITION; 
                    float2 uv : TEXCOORD0;
                };

                
                Interpolators vert(Attributes i)
                {
                    Interpolators o;
                    o.positionHCS = TransformObjectToHClip(i.positionOS);
                    o.uv = i.uv;
                    return o;
                }

                //Fragment shader and associated functions based off of this tutorial: https://www.youtube.com/watch?v=rvDo9LvfoVE
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
                    float t = _Time.y * 0.1 * _StarSpeed;
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