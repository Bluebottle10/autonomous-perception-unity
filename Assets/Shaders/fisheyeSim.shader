Shader "Custom/fisheyeSim"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }
        SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "FisheyeSupport.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f_fisheye
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
                float4 posLoc : TEXCOORD1;
                float4 posProj : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Distortion;
            float _HFOV;
            float _VFOV;

            v2f_fisheye vert(appdata_img v)
            {
                v2f_fisheye o;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = float4(v.texcoord.x, v.texcoord.y, 0, 0);
                o.posProj = mul(UNITY_MATRIX_MV, v.vertex);
                o.posLoc = v.vertex;

                return o;
            }

            fixed4 frag(v2f_fisheye input) : SV_Target
            {
                float2 uv = ComputeUV2(input.uv, _Distortion, float2(_HFOV, _VFOV));
                if (uv.x > 0 && uv.x < 1 && uv.y > 0 && uv.y < 1)
                    return tex2D(_MainTex, uv);
                else
                    return float4(0, 0, 0, 1);
            }

            ENDCG
        }
    }
}
