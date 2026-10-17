Shader "Mecha/VFX/TerrainHeatDistortion"
{
    Properties
    {
        _BaseMap("Surface mask",2D)="white"{}
        _NoiseMap("Heat flow RG",2D)="gray"{}
        _Strength("Screen displacement",Range(0,0.02))=0.0035
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent"}
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NoiseMap);SAMPLER(sampler_NoiseMap);
            CBUFFER_START(UnityPerMaterial)
                float _Strength;
            CBUFFER_END
            Varyings Vert(Attributes v)
            {Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=v.uv;o.color=v.color;return o;}
            half4 Frag(Varyings i):SV_Target
            {
                half mask=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).r;
                half2 flow=SAMPLE_TEXTURE2D(_NoiseMap,sampler_NoiseMap,i.uv*2+float2(_Time.y*0.65,-_Time.y)).rg;
                float2 uv=GetNormalizedScreenSpaceUV(i.positionCS);
                float2 offset=(flow-0.5)*_Strength*mask*i.color.a;
                return half4(SampleSceneColor(saturate(uv+offset)),mask*i.color.a);
            }
            ENDHLSL
        }
    }
}
