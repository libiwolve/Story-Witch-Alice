Shader "Custom/StencilVisible"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Geometry" }
        Blend SrcAlpha OneMinusSrcAlpha

        Stencil
        {
            Ref 1
            Comp Equal          // 只在模板值=1的地方渲染
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };
            v2f vert (appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv);

                // 提取明度
                float gray = dot(texColor.rgb, float3(0.299, 0.587, 0.114));

                // 稍微提亮暗部
                gray = pow(gray, 0.75);

                // 冷白星光色：微微偏蓝
                fixed3 starColor = _Color.rgb * gray;

                // 外发光：越靠近边缘越亮（可选，需要 UV 中心点在 (0.5, 0.5)）
                float dist = distance(i.uv, float2(0.5, 0.5));
                float glow = 1.0 - dist * 1.5;  // 中心亮，边缘暗
                glow = saturate(glow);
                starColor += _Color.rgb * glow * 0.3;  // 叠加 30% 光晕

                return fixed4(starColor, texColor.a * _Color.a);
            }
            ENDCG
        }
    }
}