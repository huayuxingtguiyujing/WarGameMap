Shader "WarGameMap/Terrain/ShowTex/HexGridShader"
{
    // this shader can fill texture surface with hex grid (but rect)
    Properties
    {
        _EdgeLineColor ("Edge Line Color", Color) = (1, 1, 1, 0.5)
        _GridScale("Hex Grid Scale", Float) = 2
        _GridSize("Hex Grid Size", Range(1, 30)) = 20
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

            int _HexGridSize;
            float _HexGridScale;
            float4 _BackgroundColor;

            sampler _HexGridTypeTexture;
            float4 _HexGridTypeTexture_ST;
            float4 _HexGridTypeTexture_TexelSize;


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
                o.uv = v.uv * _HexGridTypeTexture_ST.xy + _HexGridTypeTexture_ST.zw;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            // TODO : move to common class
            float3 PixelToHexCubeCoord(float3 worldPos){
                float q = (sqrt(3)/3 * worldPos.x  -  1./3 * worldPos.z) / _HexGridSize;
                float r = 2./3 * worldPos.z / _HexGridSize;
                float s = - q - r;

                int fix_q = round(q);
                int fix_r = round(r);
                int fix_s = round(s);

                float q_diff = abs(fix_q - q);
                float r_diff = abs(fix_r - r);
                float s_diff = abs(fix_s - s);

                int final_q = fix_q, final_r = fix_r, final_s = fix_s;
                if(q_diff > r_diff && q_diff > s_diff){
                    final_q = - fix_r - fix_s;
                }else if(r_diff > s_diff){
                    final_r = - fix_q - fix_s;
                }else{
                    final_s = - fix_q - fix_r;
                }
                return float3(final_q, final_r, final_s);
            }

            float3 CubeCoordToOffset(float3 cubePos){
                float col = cubePos.x + (cubePos.y - ((int)cubePos.y&1)) / 2;
                float row = cubePos.y;
                return float3(col, row, -col-row);
            }

            float3 OffsetCoordToCube(float3 offsetPos){
                float q = offsetPos.x - (offsetPos.y - ((int)offsetPos.y&1)) / 2;
                float r = offsetPos.y;
                return float3(q, r, -q-r);
            }


            half4 frag(v2f i) : SV_Target
            {
                fixed4 c = _BackgroundColor;

                float3 hex = PixelToHexCubeCoord(i.worldPos);
                hex = CubeCoordToOffset(hex);

                int x_idx = hex.x;
                int y_idx = hex.y;

                float2 hex_uv = float2(x_idx , y_idx ) * _HexGridTypeTexture_TexelSize.xy;

                half3 gridColor = tex2D(_HexGridTypeTexture, hex_uv);

                return half4(gridColor, c.a);
            }
            
            ENDCG
        }
    }

    FallBack "Diffuse"
}
