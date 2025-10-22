Shader "Example/DrawMeshIndirect" {
   Properties {
       _MainTex ("Albedo (RGB)", 2D) = "white" {}
       _BaseColor("Base Color", Color) = (1, 1, 1, 1)
   }
   SubShader {
       Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

       Pass {
           Name "ForwardLit"
           Tags {"LightMode"="UniversalForward"}

           HLSLPROGRAM

           #pragma vertex vert
           #pragma fragment frag
           #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
           #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
           #pragma multi_compile _ _SHADOWS_SOFT
           #pragma target 4.5

           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

           TEXTURE2D(_MainTex);
           SAMPLER(sampler_MainTex);

           CBUFFER_START(UnityPerMaterial)
               float4 _MainTex_ST;
               float4 _BaseColor;
           CBUFFER_END

       #if SHADER_TARGET >= 45
           StructuredBuffer<float4> positionBuffer;
       #endif

           struct Attributes
           {
               float4 positionOS : POSITION;
               float3 normalOS : NORMAL;
               float2 texcoord : TEXCOORD0;
               float4 color : COLOR;
           };

           struct Varyings
           {
               float4 positionCS : SV_POSITION;
               float2 uv : TEXCOORD0;
               float3 positionWS : TEXCOORD1;
               float3 normalWS : TEXCOORD2;
               float4 color : TEXCOORD3;
               float4 shadowCoord : TEXCOORD4;
           };

           void rotate2D(inout float2 v, float r)
           {
               float s, c;
               sincos(r, s, c);
               v = float2(v.x * c - v.y * s, v.x * s + v.y * c);
           }

           Varyings vert (Attributes input, uint instanceID : SV_InstanceID)
           {
           #if SHADER_TARGET >= 45
               float4 data = positionBuffer[instanceID];
           #else
               float4 data = 0;
           #endif

               float rotation = data.w * data.w * _Time.y * 0.5f;
               rotate2D(data.xz, rotation);

               float3 localPosition = input.positionOS.xyz * data.w;
               float3 worldPosition = data.xyz + localPosition;
               float3 worldNormal = input.normalOS;

               Varyings output;
               output.positionCS = mul(UNITY_MATRIX_VP, float4(worldPosition, 1.0f));
               output.positionWS = worldPosition;
               output.normalWS = worldNormal;
               output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);
               output.color = input.color;
               
               // URP shadow coord
               VertexPositionInputs vertexInput = (VertexPositionInputs)0;
               vertexInput.positionWS = worldPosition;
               output.shadowCoord = GetShadowCoord(vertexInput);
               
               return output;
           }

           half4 frag (Varyings input) : SV_Target
           {
               // Sample texture
               half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
               
               // Get main light
               Light mainLight = GetMainLight(input.shadowCoord);
               
               // Calculate lighting
               half3 normalWS = normalize(input.normalWS);
               half NdotL = saturate(dot(normalWS, mainLight.direction));
               half3 diffuse = mainLight.color * NdotL * mainLight.shadowAttenuation;
               
               // Ambient lighting (SH)
               half3 ambient = SampleSH(normalWS);
               
               // Combine lighting
               half3 lighting = diffuse + ambient;
               half3 finalColor = albedo.rgb * input.color.rgb * lighting * _BaseColor.rgb;
               
               return half4(finalColor, albedo.a);
           }

           ENDHLSL
       }
       
       // Shadow caster pass
       Pass
       {
           Name "ShadowCaster"
           Tags{"LightMode" = "ShadowCaster"}

           ZWrite On
           ZTest LEqual
           ColorMask 0

           HLSLPROGRAM
           #pragma vertex ShadowPassVertex
           #pragma fragment ShadowPassFragment
           #pragma target 4.5

           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
           #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

       #if SHADER_TARGET >= 45
           StructuredBuffer<float4> positionBuffer;
       #endif

           struct Attributes
           {
               float4 positionOS : POSITION;
               float3 normalOS : NORMAL;
           };

           struct Varyings
           {
               float4 positionCS : SV_POSITION;
           };

           void rotate2D(inout float2 v, float r)
           {
               float s, c;
               sincos(r, s, c);
               v = float2(v.x * c - v.y * s, v.x * s + v.y * c);
           }

           float3 _LightDirection;

           Varyings ShadowPassVertex(Attributes input, uint instanceID : SV_InstanceID)
           {
           #if SHADER_TARGET >= 45
               float4 data = positionBuffer[instanceID];
           #else
               float4 data = 0;
           #endif

               float rotation = data.w * data.w * _Time.y * 0.5f;
               rotate2D(data.xz, rotation);

               float3 localPosition = input.positionOS.xyz * data.w;
               float3 worldPosition = data.xyz + localPosition;
               float3 worldNormal = input.normalOS;

               Varyings output;
               float3 positionWS = worldPosition;
               float3 normalWS = worldNormal;

               output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
               
               #if UNITY_REVERSED_Z
                   output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
               #else
                   output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
               #endif

               return output;
           }

           half4 ShadowPassFragment(Varyings input) : SV_TARGET
           {
               return 0;
           }

           ENDHLSL
       }
   }
}