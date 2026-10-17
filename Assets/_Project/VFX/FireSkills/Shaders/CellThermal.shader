Shader "Mecha/VFX/CellThermal"
{
    Properties
    {
        _BaseMap("Fire cells or smoke mask", 2D) = "white" {}
        _NoiseMap("Erosion noise", 2D) = "gray" {}
        _HeatMap("Combustion detail (R heat / G variation)", 2D) = "gray" {}
        _EmissionGain("Thermal brightness", Range(0,4)) = 1
        _DensityBoost("Core density",Range(0.5,4))=2
        _OpacityPower("Center / edge profile",Range(0.3,2))=0.7
        _Cutoff("Edge threshold", Range(0,1)) = 0.12
        _Feather("Edge softness", Range(0.001,0.2)) = 0.035
        _NoiseStrength("Noise erosion", Range(0,0.5)) = 0.15
        _FlowStrength("Mask turbulence (single maps)", Range(0,0.1)) = 0
        _DensityDetail("Density breakup", Range(0,1)) = 0.3
        _ShadowLevel("Cel body shade", Range(0,1)) = 0.55
        _CelBlend("Anime / textured shading blend", Range(0,1)) = 0.65
        _CoreThreshold("Hot center", Range(0,1)) = 0.7
        _CoreStrength("Hot center strength", Range(0,2)) = 0.4
        _CoreColor("Hot center color", Color) = (1.8,1.1,0.35,1)
        _FadeErosion("Tail erosion", Range(0,1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
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
            struct Attributes { float4 positionOS:POSITION; float3 uv:TEXCOORD0; half4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float seed:TEXCOORD1; half4 color:COLOR; UNITY_VERTEX_OUTPUT_STEREO };
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NoiseMap); SAMPLER(sampler_NoiseMap);
            TEXTURE2D(_HeatMap); SAMPLER(sampler_HeatMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _CoreColor;
                half _Cutoff, _Feather, _NoiseStrength, _CoreThreshold, _CoreStrength, _FadeErosion;
                half _FlowStrength, _DensityDetail, _ShadowLevel, _EmissionGain, _DensityBoost, _OpacityPower, _CelBlend;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.uv=TRANSFORM_TEX(input.uv.xy,_BaseMap);
                output.seed=input.uv.z;
                output.color=input.color;
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float2 flowUV=input.uv*2.3+input.seed*float2(11.3,7.7)+float2(_Time.y*0.8,-_Time.y*1.1);
                half noise=SAMPLE_TEXTURE2D(_NoiseMap,sampler_NoiseMap,flowUV).r;
                half crossNoise=SAMPLE_TEXTURE2D(_NoiseMap,sampler_NoiseMap,flowUV.yx*1.73-float2(_Time.y*0.6,0)).r;
                half4 sample=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv+(half2(noise,crossNoise)-0.5)*_FlowStrength);
                half mask=sample.r*sample.a*lerp(1,0.35+0.65*crossNoise,_DensityDetail);
                half edge=_Cutoff+(1-input.color.a)*_FadeErosion+(noise-0.5)*_NoiseStrength;
                // Retain the source's graded density instead of turning every mask into a cutout.
                half density=saturate((mask-edge*0.35)*_DensityBoost);
                half alpha=pow(density,_OpacityPower)*smoothstep(0,_Feather,mask)*input.color.a;
                half2 heatDetail=SAMPLE_TEXTURE2D(_HeatMap,sampler_HeatMap,input.uv*1.8+input.seed*float2(5.7,13.1)+float2(-_Time.y*0.45,_Time.y*0.8)).rg;
                half temperature=saturate(mask*0.80+heatDetail.r*0.28+heatDetail.g*0.12);
                half heatRamp=smoothstep(_CoreThreshold-0.12,_CoreThreshold+0.22,temperature);
                half textureShade=saturate(mask*1.7+crossNoise*0.3);
                half shade=lerp(_ShadowLevel,1,textureShade);
                // Broad amber / gold / hot bands retain some fine density detail.
                half celShade=lerp(_ShadowLevel,0.78,step(0.28,textureShade));
                celShade=lerp(celShade,1,step(0.66,textureShade));
                shade=lerp(shade,celShade,_CelBlend);
                half hot=lerp(heatRamp,step(_CoreThreshold+0.10,temperature),_CelBlend)*_CoreStrength;
                return half4((input.color.rgb*shade+_CoreColor.rgb*hot)*_EmissionGain,alpha);
            }
            ENDHLSL
        }
    }
}
