Shader "Game/HitIndicatorSector"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,0,0,0.25)
        _InnerColor ("Inner Color", Color) = (1,0.3,0.3,0.6)
        _AngleDeg ("Sector Angle (deg)", Float) = 90
        _RotationDeg ("Rotation (deg)", Float) = 0
        _Progress ("Inner Progress 0..1", Range(0,1)) = 0
        _EdgeAA ("Edge AA", Range(0.001,0.1)) = 0.01
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _BaseColor, _InnerColor;
            float _AngleDeg, _RotationDeg, _Progress, _EdgeAA;

            struct appdata { float4 vertex: POSITION; float2 uv: TEXCOORD0; };
            struct v2f { float4 pos: SV_POSITION; float2 uv: TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o;
            }

            float aa(float d, float w){ return smoothstep(0.5 - w, 0.5 + w, d); }

            fixed4 frag (v2f i) : SV_Target
            {
                // Map to [-1,1] space
                float2 p = (i.uv - 0.5) * 2.0;

                // Sector mask
                float ang = radians(_AngleDeg);
                float rot = radians(_RotationDeg);
                float a = atan2(p.y, p.x) - rot;
                // Wrap to [-PI, PI]
                a = atan2(sin(a), cos(a));
                float halfA = ang * 0.5;
                float inAngle = step(abs(a), halfA);

                // Circle radii in this space: outer=1, inner=_Progress
                float d = length(p);
                float w = _EdgeAA; // antialias width

                // Base filled sector (0..1 falloff at edge)
                float baseMask = inAngle * (1.0 - smoothstep(1.0 - w, 1.0 + w, d));

                // Growing inner fill: radius = saturate(_Progress)
                float rInner = saturate(_Progress);
                float innerMask = inAngle * (1.0 - smoothstep(rInner - w, rInner + w, d));

                fixed4 col = 0;
                col.rgb = _BaseColor.rgb;
                col.a = _BaseColor.a * saturate(baseMask);
                // Add inner overlay
                col.rgb = lerp(col.rgb, _InnerColor.rgb, innerMask);
                col.a = saturate(col.a + _InnerColor.a * innerMask);
                return col;
            }
            ENDCG
        }
    }
}


