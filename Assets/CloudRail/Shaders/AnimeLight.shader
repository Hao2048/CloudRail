Shader "CloudRail/Anime Light"
{
    Properties
    {
        _BaseColor("Paint", Color)=(1,1,1,1)
        _ShadeTint("Blue-violet shadow",Color)=(.58,.7,.96,1)
        _EmissionColor("Glow",Color)=(0,0,0,0)
        _BaseMap("Base",2D)="white"{}
        _Cutoff("Cutoff",Range(0,1))=.5
        _Cull("Cull",Float)=2
        _Glass("Illustrated glass",Range(0,1))=0
        _Metallic("Legacy metal",Range(0,1))=0
        _Smoothness("Legacy smoothness",Range(0,1))=.25
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "AnimeForward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor, _ShadeTint, _EmissionColor;
                float4 _BaseMap_ST;
                float _Cutoff, _Cull, _Glass, _Metallic, _Smoothness;
            CBUFFER_END
            struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
            struct V {float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float3 positionWS:TEXCOORD1;float fog:TEXCOORD2;float3 positionOS:TEXCOORD3;};
            V vert(A i){V o;VertexPositionInputs p=GetVertexPositionInputs(i.positionOS.xyz);o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.fog=ComputeFogFactor(p.positionCS.z);o.positionOS=i.positionOS.xyz;return o;}
            half4 frag(V i):SV_Target
            {
                half3 n=normalize(i.normalWS);Light sun=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half band=smoothstep(.04,.24,dot(n,sun.direction));
                half lit=min(band,lerp(.12,1,sun.shadowAttenuation));
                half3 shade=lerp(_ShadeTint.rgb,half3(1.025,1.01,.98),lit);
                half3 color=_BaseColor.rgb*shade;
                half rim=pow(1-saturate(dot(n,GetWorldSpaceNormalizeViewDir(i.positionWS))),4)*saturate(dot(n,sun.direction))*.035;
                half streak=pow(saturate(sin((i.positionOS.z+i.positionOS.y*.6)*6)),32);
                color+=_Glass*(half3(.42,.72,.85)*pow(1-saturate(dot(n,GetWorldSpaceNormalizeViewDir(i.positionWS))),3)*.18+streak*.15);
                color+=rim+_EmissionColor.rgb*.3;
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
