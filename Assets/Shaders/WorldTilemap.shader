Shader "Custom/WorldTilemap"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {} // Required for Tilemap compatibility
        _TextureArray ("Seamless Texture Array", 2DArray) = "" {}
        _TilingScale ("Tiling Scale", Float) = 1.0 // Controls how much the texture repeats per world unit
        _MaxIndex ("Max Texture Index", Float) = 255.0 // Match to your max (e.g., 255 for alpha encoding)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "PreviewType"="Plane" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma require 2darray

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            UNITY_DECLARE_TEX2DARRAY(_TextureArray);
            float _TilingScale;
            float _MaxIndex;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Compute world-space UV for seamless tiling
                float2 worldUV = i.worldPos.xy * _TilingScale;

                // Get texture index from vertex color alpha
                float index = round(i.color.a * _MaxIndex);

                // Sample the seamless texture from the array
                fixed4 seamlessColor = UNITY_SAMPLE_TEX2DARRAY(_TextureArray, float3(worldUV.x, worldUV.y, index));

                // Optional: Combine with original sprite texture (e.g., multiply or replace)
                fixed4 spriteColor = tex2D(_MainTex, i.uv) * i.color; // Uses tile sprite if needed
                fixed4 finalColor = seamlessColor * spriteColor; // Or just return seamlessColor if no sprite needed

                return finalColor;
            }
            ENDCG
        }
    }
}
