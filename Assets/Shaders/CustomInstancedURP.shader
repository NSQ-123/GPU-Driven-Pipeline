Shader "Custom/InstancedURP"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                //uint instanceID : SV_InstanceID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // 声明颜色属性
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            // 声明实例属性缓冲区
            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_INSTANCING_BUFFER_END(Props)

            StructuredBuffer<float4x4> _MatrixBuffer;
            StructuredBuffer<float4> _ColorBuffer;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                 // 使用 UNITY_GET_INSTANCE_ID 宏来获取实例 ID
                uint instanceID = UNITY_GET_INSTANCE_ID(v);
                // 根据实例 ID 从缓冲区中获取变换矩阵
                float4x4 instanceMatrix = _MatrixBuffer[instanceID];
                float4 worldPos = mul(instanceMatrix, v.vertex);
                o.vertex = TransformWorldToHClip(worldPos);
                
                // 根据实例 ID 从缓冲区中获取颜色
                o.color = _ColorBuffer[instanceID];
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                return i.color;
            }
            ENDHLSL
        }
    }
}
