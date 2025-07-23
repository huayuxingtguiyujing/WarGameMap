Shader "WarGameMap/Terrain/ShowTex/TexGridShader"
{
    //  this shader can fill texture surface rect grid
    Properties
    {
        _MainTex   ("Main Texture", 2D) = "white" {}
        _GridNum ("Grid Num", Float) = 512
        _LineColor ("Line Color", Color) = (1, 1, 1, 0.5)
        _LineWidth("Line Width", Float) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            float     _GridNum;
            float     _LineWidth;
            float4    _LineColor;

            struct appdata
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.positionOS.xyz);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 squareUV = float2(i.uv.x, i.uv.y);

                float2 cell = squareUV * _GridNum;
                float2 distanceToCell = frac(cell);

                if (distanceToCell.x < _LineWidth || distanceToCell.y < _LineWidth ||
                    distanceToCell.x > 1.0 - _LineWidth || distanceToCell.y > 1.0 - _LineWidth)
                {
                    return _LineColor;
                }

                return tex2D(_MainTex, i.uv);
            }

            ENDCG
        }
    }
}
