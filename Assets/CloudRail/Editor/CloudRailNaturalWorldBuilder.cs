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
  static void Curve(List<Vector3> p,Vector3 c1,Vector3 c2,Vector3 end){p.Add(c1);p.Add(c2);p.Add(end);}
  static void Straight(List<Vector3> p,Vector3 end){var a=p[p.Count-1];Curve(p,Vector3.Lerp(a,end,1f/3),Vector3.Lerp(a,end,2f/3),end);}
  static void TrimPlatform(Transform detail,bool garden)
  {
   foreach(var f in detail.GetComponentsInChildren<MeshFilter>())
   {
    var src=f.sharedMesh;var vertices=src.vertices;bool changed=false;
    for(int i=0;i<vertices.Length;i++)
    {
     var p=vertices[i];
     if(garden&&p.y<1.1f&&p.x>43.85f&&p.x<46.2f&&Mathf.Abs(p.z)<8.7f){p.x=43.85f;changed=true;}
     if(!garden&&p.y<1.1f&&p.z>32.85f&&p.z<35.2f&&Mathf.Abs(p.x)<8.8f){p.z=32.85f;changed=true;}
     vertices[i]=p;
    }
    if(!changed)continue;var mesh=Object.Instantiate(src);mesh.vertices=vertices;mesh.RecalculateBounds();f.sharedMesh=SaveMesh(mesh,"StationClearance_"+(garden?"Garden_":"Clock_")+f.name);
   }
  }
  static void ClearQuay(Transform island,bool garden)
  {
   foreach(var f in island.GetComponentsInChildren<MeshFilter>())
   {
    var source=f.sharedMesh;var vertices=source.vertices;bool changed=false;
    for(int i=0;i<vertices.Length;i++){var w=f.transform.TransformPoint(vertices[i]);if(garden?w.x>43.65f:w.z>32.65f){if(garden)w.x=43.65f;else w.z=32.65f;vertices[i]=f.transform.InverseTransformPoint(w);changed=true;}}
    if(!changed)continue;var mesh=Object.Instantiate(source);mesh.vertices=vertices;mesh.RecalculateBounds();f.sharedMesh=SaveMesh(mesh,"QuayRock_"+(garden?"Garden_":"Clock_")+f.name);var collider=f.GetComponent<MeshCollider>();if(collider!=null)collider.sharedMesh=f.sharedMesh;
   }
  }
  public static void NaturalSurfaces()
  {
   visualAssets=true;gameplayAssets=true;desktopAssets=releaseAssets=false;
   Texture2D Texture(string name,bool normal,bool roof)
   {
    string path="Assets/CloudRail/Generated/Visual/"+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(saved!=null)return saved;
    const int n=256;var tex=new Texture2D(n,n,TextureFormat.RGBA32,true,normal);tex.name=name;var colors=new Color[n*n];
    float Height(float x,float y)=>Mathf.PerlinNoise(x*.19f+11,y*.19f+32)*.7f+Mathf.PerlinNoise(x*.63f,y*.63f)*.3f;
    for(int y=0;y<n;y++)for(int x=0;x<n;x++){float value=Height(x,y);if(normal){float dx=(Height(x+1,y)-Height(x-1,y))*.5f,dy=(Height(x,y+1)-Height(x,y-1))*.5f;var v=new Vector3(-dx,-dy,1).normalized;colors[y*n+x]=new Color(v.x*.5f+.5f,v.y*.5f+.5f,v.z*.5f+.5f,1);}else{float tile=roof&&((y%32<2)||((x+(y/32%2)*16)%32<1))?.88f:1;float c=(.94f+value*.06f)*tile;colors[y*n+x]=new Color(c,c,c,1);}}
    tex.SetPixels(colors);tex.wrapMode=TextureWrapMode.Repeat;tex.anisoLevel=4;tex.Apply(true,false);AssetDatabase.CreateAsset(tex,path);return tex;
   }
   var stone=Texture("Fine limestone grain",false,false);var roof=Texture("Glazed slate courses",false,true);var bump=Texture("Soft stone normal",true,false);
   foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/CloudRail/Generated/Visual/Materials"}))
   {
    var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));if(m.shader.name!="Universal Render Pipeline/Lit")continue;
    bool isRoof=m.name=="Roof"||m.name=="RoofLight"||m.name=="RoofDark";bool isStone=m.name=="Ivory"||m.name=="Porcelain"||m.name=="Limestone"||m.name.StartsWith("Rock");
    if(!isRoof&&!isStone)continue;m.SetTexture("_BaseMap",isRoof?roof:stone);m.SetTexture("_BumpMap",bump);m.SetFloat("_BumpScale",isRoof?.28f:.18f);m.EnableKeyword("_NORMALMAP");EditorUtility.SetDirty(m);
   }
   var game=Object.FindAnyObjectByType<JourneyGame>();var world=game.transform.parent;int index=0;
   foreach(string name in new[]{"15  Cloudrise terraced city","10  Garden island","11  Starclock island"})
   foreach(var f in world.Find(name).GetComponentsInChildren<MeshFilter>())
   {
    var src=f.sharedMesh;if(src.name.StartsWith("NaturalSurface_"))continue;var mesh=Object.Instantiate(src);var verts=mesh.vertices;var normals=mesh.normals;var uv=new Vector2[verts.Length];
    for(int i=0;i<uv.Length;i++){var v=verts[i];var n=normals[i];uv[i]=Mathf.Abs(n.y)>.55f?new Vector2(v.x,v.z)*.25f:Mathf.Abs(n.x)>Mathf.Abs(n.z)?new Vector2(v.z,v.y)*.25f:new Vector2(v.x,v.y)*.25f;}
    mesh.uv=uv;mesh.RecalculateTangents();f.sharedMesh=SaveMesh(mesh,"NaturalSurface_"+index++);var collider=f.GetComponent<MeshCollider>();if(collider!=null)collider.sharedMesh=f.sharedMesh;
   }
   AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
  }
  [MenuItem("Cloud Rail/Natural Clouds And Scenic Railway")]
  public static void NaturalWorld()
  {
   if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");
   visualAssets=true;gameplayAssets=true;desktopAssets=releaseAssets=false;serial=150000;rng=new System.Random(774);shapes.Clear();mats.Clear();
   foreach(string id in AssetDatabase.FindAssets("t:Material",new[]{Gen+"/Materials"})){var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(id));mats[m.name]=m;}
   var temp=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=temp.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(temp);temp=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere=temp.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(temp);
   var game=Object.FindAnyObjectByType<JourneyGame>();var root=game.transform.parent;var path=game.train.rig.path;
   float h=path.railHeight;var p=new List<Vector3>{new Vector3(0,h,-35)};
   Straight(p,new Vector3(20,h,-35));
   Curve(p,new Vector3(27.18f,h,-35),new Vector3(33.31f,3.4f,-32.46f),new Vector3(38.384f,3.4f,-27.384f));
   Curve(p,new Vector3(43.46f,3.4f,-22.31f),new Vector3(46,h,-16.18f),new Vector3(46,h,-9));
   Straight(p,new Vector3(46,h,12));
   Curve(p,new Vector3(46,h,18.35f),new Vector3(43.75f,3.3f,23.75f),new Vector3(39.263f,3.3f,28.263f));
   Curve(p,new Vector3(34.77f,3.3f,32.75f),new Vector3(29.35f,h,35),new Vector3(23,h,35));
   Straight(p,new Vector3(-12,h,35));
   Curve(p,new Vector3(-29,h,35),new Vector3(-49,7,34),new Vector3(-57,7,20));
   Curve(p,new Vector3(-65,7,6),new Vector3(-64,3,-3),new Vector3(-57,3,-16));
   Curve(p,new Vector3(-49,1,-30),new Vector3(-36,h,-35),new Vector3(-20,h,-35));
   Straight(p,new Vector3(0,h,-35));path.scenicBezier=p.ToArray();path.Bake();EditorUtility.SetDirty(path);
   var old=root.Find("14  Railway unfolding through the sky");if(old!=null)Object.DestroyImmediate(old.gameObject);
   var skyway=BuildUnfoldingRail(root,path);skyway.voyage=game.voyage;skyway.lead=49;skyway.unfoldLength=10;skyway.trail=98;game.voyage.skyway=skyway;game.voyage.Speed=6;game.train.Initialize(0);
   // Side platforms are trimmed inside the loading gauge. Station rail segments remain level.
   TrimPlatform(root.Find("10  Garden island/Flower garden"),true);TrimPlatform(root.Find("11  Starclock island/Starclock architecture"),false);
   ClearQuay(root.Find("10  Garden island/Garden floating island"),true);ClearQuay(root.Find("11  Starclock island/Starclock floating island"),false);
   var surfaces=game.transform.Find("Walkable surfaces and boundaries");
   foreach(Transform t in surfaces)
   {
    var box=t.GetComponent<BoxCollider>();if(box==null)continue;
    if(t.name=="Garden platform"){t.position=new Vector3(42.8f,.49f,0);box.size=new Vector3(2.1f,.98f,17.2f);}
    if(t.name=="Starclock platform"){t.position=new Vector3(0,.49f,31.8f);box.size=new Vector3(17.2f,.98f,2.1f);}
   }
   // Remove the disconnected puff models; the density layer below covers the entire archipelago.
   foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))foreach(var material in renderer.sharedMaterials)if(material!=null&&material.name=="Cloud")renderer.enabled=false;
   var cloudOld=root.Find("18  Continuous volumetric cloud sea");if(cloudOld!=null)Object.DestroyImmediate(cloudOld.gameObject);
   var cloud=Group("18  Continuous volumetric cloud sea",root);
   const string noisePath="Assets/CloudRail/Generated/Visual/CloudDensityNoise.asset";var noise=AssetDatabase.LoadAssetAtPath<Texture3D>(noisePath);
   if(noise==null){const int n=64;noise=new Texture3D(n,n,n,TextureFormat.RGBA32,false);noise.name="Seamless 3D cloud density";var pixels=new Color32[n*n*n];var random=new System.Random(1727);for(int i=0;i<pixels.Length;i++){byte v=(byte)random.Next(10,246);pixels[i]=new Color32(v,v,v,255);}noise.SetPixels32(pixels);noise.wrapMode=TextureWrapMode.Repeat;noise.filterMode=FilterMode.Trilinear;noise.Apply(false,false);AssetDatabase.CreateAsset(noise,noisePath);}
   const string matPath="Assets/CloudRail/Generated/Visual/Materials/VolumetricCloudSea.mat";var cm=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(cm==null){cm=new Material(Shader.Find("CloudRail/Continuous Cloud Sea"));AssetDatabase.CreateAsset(cm,matPath);}cm.SetTexture("_Noise",noise);cm.SetFloat("_Density",1.5f);cm.SetColor("_Light",new Color(1,.97f,.92f));cm.SetColor("_Shade",new Color(.61f,.7f,.82f));EditorUtility.SetDirty(cm);
   var vtx=new[]{new Vector3(-2500,-55,-2500),new Vector3(-2500,-55,2500),new Vector3(2500,-55,2500),new Vector3(2500,-55,-2500)};var surface=new Mesh();surface.vertices=vtx;surface.triangles=new[]{0,1,2,0,2,3};surface.RecalculateBounds();var shape=Shape("Cloud volume raymarch surface",SaveMesh(surface,"ContinuousCloudSea"),cloud,Vector3.zero,Vector3.one,cm);shape.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
   var cameraData=Camera.main.GetUniversalAdditionalCameraData();cameraData.requiresDepthTexture=true;Camera.main.farClipPlane=5000;DynamicGI.UpdateEnvironment();
   // Keep the fantasy architecture, but render stone, paint, metal and glass with restrained physical response.
   foreach(var m in mats.Values)
   {
    if(m.shader.name!="Universal Render Pipeline/Lit")continue;
    string name=m.name;float metal=name=="Rail"?.65f:name=="Brass"?.7f:name=="Engine"?.3f:0;
    float smooth=name.Contains("Window")?.68f:name=="Rail"?.55f:name=="Brass"?.48f:name.StartsWith("Rock")?.1f:name.StartsWith("Grass")?.08f:name.StartsWith("Roof")?.3f:.18f;
    m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);
   }
   RenderSettings.ambientSkyColor=new Color(.53f,.64f,.78f);RenderSettings.ambientEquatorColor=new Color(.38f,.46f,.57f);RenderSettings.ambientGroundColor=new Color(.27f,.32f,.39f);
   RenderSettings.sun.intensity=1.65f;RenderSettings.sun.color=new Color(1,.94f,.83f);RenderSettings.sun.shadowStrength=.82f;RenderSettings.sun.transform.rotation=Quaternion.Euler(37,-32,0);
   RenderSettings.fog=true;RenderSettings.fogDensity=.0024f;RenderSettings.fogColor=new Color(.77f,.84f,.92f);
   var profile=Object.FindAnyObjectByType<Volume>().sharedProfile;profile.TryGet<ColorAdjustments>(out var grade);grade.saturation.Override(-6);grade.contrast.Override(9);grade.postExposure.Override(.15f);EditorUtility.SetDirty(profile);
   game.world.waterfallOrigin.position=new Vector3(-19,-11,-31);NaturalSurfaces();AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
  }
 }
}
