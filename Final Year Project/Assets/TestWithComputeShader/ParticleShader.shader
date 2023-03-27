Shader "Custom/ParticleShader" { 

   Properties {
		_Colour ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_BumpMap ("Bumpmap", 2D) = "bump" {}
		_MetallicGlossMap("Metallic", 2D) = "white" {}
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_Glossiness ("Smoothness", Range(0,1)) = 1.0
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

            UNITY_INSTANCING_BUFFER_START(MyProps)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Colour)
            UNITY_INSTANCING_BUFFER_END(MyProps)

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
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;

	        };
            float4x4 _Matrix;
            float3 _BodyPosition;

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            StructuredBuffer<float3> _Positions; 

            float4x4 CreateMatrix(float3 pos, float3 dir, float3 up) {
                float3 zaxis = normalize(dir);
                float3 xaxis = normalize(cross(up, zaxis));
                float3 yaxis = cross(zaxis, xaxis);
                return float4x4(
                    xaxis.x, yaxis.x, zaxis.x, pos.x,
                    xaxis.y, yaxis.y, zaxis.y, pos.y,
                    xaxis.z, yaxis.z, zaxis.z, pos.z,
                    0, 0, 0, 1
                );
            }
     
                VertexOut vert(Attributes i)
            {
                VertexOut o;
                //Calculate the position of an instance based on its id
                _BodyPosition = _Positions[i.instanceId];
                _Matrix = CreateMatrix(_Positions[i.instanceId], float3(1.0,1.0,1.0), float3(0.0, 1.0, 0.0));
                float4 posOS = mul(_Matrix, i.positionOS);
                float4 posWS = mul(unity_ObjectToWorld, posOS);
                o.positionHCS = mul(UNITY_MATRIX_VP, posWS);
                o.uv = i.uv;
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

                surfaceInput.albedo = texel.rgb * _Colour.rgb;
                surfaceInput.alpha = texel.a * _Colour.a;
                return UniversalFragmentBlinnPhong(lightingInput, surfaceInput);
            }
            ENDHLSL
         }

         
   }
}
