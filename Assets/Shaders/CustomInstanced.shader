Shader "Custom/CustomInstanced"
{
    /*
    1.#pragma multi_compile_instancing: 这个指令告诉Unity为这个Shader编译支持GPU Instancing的变体
    2.UNITY_VERTEX_INPUT_INSTANCE_ID: 在顶点着色器的输入/输出结构体中声明，用于存储每个实例的唯一ID
    3.UNITY_INSTANCING_BUFFER宏: 用这些宏创建一个常量缓冲区，专门用于存放所有实例的共享属性（如你的 _Color数组）
    4.UNITY_DEFINE_INSTANCED_PROP: 在这个缓冲区内部，将 _Color属性定义为实例属性
    5.UNITY_SETUP_INSTANCE_ID: 在顶点着色器和片元着色器的开始处调用，使得当前实例的ID可用
    6.UNITY_ACCESS_INSTANCED_PROP: 这是最关键的一步。在片元着色器中，使用这个宏并传入实例属性所在的缓冲区名称（Props）和属性名（_Color），
    来获取当前渲染实例所对应的颜色值，它利用内置的实例ID作为索引，从C#代码设置的 batchColors数组中取出正确的颜色。
    */
    
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Default Color", Color) = (1,1,1,1) // 默认颜色，当实例数据未设置时使用
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // 关键步骤1：添加此编译指令，生成实例化变体
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                // 关键步骤2：在输入结构体中定义实例ID
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                // 关键步骤3：将实例ID从顶点着色器传递到片元着色器
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            // 关键步骤4：声明一个实例（Per-Instance）的颜色缓冲区
            UNITY_INSTANCING_BUFFER_START(Props)
            // 关键步骤5：将 _Color 定义为实例属性
            UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert(appdata v)
            {
                v2f o;

                // 关键步骤6：设置实例ID
                UNITY_SETUP_INSTANCE_ID(v);
                // 关键步骤7：传递实例ID到片元着色器（因为片元着色器要访问实例属性）
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 关键步骤8：在片元着色器中设置实例ID
                UNITY_SETUP_INSTANCE_ID(i);
                fixed4 col = tex2D(_MainTex, i.uv);
                // 关键步骤9：正确访问当前实例的 _Color 属性
                fixed4 instanceColor = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

                return col * instanceColor;
            }
            ENDCG
        }
    }
}