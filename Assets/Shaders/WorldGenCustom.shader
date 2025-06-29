Shader "Unlit/WorldGenCustom"
{
   Properties {
        _BiomeCount    ("Biome Count", Int) = 1
        _CaveNoiseScale("Cave Noise Scale", Float) = 10
        _CaveAmp       ("Cave Amp", Float) = 1
        _CaveCutoff    ("Cave Cutoff", Float) = 0.5

        _TrenchBaseWiden("Trench Widen", Float) = 1
        _TrenchBaseWidth("Trench Width", Float) = 1
        _TrenchNoiseScale("Trench Noise", Float) = 5
        _TrenchEdgeAmp ("Trench Edge Amp", Float) = 1

        _Seed          ("Seed", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            int _BiomeCount;
            float _CaveNoiseScale, _CaveAmp, _CaveCutoff;
            float _TrenchBaseWiden, _TrenchBaseWidth, _TrenchNoiseScale, _TrenchEdgeAmp;
            float _Seed;

            float _EdgeNoiseScale[4];
            float _EdgeNoiseAmp  [4];
            float _BlockNoiseScale[4];
            float _BlockNoiseAmp [4];
            float _BlockCutoff   [4];
            float _YStart        [4];
            float _YHeight       [4];
            float _HorSize       [4];

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float3 worldPos : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float Unity_SimpleNoise_float(float2 UV, float Scale) {
                float t = 0.0;

                float freq = pow(2.0, float(0));
                float amp = pow(0.5, float(3 - 0));
                t += unity_valueNoise(float2(UV.x * Scale / freq, UV.y * Scale / freq)) * amp;

                freq = pow(2.0, float(1));
                amp = pow(0.5, float(3 - 1));
                t += unity_valueNoise(float2(UV.x * Scale / freq, UV.y * Scale / freq)) * amp;

                freq = pow(2.0, float(2));
                amp = pow(0.5, float(3 - 2));
                t += unity_valueNoise(float2(UV.x * Scale / freq, UV.y * Scale / freq)) * amp;

                return t;
            }

            v2f vert (appdata v)
            {
                v2f o;
                // compute worldspace position
                float4 worldPosition4 = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = worldPosition4.xyz;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float4 col = float4(i.worldPos.xyz,1);
                return col;
            }
            ENDCG
        }
    }
}