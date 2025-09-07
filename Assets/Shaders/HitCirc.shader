Shader "Custom/HitCirc"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MultColor ("Color", Color) = (1,1,1)
        _Seed ("Seed", Float) = 0.0
        _P ("P", Float) = 0.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        
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
            float4 _MainTex_ST;
            float3 _MultColor;
            float _Seed;
            float _P;

            float rand(float2 n) {
                return frac(sin(dot(n, float2(12.9898, 78.233))) * 43758.5453);
            }

            float noise(float2 p) {
                float2 ip = floor(p);
                float2 fp = frac(p);
                
                float a = rand(ip);
                float b = rand(ip + float2(1.0, 0.0));
                float c = rand(ip + float2(0.0, 1.0));
                float d = rand(ip + float2(1.0, 1.0));
                
                float2 u = fp * fp * (3.0 - 2.0 * fp);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float circularNoise(float2 uv, float density, float seed) {
                float2 center = uv - 0.5;
                float radius = length(center) * density;
                float angle = abs(atan2(center.y, center.x));
                
                if (uv.y > 0.5) {
                    angle += sin(angle) * 2.0;
                }
                
                float2 seedOffset = float2(seed * 100.0, seed * 100.0);
                float2 polarCoord = float2(radius, angle) + seedOffset;
                
                float n = 0.0;
                n += noise(polarCoord) * 0.7;
                n += noise(polarCoord * 2.0) * 0.3;
                n += noise(polarCoord * 4.0) * 0.1;
                
                return n;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float n = circularNoise(i.uv, 50.0, _Seed);
                n = smoothstep(_P, _P, n);
                fixed4 final = tex2D(_MainTex, i.uv);
                final.rgb *= _MultColor;
                final.a *= n < 0.5 ? 0. : 1.;
                return final;
            }
            ENDCG
        }
    }
}
