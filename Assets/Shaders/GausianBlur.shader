Shader "Custom/GaussianBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        // Pass 0: Horizontal Blur
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
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
            float4 _MainTex_TexelSize;
            float _BlurSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 sum = half4(0.0, 0.0, 0.0, 0.0);
                float2 texel = _MainTex_TexelSize.xy;
                float offset = _BlurSize;

                // 9-tap Gaussian (adjust taps for strength/performance)
                sum += tex2D(_MainTex, i.uv + float2(-4.0*texel.x*offset, 0.0)) * 0.016216;
                sum += tex2D(_MainTex, i.uv + float2(-3.0*texel.x*offset, 0.0)) * 0.054054;
                sum += tex2D(_MainTex, i.uv + float2(-2.0*texel.x*offset, 0.0)) * 0.1216216;
                sum += tex2D(_MainTex, i.uv + float2(-1.0*texel.x*offset, 0.0)) * 0.1945946;
                sum += tex2D(_MainTex, i.uv) * 0.227027;
                sum += tex2D(_MainTex, i.uv + float2( 1.0*texel.x*offset, 0.0)) * 0.1945946;
                sum += tex2D(_MainTex, i.uv + float2( 2.0*texel.x*offset, 0.0)) * 0.1216216;
                sum += tex2D(_MainTex, i.uv + float2( 3.0*texel.x*offset, 0.0)) * 0.054054;
                sum += tex2D(_MainTex, i.uv + float2( 4.0*texel.x*offset, 0.0)) * 0.016216;

                return sum;
            }
            ENDCG
        }

        // Pass 1: Vertical Blur (copy-paste frag, change to y-offset)
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Same struct/v2f/vert as above...

            half4 frag (v2f i) : SV_Target
            {
                half4 sum = half4(0.0, 0.0, 0.0, 0.0);
                float2 texel = _MainTex_TexelSize.xy;
                float offset = _BlurSize;

                sum += tex2D(_MainTex, i.uv + float2(0.0, -4.0*texel.y*offset)) * 0.016216;
                sum += tex2D(_MainTex, i.uv + float2(0.0, -3.0*texel.y*offset)) * 0.054054;
                // ... (repeat for -2 to +4, same weights)
                sum += tex2D(_MainTex, i.uv + float2(0.0,  4.0*texel.y*offset)) * 0.016216;

                return sum;
            }
            ENDCG
        }
    }
}