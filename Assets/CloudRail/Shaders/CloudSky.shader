Shader "CloudRail/Pastel Sky"
{
    Properties
    {
        _Top ("Zenith", Color) = (.27,.53,.75,1)
        _Horizon ("Horizon", Color) = (.82,.9,.93,1)
        _Bottom ("Below", Color) = (.69,.79,.85,1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; float3 direction : TEXCOORD0; };
            half4 _Top, _Horizon, _Bottom;
            V vert(A v) { V o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.direction = v.positionOS.xyz; return o; }
            half4 frag(V i) : SV_Target
            {
                float3 d = normalize(i.direction);
                half3 col = lerp(_Horizon.rgb, _Top.rgb, pow(saturate(d.y), .65));
                col = lerp(_Top.rgb, _Bottom.rgb, smoothstep(-.05, .95, -d.y));
                float glow = pow(saturate(dot(d, normalize(float3(-.5,.3,-.5)))), 16);
                col += half3(.12,.09,.035) * glow;
                return half4(col,1);
            }
            ENDHLSL
        }
    }
}
