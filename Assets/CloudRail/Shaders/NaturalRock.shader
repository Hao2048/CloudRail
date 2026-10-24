Shader "CloudRail/Natural Continuous Rock"
{
 Properties{_BaseColor("Slate colour",Color)=(.55,.63,.7,1) _Noise("Stone density",3D)="white"{} _Relief("Relief",Float)=.8 _BaseMap("Base",2D)="white"{} _Cull("Cull",Float)=2 _Cutoff("Cutoff",Float)=.5}
 SubShader
 {
 Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
 Pass
 {
 Name "ForwardLit" Tags{"LightMode"="UniversalForward"}
 Offset -1, -1
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
 #pragma multi_compile_fog
 #define _SPECULARHIGHLIGHTS_OFF
 #define _ENVIRONMENTREFLECTIONS_OFF
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE3D(_Noise);SAMPLER(sampler_Noise);half4 _BaseColor;float _Relief;
 struct A{float4 position:POSITION;float3 normal:NORMAL;};struct V{float4 position:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float fog:TEXCOORD2;};
 V vert(A i){V o;o.world=TransformObjectToWorld(i.position.xyz);o.position=TransformObjectToHClip(i.position.xyz);o.normal=TransformObjectToWorldNormal(i.normal);o.fog=ComputeFogFactor(o.position.z);return o;}
 float noise(float3 p){return SAMPLE_TEXTURE3D_LOD(_Noise,sampler_Noise,p,0).r;}
 float relief(float3 p)
 {
  float coarse=noise(p*.007);float detail=noise(p*.033+float3(.23,.41,.17));
  float bands=sin(p.y*2.5+coarse*5+noise(p*.002)*3);
  float seam=pow(1-abs(bands),8);
  return (coarse-.5)*.7+(detail-.5)*.11-seam*.095;
 }
 half4 frag(V i):SV_Target
 {
  float3 p=i.world;float h=relief(p);float e=.065;
  float3 grad=float3(relief(p+float3(e,0,0))-relief(p-float3(e,0,0)),relief(p+float3(0,e,0))-relief(p-float3(0,e,0)),relief(p+float3(0,0,e))-relief(p-float3(0,0,e)))/(2*e);
  float3 n=normalize(i.normal);n=normalize(n-(grad-n*dot(n,grad))*_Relief);
  float mineral=noise(p*.011+float3(.42,.1,.29));float cracks=saturate(-h*2);
  half3 albedo=_BaseColor.rgb*lerp(.81,1.13,mineral);albedo*=1-cracks*.19;
  InputData input=(InputData)0;input.positionWS=p;input.normalWS=n;input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(p);input.shadowCoord=TransformWorldToShadowCoord(p);input.bakedGI=SampleSH(n);input.shadowMask=half4(1,1,1,1);input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.position);input.vertexLighting=0;
  SurfaceData surface=(SurfaceData)0;surface.albedo=albedo;surface.normalTS=half3(0,0,1);surface.smoothness=.03;surface.occlusion=1-cracks*.12;surface.alpha=1;
  half4 col=UniversalFragmentPBR(input,surface);col.rgb=MixFog(col.rgb,i.fog);return col;
 }
 ENDHLSL
 }
 UsePass "Universal Render Pipeline/Lit/ShadowCaster"
 UsePass "Universal Render Pipeline/Lit/DepthOnly"
 UsePass "Universal Render Pipeline/Lit/DepthNormals"
 }
}
