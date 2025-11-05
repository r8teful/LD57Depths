Shader "Hidden/GausianBlurblit"
{
    Properties
    {
        _MainTex("Base (RGB)", 2D) = "white" {}
        _BlurSize("Blur size (pixel multiplier)", Float) = 1.0
        _Sigma("Gaussian sigma", Float) = 2.0
        _Radius("Radius (samples)", Int) = 3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        // ---------------------------
        // PASS 0: Horizontal blur
        // ---------------------------
        Pass {
            Name "HORIZONTAL"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_h
            #pragma target 3.0

            #include "UnityCG.cginc"

            // explicit texture + sampler for modern HLSL usage
            Texture2D _MainTex;
            SamplerState sampler_MainTex;

            float4 _MainTex_TexelSize; // x = 1/width, y = 1/height
            float _BlurSize;
            float _Sigma;
            int _Radius;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            static inline float Gauss(float sigma, int x, bool isCenter)
            {
                float xf = (float)x;
                float s2 = sigma * sigma;
                float w = exp(- (xf * xf) / (2.0 * s2));
                return w;
            }

            static inline float GaussianWeightSum1D(float sigma, int radius)
            {
                float sum = 0.0;
                // small loop okay here; compiler will unroll if asked
                for (int i = -radius; i <= radius; ++i) {
                    sum += Gauss(sigma, i, i == 0);
                }
                return sum;
            }

            float4 GaussianBlurSeparable(Texture2D tex, SamplerState samp, float2 delta, float2 uv, float sigma, int radius)
            {
                int idx = -radius;
                float3 accumRGB = float3(0.0, 0.0, 0.0);
                float accumA = 0.0;
                const float totalWeightRcp = 1.0 / GaussianWeightSum1D(sigma, radius);

                // iterate radius+1 pairing, using bilinear sampling to fetch two texels
                for (int i = 0; i < radius + 1; ++i)
                {
                    const int x0 = idx;
                    const bool isNarrow = (radius & 1) == 0 && x0 == 0;
                    const int x1 = isNarrow ? x0 : x0 + 1;

                    const float w0 = Gauss(sigma, x0, x0 == 0);
                    const float w1 = Gauss(sigma, x1, x1 == 0);

                    const float texelOffset = isNarrow ? 0.0 : w1 / (w0 + w1);
                    const float2 sampleUV = uv + (x0 + texelOffset) * delta;
                    const float weight = (w0 + w1) * totalWeightRcp;
                    
                    float4 s = tex.Sample(samp, sampleUV);
                    accumRGB += s.rgb * s.a * weight;
                    accumA   += s.a * weight;
                    // step
                    if ((radius & 1) == 1 && x1 == 0)
                    {
                        idx = 0;
                    }
                    else
                    {
                        idx = x1 + 1;
                    }
                }
                
                float centerA = tex.Sample(samp, uv).a;
                if (centerA <= 1e-5)   // treat as fully transparent
                    return float4(0,0,0,0);
                
                // if no alpha contributed, return transparent; otherwise return straight alpha color
                if (accumA <= 1e-6)
                {
                    return float4(0, 0, 0, 0);
                }
                else
                {
                    return float4(accumRGB, saturate(accumA));
                    //float3 outRGB = accumRGB / accumA;
                    //return float4(outRGB, accumA);
                }
            }

            float4 frag_h(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 delta = float2(_MainTex_TexelSize.x * _BlurSize, 0.0);
                return GaussianBlurSeparable(_MainTex, sampler_MainTex, delta, uv, _Sigma, _Radius);
            }
            ENDHLSL
        }

        // ---------------------------
        // PASS 1: Vertical blur
        // ---------------------------
        Pass {
            Name "VERTICAL"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_v
            #pragma target 3.0

            #include "UnityCG.cginc"

            Texture2D _MainTex;
            SamplerState sampler_MainTex;

            float4 _MainTex_TexelSize;
            float _BlurSize;
            float _Sigma;
            int _Radius;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            static inline float Gauss(float sigma, int x, bool isCenter)
            {
                float xf = (float)x;
                float s2 = sigma * sigma;
                float w = exp(- (xf * xf) / (2.0 * s2));
                return w;
            }

            static inline float GaussianWeightSum1D(float sigma, int radius)
            {
                float sum = 0.0;
                for (int i = -radius; i <= radius; ++i) {
                    sum += Gauss(sigma, i, i == 0);
                }
                return sum;
            }

            float4 GaussianBlurSeparable(Texture2D tex, SamplerState samp, float2 delta, float2 uv, float sigma, int radius)
            {
                int idx = -radius;
                float3 accumRGB = float3(0.0, 0.0, 0.0);
                float accumA = 0.0;
                const float totalWeightRcp = 1.0 / GaussianWeightSum1D(sigma, radius);

                // iterate radius+1 pairing, using bilinear sampling to fetch two texels
                for (int i = 0; i < radius + 1; ++i)
                {
                    const int x0 = idx;
                    const bool isNarrow = (radius & 1) == 0 && x0 == 0;
                    const int x1 = isNarrow ? x0 : x0 + 1;

                    const float w0 = Gauss(sigma, x0, x0 == 0);
                    const float w1 = Gauss(sigma, x1, x1 == 0);

                    const float texelOffset = isNarrow ? 0.0 : w1 / (w0 + w1);
                    const float2 sampleUV = uv + (x0 + texelOffset) * delta;
                    const float weight = (w0 + w1) * totalWeightRcp;
                    
                    float4 s = tex.Sample(samp, sampleUV);
                    accumRGB += s.rgb * s.a * weight;
                    accumA   += s.a * weight;
                    // step
                    if ((radius & 1) == 1 && x1 == 0)
                    {
                        idx = 0;
                    }
                    else
                    {
                        idx = x1 + 1;
                    }
                }
                
                float centerA = tex.Sample(samp, uv).a;
                if (centerA <= 1e-5)   // treat as fully transparent
                    return float4(0,0,0,0);
                
                // if no alpha contributed, return transparent; otherwise return straight alpha color
                if (accumA <= 1e-6)
                {
                    return float4(0, 0, 0, 0);
                }
                else
                {
                    return float4(accumRGB, saturate(accumA));
                    //float3 outRGB = accumRGB / accumA;
                    //return float4(outRGB, accumA);
                }
            }

            float4 frag_v(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 delta = float2(0.0, _MainTex_TexelSize.y * _BlurSize);
                return GaussianBlurSeparable(_MainTex, sampler_MainTex, delta, uv, _Sigma, _Radius);
            }
            ENDHLSL
        }

    } // SubShader
    FallBack Off
}
