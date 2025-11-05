Shader "UI/UnlitBlurSimple"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size (px)", Float) = 2.0
        _Opacity ("Opacity", Range(0,1)) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize; // x = 1/width, y = 1/height
            float _BlurSize;
            float _Opacity;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            // Weights chosen to approximate a smooth-ish blur:
            // center 0.4, axis taps 0.12 each, diagonals 0.03 each (sums to 1)
            static const float wCenter = 0.40;
            static const float wAxis   = 0.12;
            static const float wDiag   = 0.03;

            float4 frag (v2f i) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy;
                float2 offs = texel * _BlurSize;

                float2 uv = i.uv;

                float4 c = tex2D(_MainTex, uv) * wCenter;

                // axis
                c += tex2D(_MainTex, uv + float2( offs.x, 0.0)) * wAxis;
                c += tex2D(_MainTex, uv + float2(-offs.x, 0.0)) * wAxis;
                c += tex2D(_MainTex, uv + float2(0.0,  offs.y)) * wAxis;
                c += tex2D(_MainTex, uv + float2(0.0, -offs.y)) * wAxis;

                // diagonals
                c += tex2D(_MainTex, uv + float2( offs.x,  offs.y)) * wDiag;
                c += tex2D(_MainTex, uv + float2(-offs.x,  offs.y)) * wDiag;
                c += tex2D(_MainTex, uv + float2( offs.x, -offs.y)) * wDiag;
                c += tex2D(_MainTex, uv + float2(-offs.x, -offs.y)) * wDiag;

                // multiply by opacity (keeps alpha too)
                c.a *= _Opacity;
                return c;
            }
            ENDHLSL
        }
    }
    FallBack "Unlit/Texture"
}