// MultiMeshInstancing.shader
Shader "Custom/MultiMeshInstancing"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            // 关键：启用 Procedural Instancing 以使用自定义的实例数据
            #pragma instancing_options procedural:SetupInstanceData

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct InstanceData
            {
                float4x4 objectToWorld;
                uint modelID;
            };

           

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                //uint instanceID : SV_InstanceID; // 关键：获取实例ID
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _BaseColor;

            // 定义过程化实例化设置函数
            #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
             // 包含实例数据的主缓冲区
            StructuredBuffer<InstanceData> _InstanceDataBuffer;
            
            // 此函数由 `#pragma instancing_options procedural:SetupInstanceData` 调用
            void SetupInstanceData()
            {
                // 设置Unity的内置变换矩阵
                // 使用 unity_InstanceID 从缓冲区中查找当前实例的变换矩阵
                unity_ObjectToWorld = _InstanceDataBuffer[unity_InstanceID].objectToWorld;
                unity_WorldToObject = unity_ObjectToWorld;
                // 逆转置并调整符号以计算正确的世界到物体矩阵（简化处理）
                unity_WorldToObject._14_24_34 *= -1;
                unity_WorldToObject._11_22_33 = 1.0 / unity_WorldToObject._11_22_33;
            }
            #endif

            
            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                
                // 设置实例化数据
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                // 由于使用了 procedural instancing, SetupInstanceData() 会自动被调用，
                // 从而设置了 unity_ObjectToWorld 和 unity_WorldToObject 矩阵。

                float3 positionWS = mul(unity_ObjectToWorld, float4(IN.positionOS.xyz, 1.0)).xyz;
                OUT.positionCS = mul(UNITY_MATRIX_VP, float4(positionWS, 1.0));
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                 UNITY_SETUP_INSTANCE_ID(IN);
                half4 col = tex2D(_MainTex, IN.uv) * _BaseColor;
                return col;
            }
            ENDHLSL
        }
    }
}