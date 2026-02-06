Shader "Custom/proximity"
{
    Properties
    {
        _MainTex ("MainTex (RGB)", 2D) = "white" {}
        _Location ("Location", vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            // Upgrade NOTE: excluded shader from DX11; has structs without semantics (struct v2f members worldPosition)
            // #pragma exclude_renderers d3d11
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 posWrld : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _position;
            float4 _Location;


            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.posWrld = mul(unity_ObjectToWorld, v.vertex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // float dist = distance(i.posWrld, _Location);
                float dist = sqrt((i.posWrld.x-_Location.x) * (i.posWrld.x-_Location.x) + (i.posWrld.y-_Location.y) * (i.posWrld.y-_Location.y) + (i.posWrld.z -_Location.z) * (i.posWrld.z -_Location.z));
                // return i.posWrld / 10000;
                if (dist > 2){
                    return float4(0, 0, 1, 1);
                    }
                else if (dist > 1.5){
                    return float4(0, 1, 1, 1);
                    }
                else if (dist > 1.0){
                    return float4(1, 1, 0, 1);
                    }
                else if (dist > 0.2){
                    return float4(1, 0, 0, 1);
                    }
                else if (dist > 0.1){
                    return float4(0, 0, 0, 1);
                    }
                else{
                    return float4(1, 1, 1, 1);
                    }
                // float dist = distance(_position, _location);
                // float theta = asin(vec.y / dist);
                // dist = dist * cos(theta);
                // if (dist <= 1.5)
                // {
                //     return float4(1, dist, 0, 1);
                // }
                // else
                // {
                //     return float4(0.9, 0.9, 0.9, 1);
                // }
                // sample the texture
                // fixed4 col = tex2D(_MainTex, i.uv);
                // // apply fog
                // UNITY_APPLY_FOG(i.fogCoord, col);
                // return col;
            }
            ENDCG
        }
    }
}
