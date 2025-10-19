Shader "Custom/IndirectInstanceSetup"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque" 
            "RenderPipeline"="UniversalPipeline" 
            "Queue"="Geometry"
        }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:vertInstancingSetup  // 关键修改
            #pragma target 4.5
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
            
            // 定义过程化实例化设置函数
            #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
            StructuredBuffer<float4x4> _MatrixBuffer;
            StructuredBuffer<float4> _ColorBuffer;
            
            void vertInstancingSetup()
            {
                #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                uint id = unity_InstanceID;
                float4x4 data = _MatrixBuffer[id];
                
                // 设置Unity的内置变换矩阵
                unity_ObjectToWorld = data;
                unity_WorldToObject = data;
                unity_WorldToObject._14_24_34 *= -1;
                unity_WorldToObject._11_22_33 = 1.0 / unity_WorldToObject._11_22_33;
                #endif
            }
            #endif

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o;
                
                // 设置实例化数据
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                // 现在可以直接使用Unity自动变换的顶点位置
                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
                o.vertex = vertexInput.positionCS;
                
                // 获取实例颜色
                #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                o.color = _ColorBuffer[unity_InstanceID];
                #else
                o.color = float4(1, 1, 1, 1); // 回退颜色
                #endif
                
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                return i.color;
            }
            ENDHLSL
        }
    }
}