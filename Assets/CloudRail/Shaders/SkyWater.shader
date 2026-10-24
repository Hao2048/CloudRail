Shader "CloudRail/Falling Water"
{
    Properties { _BaseColor ("Water", Color) = (.3,.75,.85,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };
            half4 _BaseColor;
            V vert(A v) { V o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.positionWS=TransformObjectToWorld(v.positionOS.xyz); return o; }
            half4 frag(V i) : SV_Target
            {
                float streak = pow(saturate(sin(i.positionWS.y * 1.8 + _Time.y * 6 + sin(i.positionWS.x * 16 + i.positionWS.z * 13))), 10);
                float fine = .5 + .5 * sin(i.positionWS.x * 35 + i.positionWS.z * 24);
                return half4(_BaseColor.rgb + streak * .2 + fine * .055, 1);
            }
            ENDHLSL
        }
    }
}
