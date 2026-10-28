// Cel water for mesh/billboard/trail particles.
// Gap vs installed shaders: PixPlays Ubershader lerps only two colors and has no colored dissolve
// edge or back-face tone; CFXR's gradient map is disabled and its dissolve edge is uncolored.
// Technique (Unity Shaders Bible 4.1.5/3.2.1/7.0.6): step/smoothstep tone bands, a foam band at the
// erosion threshold, SV_IsFrontFace back shading and a Fresnel rim.
// Particle contract: vertex streams Position, Normal, Color, UV (xy), Custom1.xy (zw):
// Custom1.x = erosion 0..1 over lifetime, Custom1.y = per-particle random seed.
Shader "Mecha/VFX/Anime Water"
{
    Properties
    {
        [HDR] _DeepColor("Deep", Color) = (0.03, 0.10, 0.45, 1)
        [HDR] _BodyColor("Body", Color) = (0.05, 0.42, 0.95, 1)
        [HDR] _LightColor("Light", Color) = (0.35, 0.95, 1.0, 1)
        [HDR] _FoamColor("Foam", Color) = (1.5, 1.7, 1.8, 1)
        _ToneAlpha("Tone alpha (deep, body, light, foam)", Vector) = (0.55, 0.9, 0.96, 1)
        _Bands("Bands (deep edge, light edge, caustic mix, fresnel)", Vector) = (0.3, 0.72, 0.5, 0.45)
        _CausticTex("Caustic shapes", 2D) = "gray" {}
        _CausticFlow("Caustic tile XY, scroll ZW", Vector) = (1.5, 1, 0, -1.1)
        _FoamTex("Foam lines", 2D) = "black" {}
        _FoamFlow("Foam tile XY, scroll ZW", Vector) = (2, 1.2, 0.05, -1.6)
        _FoamLines("Foam lines (tone start, amount)", Vector) = (0.62, 1, 0, 0)
        _ErodeTex("Erosion field", 2D) = "gray" {}
        _ErodeFlow("Erosion tile XY, scroll ZW", Vector) = (2.5, 1.5, 0, -0.5)
        _ShapeTex("Shape field (billboards)", 2D) = "white" {}
        _UseShape("Use shape field", Float) = 0
        _Distort("Distortion", Range(0, 0.3)) = 0.08
        _ErodeTop("Erode from top", Range(0, 1)) = 0.45
        _Erode("Base erosion", Range(0, 1)) = 0
        _Recover("Recovery erosion", Range(0, 1)) = 0
        _EdgeFoam("Erosion edge foam", Range(0, 0.4)) = 0.09
        _TopFoam("Top foam rim", Range(0, 0.6)) = 0.12
        _TopTear("Top tear depth", Range(0, 1)) = 0.3
        _TearTile("Tear tile XY (tall cells = pointed tongues)", Vector) = (0.5, 0.1, 0, 0)
        _SideCut("Open-sheet side cut", Range(0, 0.4)) = 0
        _BottomFade("Bottom fade", Range(0, 0.6)) = 0.04
        _BackShade("Back face shade", Range(0, 1)) = 0.55
        _Soft("Edge softness", Range(0.001, 0.1)) = 0.015
        _Wobble("Wobble (amp, freq, speed)", Vector) = (0.03, 3, 5, 0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Pass
        {
            Name "AnimeWater"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_CausticTex); SAMPLER(sampler_CausticTex);
            TEXTURE2D(_FoamTex); SAMPLER(sampler_FoamTex);
            TEXTURE2D(_ErodeTex); SAMPLER(sampler_ErodeTex);
            TEXTURE2D(_ShapeTex); SAMPLER(sampler_ShapeTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor, _BodyColor, _LightColor, _FoamColor;
                float4 _TearTile, _ToneAlpha, _Bands, _CausticFlow, _FoamFlow, _FoamLines, _ErodeFlow, _Wobble;
                float4 _CausticTex_ST, _FoamTex_ST, _ErodeTex_ST, _ShapeTex_ST;
                float _UseShape, _Distort, _ErodeTop, _Erode, _Recover, _EdgeFoam, _TopFoam, _TopTear, _SideCut, _BottomFade, _BackShade, _Soft;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                float4 uv : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uv : TEXCOORD0;
                half4 color : COLOR;
                float3 normalWS : TEXCOORD1;
                float3 viewWS : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                float3 p = input.positionOS.xyz;
                // Living surface: a slow travelling swell along the normal, stronger toward the top.
                p += input.normalOS * sin(_Time.y * _Wobble.z + p.y * _Wobble.y + input.uv.w * 6.2832) * _Wobble.x * saturate(input.uv.y);
                float3 ws = TransformObjectToWorld(p);
                o.positionCS = TransformWorldToHClip(ws);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.viewWS = GetWorldSpaceViewDir(ws);
                o.uv = input.uv;
                o.color = input.color;
                return o;
            }

            half4 Frag(Varyings i, bool front : SV_IsFrontFace) : SV_Target
            {
                float t = _Time.y;
                float2 uv = i.uv.xy;
                float seed = i.uv.w;
                float erosion = saturate(i.uv.z + _Erode + _Recover);
                float s = _Soft;

                float n = SAMPLE_TEXTURE2D(_ErodeTex, sampler_ErodeTex, uv * _ErodeFlow.xy + _ErodeFlow.zw * t + seed * float2(0.37, 0.71)).r;
                float2 d = (n - 0.5) * _Distort;
                float c = SAMPLE_TEXTURE2D(_CausticTex, sampler_CausticTex, uv * _CausticFlow.xy + _CausticFlow.zw * t + d + seed * float2(0.61, 0.23)).r;
                float fl = SAMPLE_TEXTURE2D(_FoamTex, sampler_FoamTex, uv * _FoamFlow.xy + _FoamFlow.zw * t + d * 1.6 + seed * float2(0.13, 0.47)).r;

                float nl = dot(i.normalWS, i.normalWS);
                float ndv = nl > 1e-5 ? abs(dot(i.normalWS * rsqrt(nl), normalize(i.viewWS))) : 1;
                float fres = pow(1 - ndv, 2);

                // Erosion: a threshold over the field; a foam band rides just inside the cut.
                float field = lerp(n, 1 - uv.y, _ErodeTop);
                float w = _EdgeFoam;
                float cut = erosion * (1 + w) - w;
                float alive = smoothstep(cut - s, cut + s, field);
                float edge = alive * (1 - smoothstep(cut + w - s, cut + w + s, field)) * step(0.001, erosion);

                if (_UseShape > 0.5)
                {
                    half4 sh = SAMPLE_TEXTURE2D(_ShapeTex, sampler_ShapeTex, uv);
                    float shape = min(dot(sh.rgb, 1.0 / 3.0), sh.a);
                    alive = smoothstep(0.42 - s, 0.42 + s, shape - erosion * 0.42);
                    edge = 0;
                }
                if (_SideCut > 0)
                {
                    float side = min(uv.x, 1 - uv.x) + (n - 0.5) * _SideCut;
                    alive *= smoothstep(_SideCut - s, _SideCut + s, side + _SideCut * 0.5);
                }

                // Torn top: erosion cells scallop the upper edge instead of a flat cut rim.
                // Low-frequency, vertically stretched sample: the tear forms a few pointed tongues, not speckle.
                float nt = SAMPLE_TEXTURE2D(_ErodeTex, sampler_ErodeTex, uv * _TearTile.xy + _ErodeFlow.zw * t * 0.5 + seed * float2(0.19, 0.53)).r;
                float tearField = (1 - uv.y) - nt * _TopTear + 0.02;
                float tearAlive = _TopTear > 0 ? smoothstep(-s, s, tearField) : 1;
                alive *= tearAlive;

                // Cel tone bands: deep / body / light.
                float tone = lerp(uv.y * 0.6 + 0.2, c, _Bands.z) + fres * _Bands.w;
                float bDeep = smoothstep(_Bands.x - s, _Bands.x + s, tone);
                float bLight = smoothstep(_Bands.y - s, _Bands.y + s, tone);
                half3 col = lerp(_DeepColor.rgb, _BodyColor.rgb, bDeep);
                col = lerp(col, _LightColor.rgb, bLight);
                half a = lerp(_ToneAlpha.x, _ToneAlpha.y, bDeep);
                a = lerp(a, _ToneAlpha.z, bLight);

                // Foam: selected rims, erosion edges and flowing lines on the lit regions only.
                float top = _TopTear > 0
                    ? tearAlive * (1 - smoothstep(_TopFoam - s, _TopFoam + s, tearField))
                    : (_TopFoam > 0 ? smoothstep(1 - _TopFoam - s, 1 - _TopFoam + s, uv.y + (n - 0.5) * 0.25) : 0);
                float lines = step(0.5, fl) * smoothstep(_FoamLines.x - s, _FoamLines.x + s, tone) * _FoamLines.y;
                float foam = saturate(edge + top + lines);
                col = lerp(col, _FoamColor.rgb, foam);
                a = lerp(a, _ToneAlpha.w, foam);
                if (!front) col = lerp(col, _DeepColor.rgb, _BackShade * (1 - foam));

                a *= alive * i.color.a;
                if (_BottomFade > 0) a *= smoothstep(0, _BottomFade, uv.y);
                col *= i.color.rgb;
                clip(a - 0.004);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
