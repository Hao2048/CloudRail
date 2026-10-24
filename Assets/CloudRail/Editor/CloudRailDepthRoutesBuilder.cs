using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;
namespace CloudRail.Editor
{
 public static partial class CloudRailBuilder
 {
  static void WalkDeck(Transform root,Vector3 a,Vector3 b,float width,string label,bool rails=true)
  {
   Vector3 delta=b-a;var g=Group(label,root);g.localPosition=(a+b)*.5f;g.localRotation=Quaternion.LookRotation(delta);
   // Deck is oriented along the actual incline, including its collision surface.
   Box(g,"Walkable limestone deck",new Vector3(0,-.14f,0),new Vector3(width,.28f,delta.magnitude+.15f),"Ivory");
   if(rails)for(int side=-1;side<=1;side+=2)Fence(g,new Vector3(side*(width*.5f-.08f),0,-delta.magnitude*.5f),new Vector3(side*(width*.5f-.08f),0,delta.magnitude*.5f),Mathf.CeilToInt(delta.magnitude*1.2f));
   Combine(g,"RouteDeck_"+serial++);
  }
  static void Lookout(Transform root,Vector3 at,string name)
  {
   var g=Group(name,root);g.localPosition=at;Cylinder(g,"Circular belvedere",Vector3.down*.15f,2.3f,.3f,"Ivory",40);
   for(int i=0;i<24;i++){float a=i*Mathf.PI*2/24,b=(i+1)*Mathf.PI*2/24;if(Mathf.Abs(Mathf.Cos(a))>.85f&&Mathf.Sin(a)>.0f)continue;Fence(g,new Vector3(Mathf.Cos(a)*2.15f,0,Mathf.Sin(a)*2.15f),new Vector3(Mathf.Cos(b)*2.15f,0,Mathf.Sin(b)*2.15f),1);}
   Bench(g,new Vector3(0,0,-.9f),180);Combine(g,"Lookout_"+serial++);
  }
  public static void OpenStairLandings(Transform city)
  {
   foreach(Transform terrace in city)
   {
    if(!terrace.name.StartsWith("Garden terrace"))continue;
    float top=terrace.name.Contains("11.6")?11.6f:5.8f;float front=top>8?4.5f:-4.5f;
    foreach(var f in terrace.GetComponentsInChildren<MeshFilter>())
    {
     var source=f.sharedMesh;var vertices=source.vertices;var indices=source.triangles;var kept=new List<int>();
     for(int i=0;i<indices.Length;i+=3){var p=(vertices[indices[i]]+vertices[indices[i+1]]+vertices[indices[i+2]])/3;bool opening=p.x>-.7f&&p.x<4.7f&&Mathf.Abs(p.z-front)<.5f&&p.y>top+.25f;if(!opening){kept.Add(indices[i]);kept.Add(indices[i+1]);kept.Add(indices[i+2]);}}
     if(kept.Count==indices.Length)continue;
     var mesh=Object.Instantiate(source);mesh.triangles=kept.ToArray();mesh.RecalculateBounds();string path="Assets/CloudRail/Generated/Visual/Meshes/LandingOpening_"+terrace.name.Replace(" ","_")+"_"+f.name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);}f.sharedMesh=saved;
    }
   }
  }
  [MenuItem("Cloud Rail/Improve Depth And Exploration")]
  public static void ImproveDepthRoutes()
  {
   if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");
   visualAssets=true;gameplayAssets=true;releaseAssets=desktopAssets=false;serial=120000;rng=new System.Random(322);shapes.Clear();mats.Clear();LoadJourneyFont();
   foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{Gen+"/Materials"})){var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));mats[m.name]=m;}
   var temp=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=temp.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(temp);temp=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere=temp.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(temp);
   var game=Object.FindAnyObjectByType<JourneyGame>();var world=game.transform.parent;var city=world.Find("15  Cloudrise terraced city");
   OpenStairLandings(city);
   var old=world.Find("17  Scenic exploration routes");if(old!=null)Object.DestroyImmediate(old.gameObject);
   var routes=Group("17  Scenic exploration routes",world);routes.position=city.position;
   // Correct the one metre gap between the first ceremonial stair and its terrace.
   foreach(Transform t in city)if(t.name=="Broad ceremonial stair"&&t.localPosition.y<1)t.localScale=new Vector3(1,1,1.17f);
   var physics=game.transform.Find("Walkable surfaces and boundaries");foreach(Transform t in physics)if(t.name.StartsWith("Cloudrise")&&t.name!="Cloudrise platform")t.gameObject.SetActive(false);
   foreach(Transform t in physics)if(t.name=="Walkable sloping approach"&&t.position.z< -20)t.gameObject.SetActive(false);
   var ramp=world.Find("Cloudrise approach ramp");if(ramp!=null)ramp.gameObject.SetActive(false);
   WalkDeck(routes,new Vector3(13,1,-18),new Vector3(13,.22f,-14.2f),3,"Station garden approach",false);
   WalkDeck(routes,new Vector3(13,.22f,-14.2f),new Vector3(2,.22f,-13),2.5f,"Lower promenade",false);
   WalkDeck(routes,new Vector3(2,.22f,-13),new Vector3(-9,.25f,-11),2.2f,"Blossom and pool walk",false);
   WalkDeck(routes,new Vector3(-9,.25f,-11),new Vector3(-20,.28f,-12),2.2f,"Waterfall approach",false);
   WalkDeck(routes,new Vector3(-20,.28f,-12),new Vector3(-23.5f,.3f,-14),2,"Waterfall balcony bridge");Lookout(routes,new Vector3(-24.3f,.3f,-14.8f),"Waterfall belvedere");
   // A side route links the first terrace to the upper garden without retracing the central stair.
   WalkDeck(routes,new Vector3(11,5.98f,-3.1f),new Vector3(18,5.98f,-3.1f),2.1f,"East terrace walk");
   WalkDeck(routes,new Vector3(18,5.98f,-3.1f),new Vector3(18,8.9f,4),2.1f,"East ascending arcade");
   WalkDeck(routes,new Vector3(18,8.9f,4),new Vector3(12,11.8f,6.3f),2.1f,"Upper garden return");
   Lookout(routes,new Vector3(18,6,-3.1f),"Sunrise tea balcony");
   // Clocktower exterior ascent reaches a real accessible viewing balcony.
   WalkDeck(routes,new Vector3(5.9f,11.85f,6),new Vector3(5.9f,15.1f,15),1.9f,"Clocktower east climb");
   WalkDeck(routes,new Vector3(5.9f,15.1f,15),new Vector3(-1.5f,18.35f,15),1.9f,"Clocktower north climb");
   WalkDeck(routes,new Vector3(-1.5f,18.35f,15),new Vector3(-6.5f,18.35f,14),1.9f,"Orrery balcony approach");Lookout(routes,new Vector3(-7.8f,18.35f,13.3f),"Orrery sky balcony");
   // Balustrades read as a continuous silhouette, while the openings remain at stair landings.
   for(int level=0;level<2;level++){float y=level==0?5.95f:11.75f,z=level==0?-4.55f:4.45f,w=level==0?18.5f:12;for(int side=-1;side<=1;side+=2)Fence(routes,new Vector3(side*3.2f,y,z),new Vector3(side*w,y,z),20);}
   // A visible elevated connection makes the garden island reachable on foot.
   WalkDeck(routes,new Vector3(21,.3f,-1),new Vector3(29,.3f,5),2.4f,"Cloud garden footbridge");
   WalkDeck(routes,new Vector3(29,.3f,5),new Vector3(35,.3f,7),2.4f,"Garden island landing",false);
   for(int j=0;j<3;j++){float t=(j+.5f)/3;var pos=Vector3.Lerp(new Vector3(21,.3f,-1),new Vector3(29,.3f,5),t);Shape("Footbridge open arch",SaveMesh(ArchMesh(3.4f,2.3f),"FootArch_"+j),routes,pos-Vector3.up*.4f,Vector3.one,mats["Porcelain"],Quaternion.Euler(0,-37,0));}
   // Projecting piers and cornices catch sunlight and cast readable architectural shadows.
   for(int level=0;level<2;level++){float y=level==0?5.8f:11.6f,z=level==0?-4.8f:4.2f,w=level==0?37:24;for(float x=-w*.5f;x<=w*.5f;x+=5){Box(routes,"Arcade pilaster",new Vector3(x,y-2.8f,z),new Vector3(.34f,5.45f,.38f),"Limestone");Box(routes,"Pilaster capital",new Vector3(x,y-.45f,z),new Vector3(.6f,.2f,.58f),"Porcelain");}}
   foreach(var f in city.GetComponentsInChildren<MeshFilter>())
   {
    bool terrace=false;for(var t=f.transform;t!=city&&t!=null;t=t.parent)if(t.name.StartsWith("Garden terrace"))terrace=true;
    if(!terrace&&f.GetComponent<Collider>()==null)f.gameObject.AddComponent<MeshCollider>().sharedMesh=f.sharedMesh;
   }
   for(int k=0;k<2;k++){var surface=Group("Terrace walkable volume",routes);surface.localPosition=k==0?new Vector3(0,3.1f,3.5f):new Vector3(0,8.95f,10);surface.gameObject.AddComponent<BoxCollider>().size=k==0?new Vector3(37,5.7f,15):new Vector3(24,5.6f,10);}
   foreach(var f in routes.GetComponentsInChildren<MeshFilter>())if(f.GetComponent<Collider>()==null)f.gameObject.AddComponent<MeshCollider>().sharedMesh=f.sharedMesh;
   var route=game.GetComponent<JourneyScenicRoutes>();if(route==null)route=game.gameObject.AddComponent<JourneyScenicRoutes>();route.game=game;
   Vector3[] points={new Vector3(-23.5f,.35f,-14),new Vector3(-9,6,-1),new Vector3(18,6,-3.1f),new Vector3(4,11.85f,6),new Vector3(-7.8f,18.4f,13.3f),new Vector3(26,.4f,3)};
   route.titles=new[]{"听瀑台","樱花露台","日出茶座","钟塔上庭","星环观景台","云间连桥"};route.descriptions=new[]{"从岩壁边缘看水流落入云海","回望青蓝屋顶与缓缓驶过的列车","沿东侧步道可继续绕行到上层花园","沿钟塔右侧空中步道继续登高","整座云上城镇与远方浮岛尽收眼底","继续前行便是花园岛的环形步道"};route.viewpoints=new Transform[points.Length];
   for(int i=0;i<points.Length;i++){route.viewpoints[i]=Group(route.titles[i],routes);route.viewpoints[i].localPosition=points[i];}
   // Main island starlights now encourage the pool branch and terrace ascent, preserving save IDs.
   game.stars[0].transform.position=city.TransformPoint(new Vector3(10,1,-13));game.stars[1].transform.position=city.TransformPoint(new Vector3(-19,1,-12));game.stars[2].transform.position=city.TransformPoint(new Vector3(-7,6.65f,-2));
   var controller=game.player.GetComponent<CharacterController>();controller.stepOffset=.34f;controller.slopeLimit=48;
   // Isolate the renderer used by this visual scene before adding desktop ambient occlusion.
   var pipeline=(UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
   var dataField=typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
   var datas=(ScriptableRendererData[])dataField.GetValue(pipeline);
   const string rendererPath="Assets/CloudRail/Generated/Visual/ScenicDepthRenderer.asset";
   var rd=AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(rendererPath);if(rd==null){rd=Object.Instantiate(datas[0]);rd.name="Scenic Depth Renderer";AssetDatabase.CreateAsset(rd,rendererPath);}
   var ao=rd.rendererFeatures.Find(f=>f is ScreenSpaceAmbientOcclusion) as ScreenSpaceAmbientOcclusion;if(ao==null){ao=ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();ao.name="Soft architecture contact shadows";AssetDatabase.AddObjectToAsset(ao,rd);rd.rendererFeatures.Add(ao);}
   var settings=new SerializedObject(ao);settings.FindProperty("m_Settings.Intensity").floatValue=1.25f;settings.FindProperty("m_Settings.Radius").floatValue=.65f;settings.FindProperty("m_Settings.DirectLightingStrength").floatValue=.28f;settings.FindProperty("m_Settings.Falloff").floatValue=180;settings.FindProperty("m_Settings.Downsample").boolValue=false;settings.ApplyModifiedPropertiesWithoutUndo();ao.SetActive(true);ao.Create();rd.SetDirty();
   const string pipelinePath="Assets/CloudRail/Generated/Visual/ScenicDepthPipeline.asset";var pp=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);if(pp==null){pp=Object.Instantiate(pipeline);pp.name="Scenic Depth Pipeline";AssetDatabase.CreateAsset(pp,pipelinePath);}var list=(ScriptableRendererData[])datas.Clone();list[0]=rd;dataField.SetValue(pp,list);pp.shadowDistance=180;pp.shadowCascadeCount=4;pp.cascade4Split=new Vector3(.12f,.32f,.6f);pp.mainLightShadowmapResolution=2048;pp.supportsHDR=true;QualitySettings.renderPipeline=pp;EditorUtility.SetDirty(pp);EditorUtility.SetDirty(rd);
   RenderSettings.ambientSkyColor=new Color(.55f,.68f,.84f);RenderSettings.ambientEquatorColor=new Color(.37f,.48f,.64f);RenderSettings.ambientGroundColor=new Color(.25f,.33f,.47f);RenderSettings.sun.intensity=1.8f;RenderSettings.sun.shadowStrength=.85f;RenderSettings.sun.shadowBias=.025f;RenderSettings.sun.shadowNormalBias=.16f;RenderSettings.sun.transform.rotation=Quaternion.Euler(39,-38,0);
   var profile=Object.FindAnyObjectByType<Volume>().sharedProfile;profile.TryGet<ColorAdjustments>(out var color);color.postExposure.Override(.2f);color.contrast.Override(8);color.saturation.Override(10);EditorUtility.SetDirty(profile);
   mats["Window"].SetColor("_BaseColor",new Color(.22f,.46f,.6f));mats["Roof"].SetFloat("_Smoothness",.34f);EditorUtility.SetDirty(mats["Window"]);EditorUtility.SetDirty(mats["Roof"]);
   AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
  }
 }
}
