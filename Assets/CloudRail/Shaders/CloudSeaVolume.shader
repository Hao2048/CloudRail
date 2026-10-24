Shader "CloudRail/Continuous Cloud Sea"
{
 Properties{_Noise("Cloud noise",3D)="white"{} _Density("Density",Float)=1.3 _CloudSteps("Near samples",Float)=28 _FarSteps("Far samples",Float)=16 _CloudDistance("Distance limit",Float)=640 _Light("Sunlit cloud",Color)=(1,.97,.91,1) _Shade("Blue scattering",Color)=(.46,.59,.76,1)}
 SubShader
 {
 Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+20" "RenderType"="Transparent"}
 Pass
 {
 Cull Off ZWrite Off ZTest Always Blend One OneMinusSrcAlpha
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
 TEXTURE3D(_Noise);SAMPLER(sampler_Noise);
 half4 _Light,_Shade;float _Density,_CloudSteps,_FarSteps,_CloudDistance;
 struct A{float4 p:POSITION;};struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;};
 V vert(A i){V o;o.world=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.world);return o;}
 float fbm(float3 p){return SAMPLE_TEXTURE3D_LOD(_Noise,sampler_Noise,p,0).r;}

 float field(float3 p)
 {
  float3 drift=float3(_Time.y*.000025,0,_Time.y*.000012);
  float macro=SAMPLE_TEXTURE3D_LOD(_Noise,sampler_Noise,float3(p.x*.00042,.317,p.z*.00042)+drift,0).r;
  float medium=SAMPLE_TEXTURE3D_LOD(_Noise,sampler_Noise,float3(p.x*.00115,.613,p.z*.00115)+drift,0).r;
  float top=-33+(macro-.5)*43+(medium-.5)*17;
  float erosion=(fbm(p*float3(.0021,.0032,.0021)+drift)-.5)*5;
  float weather=SAMPLE_TEXTURE3D_LOD(_Noise,sampler_Noise,float3(p.x*.00029,.817,p.z*.00029)+drift,0).r;
  float coverage=pow(smoothstep(.5,.68,weather),1.6);
  top-= (1-coverage)*6;
  float thickness=top-p.y+erosion;
  float underside=1-smoothstep(9,23,thickness);
  return saturate(thickness/8)*underside*_Density*coverage;

 }
 half4 frag(V i):SV_Target
 {
  float3 eye=GetCameraPositionWS(),ray=normalize(i.world-eye);if(ray.y>=-.001)return 0;
  float start=max(0,(-12-eye.y)/ray.y),end=(-70-eye.y)/ray.y;
  float2 uv=i.p.xy/_ScaledScreenParams.xy;float raw=SampleSceneDepth(uv);
  float eyeDepth=LinearEyeDepth(raw,_ZBufferParams);float sceneDistance=eyeDepth/max(.001,-TransformWorldToViewDir(ray).z);end=min(end,min(sceneDistance,_CloudDistance));
  if(end<=start)return 0;
  int samples=(int)(start>150?_FarSteps:_CloudSteps);float stepSize=(end-start)/samples;float jitter=.5;
  int shadowTick=0;float trans=1;float cachedSunlight=1;float3 color=0;float3 light=normalize(float3(-.45,.8,-.35));
  [loop]for(int k=0;k<samples;k++)
  {
   float d=start+(k+jitter)*stepSize;if(d>end||trans<.025)break;
   float3 p=eye+ray*d;float density=field(p);if(density<.008)continue;
   if((shadowTick++&3)==0)cachedSunlight=exp(-field(p+light*8)*1.65);float sunlight=cachedSunlight;
   float height=saturate((p.y+52)/30);float3 tint=lerp(_Shade.rgb,_Light.rgb,saturate(sunlight*.85+height*.15));
   float a=1-exp(-density*stepSize*.58);float haze=1-exp(-d*.0009);tint=lerp(tint,float3(.78,.85,.94),smoothstep(260,1100,d)*.9);
   color+=trans*a*tint;trans*=1-a;
  }
  float fade=1-smoothstep(_CloudDistance*.65,_CloudDistance,start);return half4(color*fade,(1-trans)*fade);
 }
 ENDHLSL
 }
 }
}
