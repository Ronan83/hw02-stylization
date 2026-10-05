Shader "Hidden/Normal Copy"
{
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 viewNormal : NORMAL;
                float eyeDepth : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.viewNormal = COMPUTE_VIEW_NORMAL;
                // store linear eye depth so the outline pass can tell when another object is in front
                o.eyeDepth = -UnityObjectToViewPos(v.vertex).z;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return float4(normalize(i.viewNormal) * 0.5 + 0.5, saturate(i.eyeDepth / 100.0));
            }
            ENDCG
        }
    }
}