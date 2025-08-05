Shader "WarGameMap/Terrain/RiverShader"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}

        _WaveColor ("Wave Color", Color) = (1, 1, 1, 1)
        _WaterColor ("Water Color", Color) = (0, 0.96, 1, 1) // 0 245 255

        _ShallowColor ("Shallow Water Color", Color) = (0.2, 0.6, 0.8, 1)
        _DeepFactor ("Depth Fade Factor", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _ShallowColor;
            float4 _WaveColor;
            float4 _WaterColor;
            
            float _DeepFactor;

            sampler2D _CameraDepthTexture;


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float2 tangent     : TEXCOORD2;
            };

            struct v2f
            {
                float2 uv        : TEXCOORD0;
                float4 vertex    : SV_POSITION;
                float4 projPos   : TEXCOORD1;
                float2 tangent     : TEXCOORD2;
                UNITY_FOG_COORDS(2)
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.projPos = ComputeScreenPos(o.vertex);
                o.tangent = v.tangent;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // // 获取背景视空间深度
                // float rawDepth = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos));
                // // float rawDepth = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos));
                // // float buffViewZ = LinearEyeDepth(rawDepth, _ZBufferParams);
                // float buffViewZ = LinearEyeDepth(rawDepth);
                // // 当前像素的视空间深度
                // float fragViewZ = i.projPos.z;
                // // 深度差距决定颜色融合比例
                // float fade = saturate((buffViewZ - fragViewZ) * _DeepFactor);
                // // 采样主纹理颜色
                // fixed4 baseColor = tex2D(_MainTex, i.uv);
                // // 叠加浅水颜色
                // fixed3 shallowMix = lerp(_ShallowColor.rgb * (_ShallowColor.a * 2), 0, fade);
                // baseColor.rgb += shallowMix;
                // baseColor.a *= fade;
                // 雾效支持
                // UNITY_APPLY_FOG(i.fogCoord, baseColor);


                float offsetX = i.uv.x + _Time.y * 0.1f;    // i.tangent *
                float offsetY = i.uv.y;
                float2 flowUV = float2(offsetX, offsetY);
                fixed4 baseColor = tex2D(_MainTex, flowUV);

                // float water_lerpT = baseColor.r; // 取红通道作为灰度
                //     float t = sqrt(gray); // 或者用：1 - pow(1 - gray, 2)
                float water_lerpT = sqrt(baseColor.r);
                fixed4 waterColor =  lerp( _WaterColor,  _WaveColor, water_lerpT);   // 插值颜色

                return waterColor;
            }
            ENDCG
        }
    }
}
