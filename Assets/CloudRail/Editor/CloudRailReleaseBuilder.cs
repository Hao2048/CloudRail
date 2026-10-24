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
        public const string ReleaseScenePath="Assets/Scenes/CloudRail_Release.unity";
        [MenuItem("Cloud Rail/Build Content Milestone")]
        public static void BuildRelease()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the current scene first.");
            if(File.Exists(ReleaseScenePath))throw new InvalidOperationException("The release scene already exists; edit it in place.");
            EditorSceneManager.OpenScene(JourneyScenePath);EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ReleaseScenePath);
            visualAssets=false;desktopAssets=false;releaseAssets=true;gameplayAssets=true;serial=30000;rng=new System.Random(251027);shapes.Clear();mats.Clear();
            Folder(Gen+"/Meshes");Folder(Gen+"/Materials");
            var temp=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=temp.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(temp);
            temp=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere=temp.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(temp);
            Palette();LoadJourneyFont();
            var game=Object.FindAnyObjectByType<JourneyGame>();game.saveFile="cloudrail_journey_v1.json";
            var root=Group("12  Living cloud archipelago",GameObject.Find("CLOUDLINE  |  Cloudrise Station").transform);
            var world=root.gameObject.AddComponent<JourneyWorld>();world.game=game;game.world=world;
            AddIslandArt(root);AddJourneySignals(root,world,game);AddReleaseInterface(game.hud);
            AddCameraGuards(root);
            var ambient=Object.FindAnyObjectByType<SkyAmbience>();
            var backdrop=Group("Far cloud sanctuaries",root);var islands=new List<Transform>();
            Vector3[] centers={new Vector3(-50,-6,12),new Vector3(58,-8,27),new Vector3(-30,-3,-48),new Vector3(21,-7,55)};
            for(int i=0;i<centers.Length;i++)
            {
                var g=Group("Satellite garden "+i,backdrop);g.position=centers[i];Island(g,Vector3.zero,4.5f+i*.45f,3.2f+i*.3f,8,"Distant rock",12);
                Tree(g,new Vector3(0,0,0),.7f,i%2==0);Combine(g,"ReleaseSatellite_"+i);islands.Add(g);
            }
            ambient.floatingIslets=islands.ToArray();
            world.cloudBanks=Array.ConvertAll(ambient.clouds,t=>t.gameObject);
            world.petals=ReleaseParticles(root,"Falling garden petals",new Vector3(31.5f,4,2),new Color(1,.66f,.79f,.8f),7,7,4,.11f);
            world.waterMist=ReleaseParticles(root,"Waterfall spray",new Vector3(20.8f,-23.8f,-14.4f),new Color(.87f,.96f,1,.18f),5,3,2,1.4f);
            world.waterfallOrigin=Group("Spatial waterfall sound",root);world.waterfallOrigin.position=new Vector3(20.8f,-2,-14.4f);
            world.waterfallClip=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/Waterfall.wav");
            if(world.waterfallClip==null)throw new InvalidOperationException("Generate Waterfall.wav before building.");
            var profile=Object.Instantiate(Object.FindAnyObjectByType<Volume>().sharedProfile);profile.name="Release morning";
            // Persist sub-assets together so the profile survives editor reloads and player builds.
            AssetDatabase.CreateAsset(profile,Gen+"/ReleaseMorning.asset");
            foreach(var c in profile.components)if(!AssetDatabase.Contains(c))AssetDatabase.AddObjectToAsset(c,profile);
            if(!profile.TryGet<ColorAdjustments>(out var colour)){colour=profile.Add<ColorAdjustments>(true);AssetDatabase.AddObjectToAsset(colour,profile);}
            colour.saturation.Override(9);colour.contrast.Override(5);colour.postExposure.Override(.1f);
            Object.FindAnyObjectByType<Volume>().sharedProfile=profile;
            var urp=UniversalRenderPipeline.asset;if(urp==null||!urp.supportsHDR)throw new InvalidOperationException("HDR URP is required.");
            var cameraData=Camera.main.GetComponent<UniversalAdditionalCameraData>();cameraData.renderPostProcessing=true;cameraData.volumeLayerMask|=1;
            game.gameObject.AddComponent<JourneyDiagnostics>();
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ReleaseScenePath,true)};
            PlayerSettings.defaultScreenWidth=1920;PlayerSettings.defaultScreenHeight=1080;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("Content milestone saved: living gardens, restored islands, guidance, memorial, help, stable camera, waterfall audio.");
        }
        static void AddCameraGuards(Transform root)
        {
            var guards=Group("Camera-only foliage and roof volumes",root);
            foreach(var t in root.parent.GetComponentsInChildren<Transform>())
                if(t.name=="Blossom tree"||t.name=="Sage tree")
                {
                    var go=new GameObject("Tree crown camera volume");go.transform.SetParent(guards,false);go.transform.position=t.TransformPoint(Vector3.up*2.8f);go.layer=8;
                    var collider=go.AddComponent<SphereCollider>();collider.radius=1.55f*t.lossyScale.x;collider.isTrigger=true;
                }
            foreach(var entry in new[]{(new Vector3(-1,6,-22.5f),new Vector3(12,3,6)),(new Vector3(0,8.5f,21),new Vector3(5.2f,2.6f,5.2f)),(new Vector3(31.5f,3.6f,10.35f),new Vector3(6.8f,.5f,2.5f))})
            {
                var go=new GameObject("Roof camera volume");go.transform.SetParent(guards,false);go.transform.position=entry.Item1;go.layer=8;var collider=go.AddComponent<BoxCollider>();collider.size=entry.Item2;collider.isTrigger=true;
            }
        }
        public static void PolishReleaseCamera()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
            var world=Object.FindAnyObjectByType<JourneyWorld>();
            foreach(var label in world.GetComponentsInChildren<TextMeshPro>())
            {
                if(label.GetComponent<JourneyFacingLabel>()==null)label.gameObject.AddComponent<JourneyFacingLabel>();label.fontSize=3.1f;
            }
            var parent=world.transform;
            if(parent.Find("Camera-only foliage and roof volumes")==null)AddCameraGuards(parent);
            var hud=world.game.hud;
            foreach(var toggle in new[]{hud.stableToggle,hud.qualityToggle})toggle.transform.Find("Label").GetComponent<TMP_Text>().raycastTarget=true;
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
        static void FlowerBed(Transform p,Vector3 center,float radius)
        {
            Cylinder(p,"Raised flower border",center+Vector3.up*.12f,radius,.24f,"Limestone",18);
            Cylinder(p,"Flower bed foliage",center+Vector3.up*.25f,radius*.9f,.12f,"Leaf",18);
            for(int i=0;i<18;i++)
            {
                float a=i*2.39996f,r=Mathf.Sqrt((i+.5f)/18)*radius*.8f;var at=center+new Vector3(Mathf.Cos(a)*r,.38f,Mathf.Sin(a)*r);
                Ball(p,"Blossom",at,new Vector3(.22f,.14f,.22f),i%3==0?"Porcelain":"PinkLight");
                Ball(p,"Blossom heart",at+Vector3.up*.07f,Vector3.one*.07f,"Brass");
            }
        }
        static void AddIslandArt(Transform root)
        {
            var main=Group("Cloudrise flower borders",root);
            FlowerBed(main,new Vector3(-11,0,-27),1.1f);FlowerBed(main,new Vector3(12,0,-23),1);
            Bench(main,new Vector3(-8,0,-22),35);Combine(main,"ReleaseCloudrise");
            var garden=Group("Garden pergola and parterres",root);
            foreach(var pt in new[]{new Vector3(36,0,-8),new Vector3(29,0,-7),new Vector3(40,0,6),new Vector3(24,0,8)})FlowerBed(garden,pt,1.15f);
            foreach(float x in new[]{28.5f,34.5f})foreach(float z in new[]{9.7f,11})
            {
                Cylinder(garden,"Pergola stone foot",new Vector3(x,.12f,z),.25f,.24f,"Limestone",12);
                Cylinder(garden,"Pergola column",new Vector3(x,1.7f,z),.1f,3.3f,"Ivory",12);
            }
            foreach(float z in new[]{9.7f,11})Box(garden,"Pergola cornice",new Vector3(31.5f,3.4f,z),new Vector3(6.7f,.2f,.22f),"Ivory");
            for(int i=0;i<10;i++)Box(garden,"Blue pergola slat",new Vector3(28.3f+i*.72f,3.6f,10.35f),new Vector3(.12f,.16f,2.3f),"RoofLight");
            for(int i=0;i<14;i++)Ball(garden,"Pergola hanging blossom",new Vector3(28.5f+i*.46f,3.25f,9.7f),new Vector3(.55f,.55f,.5f),i%2==0?"Pink":"Leaf");
            Bench(garden,new Vector3(31.5f,0,10.8f),0);Bench(garden,new Vector3(40,0,-6.5f),-90);
            for(int i=0;i<4;i++)
            {
                float a=i*Mathf.PI*.5f;
                for(int j=0;j<8;j++)
                {
                    float t=j/8f,u=(j+1)/8f;
                    Vector3 A=new Vector3(32,1.8f,0)+new Vector3(Mathf.Cos(a)*t*1.35f,Mathf.Sin(t*Mathf.PI)*.65f-t*.8f,Mathf.Sin(a)*t*1.35f);
                    Vector3 B=new Vector3(32,1.8f,0)+new Vector3(Mathf.Cos(a)*u*1.35f,Mathf.Sin(u*Mathf.PI)*.65f-u*.8f,Mathf.Sin(a)*u*1.35f);
                    Beam(garden,"Fountain water arc",A,B,.035f,"Water",6);
                }
            }
            Combine(garden,"ReleaseGarden");
            var clock=Group("Clock observatory details",root);
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6;Vector3 at=new Vector3(Mathf.Sin(a)*.73f,3.2f+Mathf.Cos(a)*.73f,22.99f);
                Box(clock,"Clock hour marker",at,new Vector3(.055f,.15f,.035f),"Brass",Quaternion.Euler(0,0,-i*30));
            }
            Cylinder(clock,"Clock north gold face",new Vector3(0,3.2f,22.83f),1,.14f,"Brass",32,1,Quaternion.Euler(90,0,0));
            Cylinder(clock,"Clock north dial",new Vector3(0,3.2f,22.94f),.88f,.04f,"Porcelain",32,1,Quaternion.Euler(90,0,0));
            Beam(clock,"North minute hand",new Vector3(0,3.2f,23),new Vector3(.25f,3.83f,23),.04f,"RoofDark");
            Beam(clock,"North hour hand",new Vector3(0,3.2f,23),new Vector3(-.4f,3.35f,23),.055f,"RoofDark");
            foreach(float side in new[]{-1f,1f})
            {
                Window(clock,new Vector3(side*.8f,5.6f,22.81f),.6f,1.3f);
                FlowerBed(clock,new Vector3(side*8.5f,0,28),1.1f);
            }
            for(int i=0;i<10;i++)
            {
                float a=Mathf.PI+i*Mathf.PI/9;
                Vector3 at=new Vector3(Mathf.Cos(a)*5.5f,0,21+Mathf.Sin(a)*5.5f);
                Cylinder(clock,"Observatory baluster",at+Vector3.up*.6f,.1f,1.2f,"Ivory",10);
                Ball(clock,"Observatory star finial",at+Vector3.up*1.25f,Vector3.one*.22f,"Brass");
            }
            Combine(clock,"ReleaseClock");
        }
        static TMP_Text WorldLabel(Transform parent,string name,string text,Vector3 position,Vector3 outward,float width=4)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=position;
            var label=go.AddComponent<TextMeshPro>();label.font=journeyFont;label.fontSize=3.1f;label.text=text;label.color=Ink;label.alignment=TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta=new Vector2(width,1.1f);label.transform.rotation=Quaternion.LookRotation(-outward);label.gameObject.AddComponent<JourneyFacingLabel>();return label;
        }
        static void AddJourneySignals(Transform root,JourneyWorld world,JourneyGame game)
        {
            var fixtures=Group("Rail guide fixtures",root);var lamps=new[]{new List<Renderer>(),new List<Renderer>()};
            for(int route=0;route<2;route++)for(int i=0;i<30;i++)
            {
                float distance=game.train.rig.path.Length*(.75f+route*.25f+(i+.5f)/30*.25f);
                game.train.rig.path.Evaluate(distance,out var at,out var rotation);
                foreach(int side in new[]{-1,1})
                {
                    Vector3 point=at+rotation*Vector3.right*(1.35f*side);
                    Cylinder(fixtures,"Guide post",point+Vector3.up*.25f,.075f,.5f,"RoofDark",8);
                    var go=Ball(root,"Route "+route+" guiding starlight",point+Vector3.up*.58f,Vector3.one*.25f,"Lamp");lamps[route].Add(go.GetComponent<Renderer>());
                }
            }
            Combine(fixtures,"ReleaseGuidePosts");world.firstRoute=lamps[0].ToArray();world.secondRoute=lamps[1].ToArray();
            var gates=new[]{new List<Transform>(),new List<Transform>()};world.deviceLabels=new TMP_Text[3];
            for(int i=0;i<3;i++)
            {
                var station=game.stations[i];
                world.deviceLabels[i]=WorldLabel(root,"Device instruction "+i,"星光航线装置",station.device.position+Vector3.up*2.5f,station.outward);
                if(i==2)continue;
                foreach(int side in new[]{-1,1})
                {
                    var pivot=Group("Signal wing "+i,station.device);pivot.localPosition=new Vector3(side*.16f,1.3f,0);
                    Box(pivot,"Opening signal flag",new Vector3(side*.35f,.12f,0),new Vector3(.5f,.15f,.12f),"Brass");gates[i].Add(pivot);
                }
            }
            world.firstGate=gates[0].ToArray();world.secondGate=gates[1].ToArray();
            var memorial=Group("Nine star traveler memorial",root);memorial.position=new Vector3(-6,1,-30);
            Cylinder(memorial,"Memorial pedestal",Vector3.up*.15f,.65f,.3f,"Ivory",16);
            Cylinder(memorial,"Memorial stem",Vector3.up*1.1f,.1f,1.7f,"Brass",12);
            var bulbs=new List<Renderer>();
            for(int i=0;i<9;i++)
            {
                float a=i*Mathf.PI*2/9;var at=new Vector3(Mathf.Cos(a)*.65f,2.1f+Mathf.Sin(a)*.65f,0);
                Beam(memorial,"Memorial spoke",Vector3.up*2.1f,at,.035f,"Brass");
                bulbs.Add(Ball(memorial,"Persistent memory star",at,Vector3.one*.22f,"Lamp").GetComponent<Renderer>());
            }
            world.memorialLamps=bulbs.ToArray();world.memorialLabel=WorldLabel(root,"Memorial inscription","旅人纪念灯\n集齐九枚星光",new Vector3(-6,3.9f,-30),Vector3.back,4);
            var clockBulbs=new List<Renderer>();
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4;clockBulbs.Add(Ball(root,"Awakening constellation",new Vector3(Mathf.Cos(a)*2.2f,7.6f,21+Mathf.Sin(a)*2.2f),Vector3.one*.24f,"Lamp").GetComponent<Renderer>());
            }
            world.clockLights=clockBulbs.ToArray();var light=Group("Warm clock aura",root);light.position=new Vector3(0,6.5f,23);
            world.clockGlow=light.gameObject.AddComponent<Light>();world.clockGlow.color=new Color(1,.72f,.35f);world.clockGlow.range=10;world.clockGlow.intensity=0;world.clockGlow.shadows=LightShadows.None;
        }
        static ParticleSystem ReleaseParticles(Transform parent,string name,Vector3 at,Color colour,float rate,float radius,float life,float size)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=at;
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.startColor=colour;main.startSize=new ParticleSystem.MinMaxCurve(size*.6f,size);main.startLifetime=life;main.startSpeed=.15f;main.maxParticles=100;
            main.gravityModifier=name.Contains("petals")?.025f:-.015f;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=ps.emission;emission.rateOverTime=rate;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Circle;shape.radius=radius;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Generated/Materials/Steam.mat");
            var rotation=ps.rotationOverLifetime;rotation.enabled=true;rotation.z=new ParticleSystem.MinMaxCurve(-.5f,.5f);
            var fade=ps.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.15f),new GradientAlphaKey(0,1)});fade.color=gradient;
            ps.Play();return ps;
        }
        static UnityEngine.UI.Toggle ReleaseToggle(Transform parent,string name,string text,Vector2 pos)
        {
            var rect=Rect(name,parent,Vector2.one*.5f,Vector2.one*.5f,pos,new Vector2(490,42));
            var box=Rect("Checkbox",rect,Vector2.one*.5f,Vector2.one*.5f,new Vector2(-225,0),new Vector2(26,26));var bg=Image(box,new Color(.72f,.8f,.77f),true);
            var check=Rect("Checked",box,Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(16,16));var ink=Image(check,Teal);
            var toggle=rect.gameObject.AddComponent<UnityEngine.UI.Toggle>();toggle.targetGraphic=bg;toggle.graphic=ink;
            Label("Label",rect,text,new Vector2(25,0),new Vector2(430,40),23,Ink).raycastTarget=true;return toggle;
        }
        static void AddReleaseInterface(JourneyHUD hud)
        {
            var pause=hud.pausePanel.transform.Find("Content") as RectTransform;pause.sizeDelta=new Vector2(620,720);
            (pause.Find("Title") as RectTransform).anchoredPosition=new Vector2(0,300);
            string[] labels={"Music label","Effects label","Sensitivity label"};var sliders=new[]{hud.musicSlider,hud.volumeSlider,hud.sensitivitySlider};
            for(int i=0;i<3;i++){(pause.Find(labels[i]) as RectTransform).anchoredPosition=new Vector2(-175,205-i*60);(sliders[i].transform as RectTransform).anchoredPosition=new Vector2(66,205-i*60);}
            hud.stableToggle=ReleaseToggle(pause,"Stable camera","稳定镜头（减少晃动）",new Vector2(0,21));hud.stableToggle.isOn=true;
            hud.qualityToggle=ReleaseToggle(pause,"Light clouds","轻量云层（30 帧节能）",new Vector2(0,-34));hud.qualityToggle.isOn=false;
            (hud.resumeButton.transform as RectTransform).anchoredPosition=new Vector2(0,-112);
            hud.helpButton=Button("Help",pause,"操作与旅程指引",new Vector2(0,-184),new Vector2(360,54),out _);
            (hud.restartButton.transform as RectTransform).anchoredPosition=new Vector2(-135,-259);(hud.restartButton.transform as RectTransform).sizeDelta=new Vector2(240,52);
            hud.quitButton=Button("Exit",pause,"退出游戏",new Vector2(135,-259),new Vector2(240,52),out _);
            var help=Modal("Journey handbook",hud.transform,new Vector2(740,630));hud.helpPanel=help.parent.gameObject;
            Label("Title",help,"旅人的随身手册",new Vector2(0,247),new Vector2(650,65),35,Ink,TextAlignmentOptions.Center);
            Label("Controls",help,"WASD  行走     右键拖动  观察     滚轮  缩放\nE  装置交互 / 上下车     F  选择相邻目的地\nC  切换三种乘车镜头     Esc  暂停     H  操作帮助",new Vector2(0,126),new Vector2(640,140),24,Ink,TextAlignmentOptions.Left);
            Label("Route",help,"云起站：找到三枚星光，回月台激活装置。\n花园岛：累计六枚星光，开启通往钟塔的航线。\n星钟岛：沿步道抵达钟塔，按 E 点亮信标。\n星光不会消耗。可往返补齐九枚，点亮纪念灯。",new Vector2(0,-55),new Vector2(640,185),25,Ink);
            hud.closeHelpButton=Button("Close handbook",help,"回到旅程",new Vector2(0,-238),new Vector2(350,56),out _);
            hud.helpPanel.SetActive(false);
            hud.prompt.text="WASD 行走 · E 交互 · H 帮助 · Esc 暂停";
            var curtain=hud.curtain.transform;curtain.SetAsLastSibling();EditorUtility.SetDirty(hud);
        }
    }
}
