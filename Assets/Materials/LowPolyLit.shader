Shader "Custom/LowPolyLit"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Ambient ("Ambient", Range(0,1)) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        Cull Back
        ZWrite On

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            sampler2D _MainTex; float4 _MainTex_ST; float4 _Color; float _Ambient;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Flat-shaded normal from screen-space derivatives of world position
                float3 dpdx = ddx(i.worldPos);
                float3 dpdy = ddy(i.worldPos);
                float3 N = normalize(cross(dpdy, dpdx));

                // Main directional light (ForwardBase)
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = saturate(dot(N, L));

                fixed4 albedo = tex2D(_MainTex, i.uv) * _Color;
                float3 litCol = albedo.rgb * ( _Ambient + NdotL * _LightColor0.rgb );

                fixed4 col = fixed4(litCol, albedo.a);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}



