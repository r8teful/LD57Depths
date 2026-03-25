Shader "Custom/WorldTilemap"
{
    Properties
    {
        [HideInInspector] _MainTex ("Sprite Texture", 2D) = "white" {} // Required for Tilemap compatibility
        _TextureArray ("Seamless Texture Array", 2DArray) = "" {}
        _TilingScale ("Tiling Scale", Float) = 1.0 // Controls how much the texture repeats per world unit
        _DebugMode ("DebugMode", Float) = 0.0 
        //_PixelSize ("_PixelSize", Float) = 0.05
        //_DistortionStrength ("DistortionStrength", Float) = 0.01 
        //_DistortionSpeed ("DistortionSpeed", Float) = 1.0
        //_DistortionLength ("_DistortionLength", Float) = 1.0
        //_RippleFrequency ("RippleFrequency", Float) = 8.0 
        //_RippleSpeed ("RippleSpeed", Float) = 1.5 
        //_RippleSteps ("RippleSteps", Float) = 4.0 
        //_RippleStrength ("RippleStrength", Float) = 0.01 
        _NoiseSpeed ("_NoiseSpeed", Float) = 0.01 
        _NoiseScale ("_NoiseScale", Float) = 0.01 
        _NoiseStrength ("_NoiseStrength", Float) = 0.01 
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

            float4 _TextureArray_TexelSize; // set by unity
            sampler2D _MainTex;
            UNITY_DECLARE_TEX2DARRAY(_TextureArray);
            float _TilingScale;
            float _DebugMode;
            //float _PixelSize;
            //float _DistortionSpeed   ;
            //float _RippleFrequency   ;
            //float _RippleSpeed       ;
            //float _RippleSteps       ;
            //float _RippleStrength    ;
            //float _DistortionLength    ;
            float _NoiseSpeed    ;
            float _NoiseScale    ;
            float _NoiseStrength;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.pos = UnityPixelSnap(o.pos); // This might or might not work 
                o.uv = v.texcoord;
                o.color = v.color;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            float2 PixelateUV(float2 UV, float Resolution) {
                return floor(UV * Resolution) / Resolution;
            }

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }
            
            float2 hash22(float2 p)
            {
                float x = hash(p);
                float y = hash(p + 19.19);
                return float2(x, y) * 2.0 - 1.0; // range: -1..1
            }
            float perlinNoise(float2 p)
            {
                float2 pi = floor(p);
                float2 pf = frac(p);
                
                // Smoothstep interpolation
                float2 w = pf * pf * (3.0 - 2.0 * pf);
            
                float n00 = dot(hash22(pi + float2(0.0, 0.0)), pf - float2(0.0, 0.0));
                float n10 = dot(hash22(pi + float2(1.0, 0.0)), pf - float2(1.0, 0.0));
                float n01 = dot(hash22(pi + float2(0.0, 1.0)), pf - float2(0.0, 1.0));
                float n11 = dot(hash22(pi + float2(1.0, 1.0)), pf - float2(1.0, 1.0));
            
                return lerp(lerp(n00, n10, w.x), lerp(n01, n11, w.x), w.y);
            }
            fixed4 frag (v2f i) : SV_Target
            {
                // Compute world-space UV for seamless tiling
                float2 worldUV = i.worldPos.xy * _TilingScale;

                // ----------- OPTION 1 ------------- This looked way to ripply
                /*
                float2 cellSize = float2(_DistortionCellSize, _DistortionCellSize);

                float2 cell = floor(worldUV / cellSize);

                float t = _Time.y * _DistortionSpeed;

                float2 n = hash22(cell + float2(t, -t * 0.73));

                float ripple = sin((worldUV.y * _RippleFrequency) + (t * _RippleSpeed));
                ripple = floor(ripple * _RippleSteps) / max(_RippleSteps - 1.0, 1.0);

                float2 distortion = n * _DistortionStrength;
                distortion.y += ripple * _RippleStrength;
                float2 distortedUV = worldUV + distortion;

                // ----------- OPTION 2 ------------- Didn't look bad, it doesn't actually shift anything just wobbles in place

                float2 pixelUV = PixelateUV(worldUV,_DistortionCellSize);
                float2 wobbleOffset = float2(
                    sin(pixelUV.y * _DistortionLength + _Time.y* _DistortionSpeed), 
                    cos(pixelUV.x *_DistortionLength + _Time.y* _DistortionSpeed)
                ) * _DistortionStrength; // Multiply by 0.05 to keep the distortion small
                
                // Add the wobble to the static world UV
                float2 distortedUV = pixelUV + wobbleOffset;
                */
               
                // ----------- OPTION 3 -------------
                float2 texResolution = _TextureArray_TexelSize.zw; 
    
                // Get the exact size of 1 pixel in UV space (e.g., 1.0 / 16.0)
                float2 pixelUVSize = _TextureArray_TexelSize.xy;   

               
                float2 pixelatedUV = floor(worldUV * texResolution) / pixelUVSize;

             
                float2 noiseCoord = pixelatedUV * _NoiseScale;
                noiseCoord = float2(noiseCoord.x,noiseCoord.y + (_Time.y * _NoiseSpeed));
                
                float xDistortion = perlinNoise(noiseCoord);
                float yDistortion = perlinNoise(noiseCoord + float2(12.34, 56.78)); 

                float strength = 0.2 * _NoiseStrength;
                float xJump = step(strength, xDistortion) - step(xDistortion, -strength);
                float yJump = step(strength, yDistortion) - step(yDistortion, -strength);


                float2 distortion = float2(xJump, yJump) * pixelUVSize;

                float2 distortedUV = worldUV + distortion;

                // Get texture index from vertex r colour
                //float linearRed = pow(i.color.r, 2.2);
                float index = round(i.color.r * 16.0); // 16.0 is the index scale
                //float index = round(linearRed * 255); 

                // Sample the seamless texture from the array
                fixed4 seamlessColor = UNITY_SAMPLE_TEX2DARRAY(_TextureArray, float3(distortedUV.x, distortedUV.y, index));
                
				// Optional: Combine with original sprite texture (e.g., multiply or replace)
                fixed4 spriteColor = tex2D(_MainTex, i.uv);

                fixed4 finalColor = lerp(seamlessColor,spriteColor, spriteColor.a);
                //fixed4 finalColor = fixed4(seamlessColor.rgb, spriteColor.a);
                if(_DebugMode > 0.5){
                    if(_DebugMode < 1){
                        float debugValue = index / 25.5; 
                        return fixed4(debugValue,debugValue,debugValue,1);
                    }
                    if(_DebugMode < 2){
                        return fixed4(spriteColor.rgb,spriteColor.a);
                    }
                     if(_DebugMode < 3){                    
                        return fixed4(spriteColor.a, spriteColor.a, spriteColor.a, 1.0);
                    }
                    if(_DebugMode < 4){                    
                       // return fixed4(i.uv.x, i.uv.y, 0.0, 1.0);
                        return fixed4(frac(worldUV).x, frac(worldUV).y, 0, 1); 
                   } if(_DebugMode <5){                    
                       // return fixed4(i.uv.x, i.uv.y, 0.0, 1.0);
                       float2 vis = distortion / pixelUVSize; // back to -1..1
                        vis = vis * 0.5 + 0.5;
                        return float4(vis.x, vis.y, 0, 1);
                        //return fixed4(distortedUV.x, distortedUV.y, 0, 1); 
                   }
                }
                return finalColor;
            }
            ENDCG
        }
    }
}