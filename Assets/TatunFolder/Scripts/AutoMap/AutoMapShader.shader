Shader "Tatun/AutoMap/EdgeDetectOverlay"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _EdgeColor ("Edge Color", Color) = (1,1,1,1)
        _Threshold ("Edge Threshold", Range(0.001,1)) = 0.2
        _Intensity ("Edge Intensity", Range(0,4)) = 1.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _EdgeColor;
            float _Threshold;
            float _Intensity;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Simple luminance
            static float lum(float3 c) { return dot(c, float3(0.299, 0.587, 0.114)); }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 t = _MainTex_TexelSize.xy;

                // sample neighbors
                float c00 = lum(tex2D(_MainTex, uv).rgb);
                float cL  = lum(tex2D(_MainTex, uv + float2(-t.x, 0)).rgb);
                float cR  = lum(tex2D(_MainTex, uv + float2( t.x, 0)).rgb);
                float cU  = lum(tex2D(_MainTex, uv + float2(0,  t.y)).rgb);
                float cD  = lum(tex2D(_MainTex, uv + float2(0, -t.y)).rgb);

                // Sobel-like (simple)
                float gx = cR - cL;
                float gy = cU - cD;
                float g = sqrt(gx*gx + gy*gy);

                float edge = smoothstep(_Threshold, _Threshold * 0.5, g) * _Intensity;
                edge = saturate(edge);

                // output edge color (white) on transparent background to overlay
                float4 outC = float4(_EdgeColor.rgb * edge, edge);
                return outC;
            }
            ENDCG
        }
    }
}