Shader "Mecha/VFX/Rising water crown"
{
    Properties
    {
        [HDR] _Color("Layer color", Color) = (0.02, 0.3, 0.8, 1)
        [HDR] _FlowColor("Flow bands", Color) = (0.03, 1.05, 1.65, 1)
        [HDR] _FoamColor("Broken foam", Color) = (1.35, 2.25, 2.6, 1)
        _FlowMap("Gabriel graphic flow", 2D) = "white" {}
        _NoiseMap("Gabriel contour breakup", 2D) = "gray" {}
        _Coverage("Sheet coverage", Range(0, 1)) = 1
        _Layer("Body / flow / foam / droplet", Float) = 0
        _FlowSpeed("Upward flow speed", Float) = 0.8
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "Water"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_FlowMap); SAMPLER(sampler_FlowMap);
            TEXTURE2D(_NoiseMap); SAMPLER(sampler_NoiseMap);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color, _FlowColor, _FoamColor;
                float _Coverage, _Layer, _FlowSpeed;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                if (_Layer > 2.5) return half4(_Color.rgb, 1);
                float2 flowUV = uv * float2(1.45, 1.1) - float2(0, _Time.y * _FlowSpeed);
                half flow = SAMPLE_TEXTURE2D(_FlowMap, sampler_FlowMap, flowUV).r;
                half breakup = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, uv * float2(2.3, 1.7)).r;
                // Reveal and retract real source meshes; the maps supply the contour detail.
                clip(_Coverage - uv.y * .82 - (breakup - .5) * .12 - .025);
                clip(uv.x - .015 - breakup * .075 * uv.y);
                clip(1 - uv.x - .015 - (1 - breakup) * .085 * uv.y);
                clip(1.015 - uv.y - breakup * .10);
                if (_Layer < .5)
                {
                    half3 body = _Color.rgb * lerp(.68, 1.15, step(.52, breakup));
                    half3 water = lerp(body, _FlowColor.rgb, step(.58, flow) * .88);
                    half foam = step(.84 - breakup * .08, uv.y) * step(.58, flow);
                    return half4(lerp(water, _FoamColor.rgb, foam), .97);
                }
                if (_Layer < 1.5)
                {
                    clip(flow - .52);
                    return half4(_Color.rgb, .88);
                }
                clip(uv.y - (.68 - breakup * .15));
                clip(flow - .58);
                return half4(_Color.rgb, .96);
            }
            ENDHLSL
        }
    }
}
