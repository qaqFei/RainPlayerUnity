Shader "Custom/StrokeHalfCirc"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _R ("R", Float) = 0.5
        _MultColor ("Color", Color) = (1,1,1,1)
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
            float _R;
            float4 _MultColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 final = tex2D(_MainTex, i.uv);
                float len = length((i.uv - float2(0.5, 0.0)) * float2(2.0, 1.0));
                if (len < _R) final.a = 0.0;
                final *= _MultColor;
                return final;
            }
            ENDCG
        }
    }
}
