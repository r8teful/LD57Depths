Shader "Custom/WorldTilemap"
{
    Properties
    {
        [HideInInspector] _MainTex ("Sprite Texture", 2D) = "white" {} // Required for Tilemap compatibility
        _TextureArray ("Seamless Texture Array", 2DArray) = "" {}
        _TilingScale ("Tiling Scale", Float) = 1.0 // Controls how much the texture repeats per world unit
        _DebugMode ("DebugMode", Float) = 0.0 
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "PreviewType"="Plane" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
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
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
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
            float _DebugMode;

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

                // Get texture index from vertex r colour
                //float linearRed = pow(i.color.r, 2.2);
                float index = round(i.color.r * 16.0); // 16.0 is the index scale
                //float index = round(linearRed * 255); 

                // Sample the seamless texture from the array
                fixed4 seamlessColor = UNITY_SAMPLE_TEX2DARRAY(_TextureArray, float3(worldUV.x, worldUV.y, index));

                // Optional: Combine with original sprite texture (e.g., multiply or replace)
                
                fixed4 spriteColor = tex2D(_MainTex, i.uv);

                fixed4 finalColor = lerp(seamlessColor,spriteColor, spriteColor.a);
                //fixed4 finalColor = seamlessColor;
                //fixed4 finalColor = fixed4(seamlessColor.rgb, spriteColor.a);
                if(_DebugMode > 0.5){
                    if(_DebugMode < 1){
                        float debugValue = index / 25.5; 
                        return fixed4(debugValue,debugValue,debugValue,1);
                    }
                    if(_DebugMode < 2){
                        return fixed4(finalColor.rgb,finalColor.a);
                    }
                     if(_DebugMode < 3){                    
                        return fixed4(finalColor.a, finalColor.a, finalColor.a, 1.0);
                    }
                    if(_DebugMode < 4){                    
                        return fixed4(i.uv.x, i.uv.y, 0.0, 1.0);
                    }
                }
                return finalColor;
            }
            ENDCG
        }
    }
}
