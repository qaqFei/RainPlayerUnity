Shader "Custom/StrokeRoundRect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Width ("Width", Float) = 100.0
        _Height ("Height", Float) = 100.0
        _FillColor ("FillColor", Color) = (1,1,1,1)
        _StrokeColor("StrokeColor", Color) = (0,0,0,1)
        _StrokeWidth ("StrokeWidth", Float) = 5.0
        _RoundRadius ("RoundRadius", Float) = 10.0
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
            float _Width;
            float _Height;
            float4 _FillColor;
            float4 _StrokeColor;
            float _StrokeWidth;
            float _RoundRadius;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 colorFormD(float d) {
                if (d > _RoundRadius) return float4(0,0,0,0);
                else if (d < _RoundRadius - _StrokeWidth) return _FillColor;
                else return _StrokeColor;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv * float2(_Width, _Height);
                float4 final = float4(0,0,0,1);

                if (uv.x < _RoundRadius) {
                    if (uv.y < _RoundRadius) {
                        float d = length(uv - float2(_RoundRadius, _RoundRadius));
                        final = colorFormD(d);
                    } else if (uv.y < _Height - _RoundRadius) final = uv.x < _StrokeWidth ? _StrokeColor : _FillColor;
                    else {
                        float d = length(uv - float2(_RoundRadius, _Height - _RoundRadius));
                        final = colorFormD(d);
                    }
                } else if (uv.x < _Width - _RoundRadius) {
                    if (uv.y < _StrokeWidth) final = _StrokeColor;
                    else if (uv.y < _Height - _StrokeWidth) final = _FillColor;
                    else final = _StrokeColor;
                } else {
                    if (uv.y < _RoundRadius) {
                        float d = length(uv - float2(_Width - _RoundRadius, _RoundRadius));
                        final = colorFormD(d);
                    } else if (uv.y < _Height - _RoundRadius) final = uv.x > _Width - _StrokeWidth ? _StrokeColor : _FillColor;
                    else {
                        float d = length(uv - float2(_Width - _RoundRadius, _Height - _RoundRadius));
                        final = colorFormD(d);
                    }
                }

                return final;
            }
            ENDCG
        }
    }
}
