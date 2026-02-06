Shader "Custom/ExtractDepth"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // This is the magic variable where Unity stores the screen's depth
            sampler2D _CameraDepthTexture; 

            v2f vert(appdata_img v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                // o.uv = v.texcoord;
                // THE FIX: Flip the Y coordinate (1.0 - y)
                o.uv = float2(v.texcoord.x, 1.0 - v.texcoord.y);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Read the depth from the screen buffer
                float d = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                
                // Convert non-linear depth to Linear 0-1 (Near to Far)
                float linearD = Linear01Depth(d);
                
                // Return Grayscale
                return fixed4(linearD, linearD, linearD, 1);
            }
            ENDCG
        }
    }
}