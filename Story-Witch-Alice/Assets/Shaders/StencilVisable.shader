Shader "Custom/StencilVisible_HDR"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _EmissionIntensity ("Emission Intensity", Range(0, 5)) = 1.5  // 新增：控制泛光强度
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Geometry" }

        Blend SrcAlpha One
        ZWrite Off

        Stencil
        {
            Ref 1
            Comp Equal
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            float _EmissionIntensity;  // 新增

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            v2f vert (appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv);

                // 原有的灰度转换
                float gray = dot(texColor.rgb, float3(0.299, 0.587, 0.114));
                gray = pow(gray, 0.75);

                // 原有的颜色计算
                fixed3 starColor = _Color.rgb * gray;

                // 原有的边缘发光
                float dist = distance(i.uv, float2(0.5, 0.5));
                float glow = 1.0 - dist * 1.5;
                glow = saturate(glow);
                starColor += _Color.rgb * glow * 0.3;

                // 🎯 关键：乘以亮度强度，让颜色可以超过1
                starColor *= _EmissionIntensity;

                // 🎯 保留原有的透明度
                float alpha = texColor.a * _Color.a;

                // 🎯 注意：Bloom需要在Linear空间工作，输出值可以直接 > 1
                return fixed4(starColor, alpha);
            }
            ENDCG
        }
    }
}