Shader "Custom/StarSkyDome"
{
    Properties
    {
        _Color ("Sky Color", Color) = (0.039, 0.039, 0.165, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" }
        // Render only the inside faces of the dome (inside-out sphere)
        Cull Front
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;

            struct appdata { float4 vertex : POSITION; };
            struct v2f     { float4 pos : SV_POSITION; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}
