Shader "Custom/GalaxyShader" { 

   Properties {
		_Colour ("Colour", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
        _EmissionMap("Emission Map", 2D) = "black"{}
        [HDR] _EmissionColour("Emission colour", Color) = (0,0,0,0)
        //_BumpMap ("Bumpmap", 2D) = "bump" {}
		//_MetallicGlossMap("Metallic", 2D) = "white" {}
		//_Metallic ("Metallic", Range(0,1)) = 0.0
		//_Glossiness ("Smoothness", Range(0,1)) = 1.0
	}

   SubShader {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 200
        //Blend SrcAlpha one
 
        Pass{
	        HLSLPROGRAM

            #pragma vertex vert 
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


            float GenerateRandom(int x)
            {
                float2 p = float2(x, sqrt(x));
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 colour : COLOR;
                float3 normalOS : NORMAL0;
                float2 uv : TEXCOORD0;
                uint instanceId : SV_InstanceID;
                
            };
	        struct VertexOut 
            {
                float4 positionHCS : SV_POSITION;
                float4 colour : COLOR;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
	        };
            float4x4 _Matrix;
            float3 _BodyPosition;

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            float4 _EmissionColour;
            float4 _Colour;

            StructuredBuffer<float3> _Positions; 


            float4x4 CreateMatrix(float3 pos, float3 dir, float3 up, uint id) {
                float3 zaxis = normalize(dir);
                float3 xaxis = normalize(cross(up, zaxis));
                float3 yaxis = cross(zaxis, xaxis);
                //float scale = GenerateRandom(id) * _MaxStarSize;
                //Transform the vertex into the object space of the currently drawn mesh using a Transform Rotation Scale matrix.
                return float4x4(
                    xaxis.x * 1, yaxis.x, zaxis.x, pos.x,
                    xaxis.y, yaxis.y * 1, zaxis.y, pos.y,
                    xaxis.z, yaxis.z, zaxis.z * 1, pos.z,
                    0, 0, 0, 1
                );
            }
     
                VertexOut vert(Attributes i)
            {
                VertexOut o;

                //Calculate the position of an instance based on its id. This position is the centre of the mesh we draw
                _Matrix = CreateMatrix(_Positions[i.instanceId], float3(1.0,1.0,1.0), float3(0.0, 1.0, 0.0), i.instanceId);
                //float4 posOS = float4(_Positions[i.instanceId], 1) + i.positionOS;
                float4 posOS = mul(_Matrix, i.positionOS); //transform the current vertex position so it is positioned and rotated relative to our the centre of the mesh
                float4 posWS = mul(unity_ObjectToWorld, posOS); //transform the transformed position to world space 
                o.positionHCS = mul(UNITY_MATRIX_VP, posWS); //transform the world space position to homogeneous clip space
                o.uv = i.uv;
                o.colour = (i.instanceId % 100 == 0) ? float4(1.0, 1.0, 1.0, 1.0) : _Colour;
                o.colour += _EmissionColour;
                //convert vertex normal to world space. The UniversalFragmentBlinnPhong expects normals in WS.
                //It's good to calculate normals in the vertex shader since its run fewer times than the fragment shader'
                float4 normalOS = mul(_Matrix, i.normalOS);
                o.normalWS = mul(unity_ObjectToWorld, normalOS);  
                return o;
            }

            float4 frag(VertexOut i) : SV_TARGET0 
            {
                float4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                //return texel * _Colour;

                //Lighting calculations
                //Cast the lighting and surface input to zero to initialise all its fields to zero
                InputData lightingInput = (InputData)0; //struct holds information about the position and orientation of the current fragment
                SurfaceData surfaceInput = (SurfaceData)0;//holds information about the surface material's physical properties.
                
                lightingInput.normalWS = normalize(i.normalWS);

                surfaceInput.albedo = texel.rgb * i.colour;
                surfaceInput.alpha = texel.a * i.colour;
                return UniversalFragmentBlinnPhong(lightingInput, surfaceInput);
            }
            ENDHLSL
         }

         
   }
}
