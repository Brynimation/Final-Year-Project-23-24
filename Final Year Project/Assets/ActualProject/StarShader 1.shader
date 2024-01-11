Shader "Custom/StarShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _CellSize("CellSize", Float) = 1.0
        _Colour("Colour", Color) = (1.0, 0.0, 0.0, 1.0)
        _BorderColour("BorderColour", Color) = (1.0, 0.2, 0.2, 1.0)
        _BorderWidth("BorderWidth", Float) = 0.1
        _WobbleMagnitude("WobbleMagnitude", Float) = 0.03
    }
    SubShader
    {

        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #include "Assets/ActualProject/Voronoi.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #pragma target 5.0
            #pragma vertex vert 
            #pragma fragment frag
            #pragma multi_compile_instancing


            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            StructuredBuffer<SolarSystem> _SolarSystems;
            StructuredBuffer<float3> _VertexBuffer;
            StructuredBuffer<float3> _NormalBuffer;
            StructuredBuffer<float2> _UVBuffer;

            uniform float _CellSize;
            uniform float4 _Colour;
            uniform float4 _BorderColour;
            uniform float _BorderWidth;
            uniform float _WobbleMagnitude;
            uniform float solarSystemSwitchDist;
            uniform float minDist;
            uniform float3 playerPosition;

            struct Attributes
            {
                uint instanceId : SV_INSTANCEID;
                uint vertexId : SV_VERTEXID;
            };

            struct Interpolators
            {
                float2 uv : TEXCOORD0;
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                float3 normWS : TEXCOORD2;
                float4 mainColour : COLOR0;
                float fade : TEXCOORD3;
            };

            
            Interpolators vert (Attributes i)
            {
                Interpolators o;
                SolarSystem systemData = _SolarSystems[i.instanceId];
                float4x4 modelMatrix = GenerateTRSMatrix(systemData.starPosition, systemData.starRadius); //Create TRS matrix

                float4 vertexPosOS = mul(modelMatrix, float4(_VertexBuffer[i.vertexId], 1.0));

                //wobble
                float noiseValue = pNoise(vertexPosOS.xyz);
                float dist = distance(systemData.starPosition, playerPosition);
                float maxWobbleMagnitude = _WobbleMagnitude * systemData.starRadius / 2.0;
                float wobbleMagnitude = lerp(0.0, _WobbleMagnitude, systemData.fade);
                vertexPosOS.xyz +=_NormalBuffer[i.vertexId] * wobbleMagnitude * sin(_Time.w * noiseValue); 


                VertexPositionInputs positionData = GetVertexPositionInputs(vertexPosOS); //compute world space and clip space position
                VertexNormalInputs normalData = GetVertexNormalInputs(_NormalBuffer[i.vertexId]);
                o.positionHCS = positionData.positionCS;

                float2 uv = _UVBuffer[i.vertexId];
                o.uv = uv;
                o.fade = systemData.fade;
                o.normWS = normalData.normalWS;
                o.positionWS = positionData.positionWS.xyz;
                o.positionHCS = positionData.positionCS;
                o.mainColour = systemData.starColour;
                return o;

            }

            
            float4 frag (Interpolators i) : SV_Target
            {
                //return float4(1.0, 0.0, 0.0, 1.0);
                // sample the texture
                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                //LOD cross fade
                float dither = InterleavedGradientNoise(i.positionHCS, i.fade);
                clip(i.fade - dither);
                float pulsatingCellSize = _CellSize;

                //Our voronoi noise function takes as input the world space position of the current fragment, scaled by the cell size.
                float3 value = i.positionWS.xyz / pulsatingCellSize;

                //Make cells move by having y be time dependent
                value.y += _Time.y;


                //value.y += _Time.y;
                float3 voronoiVal = voronoiNoise3D(value, _BorderWidth);

                //fwidth(value) is the absolute value of the partial derivative of value; it measures how much value changes across the pixel's surface
	            float valueChange = length(fwidth(value)) * 0.5;
	            float isBorder = 1 - smoothstep(0.05 - valueChange, 0.05 + valueChange, voronoiVal.z); //smoothly interpolate between 0 and 1 as the distance to the border varies around 0.05
                return lerp(i.mainColour, _BorderColour, isBorder); //linearly interpolate between the border and cell colour based on the value of the parameter above.
            }
            ENDHLSL
        }
    }
}