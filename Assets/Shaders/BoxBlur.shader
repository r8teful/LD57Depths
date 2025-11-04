Shader "Custom/BoxBlur"
{
    Properties
    {
        _Blur ("Blur strength (size of filter (2n+1)^2)", Integer) = 1
        _Scale ("Scale (texel offset)", Range(1, 5)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                // float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            TEXTURE2D(_CameraSortingLayerTexture);
            SAMPLER(sampler_CameraSortingLayerTexture);

            CBUFFER_START(UnityPerMaterial)
                int _Blur;
                float _Scale;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                // Calculate the position of the vertex on the screen.
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                 //return half4(0,0,0, 1.0);
                // float rather than half as accumulation may exceed half precision.
                float4 OUT = 0.0;
                half2 pos = IN.screenPos.xy / IN.screenPos.w;
                //return SAMPLE_TEXTURE2D(_CameraSortingLayerTexture, sampler_CameraSortingLayerTexture,pos);
                // ComputeScreenPos requires you to do the perspective divide.
                
                // Calculate the size of a texel in screen space, scaled up.
                half2 texel = _Scale * (1.0 / _ScreenParams.xy);

                // Ensure blur is at least 1.
                int blur_size = _Blur > 0 ? _Blur : 1;

                // Iterate over the pixels in our convolution filter.
                for (int i = -blur_size; i <= blur_size; i++) {
                    for (int j = -blur_size; j <= blur_size; j++) {
                        // Simply sum up each pixel.
                        OUT += SAMPLE_TEXTURE2D(
                          _CameraSortingLayerTexture, 
                          sampler_CameraSortingLayerTexture, 
                          pos + (half2(i, j) * texel));
                    }  
                }

                // Normalise by the number of points we've sampled, to maintain brightness.
                OUT = OUT / ((2 * blur_size + 1) * (2 * blur_size + 1));

                return half4(OUT.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}