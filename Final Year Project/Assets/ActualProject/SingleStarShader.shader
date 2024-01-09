Shader "Custom/SingleStarShader"
{
//https://www.ronja-tutorials.com/post/024-white-noise/
//https://www.ronja-tutorials.com/post/028-voronoi-noise/
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GridSize("Grid Size", Int) = 5
        _NumStarLayers("Num star layers", Int) = 5
        _CellSize("CellSize", Float) = 1.0
        _Colour("Colour", Color) = (1.0, 0.0, 0.0, 1.0)
        _BorderColour("BorderColour", Color) = (1.0, 0.2, 0.2, 1.0)
        _BorderWidth("BorderWidth", Float) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        HLSLINCLUDE

        #pragma target 5.0
        #include "Assets/ActualProject/Utility.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #define PI 3.141519

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        uniform float _CellSize;
        uniform float4 _Colour;
        uniform float4 _BorderColour;
        uniform float _BorderWidth;

        ENDHLSL
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                uint id : SV_VERTEXID;
            };
            

            struct Interpolators
            {
                float2 uv : TEXCOORD0;
                float4 positionHCS : SV_POSITION;
            };

            

            Interpolators vert (Attributes i)
            {
                Interpolators o;
                o.positionHCS = TransformObjectToHClip(i.positionOS);
                o.uv = i.uv;
                return o;
            }

            float2x2 Rotate(float angle)
            {
                float s = sin(angle);
                float c = cos(angle);
                return float2x2(c, -s, s, c);
            }


            float rand1dTo1d(float3 value, float mutator = 0.546){
	            float random = frac(sin(value + mutator) * 143758.5453);
	            return random;
            }
            float rand2dTo1d(float2 value, float2 dotDir = float2(12.9898, 78.233)){
                float2 smallValue = sin(value);
                float random = dot(smallValue, dotDir);
                random = frac(sin(random) * 143758.5453);
                return random;
            }




            float2 rand2dTo2d(float2 value){
                return float2(
                    rand2dTo1d(value, float2(12.989, 78.233)),
                    rand2dTo1d(value, float2(39.346, 11.135))
                );
            }
            float3 rand1dTo3d(float value){
                return float3(
                    rand1dTo1d(value, 3.9812),
                    rand1dTo1d(value, 7.1536),
                    rand1dTo1d(value, 5.7241)
                );
             }
            float voronoiNoise(float2 uv)
            {
                float2 cell = floor(uv);
                float2 cellPos = cell + rand2dTo2d(cell);
                float2 toCell = cellPos - uv;
                float distToCell = length(toCell);
                return distToCell;
            }
            float3 voronoiNoiseImproved(float2 uv)
            {
                float2 baseCell = floor(uv);

                //first pass to find the closest cell
                float2 closestCell;
                float2 toClosestCell;
                float minDistToCell = 10.0;
                for(int i = -1; i <= 1; i++)
                {
                    for(int j = -1; j <= 1; j++)
                    {
                        float2 cell = baseCell + float2(i, j);
                        float2 cellPos = cell + rand2dTo2d(cell);
                        float2 toCell = cellPos - uv;
                        float dist = length(toCell);
                        if(dist < minDistToCell)
                        {
                            minDistToCell = dist;
                            closestCell = cell;
                            toClosestCell = toCell;
                        }
                    }
                }

                //second pass to find the distance to the closest edge
                float minEdgeDist = 10.0;
                for(int i = -1; i <= 1; i++)
                {
                    for(int j = -1; j <= 1; j++)
                    {
                        float2 cell = baseCell + float2(i, j);
                        float2 cellPos = cell + rand2dTo2d(cell);
                        float2 toCell = cellPos - uv;

                        float2 diffToClosestCell = abs(closestCell - cell);
                        bool isClosest = diffToClosestCell.x + diffToClosestCell.y < 0.1;
                        if(!isClosest)
                        {
                            float2 toCentre = (toClosestCell + toCell) * 0.5;
                            float2 cellDiff = normalize(toCell - toClosestCell);
                            float edgeDist = dot(toCentre, cellDiff);
                            minEdgeDist = min(minEdgeDist, edgeDist);
                        }
                    }
                }
                float random = rand2dTo1d(closestCell);
                return float3(minDistToCell, random, minEdgeDist/_BorderWidth);
            }

            float4 frag (Interpolators i) : SV_Target
            {
                // sample the texture
                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float pulsatingCellSize = _CellSize;// * (1.0 + 0.5 * sin(_Time.y * 2.0));
                float2 p = i.uv * 2.0 - 1.0;//Convert range of uv coordinates to (-1, 1)
                float2 value = p / pulsatingCellSize;
                value.y += _Time.y;
                float3 voronoiVal = voronoiNoiseImproved(value);
                bool isBorder = voronoiVal.z < 1.0;
                return isBorder ? _BorderColour : voronoiVal.z * _Colour;

            }
            ENDHLSL
        }
    }
}
