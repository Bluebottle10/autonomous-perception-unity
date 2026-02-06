// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Seg"
{
    Properties
    {
    }
   
    SubShader
    {
        Tags { "RenderType"="Opaque" }
 
        Pass
        {
            CGPROGRAM
            
            #pragma target 4.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma glsl
            #include "UnityCG.cginc"
  
            struct v2f
            {
                float4 pos : SV_POSITION;
				float4 color: COLOR;
            };

			float _DetailNormalMapScale;

            v2f vert(appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
				o.color = v.color;
				return o;
            }
 
            float4 frag(v2f input) : COLOR
            {
				if( _DetailNormalMapScale < 2 )
					_DetailNormalMapScale= 0;

            	// return float4(0, 0, 0, 1);

				if (_DetailNormalMapScale == 2)
					return float4(1, 0, 0, 1);
            	else if (_DetailNormalMapScale == 3)
            		return float4(0, 1, 0, 1);
            	else if (_DetailNormalMapScale == 4)
            		return float4(0, 0, 1, 1);
				else
					return float4(0, 0, 0, 1);
			}
 
            ENDCG
        }
    }
}