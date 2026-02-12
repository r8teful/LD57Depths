Shader "Custom/WorldTilemapSimple"
{
    Properties
    {
        [HideInInspector] _MainTex ("Sprite Texture", 2D) = "white" {}
        _TestTex ("TiledTextureTest", 2D) = "white" {}
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
            sampler2D _TestTex;

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
                return tex2D(_TestTex, i.worldPos.xy);
            }
            ENDCG
        }
    }
}