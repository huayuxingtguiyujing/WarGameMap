Shader "WarGameMap/Terrain/ShowTex/HexGridShader"
{
    // this shader can fill texture surface with hex grid (but rect)
    Properties
    {
        _BackgroundColor ("Background Color", Color) = (1, 1, 1, 0.5)
        _EdgeLineColor ("Edge Line Color", Color) = (1, 1, 1, 0.5)
        _HexGridScale("Hex Grid Scale", Float) = 2
        _HexGridSize("Hex Grid Size", Range(1, 30)) = 20
        _HexGridEdgeRatio("Hex Grid Edge Ratio", Range(0, 1)) = 0.1     // HexOutline need, control the aphla of hex outline rect
        _HexGridEdgeStartLerp("Hex Grid Edge StartLerp", Range(0, 0.95)) = 0.9     // HexOutline need, control the ratio of hex outline rect

        _HexGridTexture("Hex Grid Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            #include "../Utils/HexLibrary.hlsl"
            #include "../Utils/HexOutline.hlsl"

            float4 _BackgroundColor;

            sampler _HexGridTexture;
            float4 _HexGridTexture_ST;
            float4 _HexGridTexture_TexelSize;


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * _HexGridTexture_ST.xy + _HexGridTexture_ST.zw;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            // TODO : 要有Hex边界才行...涂起来更好看...
            half4 frag(v2f i) : SV_Target
            {
                fixed4 c = _BackgroundColor;
                float3 hex = PixelToHexCubeCoord(i.worldPos, _HexGridSize);
                hex = CubeCoordToOffset(hex);

                int x_idx = hex.x;
                int y_idx = hex.y;

                float2 hex_uv = float2(x_idx , y_idx ) * _HexGridTexture_TexelSize.xy;

                half3 gridColor = tex2D(_HexGridTexture, hex_uv);
                // return half4(gridColor, c.a);
                float3 outlineColor = GetHexOutlineColor(i.worldPos, gridColor);
                return float4(outlineColor, 1); //  finalColor.a;
            }
            
            ENDCG
        }
    }

    FallBack "Diffuse"
}
