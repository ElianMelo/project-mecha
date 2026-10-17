Shader "Mecha/VFX/CellThermal"
{
    Properties
    {
        _BaseMap("Fire cells or smoke mask", 2D) = "white" {}
        _NoiseMap("Erosion noise", 2D) = "gray" {}
        _Cutoff("Edge threshold", Range(0,1)) = 0.12
        _Feather("Edge softness", Range(0.001,0.2)) = 0.035
        _NoiseStrength("Noise erosion", Range(0,0.5)) = 0.15
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
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; UNITY_VERTEX_OUTPUT_STEREO };
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NoiseMap); SAMPLER(sampler_NoiseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _CoreColor;
                half _Cutoff, _Feather, _NoiseStrength, _CoreThreshold, _CoreStrength, _FadeErosion;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.uv=TRANSFORM_TEX(input.uv,_BaseMap);
                output.color=input.color;
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                half4 sample=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv);
                half noise=SAMPLE_TEXTURE2D(_NoiseMap,sampler_NoiseMap,input.uv*2.3+float2(_Time.y*0.8,-_Time.y*1.1)).r;
                half mask=sample.r*sample.a;
                half edge=_Cutoff+(1-input.color.a)*_FadeErosion+(noise-0.5)*_NoiseStrength;
                half alpha=smoothstep(edge-_Feather,edge+_Feather,mask)*input.color.a;
                half hot=smoothstep(_CoreThreshold,1,mask)*_CoreStrength;
                return half4(input.color.rgb+_CoreColor.rgb*hot,alpha);
            }
            ENDHLSL
        }
    }
}
