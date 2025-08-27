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

        // Alpha shaping for smoke-like effect
        [Range(0,1)] _AlphaStrength ("Alpha Strength (use caustics as alpha)", Range(0,1)) = 1
        _AlphaPower ("Alpha Power (contrast)", float) = 1
        [Toggle] _AlphaInvert ("Invert Alpha", Float) = 0

        // UI Masking standard properties (hidden, driven by Mask/MaskableGraphic)
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        ZTest [unity_GUIZTestMode]

        Pass
        {
            Stencil
            {
                Ref [_Stencil]
                Comp [_StencilComp]
                Pass [_StencilOp]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
            }
            ColorMask [_ColorMask]

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
            float _AlphaStrength;
            float _AlphaPower;
            float _AlphaInvert;
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
                // Base color
                fixed4 col = tex2D(_MainTex, i.uv);

                // Caustics texture used as moving alpha mask
                float2 causticsUV = i.uv + float2(_Time.y * _CausticsSpeed, _Time.y * _CausticsSpeed);
                fixed4 cau = tex2D(_CausticsTex, causticsUV);
                // Grayscale from RGB (or use cau.a if authored)
                float aMap = dot(cau.rgb, float3(0.299, 0.587, 0.114));
                aMap = aMap * _CausticsIntensity;
                if (_AlphaInvert > 0.5) aMap = 1.0 - aMap;
                aMap = saturate(pow(max(1e-5, aMap), max(1e-5, _AlphaPower)));

                // Blend main alpha toward caustic alpha
                col.a = lerp(col.a, col.a * aMap, _AlphaStrength);

                // Optional subtle color blend using previous behavior (kept under _BlendAmount)
                fixed3 causticRGB = cau.rgb * _CausticsIntensity;
                col.rgb = lerp(col.rgb, saturate(col.rgb + causticRGB), _BlendAmount);
                return col;
            }
            ENDCG
        }
    }
}