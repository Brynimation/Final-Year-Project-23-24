Shader "Custom/InterstellarCloudShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _CameraUp("Camera Up", vector) = (0.0,0.0,0.0)
        _BaseColour ("Base Colour", Color) = (1,1,1,1)
        _CloudSize("Cloud Size", float) = 2.0
        _CameraPosition("Camera Position", vector) = (0.0,0.0,0.0)
        _EmissionMap("Emission Map", 2D) = "black"{}
        [HDR] _EmissionColour("Emission colour", Color) = (0,0,0,0)
        _Emission("Emission", Range(0, 100)) = 50
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
            Blend SrcAlpha OneMinusSrcAlpha
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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            float4 _EmissionColour;
            float _Emission;
            float3 _CameraPosition;
            float _CloudSize;
            RWStructuredBuffer<GalaxyStar> _Positions;

            struct GeomData
            {
                //float size : PSIZE;
                float4 positionWS : POSITION;
                float4 colour : COLOR;
                float2 uv : TEXCOORD0;
                float radius : TEXCOORD2;
                float3 forward : TEXCOORD3;
                float3 right : TEXCOORD4;
                float3 up : TEXCOORD5;
            };

             struct Interpolators 
            {
                float4 positionHCS : SV_POSITION; //SV_POSITION = semantic = System Value position - pixel position
                float4 colour : COLOR;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 centreHCS : TEXCOORD2;
            };

            GeomData vert(uint id : SV_INSTANCEID)
            {
                GeomData o;
                GalaxyStar star = _Positions[id];
                o.positionWS = mul(unity_ObjectToWorld, float4(star.position, 1.0));
                o.colour = star.colour;
                o.radius = star.radius * _CloudSize;
                o.colour += star.colour;
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
            float4 frag(Interpolators i) : SV_Target 
            {

                float2 p = i.uv * 2.0 - 1.0;//Convert range of uv coordinates to (-1, 1)
                float radius = 1.0;
                float2 centre = float2(0.0, 0.0);
                float dist = distance(p, centre);
                if(dist > radius) discard;

                return float4(i.colour.rgb/2.0, 0.05 * pow((1 - dist), 2.0)); //_BaseColour;
            }
            ENDHLSL
        }
    }
}
