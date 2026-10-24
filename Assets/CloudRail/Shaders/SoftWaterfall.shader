Shader "CloudRail/Soft Waterfall"
{
 SubShader
 {
 Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
 Pass
 {
 Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A{float4 position:POSITION;float2 uv:TEXCOORD0;};struct V{float4 position:SV_POSITION;float2 uv:TEXCOORD0;};
 V vert(A i){V o;o.position=TransformObjectToHClip(i.position.xyz);o.uv=i.uv;return o;}
 half4 frag(V i):SV_Target{float streak=.5+.5*sin(i.uv.x*43+sin(i.uv.y*27-_Time.y*5));float edge=pow(saturate(sin(i.uv.x*3.14159)),.7);float fade=1-smoothstep(.65,1,i.uv.y);return half4(lerp(float3(.36,.71,.83),float3(.85,.94,1),streak*.7),edge*fade*(.25+streak*.32));}
 ENDHLSL
 }
 }
}
