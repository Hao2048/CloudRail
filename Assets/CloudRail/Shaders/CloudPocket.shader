Shader "CloudRail/Soft Cloud Cluster"
{
 Properties{_Noise("Density texture",3D)="white"{} _Density("Density",Float)=1.8 _Light("Light scattering",Color)=(1,.98,.95,1) _Shade("Shadow scattering",Color)=(.54,.65,.8,1)}
 SubShader
 {
 Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+20" "RenderType"="Transparent"}
 Pass
 {
 Cull Front ZWrite Off ZTest Always Blend One OneMinusSrcAlpha
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
 TEXTURE3D(_Noise);SAMPLER(sampler_Noise);float _Density;half4 _Light,_Shade;
 struct A{float4 p:POSITION;};struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;};
 V vert(A i){V o;o.world=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.world);return o;}
 float noise(float3 q){return SAMPLE_TEXTURE3D_LOD(_Noise,sampler_Noise,q,0).r;}
 float ellipsoid(float3 p,float3 c,float3 r){float3 q=(p-c)/r;return 1-dot(q,q);}
 float field(float3 p)
 {
  float3 q=TransformWorldToObject(p)*2;
  float shape=max(ellipsoid(q,float3(-.32,-.12,0),float3(.63,.6,.8)),ellipsoid(q,float3(.3,-.06,.06),float3(.63,.64,.78)));
  shape=max(shape,ellipsoid(q,float3(0,.3,-.04),float3(.5,.6,.64)));
  float n=noise(p*.0035+float3(_Time.y*.00008,0,0));
  return max(0,shape+(n-.5)*.75-.13)*_Density;
 }
 half4 frag(V i):SV_Target
 {
  float3 eye=GetCameraPositionWS(),ray=normalize(i.world-eye);
  float3 origin=TransformWorldToObject(eye),dir=TransformWorldToObjectDir(ray,false),inv=1/(dir+1e-7);
  float3 a=(-.5-origin)*inv,b=(.5-origin)*inv;float3 mn=min(a,b),mx=max(a,b);
  float entry=max(0,max(mn.x,max(mn.y,mn.z))),exit=min(mx.x,min(mx.y,mx.z));
  float raw=SampleSceneDepth(i.p.xy/_ScaledScreenParams.xy);float eyeDepth=LinearEyeDepth(raw,_ZBufferParams);
  exit=min(exit,eyeDepth/max(.001,-TransformWorldToViewDir(ray).z));if(exit<=entry)return 0;
  float stepSize=(exit-entry)/20;int shadowTick=0;float trans=1;float sunlight=1;float3 color=0;float3 sun=normalize(float3(-.5,.8,-.35));
  [loop]for(int k=0;k<20;k++)
  {
   float d=entry+(k+.5)*stepSize;float3 p=eye+ray*d;float density=field(p);if(density<.005)continue;
   if((shadowTick++&1)==0)sunlight=exp(-field(p+sun*4)*1.45);float shadow=sunlight;
   float3 tint=lerp(_Shade.rgb,_Light.rgb,saturate(.22+shadow*.78));
   tint=lerp(tint,float3(.68,.79,.92),1-exp(-d*.0015));
   float alpha=1-exp(-density*stepSize*.7);color+=trans*alpha*tint;trans*=1-alpha;if(trans<.025)break;
  }
  return half4(color,1-trans);
 }
 ENDHLSL
 }
 }
}
