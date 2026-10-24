using System;
using System.Collections.Generic;
using TMPro;
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
  static float TrackClearance(SkyRailPath path,Vector3 point,out Vector3 nearest)
  {
   float best=float.PositiveInfinity;nearest=Vector3.zero;
   for(int i=0;i<1400;i++){path.Evaluate(path.Length*i/1400,out var p,out _);float d=new Vector2(p.x-point.x,p.z-point.z).sqrMagnitude;if(d<best){best=d;nearest=p;}}
   return Mathf.Sqrt(best);
  }
  public static void RepairBackdropBuildingAnchors()
  {
   var game=Object.FindAnyObjectByType<JourneyGame>();
   var backdrop=game.transform.parent.Find("16  Cloud sea and stepping islands");
   var building=backdrop.Find("Arcaded azure villa");
   var island=backdrop.Find("Distant meadow 2");
   if(building==null||island==null)return;
   // This villa belongs to the meadow. Moving its former sibling left it in the train corridor.
   building.SetParent(island,false);building.localPosition=Vector3.zero;building.localRotation=Quaternion.identity;
  }
  static void MeadowRocks(Transform g,int count)
  {
   for(int k=0;k<count;k++){float a=R(0,6.28f),r=R(2.8f,4.6f);Box(g,"Weathered outcrop",new Vector3(Mathf.Cos(a)*r,.17f,Mathf.Sin(a)*r),new Vector3(R(.35f,1),R(.25f,.7f),R(.45f,1.1f)),k%2==0?"RockB":"RockD",Quaternion.Euler(R(-14,14),R(0,180),R(-12,12)));}
  }
  static void Windmill(Transform root)
  {
   var fixedPart=Group("Whitewashed windmill",root);Cylinder(fixedPart,"Stone tower",Vector3.up*2.5f,1.2f,5,"Ivory",24,.72f);Hip(fixedPart,Vector3.up*5,2.7f,2.7f,1.5f);Window(fixedPart,new Vector3(0,1.6f,-1.14f),.7f,1.2f);Combine(fixedPart,"WindmillStone");
   var blades=Group("Slow turning sails",root);blades.localPosition=new Vector3(0,4.2f,-1.35f);Cylinder(blades,"Wooden hub",Vector3.zero,.22f,.35f,"Timber",16,1,Quaternion.Euler(90,0,0));
   for(int i=0;i<4;i++){var sail=Group("Linen sail",blades);sail.localRotation=Quaternion.Euler(0,0,90*i+22);Beam(sail,"Sail spar",Vector3.zero,Vector3.up*3.2f,.06f,"Timber");for(int j=0;j<5;j++)Box(sail,"Linen slat",new Vector3(.35f,1.4f+j*.35f,0),new Vector3(.8f,.27f,.055f),"Porcelain");}
   Combine(blades,"WindmillSails");blades.gameObject.AddComponent<ScenicRotor>();
  }
  static void Observatory(Transform root)
  {
   Cylinder(root,"Observatory terrace",Vector3.up*.15f,3,.3f,"Limestone",36);
   for(int k=0;k<5;k++){float a=k*Mathf.PI*2/5;Cylinder(root,"Sky pavilion pillar",new Vector3(Mathf.Cos(a)*2.15f,1.65f,Mathf.Sin(a)*2.15f),.13f,3,"Ivory",12);}
   var v=new List<Vector3>();var t=new List<int>();for(int ring=0;ring<10;ring++)for(int k=0;k<40;k++){
    Func<int,int,Vector3> pos=(r,s)=>{float a=s*Mathf.PI*2/40,b=r*Mathf.PI*.5f/10;return new Vector3(Mathf.Cos(a)*2.65f*Mathf.Cos(b),3.2f+1.7f*Mathf.Sin(b),Mathf.Sin(a)*2.65f*Mathf.Cos(b));};
    Tri(v,t,pos(ring,k),pos(ring+1,k+1),pos(ring,k+1));Tri(v,t,pos(ring,k),pos(ring+1,k),pos(ring+1,k+1));}
   var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();Shape("Copper observatory dome",SaveMesh(mesh,"ScenicObservatoryDome"),root,Vector3.zero,Vector3.one,mats["RoofDark"]);
   Cylinder(root,"Telescope stand",Vector3.up*.9f,.12f,1.5f,"Brass");Beam(root,"Bronze telescope",new Vector3(-.7f,1.2f,.6f),new Vector3(1,2.35f,-.8f),.2f,"Brass",20);Ball(root,"Lens",new Vector3(1,2.35f,-.8f),Vector3.one*.35f,"Window");
  }
  [MenuItem("Cloud Rail/Clustered Clouds And Route Landmarks")]
  public static void RouteScenery()
  {
   if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");visualAssets=true;gameplayAssets=true;releaseAssets=desktopAssets=false;serial=180000;rng=new System.Random(3982);shapes.Clear();mats.Clear();
   foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{Gen+"/Materials"})){var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));mats[m.name]=m;}
   var temp=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=temp.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(temp);temp=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere=temp.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(temp);
   var game=Object.FindAnyObjectByType<JourneyGame>();var world=game.transform.parent;var path=game.train.rig.path;
   var oldCloud=world.Find("18  Continuous volumetric cloud sea");if(oldCloud!=null)oldCloud.gameObject.SetActive(false);
   foreach(string name in new[]{"19  Cloud clusters around the islands","20  Railway scenic landmarks"}){var old=world.Find(name);if(old!=null)Object.DestroyImmediate(old.gameObject);}
   var cloudsRoot=Group("19  Cloud clusters around the islands",world);
   const string cloudMatPath="Assets/CloudRail/Generated/Visual/Materials/SoftCloudClusters.mat";var cloudMat=AssetDatabase.LoadAssetAtPath<Material>(cloudMatPath);if(cloudMat==null){cloudMat=new Material(Shader.Find("CloudRail/Soft Cloud Cluster"));AssetDatabase.CreateAsset(cloudMat,cloudMatPath);}cloudMat.SetTexture("_Noise",AssetDatabase.LoadAssetAtPath<Texture3D>("Assets/CloudRail/Generated/Visual/CloudDensityNoise.asset"));cloudMat.SetFloat("_Density",1.7f);cloudMat.SetColor("_Shade",new Color(.61f,.7f,.82f));EditorUtility.SetDirty(cloudMat);
   Vector3[] centers={new Vector3(-22,-25,-17),new Vector3(7,-28,-24),new Vector3(28,-22,2),new Vector3(-1,-25,28),new Vector3(-38,-24,17),new Vector3(43,-23,35),new Vector3(-49,-27,-34),new Vector3(34,-26,-49),new Vector3(11,-32,59),new Vector3(-38,-4,-8),new Vector3(56,-3,-5),new Vector3(-21,8,34),new Vector3(31,7,58),new Vector3(-77,1,18),new Vector3(70,12,31),new Vector3(-4,-6,-55),new Vector3(76,-20,-22),new Vector3(-75,-24,-25),new Vector3(-24,-22,-43),new Vector3(13,-24,-44)};
   for(int i=0;i<centers.Length;i++){Vector3 radii=(i<9||i>=18)?new Vector3(R(14,22),R(7,10),R(11,17)):new Vector3(R(8,13),R(4.5f,7),R(6,10));var go=Shape("Soft cumulus cluster "+i,cube,cloudsRoot,centers[i],radii*2,cloudMat);go.layer=9;go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;}
   RepairBackdropBuildingAnchors();
   // Push the existing small backdrop islets out of the rail loading gauge.
   var backdrop=world.Find("16  Cloud sea and stepping islands");foreach(Transform island in backdrop)
   {
    if(!island.name.StartsWith("Distant meadow"))continue;
    for(int n=0;n<18;n++){float clearance=TrackClearance(path,island.position,out var near);if(clearance>=10.2f)break;var direction=Vector3.ProjectOnPlane(island.position-near,Vector3.up).normalized;if(direction.sqrMagnitude<.1f)direction=Vector3.right;island.position+=direction*2.5f;}
   }
   var routeRoot=Group("20  Railway scenic landmarks",world);float[] fractions={.07f,.23f,.42f,.58f,.77f,.94f};
   string[] names={"Lavender cliff meadow","Windmill above the clouds","Broken sky sanctuary","The little observatory","Birch and stone garden","Waterfall keeper cottage"};
   for(int i=0;i<fractions.Length;i++)
   {
    path.Evaluate(path.Length*fractions[i],out var at,out var q);Vector3 side=Vector3.ProjectOnPlane(q*Vector3.right,Vector3.up).normalized;Vector3 center=at+side*18;center.y=at.y-2.5f;
    // Each island fits within a six metre footprint; the closest track must be at least ten metres away.
    for(int k=0;k<12;k++){if(TrackClearance(path,center,out var near)>=11)break;center+=side*3;}
    var g=Group(names[i],routeRoot);g.position=center;g.rotation=Quaternion.Euler(0,R(-20,25),0);Island(g,Vector3.zero,5.5f,4.8f,9+i%3*2,"Roadside hanging meadow",22);MeadowRocks(g,9);
    if(i==0){for(int k=0;k<6;k++)FlowerBed(g,new Vector3(R(-3,3),0,R(-2.5f,2.5f)),R(.5f,.9f));Tree(g,new Vector3(-3.4f,0,1.5f),.7f,true);}
    else if(i==1){Windmill(g);FlowerBed(g,new Vector3(3,0,0),.9f);}
    else if(i==2){for(int j=-1;j<=1;j+=2)Cylinder(g,"Ancient fluted pillar",new Vector3(j*2,2,0),.3f,4,"Limestone",16);Shape("Broken sanctuary arch",SaveMesh(ArchMesh(4,1),"SanctuaryArch"),g,new Vector3(0,4.1f,0),Vector3.one,mats["Ivory"]);Cylinder(g,"Fallen column",new Vector3(0,.3f,-2),.28f,3.2f,"Ivory",16,1,Quaternion.Euler(0,0,83));}
    else if(i==3){Observatory(g);Tree(g,new Vector3(-3.5f,0,2),.7f,false);}
    else if(i==4){for(int k=0;k<5;k++)Tree(g,new Vector3(R(-3,3),0,R(-2,2)),R(.7f,1.1f),false);Bench(g,new Vector3(1,0,-2.2f),15);}
    else{Villa(g,new Vector3(0,0,1),3.5f,3,3.1f);Cylinder(g,"Spring pool",new Vector3(0,.1f,-2.5f),1.5f,.2f,"WindowLight",32);for(int k=0;k<7;k++)Box(g,"Spring waterfall",new Vector3((k-3)*.13f,-7,-4.8f),new Vector3(.1f,14,.12f),k%3==0?"Porcelain":"WindowLight");}
    // Keep the windmill rotor separate from static mesh batching.
    if(i!=1)Combine(g,"ScenicLandmark_"+i);
   }
   // Clear distant, obsolete camera trigger volumes left over from the former main station.
   var guards=world.Find("12  Living cloud archipelago/Camera-only foliage and roof volumes");if(guards!=null)foreach(Transform t in guards)if(t.position.z< -15)t.gameObject.SetActive(false);
   var profile=Object.FindAnyObjectByType<Volume>().sharedProfile;profile.TryGet<ColorAdjustments>(out var color);color.saturation.Override(-2);color.contrast.Override(11);color.postExposure.Override(.05f);EditorUtility.SetDirty(profile);
   RenderSettings.ambientSkyColor=new Color(.57f,.66f,.78f);RenderSettings.ambientEquatorColor=new Color(.39f,.46f,.56f);RenderSettings.ambientGroundColor=new Color(.25f,.3f,.38f);RenderSettings.sun.intensity=1.7f;RenderSettings.sun.shadowStrength=.86f;
   var hint=game.voyage.overlay.transform.Find("Desktop watching keys");if(hint!=null)hint.GetComponentInChildren<TMP_Text>().text="C 切换机位   ·   右键环绕   ·   中键拖动 / 滚轮调距   ·   空格暂停   ·   Tab 探索";
   var probeRoot=world.Find("21  Local environment reflections");if(probeRoot==null)probeRoot=Group("21  Local environment reflections",world);probeRoot.position=new Vector3(13,15,-13);var probe=probeRoot.GetComponent<ReflectionProbe>();if(probe==null)probe=probeRoot.gameObject.AddComponent<ReflectionProbe>();probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.OnAwake;probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.IndividualFaces;probe.resolution=128;probe.size=new Vector3(240,120,240);probe.cullingMask=1;probe.farClipPlane=220;probe.intensity=.65f;probe.boxProjection=false;
   AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
  }
 }
}
