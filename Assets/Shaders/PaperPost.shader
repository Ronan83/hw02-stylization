// Full-screen post process: warm colour grade + paper grain texture + coloured vignette,
// so the render looks like it was painted onto a sheet of paper.
Shader "Hidden/Paper Post"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _Tint ("Paper Tint", Color) = (1.0, 0.95, 0.86, 1)
        _TintStrength ("Tint Strength", Range(0, 1)) = 0.35
        _Saturation ("Saturation", Range(0, 2)) = 0.9
        _GrainStrength ("Paper Grain Strength", Range(0, 1)) = 0.25
        _GrainScale ("Paper Grain Scale (px)", Float) = 3
        _VignetteColor ("Vignette Color", Color) = (0.35, 0.12, 0.16, 1)
        _VignetteStrength ("Vignette Strength", Range(0, 1)) = 0.6
        _VignetteRadius ("Vignette Radius", Range(0, 1.5)) = 0.55
        _VignetteSoftness ("Vignette Softness", Range(0.01, 1)) = 0.45
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "PaperPost"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            // set globally by StyleSwitcher.cs: 0 = paper mode, 1 = "night print" halftone mode
            float _StyleMode;

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Tint;
                float _TintStrength;
                float _Saturation;
                float _GrainStrength;
                float _GrainScale;
                float4 _VignetteColor;
                float _VignetteStrength;
                float _VignetteRadius;
                float _VignetteSoftness;
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

            float hash21(float2 p)
            {
                p = frac(p * float2(233.34, 851.73));
                p += dot(p, p + 23.45);
                return frac(p.x * p.y);
            }
            float valueNoise(float2 p)
            {
                float2 i = floor(p); float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash21(i), hash21(i + float2(1, 0)), u.x),
                            lerp(hash21(i + float2(0, 1)), hash21(i + float2(1, 1)), u.x), u.y);
            }
            // fractal brownian motion: several octaves of noise = paper fibres
            float fbm(float2 p)
            {
                float v = 0.0, a = 0.5;
                for (int k = 0; k < 4; k++) { v += a * valueNoise(p); p *= 2.03; a *= 0.5; }
                return v;
            }

            float4 frag (Varyings i) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                if (_StyleMode > 0.5)
                {
                    // ---- Night print mode: CMYK-style halftone dots on dark blue paper ----
                    float lum = dot(col.rgb, float3(0.299, 0.587, 0.114));
                    float2 p = i.uv * _ScreenParams.xy;
                    float s45 = 0.70710678;
                    float2 rp = float2(p.x * s45 - p.y * s45, p.x * s45 + p.y * s45) / 9.0;   // rotated 45 degree grid
                    float dist = length(frac(rp) - 0.5);
                    float radius = sqrt(saturate(1.0 - lum)) * 0.62;                       // darker = bigger dot
                    float dots = 1.0 - smoothstep(radius - 0.06, radius + 0.06, dist);
                    float3 paperNight = saturate(col.rgb * float3(0.75, 0.95, 1.4) + float3(0.04, 0.06, 0.16));
                    float3 ink = float3(0.03, 0.03, 0.10);
                    float3 night = lerp(paperNight, ink, dots * 0.7);
                    float2 dn = (i.uv - 0.5) * float2(_ScreenParams.x / _ScreenParams.y, 1.0);
                    night *= 1.0 - smoothstep(0.4, 1.0, length(dn)) * 0.5;
                    return float4(night, col.a);
                }

                // colour grade: pull saturation a bit, multiply toward a warm paper tint
                float luma = dot(col.rgb, float3(0.299, 0.587, 0.114));
                col.rgb = lerp(luma.xxx, col.rgb, _Saturation);
                col.rgb = lerp(col.rgb, col.rgb * _Tint.rgb, _TintStrength);

                // paper grain: static fbm in pixel space + faint horizontal fibres
                float2 pix = i.uv * _ScreenParams.xy / max(_GrainScale, 0.001);
                float grain = fbm(pix * 0.15) * 0.7 + valueNoise(float2(pix.x * 0.02, pix.y * 0.6)) * 0.3;
                col.rgb *= lerp(1.0, 0.8 + 0.4 * grain, _GrainStrength);

                // vignette (aspect-corrected), tinted instead of plain black
                float2 d = (i.uv - 0.5) * float2(_ScreenParams.x / _ScreenParams.y, 1.0);
                float v = smoothstep(_VignetteRadius, _VignetteRadius + _VignetteSoftness, length(d));
                col.rgb = lerp(col.rgb, col.rgb * _VignetteColor.rgb, v * _VignetteStrength);
                return col;
            }
            ENDHLSL
        }
    }
}
