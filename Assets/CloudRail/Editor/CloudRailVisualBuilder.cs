using System;
using System.Collections.Generic;
using System.IO;
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
        public const string VisualScenePath="Assets/Scenes/CloudRail_Visual.unity";
        static Mesh BeveledBox(Vector3 size)
        {
            string key="SoftBox_"+size.x.ToString("F3")+"_"+size.y.ToString("F3")+"_"+size.z.ToString("F3");
            if(shapes.TryGetValue(key,out var cached))return cached;
            Vector3 half=size*.5f;float bevel=Mathf.Min(.13f,Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.19f);Vector3 inner=half-Vector3.one*bevel;
            var verts=new List<Vector3>();var tris=new List<int>();
            for(int face=0;face<6;face++)
            {
                int axis=face/2;float sign=face%2==0?1:-1;int u=(axis+1)%3,v=(axis+2)%3;
                float[] us={-half[u],-inner[u],inner[u],half[u]},vs={-half[v],-inner[v],inner[v],half[v]};
                for(int a=0;a<3;a++)for(int b=0;b<3;b++)
                {
                    Vector3[] points=new Vector3[4];int[] aa={a,a+1,a+1,a},bb={b,b,b+1,b+1};
                    for(int k=0;k<4;k++)
                    {
                        var p=Vector3.zero;p[axis]=half[axis]*sign;p[u]=us[aa[k]];p[v]=vs[bb[k]];
                        var c=new Vector3(Mathf.Clamp(p.x,-inner.x,inner.x),Mathf.Clamp(p.y,-inner.y,inner.y),Mathf.Clamp(p.z,-inner.z,inner.z));points[k]=c+(p-c).normalized*bevel;
                    }
                    if(sign>0){Tri(verts,tris,points[0],points[1],points[2]);Tri(verts,tris,points[0],points[2],points[3]);}
                    else{Tri(verts,tris,points[0],points[2],points[1]);Tri(verts,tris,points[0],points[3],points[2]);}
                }
            }
            var mesh=new Mesh();mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh=SaveMesh(mesh,key);shapes[key]=mesh;return mesh;
        }
        static void SoftRoof(Transform p,Vector3 center,float width,float depth,float height,string name)
        {
            var verts=new List<Vector3>();var tris=new List<int>();bool cab=name.Contains("Cab");int count=18;
            for(int i=0;i<count;i++)
            {
                float a=-1+2f*i/count,b=-1+2f*(i+1)/count;
                float ya=height*Mathf.Sqrt(Mathf.Max(0,1-a*a)),yb=height*Mathf.Sqrt(Mathf.Max(0,1-b*b));
                Vector3 A=cab?new Vector3(a*width*.5f,ya,-depth*.5f):new Vector3(-width*.5f,ya,a*depth*.5f);
                Vector3 B=cab?new Vector3(b*width*.5f,yb,-depth*.5f):new Vector3(-width*.5f,yb,b*depth*.5f);
                Vector3 delta=cab?Vector3.forward*depth:Vector3.right*width;
                if(cab){Tri(verts,tris,A,B+delta,B);Tri(verts,tris,A,A+delta,B+delta);}
                else{Tri(verts,tris,A,B,B+delta);Tri(verts,tris,A,B+delta,A+delta);}
                Tri(verts,tris,A,new Vector3(A.x,-.1f,A.z),B);Tri(verts,tris,B,new Vector3(A.x,-.1f,A.z),new Vector3(B.x,-.1f,B.z));
                Tri(verts,tris,A+delta,B+delta,new Vector3(A.x,-.1f,A.z)+delta);Tri(verts,tris,B+delta,new Vector3(B.x,-.1f,B.z)+delta,new Vector3(A.x,-.1f,A.z)+delta);
            }
            var mesh=new Mesh();mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();Shape(name,SaveMesh(mesh,"CurvedRoof_"+serial++),p,center,Vector3.one,mats["Roof"]);
            // The bright, beveled eave gives the blue silhouette a clean illustrated edge.
            Box(p,"Porcelain roof eave",center-Vector3.up*.08f,new Vector3(width+.15f,.15f,depth+.15f),"Porcelain");
        }
        static void AnimePalette()
        {
            Palette();
            string[] names={"Ivory","Limestone","Porcelain","Roof","RoofLight","RoofDark","Grass","GrassLight","GrassDark","RockA","RockB","RockC","RockD","RockE","Pink","PinkLight","Leaf","LeafLight","Cloud","Engine","EngineDark","Window","WindowLight","Brass","Rail","Sleepers","Trunk","Timber","Wheel","Lamp"};
            string[] colors={"#FFF3D8","#E9D5AD","#FFFDF0","#25B8D5","#73E0EB","#287AAB","#8DD94D","#BAEA70","#5CBC73","#82ACCF","#B3D7E6","#7195C4","#C7DEF0","#637FAF","#F38BBC","#FFBED9","#5ECF9B","#9BE3A6","#FFFFFF","#269CC4","#255C94","#3778B5","#8CE7F2","#F6C75F","#577DA6","#B98B5C","#B78F7A","#B78F65","#385D87","#FFE5A3"};
            for(int i=0;i<names.Length;i++){ColorUtility.TryParseHtmlString(colors[i],out var color);mats[names[i]].SetColor("_BaseColor",color);mats[names[i]].SetColor("_ShadeTint",new Color(.62f,.74f,1));mats[names[i]].SetColor("_EmissionColor",names[i]=="Lamp"?new Color(.8f,.38f,.04f):Color.black);mats[names[i]].SetFloat("_Glass",names[i].StartsWith("Window")?1:0);EditorUtility.SetDirty(mats[names[i]]);}
        }
        [MenuItem("Cloud Rail/Build Anime Cloud Voyage")]
        public static void BuildVisual()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
            if(File.Exists(VisualScenePath))throw new InvalidOperationException("Visual scene exists; edit it in place.");
            var current=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(current.path!=ReleaseScenePath)EditorSceneManager.OpenScene(ReleaseScenePath);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),VisualScenePath,true);EditorSceneManager.OpenScene(VisualScenePath);
            visualAssets=true;desktopAssets=false;releaseAssets=false;gameplayAssets=true;serial=50000;rng=new System.Random(20261023);shapes.Clear();mats.Clear();wheels.Clear();
            Folder(Gen+"/Meshes");Folder(Gen+"/Materials");
            var temp=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=temp.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(temp);temp=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere=temp.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(temp);
            AnimePalette();LoadJourneyFont();var game=Object.FindAnyObjectByType<JourneyGame>();var root=game.transform.parent;
            // Refresh hero architecture and train geometry instead of grading the old block meshes.
            foreach(string name in new[]{"02  Cloudrise station","03  Gardens and promenade","10  Garden island","11  Starclock island"})Object.DestroyImmediate(root.Find(name).gameObject);
            var station=Group("02  Cloudrise station",root);station.position=Vector3.back*17;Station(station);
            var gardens=Group("03  Gardens and promenade",root);gardens.position=Vector3.back*17;Gardens(gardens);
            game.stations[2].signalParts=new[]{game.stations[2].signalParts[0]};GardenIsland(root);ClockIsland(root,game.stations[2]);
            foreach(string name in new[]{"10  Garden island","11  Starclock island"})foreach(var filter in root.Find(name).GetComponentsInChildren<MeshFilter>())if(filter.name=="Grass")filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
            var rig=game.train.rig;foreach(Transform part in new[]{rig.locomotive,rig.carriage})while(part.childCount>0)Object.DestroyImmediate(part.GetChild(0).gameObject);
            var rods=new List<Transform>();rig.chimney=Engine(rig.locomotive,rods);Coach(rig.carriage);rig.steam=Steam(rig.chimney);rig.wheels=wheels.ToArray();rig.connectingRods=rods.ToArray();game.train.Initialize(0);
            // Existing decorative geometry shares the new illustrated palette, too.
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials=renderer.sharedMaterials;for(int i=0;i<materials.Length;i++)if(materials[i]!=null&&mats.TryGetValue(materials[i].name,out var replacement))materials[i]=replacement;
                renderer.sharedMaterials=materials;
            }
            var world=game.world;Object.DestroyImmediate(root.Find("04  Three station railway/Continuous limestone arch viaduct").gameObject);
            var guide=root.Find("12  Living cloud archipelago/Rail guide fixtures");if(guide!=null)guide.gameObject.SetActive(false);
            foreach(var renderer in world.firstRoute)if(renderer!=null)renderer.gameObject.SetActive(false);
            foreach(var renderer in world.secondRoute)if(renderer!=null)renderer.gameObject.SetActive(false);
            world.firstRoute=Array.Empty<Renderer>();world.secondRoute=Array.Empty<Renderer>();
            var voyage=game.gameObject.AddComponent<SkyVoyage>();voyage.game=game;game.voyage=voyage;
            var skyway=BuildUnfoldingRail(root,rig.path);skyway.voyage=voyage;voyage.skyway=skyway;
            BuildVoyageOverlay(root,voyage);VisualLighting(root);skyway.lead=22;skyway.unfoldLength=6;
            // Keep the clouds below the railway so the wave of track remains legible.
            foreach(var cloud in Object.FindAnyObjectByType<SkyAmbience>().clouds)if(cloud.position.y>8)cloud.position+=Vector3.up*5;
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(VisualScenePath,true)};
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
        static UnfoldingSkyway BuildUnfoldingRail(Transform root,SkyRailPath path)
        {
            var parent=Group("14  Railway unfolding through the sky",root);var reveal=parent.gameObject.AddComponent<UnfoldingSkyway>();reveal.path=path;
            int count=path.HasScenicRoute?Mathf.CeilToInt(path.Length/3.5f):72;float step=path.Length/count;reveal.foundations=new Transform[count];reveal.sleepers=new Transform[count];reveal.rails=new Transform[count];reveal.distances=new float[count];
            for(int i=0;i<count;i++)
            {
                float d=(i+.5f)*step;path.Evaluate(d,out var at,out var q);at-=q*Vector3.up*path.railHeight;
                var chunk=Group("Skyway segment "+i.ToString("D2"),parent);chunk.SetPositionAndRotation(at,q);
                var stone=Group("01  Bridge rises",chunk);var ties=Group("02  Sleepers settle",chunk);var rails=Group("03  Rails join",chunk);
                Shape("Sculpted arch",SaveMesh(ArchMesh(step+.06f,2.65f),"VoyageArch_"+i),stone,Vector3.zero,new Vector3(1,.85f,1),mats["Ivory"],Quaternion.Euler(0,90,0));
                Box(stone,"Bridge deck",new Vector3(0,.42f,0),new Vector3(2.7f,.17f,step+.06f),"Porcelain");
                Box(stone,"Capped pier",new Vector3(0,-3.7f,-step*.5f),new Vector3(2.76f,1.4f,.4f),"Limestone");
                for(int side=-1;side<=1;side+=2)
                {
                    for(int k=0;k<16;k++)
                    {
                        float a=Mathf.Lerp(-step*.5f,step*.5f,k/16f),b=Mathf.Lerp(-step*.5f,step*.5f,(k+1)/16f);
                        float ya=(-3.8f+3.6f*Mathf.Sqrt(Mathf.Max(0,1-a*a/(step*step*.25f))))*.85f;
                        float yb=(-3.8f+3.6f*Mathf.Sqrt(Mathf.Max(0,1-b*b/(step*step*.25f))))*.85f;
                        Beam(stone,"Golden arch edging",new Vector3(side*1.35f,ya,-a),new Vector3(side*1.35f,yb,-b),.035f,"Brass",6);
                    }
                    Box(stone,"Blue inset cornice",new Vector3(side*1.35f,.28f,0),new Vector3(.1f,.14f,step+.05f),"RoofLight");
                }
                for(float f=-step*.5f+.15f;f<step*.5f;f+=.58f)Box(ties,"Polished sleeper",new Vector3(0,.53f,f),new Vector3(2.22f,.16f,.24f),"Sleepers");
                for(int k=0;k<8;k++)
                {
                    float da=i*step+k*step/8,db=i*step+(k+1)*step/8;path.Evaluate(da,out var a,out var qa);path.Evaluate(db,out var b,out var qb);
                    for(int side=-1;side<=1;side+=2)
                    {
                        Vector3 A=chunk.InverseTransformPoint(a+qa*Vector3.right*(side*.78f)),B=chunk.InverseTransformPoint(b+qb*Vector3.right*(side*.78f));A.y=B.y=.79f;
                        Box(rails,"Continuous blue steel rail",(A+B)*.5f,new Vector3(.13f,.23f,Vector3.Distance(A,B)+.022f),"Rail",Quaternion.LookRotation(B-A));
                    }
                }
                Combine(stone,"SkywayStone_"+i);Combine(ties,"SkywayTies_"+i);Combine(rails,"SkywayRails_"+i);
                reveal.foundations[i]=stone;reveal.sleepers[i]=ties;reveal.rails[i]=rails;reveal.distances[i]=d;
            }
            return reveal;
        }
        static void BuildVoyageOverlay(Transform root,SkyVoyage voyage)
        {
            var go=new GameObject("A quiet frame for watching",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));go.transform.SetParent(root,false);
            go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;go.GetComponent<Canvas>().sortingOrder=20;
            var scaler=go.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
            var title=Rect("Voyage title",go.transform,new Vector2(0,1),new Vector2(0,1),new Vector2(32,-28),new Vector2(400,90));
            Label("Title",title,"云上列车",new Vector2(0,12),new Vector2(370,52),34,new Color(.09f,.32f,.5f));
            voyage.caption=Label("Subtitle",title,"随光铺开的云端铁道",new Vector2(0,-25),new Vector2(370,32),20,new Color(.19f,.47f,.58f));
            var hint=Rect("Desktop watching keys",go.transform,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,24),new Vector2(1100,40));
            Label("Keys",hint,"C 切换机位   ·   右键环绕 / 中键拖动或滚轮调距   ·   空格暂停   ·   Tab 切换探索",Vector2.zero,new Vector2(1080,38),22,new Color(.08f,.29f,.44f),TextAlignmentOptions.Center);
            voyage.overlay=go;
        }
        static void VisualLighting(Transform root)
        {
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.0012f;RenderSettings.fogColor=new Color(.64f,.87f,1);
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.62f,.8f,1);RenderSettings.ambientEquatorColor=new Color(.61f,.72f,.94f);RenderSettings.ambientGroundColor=new Color(.42f,.54f,.8f);
            RenderSettings.sun.color=new Color(1,.95f,.83f);RenderSettings.sun.intensity=1.35f;RenderSettings.sun.shadowStrength=.65f;RenderSettings.sun.transform.rotation=Quaternion.Euler(48,-35,0);
            var sky=Object.Instantiate(RenderSettings.skybox);sky.name="Clear azure illustrated sky";sky.SetColor("_Top",new Color(.15f,.56f,.96f));sky.SetColor("_Horizon",new Color(.64f,.87f,1));sky.SetColor("_Bottom",new Color(.37f,.71f,.96f));AssetDatabase.CreateAsset(sky,Gen+"/Materials/AnimeSky.mat");RenderSettings.skybox=sky;
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,Gen+"/AnimeDaylight.asset");
            var bloom=profile.Add<Bloom>(true);bloom.threshold.Override(1.25f);bloom.intensity.Override(.12f);bloom.scatter.Override(.5f);AssetDatabase.AddObjectToAsset(bloom,profile);
            var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.None);AssetDatabase.AddObjectToAsset(tone,profile);
            var color=profile.Add<ColorAdjustments>(true);color.saturation.Override(8);color.contrast.Override(5);AssetDatabase.AddObjectToAsset(color,profile);
            Object.FindAnyObjectByType<Volume>().sharedProfile=profile;
            Camera.main.fieldOfView=43;Camera.main.backgroundColor=new Color(.5f,.8f,1);
        }
    }
}
