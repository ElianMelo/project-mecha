Shader "Mecha/Zero Water"
{
    Properties
    {
        _BaseMap("Density source", 2D) = "white" {}
        _Color("Deep water", Color) = (0.025,0.25,0.5,1)
        _EdgeColor("Foam highlights", Color) = (0.35,0.85,1,1)
        _Fresnel("Shell Fresnel", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST, _Color, _EdgeColor;
            float _Fresnel;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD1; float3 normalWS:TEXCOORD2; float4 color:COLOR; float2 uv:TEXCOORD0; };
            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionWS=TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(i.normalOS);
                o.color=i.color; o.uv=TRANSFORM_TEX(i.uv,_BaseMap); return o;
            }
            half4 frag(Varyings i):SV_Target
            {
                half density=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).r;
                half rim=pow(1-saturate(abs(dot(normalize(i.normalWS),normalize(GetWorldSpaceViewDir(i.positionWS))))),2.5);
                half value=lerp(density,rim,_Fresnel);
                half band=smoothstep(.3,.4,value)*.45+smoothstep(.65,.72,value)*.55;
                half3 color=lerp(_Color.rgb,_EdgeColor.rgb,band)*i.color.rgb;
                half alpha=lerp(density,saturate(.12+rim*.9),_Fresnel)*i.color.a*_Color.a;
                return half4(color,alpha);
            }
            ENDHLSL
        }
    }
}
