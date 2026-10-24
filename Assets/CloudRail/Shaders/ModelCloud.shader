Shader "CloudRail/Soft Model Cloud"
{
 Properties{_BaseColor("Sunlit ivory",Color)=(.98,.98,.97,1) _Shade("Cool underside",Color)=(.59,.7,.84,1) _BaseMap("Base",2D)="white"{} _Cull("Cull",Float)=2 _Cutoff("Cutoff",Float)=.5}
 SubShader
 {
 Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
 Pass
 {
 Tags{"LightMode"="UniversalForward"}
 Cull Back
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 CBUFFER_START(UnityPerMaterial)
 half4 _BaseColor,_Shade;
 CBUFFER_END
 struct A{float4 p:POSITION;float3 n:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
 struct V{float4 p:SV_POSITION;float3 normal:TEXCOORD0;float3 world:TEXCOORD1;float height:TEXCOORD2;float fog:TEXCOORD3;};
 V vert(A i){UNITY_SETUP_INSTANCE_ID(i);V o;o.world=TransformObjectToWorld(i.p.xyz);o.p=TransformObjectToHClip(i.p.xyz);o.normal=TransformObjectToWorldNormal(i.n);o.height=saturate((i.p.y+2.7)/7.4);o.fog=ComputeFogFactor(o.p.z);return o;}
 half4 frag(V i):SV_Target
 {
  float3 n=normalize(i.normal);Light sun=GetMainLight();float wrap=saturate(dot(n,sun.direction)*.5+.5);
  float bright=smoothstep(.08,.96,saturate(n.y*.5+.5)*.52+wrap*.26+i.height*.22);
  half3 col=lerp(_Shade.rgb,_BaseColor.rgb,bright);
  float rim=pow(1-saturate(dot(n,GetWorldSpaceNormalizeViewDir(i.world))),3);
  col+=rim*.025;return half4(MixFog(col,i.fog),1);
 }
 ENDHLSL
 }


 }
}
