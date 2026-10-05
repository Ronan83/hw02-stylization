// Post-process outline: Roberts Cross edge detection on the depth and normal buffers.
// Depth (silhouette) edges wobble with stepped, animated noise for a hand-drawn look;
// normal (interior crease) edges stay still so the drawing keeps some structure.
Shader "Hidden/Stylized Outline"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _NormalsBuffer ("Normals Buffer", 2D) = "black" {}
        _OutlineColor ("Outline Color", Color) = (0.22, 0.07, 0.09, 1)
        _Thickness ("Thickness (px)", Range(0.5, 5)) = 1.5
        _DepthThreshold ("Depth Threshold", Range(0.01, 1)) = 0.12
        _NormalThreshold ("Normal Threshold", Range(0.05, 2)) = 0.5
        _WobbleAmount ("Wobble Amount (px)", Range(0, 8)) = 2.5
        _WobbleScale ("Wobble Noise Scale", Float) = 18
        _WobbleFPS ("Wobble FPS", Float) = 6
        [Toggle] _EdgesOnly ("Debug: Show Edges Only", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "Outline"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalsBuffer);  SAMPLER(sampler_point_clamp);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _OutlineColor;
                float _Thickness;
                float _DepthThreshold;
                float _NormalThreshold;
                float _WobbleAmount;
                float _WobbleScale;
                float _WobbleFPS;
                float _EdgesOnly;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            // ---- toolbox: hash + smooth value noise ----
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }
            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);          // smoothstep curve
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float EyeDepth(float2 uv)
            {
                return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
            }
            float3 ViewNormal(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_NormalsBuffer, sampler_point_clamp, uv).rgb * 2.0 - 1.0;
            }

            // Roberts Cross on linear depth, relative to the centre depth so far edges aren't over-detected
            float DepthEdge(float2 uv, float2 px)
            {
                float d00 = EyeDepth(uv + px * float2(-1, -1));
                float d11 = EyeDepth(uv + px * float2( 1,  1));
                float d10 = EyeDepth(uv + px * float2( 1, -1));
                float d01 = EyeDepth(uv + px * float2(-1,  1));
                float g = sqrt((d11 - d00) * (d11 - d00) + (d01 - d10) * (d01 - d10));
                float centre = min(min(d00, d11), min(d10, d01));
                return smoothstep(_DepthThreshold, _DepthThreshold * 1.5, g / max(centre, 1e-3));
            }

            // Roberts Cross on view-space normals
            float NormalEdge(float2 uv, float2 px)
            {
                float3 n00 = ViewNormal(uv + px * float2(-1, -1));
                float3 n11 = ViewNormal(uv + px * float2( 1,  1));
                float3 n10 = ViewNormal(uv + px * float2( 1, -1));
                float3 n01 = ViewNormal(uv + px * float2(-1,  1));
                float g = sqrt(dot(n11 - n00, n11 - n00) + dot(n01 - n10, n01 - n10));
                return smoothstep(_NormalThreshold, _NormalThreshold * 1.3, g);
            }

            float4 frag (Varyings i) : SV_Target
            {
                float2 texel = 1.0 / _ScreenParams.xy;
                float2 px = texel * _Thickness;

                // stepped time -> the wobble "boils" a few times a second like hand-drawn animation
                float t = floor(_Time.y * _WobbleFPS);
                float2 noiseUV = i.uv * _WobbleScale + float2(t * 7.31, t * 3.17);
                float2 wobble = float2(valueNoise(noiseUV), valueNoise(noiseUV + 17.7)) * 2.0 - 1.0;
                float2 wobbledUV = i.uv + wobble * _WobbleAmount * texel;

                float depthEdge  = DepthEdge(wobbledUV, px);   // animated silhouette lines
                float normalEdge = NormalEdge(i.uv, px);       // static interior lines

                // objects on the "No Normal" layer (e.g. the vertex-animated hero) are not in the normal
                // buffer; hide normal edges from surfaces that such an object is covering.
                float normalBufferDepth = SAMPLE_TEXTURE2D(_NormalsBuffer, sampler_point_clamp, i.uv).a * 100.0;
                if (EyeDepth(i.uv) < normalBufferDepth - 1.0) normalEdge = 0;

                // break the animated lines up a little so they look sketched, not perfect
                float pressure = lerp(0.65, 1.0, valueNoise(i.uv * _WobbleScale * 3.0 + t));
                float edge = saturate(max(depthEdge * pressure, normalEdge));

                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                if (_EdgesOnly > 0.5) return float4((1 - edge).xxx, 1);
                col.rgb = lerp(col.rgb, _OutlineColor.rgb, edge * _OutlineColor.a);
                return col;
            }
            ENDHLSL
        }
    }
}
