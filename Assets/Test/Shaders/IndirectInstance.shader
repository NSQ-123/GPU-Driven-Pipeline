Shader "Custom/IndirectInstance"
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
           #pragma instancing_options procedural:setup
            #pragma target 4.5 // 或 4.5，确保支持实例化
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct appdata
            {
                float4 vertex : POSITION;
                uint instanceID : SV_InstanceID;  // 直接声明SV_InstanceID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };
            

            StructuredBuffer<float4x4> _MatrixBuffer;
            StructuredBuffer<float4> _ColorBuffer;
           
            v2f vert (appdata v,uint instanceID : SV_InstanceID)
            {
                v2f o;
                // 根据实例ID从缓冲区中获取数据
                float4x4 instanceMatrix = _MatrixBuffer[instanceID];
                float4 worldPos = mul(instanceMatrix, v.vertex);
                o.vertex = TransformWorldToHClip(worldPos.xyz);
                o.color = _ColorBuffer[instanceID];
                return o;
            }
            
            half4 frag (v2f i) : SV_Target
            {
                return i.color;
            }


           
            ENDHLSL
        }
    }
}