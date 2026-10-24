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
  static void Hip(Transform p,Vector3 at,float w,float d,float h)
  {
   var v=new List<Vector3>();var t=new List<int>();
   var a=new Vector3(-w/2,0,-d/2);var b=new Vector3(w/2,0,-d/2);var c=new Vector3(w/2,0,d/2);var e=new Vector3(-w/2,0,d/2);
   float ridge=Mathf.Max(0,(w-d)*.5f);var l=new Vector3(-ridge,h,0);var r=new Vector3(ridge,h,0);
   Tri(v,t,a,l,r);Tri(v,t,a,r,b);Tri(v,t,b,r,c);Tri(v,t,c,r,l);Tri(v,t,c,l,e);Tri(v,t,e,l,a);
   var m=new Mesh();m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();
   Shape("Azure hipped roof",SaveMesh(m,"Hip_"+serial++),p,at,Vector3.one,mats["Roof"]);
   Box(p,"Ivory eaves",at-Vector3.up*.08f,new Vector3(w+.12f,.16f,d+.12f),"Porcelain");
   if(ridge>.1f)Beam(p,"Roof ridge",at+l,at+r,.065f,"RoofLight");
  }
  static void Villa(Transform p,Vector3 at,float w,float d,float h,float yaw=0)
  {
   var g=Group("Arcaded azure villa",p);g.localPosition=at;g.localRotation=Quaternion.Euler(0,yaw,0);
   Box(g,"Cream plaster",Vector3.up*h*.5f,new Vector3(w,h,d),"Ivory");
   Box(g,"Foundation course",Vector3.up*.16f,new Vector3(w+.3f,.32f,d+.3f),"Limestone");
   Hip(g,Vector3.up*h,w+.7f,d+.7f,Mathf.Min(w,d)*.34f);
   for(int floor=0;floor<Mathf.Max(1,Mathf.FloorToInt(h/2.4f));floor++)
   {
    float y=1.3f+floor*2.5f;
    for(int x=0;x<3;x++)Window(g,new Vector3((x-1)*w*.29f,y,-d*.5f-.07f),w*.15f,1.2f);
    foreach(int side in new[]{-1,1}){var s=Group("Side arched window",g);s.localPosition=new Vector3(side*w*.5f,y,0);s.localRotation=Quaternion.Euler(0,-side*90,0);Window(s,Vector3.zero,.85f,1.2f);}
    if(floor>0)Box(g,"Floor stringcourse",new Vector3(0,y-1.1f,0),new Vector3(w+.18f,.12f,d+.18f),"Porcelain");
   }
   // The central dormer breaks the broad roof silhouette.
   Box(g,"Dormer",new Vector3(0,h+.45f,-d*.27f),new Vector3(1.15f,1.25f,1.1f),"Ivory");
   Window(g,new Vector3(0,h+.35f,-d*.27f-.58f),.56f,.65f);Hip(g,new Vector3(0,h+1.1f,-d*.27f),1.5f,1.45f,.65f);
   for(int i=0;i<4;i++){float x=(i-1.5f)*w/4;Cylinder(g,"Portico column",new Vector3(x,.95f,-d*.5f-.8f),.1f,1.9f,"Porcelain",12);}
   Box(g,"Porch canopy",new Vector3(0,2,-d*.5f-.8f),new Vector3(w+.3f,.2f,1.7f),"Porcelain");
   Combine(g,"Villa_"+serial++);
  }
  static void Terrace(Transform p,float y,float z,float width,float depth)
  {
   var g=Group("Garden terrace "+y,p);
   Box(g,"Upper terrace body",new Vector3(0,y-2.8f,z+depth*.5f),new Vector3(width,5.5f,depth),"Ivory");
   for(float x=-width*.5f+2.5f;x<width*.5f;x+=5)
   {
    Shape("Open limestone arcade",SaveMesh(ArchMesh(4.7f,1.1f),"TerraceArch_"+serial++),g,new Vector3(x,y-.45f,z-.12f),new Vector3(1,1.45f,1),mats["Porcelain"]);
    // Deep blue alcove behind the open arch, rather than a painted black window.
    Box(g,"Recess shadow",new Vector3(x,y-3,z+.53f),new Vector3(4.2f,4.8f,.08f),"RockC");
   }
   Box(g,"Garden lawn",new Vector3(0,y+.06f,z+depth*.5f),new Vector3(width,.18f,depth),"Grass");
   Box(g,"Terrace cornice",new Vector3(0,y-.12f,z-.65f),new Vector3(width+.3f,.3f,.65f),"Porcelain");
   Fence(g,new Vector3(-width*.5f,y+.1f,z-.5f),new Vector3(width*.5f,y+.1f,z-.5f),(int)(width*1.1f));
   Combine(g,"Terrace_"+serial++);
  }
  static void Steps(Transform p,Vector3 at,float rise,float length,float width)
  {
   var g=Group("Broad ceremonial stair",p);g.localPosition=at;int n=30;
   for(int i=0;i<n;i++)Box(g,"Pale stone step",new Vector3(0,(i+1)*rise/n*.5f,i*length/n),new Vector3(width,(i+1)*rise/n,length/n+.04f),"Ivory");
   for(int side=-1;side<=1;side+=2){var a=new Vector3(side*(width*.5f+.16f),.55f,0);var b=new Vector3(a.x,rise+.55f,length);Beam(g,"Stair balustrade",a,b,.17f,"Porcelain",8);for(int k=0;k<=15;k++){var q=Vector3.Lerp(a,b,k/15f);Cylinder(g,"Stair post",q-Vector3.up*.25f,.06f,.6f,"Porcelain",8);}}
   Combine(g,"Stairs_"+serial++);
  }
  static void CastleTower(Transform p,Vector3 at)
  {
   var g=Group("Celestial clocktower",p);g.localPosition=at;
   float y=0;
   float[] widths={5.8f,4.5f,3.3f};float[] heights={7.8f,7.3f,6.4f};
   for(int i=0;i<3;i++)
   {
    float w=widths[i],h=heights[i];Box(g,"Tower tier",new Vector3(0,y+h*.5f,0),new Vector3(w,h,w),"Ivory");
    foreach(int side in new[]{0,1,2,3}){var f=Group("Tower facade",g);f.localRotation=Quaternion.Euler(0,side*90,0);Window(f,new Vector3(0,y+h*.47f,-w*.5f-.07f),w*.37f,h*.54f);}
    Box(g,"Tower cornice",new Vector3(0,y+h,0),new Vector3(w+.65f,.36f,w+.65f),"Porcelain");y+=h;
   }
   // Clock sits on the middle tier, with a crisp raised face and visible hands.
   var clock=Group("Clock face",g);clock.localPosition=new Vector3(0,14.1f,-2.34f);
   Cylinder(clock,"Gold clock bezel",Vector3.zero,1.04f,.13f,"Brass",48,1,Quaternion.Euler(90,0,0));
   Cylinder(clock,"Ivory dial",Vector3.back*.09f,.91f,.05f,"Porcelain",48,1,Quaternion.Euler(90,0,0));
   Beam(clock,"Minute hand",new Vector3(0,0,-.14f),new Vector3(.06f,.66f,-.14f),.045f,"RoofDark");Beam(clock,"Hour hand",new Vector3(0,0,-.15f),new Vector3(-.47f,.17f,-.15f),.055f,"RoofDark");
   Hip(g,new Vector3(0,y,0),4.1f,4.1f,3.4f);
   for(int side=-1;side<=1;side+=2){var turret=Group("Balcony turret",g);turret.localPosition=new Vector3(side*4.1f,6.6f,0);Box(turret,"Turret",Vector3.up*2.4f,new Vector3(2.2f,4.8f,2.2f),"Ivory");Window(turret,new Vector3(0,2,-1.15f),.8f,1.65f);Hip(turret,Vector3.up*4.8f,2.8f,2.8f,1.7f);Box(g,"Flying balcony",new Vector3(side*3.1f,6.6f,0),new Vector3(3,.4f,3.2f),"Porcelain");}
   var ring=Group("Golden orrery",g);ring.localPosition=Vector3.up*(y+2.5f);
   for(int j=0;j<2;j++){Quaternion q=Quaternion.Euler(j==0?5:27,0,j==0?0:18);for(int k=0;k<100;k++){float a=k*Mathf.PI*2/100,b=(k+1)*Mathf.PI*2/100;Beam(ring,"Thin celestial orbit",q*new Vector3(Mathf.Cos(a)*5.8f,0,Mathf.Sin(a)*5.8f),q*new Vector3(Mathf.Cos(b)*5.8f,0,Mathf.Sin(b)*5.8f),.065f,"Brass",8);}}
   Cylinder(g,"Golden finial",Vector3.up*(y+4),.06f,2,"Brass",10);Ball(g,"North star",Vector3.up*(y+5),Vector3.one*.42f,"Brass");Combine(g,"HeroClocktower");
  }
  [MenuItem("Cloud Rail/Refine Reference Art Direction")]
  public static void RefineReference()
  {
   if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");
   EditorSceneManager.OpenScene(VisualScenePath);visualAssets=true;desktopAssets=false;releaseAssets=false;gameplayAssets=true;serial=90000;rng=new System.Random(1298);shapes.Clear();mats.Clear();
   var temp=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=temp.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(temp);temp=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere=temp.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(temp);
   foreach(string path in AssetDatabase.FindAssets("t:Material",new[]{Gen+"/Materials"})) {var mat=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(path));mats[mat.name]=mat;}
   string[] names={"Ivory","Limestone","Porcelain","Roof","RoofLight","RoofDark","Grass","GrassLight","GrassDark","RockA","RockB","RockC","RockD","RockE","Pink","PinkLight","Leaf","LeafLight","Cloud","Engine","EngineDark","Window","WindowLight","Brass","Rail","Sleepers"};
   string[] colors={"#FFF1D8","#E7D7BB","#FFF9E9","#65C6D8","#91DCE5","#428BA4","#B4D67B","#C4DF96","#94B879","#92A6BD","#B5C4D2","#7A92AE","#C6D1DE","#8299B2","#EAB3CE","#FAD5E1","#88B99A","#B8D3A4","#FFFFFF","#59ABC2","#3C6A89","#68A3BB","#B5DEDF","#E5C781","#9C927B","#C7AD87"};
   for(int i=0;i<names.Length;i++){var m=mats[names[i]];m.shader=Shader.Find("Universal Render Pipeline/Lit");ColorUtility.TryParseHtmlString(colors[i],out var col);m.SetColor("_BaseColor",col);m.SetFloat("_Metallic",0);m.SetFloat("_Smoothness",.22f);m.SetColor("_EmissionColor",Color.black);m.DisableKeyword("_EMISSION");EditorUtility.SetDirty(m);}
   var game=Object.FindAnyObjectByType<JourneyGame>();var root=game.transform.parent;
   foreach(string name in new[]{"01  Floating island","02  Cloudrise station","03  Gardens and promenade","05  Waterfall","06  Distant archipelago"}){var old=root.Find(name);if(old!=null)old.gameObject.SetActive(false);}
   var previous=root.Find("15  Cloudrise terraced city");if(previous!=null)Object.DestroyImmediate(previous.gameObject);
   var city=Group("15  Cloudrise terraced city",root);city.localPosition=new Vector3(-9,0,-12);
   Island(city,Vector3.zero,24,19,29,"White cliff island",30);
   Terrace(city,5.8f,-4,37,15);Terrace(city,11.6f,5,24,10);
   Steps(city,new Vector3(2,0,-13),5.8f,8,4.5f);Steps(city,new Vector3(2,5.8f,-3),5.8f,8,4);
   Villa(city,new Vector3(-14,5.8f,0),6.3f,5.4f,5);Villa(city,new Vector3(13,5.8f,2),6,5,5.2f,-10);
   Villa(city,new Vector3(-15,0,-11),6.8f,5.3f,5);Villa(city,new Vector3(13,0,-10),5.4f,4.6f,4.5f);
   Villa(city,new Vector3(-7,11.6f,9),4.2f,4,4.7f);Villa(city,new Vector3(8.7f,11.6f,10),3.6f,3.8f,4.2f);
   CastleTower(city,new Vector3(-1.5f,11.6f,10));
   foreach(var v in new[]{new Vector3(-21,0,-4),new Vector3(18,0,-5),new Vector3(-8,0,-13),new Vector3(-9,5.8f,-1),new Vector3(9,5.8f,0),new Vector3(-9,11.6f,8),new Vector3(10,11.6f,7)})Tree(city,v,.83f,true);
   // Pool and a long waterfall make a bright vertical accent in the cliff.
   Cylinder(city,"Pool limestone rim",new Vector3(-10,.12f,-14),3.3f,.18f,"Porcelain",64,1);Cylinder(city,"Turquoise water",new Vector3(-10,.23f,-14),2.95f,.08f,"WindowLight",64,1);
   Box(city,"Water channel",new Vector3(-10,.13f,-16.8f),new Vector3(1.4f,.08f,5),"WindowLight");
   for(int k=0;k<12;k++)Box(city,"Waterfall ribbon",new Vector3(-10+(k-5.5f)*.12f,-12,-19),new Vector3(.09f,24,.1f),k%3==0?"Porcelain":"WindowLight");
   // A small station at the cliff foot preserves the sense of scale of the train.
   Villa(city,new Vector3(5,0,-17.2f),6.4f,2.8f,2.6f);
   var nature=Group("16  Cloud sea and stepping islands",root);
   for(int i=0;i<6;i++){var pos=new Vector3(35+(i%2)*13,1+i*.9f,13+i*10);Island(nature,pos,6.3f-i*.3f,5.3f,11+i,"Distant meadow "+i,18);if(i==2)Villa(nature.Find("Distant meadow "+i),Vector3.zero,2.4f,2.2f,2.8f);}
   for(int i=0;i<24;i++){var c=Group("Soft cloud bank",nature);c.localPosition=new Vector3(R(-90,95),R(-30,-21),R(-70,100));for(int k=0;k<6;k++)Ball(c,"Soft cumulus",new Vector3(R(-9,9),R(-1,2),R(-5,5)),new Vector3(R(10,20),R(5,9),R(9,15)),"Cloud");c.localScale=new Vector3(1.65f,1.8f,1.65f);c.localPosition+=Vector3.down*6;Combine(c,"CloudBank_"+i);}
   RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.79f,.85f,.95f);RenderSettings.ambientEquatorColor=new Color(.66f,.74f,.85f);RenderSettings.ambientGroundColor=new Color(.5f,.59f,.73f);
   RenderSettings.sun.intensity=1.15f;RenderSettings.sun.color=new Color(1,.96f,.88f);RenderSettings.sun.shadowStrength=.65f;RenderSettings.sun.transform.rotation=Quaternion.Euler(48,-32,0);
   RenderSettings.fogColor=new Color(.77f,.84f,.95f);RenderSettings.fogDensity=.0028f;
   RenderSettings.skybox.SetColor("_Top",new Color(.55f,.75f,1f));RenderSettings.skybox.SetColor("_Horizon",new Color(.76f,.83f,.94f));RenderSettings.skybox.SetColor("_Bottom",new Color(.94f,.97f,1f));EditorUtility.SetDirty(RenderSettings.skybox);
   var profile=Object.FindAnyObjectByType<Volume>().sharedProfile;if(!profile.TryGet<Tonemapping>(out var tone)){tone=profile.Add<Tonemapping>(true);AssetDatabase.AddObjectToAsset(tone,profile);}tone.mode.Override(TonemappingMode.Neutral);if(profile.TryGet<ColorAdjustments>(out var color)){color.saturation.Override(10);color.contrast.Override(2);color.postExposure.Override(.5f);}EditorUtility.SetDirty(profile);
   game.voyage.skyway.lead=30;game.voyage.skyway.unfoldLength=8;Camera.main.fieldOfView=43;
   AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
  }
 }
}
