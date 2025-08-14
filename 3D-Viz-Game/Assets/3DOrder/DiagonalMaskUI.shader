Shader "UI/DiagonalMask"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Angle   ("Angle (deg)", Range(-180,180)) = 45
        _Soft    ("Soft Edge", Range(0,0.2)) = 0.02
        _Invert  ("Invert", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        ZWrite Off Cull Off Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Angle;
            float _Soft;
            float _Invert;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                float4 col : COLOR;
            };

            v2f vert (appdata v){
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                o.col = v.color;
                return o;
            }

            // rotate UV around center (0.5,0.5)
            float2 rotateUV(float2 uv, float angDeg){
                float a = radians(angDeg);
                float s = sin(a), c = cos(a);
                uv -= 0.5;
                float2 r = float2( c*uv.x - s*uv.y, s*uv.x + c*uv.y );
                return r + 0.5;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = rotateUV(i.uv, _Angle);

                // diagonal line test: uv.x + uv.y ≶ 1.0 (passes through the top-right corner)
                // shift by -0.5 to center; threshold near 0 to cut through center
                float d = (uv.x + uv.y) - 1.0;

                // soft edge
                float alphaLine = saturate((_Invert>0 ?  d : -d) / _Soft + 0.5);

                fixed4 col = tex2D(_MainTex, i.uv) * i.col;
                col.a *= alphaLine; // mask result
                return col;
            }
            ENDCG
        }
    }
}
