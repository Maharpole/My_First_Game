// 8/26/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

Shader "UI/WaterCausticsWithScrollAndBlend"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _CausticsTex ("Caustics Texture", 2D) = "white" {}
        _CausticsSpeed ("Caustics Speed", float) = .01
        _CausticsIntensity ("Caustics Intensity", float) = 1.0
        [Range(0, 1)] _BlendAmount ("Blend Amount", Range(0, 1)) = 0.1
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Stencil
            {
                Ref 1
                Comp Equal
                Pass Keep
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _CausticsTex;
            float _CausticsSpeed;
            float _CausticsIntensity;
            float _BlendAmount;
            float4 _MainTex_ST;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Main texture (health bar background)
                fixed4 col = tex2D(_MainTex, i.uv);

                // Caustics texture overlay with scrolling
                float2 causticsUV = i.uv + float2(_Time.y * _CausticsSpeed, _Time.y * _CausticsSpeed);
                fixed4 caustics = tex2D(_CausticsTex, causticsUV) * (_CausticsIntensity * _BlendAmount);

                // Add caustics scaled by BlendAmount, keep alpha
                col.rgb = saturate(col.rgb + caustics.rgb);
                return col;
            }
            ENDCG
        }
    }
}