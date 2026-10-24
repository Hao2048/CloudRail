using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;
using Object = UnityEngine.Object;

namespace CloudRail.Editor
{
    public static partial class CloudRailBuilder
    {
        public const string JourneyScenePath = "Assets/Scenes/CloudRail_Journey.unity";
        static TMP_FontAsset journeyFont;
        static readonly Color Ink = new Color(.13f,.25f,.3f);
        static readonly Color Cream = new Color(.98f,.95f,.87f);
        static readonly Color Teal = new Color(.12f,.31f,.36f,.94f);

        [MenuItem("Cloud Rail/Build Playable Journey")]
        public static void BuildJourney()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before building the journey.");
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the active scene before building the journey.");
            // Open a saved copy, preserving the first milestone scene and all of its geometry assets.
            EditorSceneManager.OpenScene(ScenePath);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),JourneyScenePath);
            visualAssets=false;desktopAssets=false;releaseAssets=false;gameplayAssets=true;serial=10000;rng=new System.Random(251024);shapes.Clear();mats.Clear();
            Folder(Gen+"/Meshes");Folder(Gen+"/Materials");
            var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=primitive.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(primitive);
            primitive=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere=primitive.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(primitive);
            Palette();LoadJourneyFont();
            var world=GameObject.Find("CLOUDLINE  |  Cloudrise Station").transform;
            foreach(string name in new[]{"01  Floating island","02  Cloudrise station","03  Gardens and promenade","05  Waterfall"})
                world.Find(name).position+=Vector3.back*17;
            Object.DestroyImmediate(world.Find("04  Aerial railway").gameObject);
            Object.DestroyImmediate(world.Find("06  Distant archipelago").gameObject);
            world.GetComponent<SkyAmbience>().floatingIslets=Array.Empty<Transform>();
            var railway=Group("04  Three station railway",world);
            var path=railway.gameObject.AddComponent<SkyRailPath>();path.radiusX=46;path.radiusZ=35;path.Bake();Railway(railway,path);
            var rig=world.Find("08  Little cloud express").GetComponent<CloudTrain>();rig.enabled=false;rig.path=path;
            Camera.main.GetComponent<CloudRailCamera>().enabled=false;

            var systems=Group("09  Playable journey",world);
            var game=systems.gameObject.AddComponent<JourneyGame>();
            var train=rig.gameObject.AddComponent<JourneyTrain>();train.rig=rig;train.game=game;game.train=train;
            var stationRoot=Group("Stations and activators",systems);
            var stations=new JourneyStation[3];
            stations[0]=CreateJourneyStation(stationRoot,0,"云起站",new Vector3(4,1.04f,-30),new Vector3(5,1,-31.2f),Vector3.back);
            stations[1]=CreateJourneyStation(stationRoot,1,"花园岛",new Vector3(43.5f,1.04f,3),new Vector3(43,1,-1.5f),Vector3.right);
            stations[2]=CreateJourneyStation(stationRoot,2,"星钟岛",new Vector3(-4,1.04f,32.7f),new Vector3(5,.04f,21),Vector3.forward);
            game.stations=stations;
            GardenIsland(world);ClockIsland(world,stations[2]);
            var physics=Group("Walkable surfaces and boundaries",systems);
            BuildJourneyColliders(world,physics);
            var player=BuildJourneyPlayer(systems);game.player=player;player.game=game;
            var cam=Camera.main.gameObject.AddComponent<JourneyCamera>();game.journeyCamera=cam;cam.game=game;
            Camera.main.fieldOfView=48;

            var pickups=Group("Nine persistent starlights",systems);
            var stars=new List<JourneyStar>();
            stars.Add(CreateStar(pickups,game,"cloud_01",new Vector3(-1,1.65f,-30),false));
            stars.Add(CreateStar(pickups,game,"cloud_02",new Vector3(9,.75f,-24),false));
            stars.Add(CreateStar(pickups,game,"cloud_03",new Vector3(4,.75f,-15),false));
            Vector3[] gardenStars={new Vector3(39,.75f,-3),new Vector3(34,.75f,4),new Vector3(28,.75f,-2),new Vector3(30,.75f,9),new Vector3(23,.75f,-5),new Vector3(27,.75f,6)};
            for(int i=0;i<6;i++)stars.Add(CreateStar(pickups,game,"garden_0"+(i+1),gardenStars[i],i>=3));
            game.stars=stars.ToArray();
            var hud=BuildJourneyUI(world,game);game.hud=hud;
            ConfigureJourneyAudio(game);
            player.transform.position=stations[0].safeSpawn.position;
            cam.Snap();train.Initialize(0);
            // Every new project run starts in this playable scene when building a player.
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(JourneyScenePath,true)};
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),JourneyScenePath);
            Selection.activeGameObject=systems.gameObject;
            Debug.Log("Playable cloud railway saved: "+JourneyScenePath+" | 3 stations | 9 stars | "+path.Length.ToString("F1")+" m railway");
        }

        static void LoadJourneyFont()
        {
            const string fontPath=Root+"/Fonts/CloudRailChinese.asset";
            journeyFont=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if(journeyFont!=null)return;
            var font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/NotoSansCJKsc-Regular.otf");
            if(font==null)throw new InvalidOperationException("The Chinese source font has not finished importing.");
            journeyFont=TMP_FontAsset.CreateFontAsset(font,48,6,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);
            if(journeyFont==null)throw new InvalidOperationException("Chinese font asset creation failed.");
            journeyFont.name="CloudRailChinese";AssetDatabase.CreateAsset(journeyFont,fontPath);
            AssetDatabase.AddObjectToAsset(journeyFont.material,journeyFont);
            foreach(var texture in journeyFont.atlasTextures)AssetDatabase.AddObjectToAsset(texture,journeyFont);
            var chars=new HashSet<char>();
            foreach(string path in Directory.GetFiles(Root+"/Scripts","Journey*.cs"))
                foreach(char ch in File.ReadAllText(path))if(!char.IsControl(ch))chars.Add(ch);
            foreach(string path in Directory.GetFiles(Root+"/Editor","*Journey*.cs"))
                foreach(char ch in File.ReadAllText(path))if(!char.IsControl(ch))chars.Add(ch);
            journeyFont.TryAddCharacters(new string(new List<char>(chars).ToArray()),out string missing);
            EditorUtility.SetDirty(journeyFont);AssetDatabase.SaveAssets();
        }
        static JourneyStation CreateJourneyStation(Transform p,int index,string name,Vector3 spawn,Vector3 devicePos,Vector3 outward)
        {
            var root=Group(name,p);var station=root.gameObject.AddComponent<JourneyStation>();station.index=index;station.displayName=name;station.outward=outward;
            station.safeSpawn=Group("Safe platform spawn",root);station.safeSpawn.position=spawn;
            var device=Group("Star route activator",root);device.position=devicePos;station.device=device;
            Cylinder(device,"Activator plinth",Vector3.up*.12f,.55f,.24f,"Ivory",12);
            Cylinder(device,"Activator blue pedestal",Vector3.up*.62f,.3f,.9f,"RoofDark",12,.8f);
            var signal=Ball(device,"Starlight beacon",Vector3.up*1.3f,Vector3.one*.55f,"Lamp");
            station.signalParts=new[]{signal.GetComponent<Renderer>()};
            for(int i=0;i<16;i++)
            {
                float a=i*Mathf.PI/8,b=(i+1)*Mathf.PI/8;
                Beam(device,"Activator brass ring",new Vector3(Mathf.Cos(a)*.48f,1.3f,Mathf.Sin(a)*.48f),new Vector3(Mathf.Cos(b)*.48f,1.3f,Mathf.Sin(b)*.48f),.035f,"Brass",6);
            }
            var label=Group("Chinese station name",root);label.position=spawn+Vector3.up*2.8f;
            var text=label.gameObject.AddComponent<TextMeshPro>();text.font=journeyFont;text.text=name;text.fontSize=3.4f;text.alignment=TextAlignmentOptions.Center;text.color=Ink;
            text.rectTransform.sizeDelta=new Vector2(6,1);
            // TextMeshPro faces local -Z, so its back points toward the island.
            text.transform.rotation=Quaternion.LookRotation(-outward);
            return station;
        }
        static JourneyPlayer BuildJourneyPlayer(Transform p)
        {
            var root=Group("Traveler",p);root.gameObject.layer=2;
            var controller=root.gameObject.AddComponent<CharacterController>();controller.height=1.8f;controller.radius=.28f;controller.center=Vector3.up*.9f;controller.stepOffset=.32f;controller.skinWidth=.035f;controller.slopeLimit=50;
            var model=Group("Blue coated traveler",root);
            Cylinder(model,"Coat",Vector3.up*.85f,.29f,.85f,"Engine",12,.8f);
            Ball(model,"Head",Vector3.up*1.47f,Vector3.one*.49f,"Porcelain");
            Cylinder(model,"Traveler cap",Vector3.up*1.73f,.33f,.11f,"RoofDark",16);
            Ball(model,"Cap crown",Vector3.up*1.8f,new Vector3(.48f,.2f,.48f),"Roof");
            Box(model,"Scarf",new Vector3(0,1.24f,.2f),new Vector3(.4f,.14f,.13f),"Ivory");
            Box(model,"Backpack",new Vector3(0,.94f,-.28f),new Vector3(.43f,.5f,.22f),"Timber");
            foreach(float side in new[]{-1f,1f})
            {
                Cylinder(model,"Arm",new Vector3(side*.35f,.92f,0),.09f,.55f,"Engine",8);
                Ball(model,"Hand",new Vector3(side*.35f,.6f,0),Vector3.one*.17f,"Porcelain");
                Box(model,"Boot",new Vector3(side*.15f,.2f,.05f),new Vector3(.22f,.4f,.35f),"EngineDark");
                Ball(model,"Eye",new Vector3(side*.085f,1.51f,.22f),Vector3.one*.035f,"EngineDark");
                Box(model,"Backpack strap",new Vector3(side*.18f,1.04f,.23f),new Vector3(.05f,.37f,.06f),"Brass");
            }
            Combine(model,"Traveler");foreach(Transform t in model)t.gameObject.layer=2;
            var player=root.gameObject.AddComponent<JourneyPlayer>();player.model=model;
            return player;
        }
        static JourneyStar CreateStar(Transform p,JourneyGame game,string id,Vector3 pos,bool optional)
        {
            var root=Group(id,p);root.position=pos;var star=root.gameObject.AddComponent<JourneyStar>();star.starId=id;star.optional=optional;star.game=game;
            var visual=Group("Rotating gold star",root);star.visual=visual;
            string key="FivePointStar";
            if(!shapes.TryGetValue(key,out var mesh))
            {
                var v=new List<Vector3>();var triangles=new List<int>();
                for(int i=0;i<10;i++)
                {
                    float a=Mathf.PI*.5f+i*Mathf.PI/5,b=Mathf.PI*.5f+(i+1)*Mathf.PI/5;
                    float ra=i%2==0?.43f:.2f,rb=(i+1)%2==0?.43f:.2f;
                    var aa=new Vector3(Mathf.Cos(a)*ra,Mathf.Sin(a)*ra,-.12f);var bb=new Vector3(Mathf.Cos(b)*rb,Mathf.Sin(b)*rb,-.12f);
                    Tri(v,triangles,new Vector3(0,0,-.19f),bb,aa);Tri(v,triangles,new Vector3(0,0,.19f),aa+Vector3.forward*.24f,bb+Vector3.forward*.24f);
                    Tri(v,triangles,aa,bb,bb+Vector3.forward*.24f);Tri(v,triangles,aa,bb+Vector3.forward*.24f,aa+Vector3.forward*.24f);
                }
                mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh=SaveMesh(mesh,key);shapes[key]=mesh;
            }
            Shape("Five point starlight",mesh,visual,Vector3.zero,Vector3.one,mats["Lamp"]);
            Cylinder(root,"Star shadow marker",new Vector3(0,-.65f,0),.27f,.035f,"Brass",20);
            return star;
        }
        static void GardenIsland(Transform world)
        {
            var g=Group("10  Garden island",world);Island(g,new Vector3(31.5f,0,0),14.5f,12.3f,18,"Garden floating island",22);
            var details=Group("Flower garden",g);
            Box(details,"Garden loop east",new Vector3(38,.1f,1),new Vector3(2.2f,.16f,16),"Ivory");
            Box(details,"Garden loop north",new Vector3(32,.1f,7),new Vector3(14,.16f,2.2f),"Ivory");
            Box(details,"Garden loop west",new Vector3(26,.1f,1),new Vector3(2.2f,.16f,14),"Ivory");
            Box(details,"Garden loop south",new Vector3(32,.1f,-5),new Vector3(14,.16f,2.2f),"Ivory");
            foreach(var pos in new[]{new Vector3(35,0,0),new Vector3(30,0,3),new Vector3(24,0,5),new Vector3(31,0,-8),new Vector3(39,0,7),new Vector3(23,0,-2)})Tree(details,pos,.95f,true);
            Cylinder(details,"Garden fountain",new Vector3(32,.45f,0),2.15f,.9f,"Ivory",24);
            Cylinder(details,"Garden fountain water",new Vector3(32,.92f,0),1.8f,.05f,"Water",24);
            Cylinder(details,"Garden fountain stem",new Vector3(32,1.4f,0),.18f,1,"Limestone",12);
            Ball(details,"Garden fountain pearl",new Vector3(32,2,0),Vector3.one*.4f,"Brass");
            var pavilion=Group("Flower garden belvedere",details);pavilion.position=new Vector3(26,0,7);
            Cylinder(pavilion,"Belvedere floor",Vector3.up*.2f,2.3f,.4f,"Ivory",12);
            for(int i=0;i<6;i++){float a=i*Mathf.PI/3;Cylinder(pavilion,"Belvedere column",new Vector3(Mathf.Cos(a)*1.8f,1.8f,Mathf.Sin(a)*1.8f),.1f,3.2f,"Ivory",10);}
            Cylinder(pavilion,"Belvedere roof",Vector3.up*3.9f,2.5f,1.4f,"Roof",6,.03f);
            Box(details,"Garden station platform",new Vector3(43.8f,.45f,0),new Vector3(3.9f,.9f,17),"Limestone");
            Box(details,"Garden station paving",new Vector3(43.8f,.94f,0),new Vector3(4.1f,.08f,17.2f),"Ivory");
            Bench(details,new Vector3(42.8f,1,5),90);Lamp(details,new Vector3(43,1,7.3f));Lamp(details,new Vector3(43,1,-7.3f));
            Fence(details,new Vector3(25,.1f,-9),new Vector3(35,.1f,-10),8);
            Ramp(details,new Vector3(38,.1f,3),new Vector3(42,1,3),3,"Garden accessible approach");
            Combine(details,"GardenDetails");
        }
        static void ClockIsland(Transform world,JourneyStation station)
        {
            var g=Group("11  Starclock island",world);Island(g,new Vector3(0,0,21),13,12.5f,20,"Starclock floating island",22);
            var details=Group("Starclock architecture",g);
            Box(details,"Clock promenade",new Vector3(5,.1f,26),new Vector3(2.7f,.16f,11),"Ivory");
            Cylinder(details,"Clock terrace",new Vector3(0,.2f,21),4.5f,.4f,"Limestone",24);
            Cylinder(details,"Clock terrace paving",new Vector3(0,.45f,21),4.25f,.15f,"Ivory",24);
            Box(details,"Starclock tower",new Vector3(0,4,21),new Vector3(3.5f,7,3.5f),"Ivory");
            foreach(float y in new[]{.8f,3.6f,7.5f})Box(details,"Clock cornice",new Vector3(0,y,21),new Vector3(3.85f,.23f,3.85f),"Porcelain");
            Cylinder(details,"Clock roof",new Vector3(0,8.5f,21),2.7f,2,"Roof",4,.02f,Quaternion.Euler(0,45,0));
            for(int side=-1;side<=1;side+=2)Window(details,new Vector3(side*.8f,5.6f,19.2f),.6f,1.3f);
            Cylinder(details,"Clock golden face",new Vector3(0,3.2f,19.16f),1,.14f,"Brass",32,1,Quaternion.Euler(90,0,0));
            Cylinder(details,"Clock dial",new Vector3(0,3.2f,19.05f),.88f,.04f,"Porcelain",32,1,Quaternion.Euler(90,0,0));
            Beam(details,"Clock hands",new Vector3(0,3.2f,19),new Vector3(.25f,3.83f,19),.04f,"RoofDark");
            Beam(details,"Clock hands",new Vector3(0,3.2f,18.98f),new Vector3(-.4f,3.35f,18.98f),.05f,"RoofDark");
            Box(details,"Starclock platform",new Vector3(0,.45f,33),new Vector3(17,.9f,3.9f),"Limestone");
            Box(details,"Starclock paving",new Vector3(0,.94f,33),new Vector3(17.2f,.08f,4.1f),"Ivory");
            Lamp(details,new Vector3(-7,1,32.5f));Lamp(details,new Vector3(7,1,32.5f));
            Bench(details,new Vector3(-4,1,32),180);
            Tree(details,new Vector3(-7,0,24),1.2f,true);Tree(details,new Vector3(8,0,23),1.1f,false);Tree(details,new Vector3(-7,0,17),.9f,false);
            Ramp(details,new Vector3(5,.1f,27),new Vector3(5,1,31.5f),3,"Clock accessible approach");
            Combine(details,"ClockDetails");
            var ring=Group("Awakening celestial ring",g);ring.position=new Vector3(0,10.1f,21);station.towerRing=ring;
            for(int j=0;j<2;j++)for(int i=0;i<48;i++)
            {
                float a=i*Mathf.PI/24,b=(i+1)*Mathf.PI/24;var tilt=Quaternion.Euler(j==0?18:-28,0,0);
                Beam(ring,"Clock celestial arc",tilt*new Vector3(Mathf.Cos(a)*3.2f,0,Mathf.Sin(a)*3.2f),tilt*new Vector3(Mathf.Cos(b)*3.2f,0,Mathf.Sin(b)*3.2f),.06f,"Brass");
            }
            var crown=Ball(ring,"Awakening star crown",Vector3.up*.8f,Vector3.one*.8f,"Lamp");
            var signals=new List<Renderer>(station.signalParts){crown.GetComponent<Renderer>()};station.signalParts=signals.ToArray();
        }
        static void Ramp(Transform p,Vector3 a,Vector3 b,float width,string name)
        {
            Vector3 side=Vector3.Cross(Vector3.up,(b-a).normalized).normalized*width*.5f;
            var verts=new List<Vector3>();var triangles=new List<int>();
            Tri(verts,triangles,a-side,b+side,a+side);Tri(verts,triangles,a-side,b-side,b+side);
            var m=new Mesh();m.SetVertices(verts);m.SetTriangles(triangles,0);m.RecalculateNormals();m.RecalculateBounds();
            Shape(name,SaveMesh(m,"Ramp_"+serial++),p,Vector3.zero,Vector3.one,mats["Ivory"]);
        }
        static void ColliderBox(Transform p,string name,Vector3 center,Vector3 size)
        {
            var g=Group(name,p);g.position=center;g.gameObject.AddComponent<BoxCollider>().size=size;
        }
        static void RampCollider(Transform p,Vector3 a,Vector3 b,float width)
        {
            Vector3 delta=b-a;var g=Group("Walkable sloping approach",p);g.position=(a+b)*.5f-Vector3.up*.1f;
            g.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(delta,Vector3.up))*Quaternion.Euler(-Mathf.Atan2(delta.y,Vector3.ProjectOnPlane(delta,Vector3.up).magnitude)*Mathf.Rad2Deg,0,0);
            g.gameObject.AddComponent<BoxCollider>().size=new Vector3(width,.2f,delta.magnitude+.1f);
        }
        static void BuildJourneyColliders(Transform world,Transform p)
        {
            foreach(string name in new[]{"01  Floating island","10  Garden island","11  Starclock island"})
                foreach(var f in world.Find(name).GetComponentsInChildren<MeshFilter>())
                    if(f.gameObject.name=="Grass")f.gameObject.AddComponent<MeshCollider>().sharedMesh=f.sharedMesh;
            ColliderBox(p,"Cloudrise station walls",new Vector3(-1,2.75f,-22.6f),new Vector3(11.5f,4.4f,5.8f));
            foreach(float side in new[]{-1f,1f})ColliderBox(p,"Cloudrise wing walls",new Vector3(-1+side*7.3f,1.85f,-22.5f),new Vector3(2.7f,2.6f,4.9f));
            ColliderBox(p,"Cloudrise platform",new Vector3(-1,.5f,-31),new Vector3(18.15f,.97f,4.4f));
            ColliderBox(p,"Cloudrise fountain",new Vector3(8,.55f,-15),new Vector3(4.8f,1.1f,4.8f));
            // A continuous ramp replaces precision stair climbing for the playable prototype.
            var main=Group("Cloudrise approach ramp",world);Ramp(main,new Vector3(9,.04f,-24),new Vector3(9,.99f,-29.2f),3.6f,"Station approach slope");
            RampCollider(p,new Vector3(9,.04f,-24),new Vector3(9,.99f,-29.2f),3.6f);
            ColliderBox(p,"Garden platform",new Vector3(43.8f,.49f,0),new Vector3(4.1f,.98f,17.2f));
            ColliderBox(p,"Garden fountain",new Vector3(32,.45f,0),new Vector3(4.2f,.9f,4.2f));
            RampCollider(p,new Vector3(38,.1f,3),new Vector3(42,1,3),3);
            ColliderBox(p,"Starclock platform",new Vector3(0,.49f,33),new Vector3(17.2f,.98f,4.1f));
            ColliderBox(p,"Starclock tower walls",new Vector3(0,4,21),new Vector3(3.5f,7,3.5f));
            RampCollider(p,new Vector3(5,.1f,27),new Vector3(5,1,31.5f),3);
        }

        static RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=position;r.sizeDelta=size;return r;
        }
        static UnityEngine.UI.Image Image(RectTransform r,Color color,bool interactive=false)
        {
            var image=r.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=interactive;return image;
        }
        static TextMeshProUGUI Label(string name,Transform p,string value,Vector2 pos,Vector2 size,int fontSize,Color color,TextAlignmentOptions alignment=TextAlignmentOptions.MidlineLeft)
        {
            var r=Rect(name,p,new Vector2(.5f,.5f),new Vector2(.5f,.5f),pos,size);
            var text=r.gameObject.AddComponent<TextMeshProUGUI>();text.font=journeyFont;text.text=value;text.fontSize=fontSize;text.color=color;text.alignment=alignment;
            text.textWrappingMode=TextWrappingModes.Normal;text.raycastTarget=false;return text;
        }
        static UnityEngine.UI.Button Button(string name,Transform p,string value,Vector2 pos,Vector2 size,out TMP_Text label)
        {
            var r=Rect(name,p,Vector2.one*.5f,Vector2.one*.5f,pos,size);var image=Image(r,Teal,true);
            var button=r.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.85f,.95f,.97f);colors.pressedColor=new Color(.7f,.85f,.87f);colors.disabledColor=new Color(.7f,.7f,.7f,.55f);button.colors=colors;
            label=Label(name+" label",r,value,Vector2.zero,size-new Vector2(26,4),24,Cream,TextAlignmentOptions.Center);
            return button;
        }
        static UnityEngine.UI.Slider Slider(string name,Transform p,Vector2 pos,float min,float max)
        {
            var r=Rect(name,p,Vector2.one*.5f,Vector2.one*.5f,pos,new Vector2(340,26));
            var background=Rect("Track",r,Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(340,6));Image(background,new Color(.7f,.75f,.72f));
            var fillArea=Rect("Fill Area",r,Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(324,6));
            var fill=Rect("Fill",fillArea,Vector2.zero,Vector2.zero,Vector2.zero,new Vector2(324,6));Image(fill,Teal);
            fill.anchorMin=Vector2.zero;fill.anchorMax=Vector2.one;fill.offsetMin=fill.offsetMax=Vector2.zero;
            var handleArea=Rect("Handle Area",r,Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(324,26));
            var handle=Rect("Handle",handleArea,Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(20,26));var handleImage=Image(handle,Ink,true);
            var slider=r.gameObject.AddComponent<UnityEngine.UI.Slider>();slider.minValue=min;slider.maxValue=max;slider.fillRect=fill;slider.handleRect=handle;slider.targetGraphic=handleImage;slider.direction=UnityEngine.UI.Slider.Direction.LeftToRight;
            return slider;
        }
        static RectTransform Modal(string name,Transform canvas,Vector2 size)
        {
            var overlay=Rect(name,canvas,Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,Vector2.zero);overlay.anchorMin=Vector2.zero;overlay.anchorMax=Vector2.one;overlay.offsetMin=overlay.offsetMax=Vector2.zero;
            Image(overlay,new Color(.06f,.15f,.2f,.44f),true);
            var panel=Rect("Content",overlay,Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,size);Image(panel,Cream,true);return panel;
        }
        static JourneyHUD BuildJourneyUI(Transform world,JourneyGame game)
        {
            var canvasGO=new GameObject("Chinese journey interface",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));canvasGO.transform.SetParent(world,false);
            var canvas=canvasGO.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=10;
            var scaler=canvasGO.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
            var hud=canvasGO.AddComponent<JourneyHUD>();hud.game=game;
            var task=Rect("Current journey",canvasGO.transform,new Vector2(0,1),new Vector2(0,1),new Vector2(28,-28),new Vector2(510,100));Image(task,Teal);
            hud.stationName=Label("Station name",task,"云起站",new Vector2(0,23),new Vector2(462,28),21,new Color(.87f,.77f,.52f));
            hud.objective=Label("Current objective",task,"沿步道寻找三枚星光",new Vector2(0,-17),new Vector2(462,48),24,Cream);
            var counter=Rect("Starlight count",canvasGO.transform,new Vector2(1,1),new Vector2(1,1),new Vector2(-28,-28),new Vector2(190,65));Image(counter,Teal);
            hud.starCount=Label("Count",counter,"星光  0 / 9",Vector2.zero,new Vector2(170,55),27,Cream,TextAlignmentOptions.Center);
            var hint=Rect("Contextual controls",canvasGO.transform,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,25),new Vector2(780,54));Image(hint,Teal);
            hud.prompt=Label("Interaction prompt",hint,"WASD 行走   ·   右键拖动观察   ·   Esc 暂停",Vector2.zero,new Vector2(750,50),23,Cream,TextAlignmentOptions.Center);
            hud.toast=Label("Toast",canvasGO.transform,"",new Vector2(0,260),new Vector2(1000,65),26,Ink,TextAlignmentOptions.Center);
            var intro=Modal("Begin journey",canvasGO.transform,new Vector2(650,430));hud.introPanel=intro.parent.gameObject;
            Label("Title",intro,"云上列车",new Vector2(0,115),new Vector2(540,70),46,Ink,TextAlignmentOptions.Center);
            Label("Introduction",intro,"沿着星光，重新点亮云端钟塔。\n乘车穿越群岛，下车寻找九枚星光。",new Vector2(0,24),new Vector2(550,96),25,Ink,TextAlignmentOptions.Center);
            Label("Controls",intro,"WASD 行走  ·  右键观察  ·  E 交互",new Vector2(0,-61),new Vector2(550,38),21,Ink,TextAlignmentOptions.Center);
            hud.beginButton=Button("Start",intro,"启程",new Vector2(0,-143),new Vector2(330,58),out _);
            var destinations=Modal("Choose destination",canvasGO.transform,new Vector2(720,410));hud.destinationsPanel=destinations.parent.gameObject;
            Label("Title",destinations,"选择下一站",new Vector2(0,137),new Vector2(620,60),34,Ink,TextAlignmentOptions.Center);
            hud.destinationButtons=new UnityEngine.UI.Button[2];hud.destinationLabels=new TMP_Text[2];
            for(int i=0;i<2;i++)hud.destinationButtons[i]=Button("Destination "+i,destinations,"下一站",new Vector2(0,52-i*82),new Vector2(650,68),out hud.destinationLabels[i]);
            hud.cancelDestinationsButton=Button("Cancel",destinations,"暂不出发",new Vector2(0,-139),new Vector2(300,54),out _);
            var pause=Modal("Pause journey",canvasGO.transform,new Vector2(620,590));hud.pausePanel=pause.parent.gameObject;
            Label("Title",pause,"在云端歇一会",new Vector2(0,225),new Vector2(530,64),36,Ink,TextAlignmentOptions.Center);
            Label("Music label",pause,"音乐音量",new Vector2(-175,132),new Vector2(150,36),23,Ink);
            hud.musicSlider=Slider("Music volume",pause,new Vector2(66,132),0,1);
            Label("Effects label",pause,"音效音量",new Vector2(-175,56),new Vector2(150,36),23,Ink);
            hud.volumeSlider=Slider("Effects volume",pause,new Vector2(66,56),0,1);
            Label("Sensitivity label",pause,"观察灵敏度",new Vector2(-175,-20),new Vector2(150,36),23,Ink);
            hud.sensitivitySlider=Slider("Look sensitivity",pause,new Vector2(66,-20),.4f,2);
            hud.resumeButton=Button("Resume",pause,"继续旅程",new Vector2(0,-117),new Vector2(360,58),out _);
            hud.restartButton=Button("Restart",pause,"重新开始",new Vector2(0,-199),new Vector2(360,54),out _);
            var ending=Modal("Clock awakened",canvasGO.transform,new Vector2(650,380));hud.endingPanel=ending.parent.gameObject;
            Label("Title",ending,"钟塔已经苏醒",new Vector2(0,100),new Vector2(550,70),39,Ink,TextAlignmentOptions.Center);
            Label("Description",ending,"云端航线重新点亮。\n你可以继续乘车，补齐余下的星光。",new Vector2(0,10),new Vector2(550,96),25,Ink,TextAlignmentOptions.Center);
            hud.continueButton=Button("Continue exploring",ending,"继续探索",new Vector2(0,-110),new Vector2(350,58),out _);
            var restart=Modal("Confirm new journey",canvasGO.transform,new Vector2(680,360));hud.restartPanel=restart.parent.gameObject;
            Label("Title",restart,"重新开始这段旅程",new Vector2(0,100),new Vector2(590,65),33,Ink,TextAlignmentOptions.Center);
            Label("Description",restart,"将重置星光、航线与钟塔进度，保留声音和操作设置。",new Vector2(0,23),new Vector2(590,80),24,Ink,TextAlignmentOptions.Center);
            hud.cancelRestartButton=Button("Keep journey",restart,"保留旅程",new Vector2(-150,-105),new Vector2(260,56),out _);
            hud.confirmRestartButton=Button("Confirm restart",restart,"确认重新开始",new Vector2(150,-105),new Vector2(260,56),out _);
            var curtain=Rect("Station turnaround fade",canvasGO.transform,Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,Vector2.zero);curtain.anchorMin=Vector2.zero;curtain.anchorMax=Vector2.one;curtain.offsetMin=curtain.offsetMax=Vector2.zero;Image(curtain,new Color(.05f,.12f,.16f),true);
            hud.curtain=curtain.gameObject.AddComponent<CanvasGroup>();hud.curtain.alpha=0;hud.curtain.blocksRaycasts=false;
            hud.ClosePanels();hud.SetIntro(true);
            var events=new GameObject("Journey EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));events.transform.SetParent(world,false);
            var binding=events.AddComponent<JourneyUIInput>();binding.actions=AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            JourneyUIInput.Bind(events.GetComponent<InputSystemUIInputModule>(),binding.actions);
            return hud;
        }
    }
}
