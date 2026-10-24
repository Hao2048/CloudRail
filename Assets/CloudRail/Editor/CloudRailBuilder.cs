using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CloudRail.Editor
{
    /// <summary>Deterministic, editable milestone scene. All generated meshes and materials are saved assets.</summary>
    public static partial class CloudRailBuilder
    {
        const string Root = "Assets/CloudRail";
        static bool gameplayAssets;
        static bool releaseAssets;
        static bool desktopAssets;
        static bool visualAssets;
        static string Gen => Root + "/Generated" + (visualAssets ? "/Visual" : desktopAssets ? "/Desktop" : releaseAssets ? "/Release" : gameplayAssets ? "/Gameplay" : "");
        public const string ScenePath = "Assets/Scenes/CloudRail_Milestone01.unity";
        static readonly Dictionary<string, Mesh> shapes = new Dictionary<string, Mesh>();
        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        static System.Random rng;
        static int serial;
        static Mesh cube, sphere;
        static readonly List<Transform> wheels = new List<Transform>();
        static readonly List<Transform> clouds = new List<Transform>();
        static readonly List<Transform> islets = new List<Transform>();

        [MenuItem("Cloud Rail/Build Milestone One")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before building the scene.");
            if (SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save the current scene before building the milestone scene.");
            visualAssets=false;desktopAssets=false;releaseAssets = false;gameplayAssets = false;
            rng = new System.Random(251023);
            serial = 0; shapes.Clear(); mats.Clear(); wheels.Clear(); clouds.Clear(); islets.Clear();
            Folder(Gen + "/Meshes"); Folder(Gen + "/Materials"); Folder(Root + "/Prefabs");
            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube); cube = temp.GetComponent<MeshFilter>().sharedMesh; Object.DestroyImmediate(temp);
            temp = GameObject.CreatePrimitive(PrimitiveType.Sphere); sphere = temp.GetComponent<MeshFilter>().sharedMesh; Object.DestroyImmediate(temp);
            Palette();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var world = Group("CLOUDLINE  |  Cloudrise Station");
            var island = Group("01  Floating island", world);
            Island(island, Vector3.zero, 20.5f, 14.8f, 26f, "Main island", 26);
            var architecture = Group("02  Cloudrise station", world);
            Station(architecture);
            var garden = Group("03  Gardens and promenade", world);
            Gardens(garden);

            var railway = Group("04  Aerial railway", world);
            var path = railway.gameObject.AddComponent<SkyRailPath>(); path.Bake();
            Railway(railway, path);
            var waterfall = Group("05  Waterfall", world);
            Waterfall(waterfall);
            var distant = Group("06  Distant archipelago", world);
            Backdrop(distant);
            var atmosphere = Group("07  Cloud layers", world);
            Clouds(atmosphere);

            var trainRoot = Group("08  Little cloud express", world);
            var engine = Group("Locomotive", trainRoot);
            var coach = Group("Observation carriage", trainRoot);
            var rods = new List<Transform>();
            var chimney = Engine(engine, rods);
            Coach(coach);
            var smoke = Steam(chimney);
            var train = trainRoot.gameObject.AddComponent<CloudTrain>();
            train.path = path; train.locomotive = engine; train.carriage = coach;
            train.wheels = wheels.ToArray(); train.connectingRods = rods.ToArray();
            train.chimney = chimney; train.steam = smoke;
            // Reusable models are saved at their local origins, independently of the railway pose.
            PrefabUtility.SaveAsPrefabAsset(engine.gameObject, Root + "/Prefabs/CloudLocomotive.prefab");
            PrefabUtility.SaveAsPrefabAsset(coach.gameObject, Root + "/Prefabs/ObservationCarriage.prefab");
            train.PreviewPose();

            Lighting(world);
            var cameraGO = new GameObject("Main Camera"); cameraGO.tag = "MainCamera";
            cameraGO.transform.SetParent(world, false);
            var camera = cameraGO.AddComponent<Camera>(); camera.fieldOfView = 37f; camera.nearClipPlane = .15f; camera.farClipPlane = 350;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.allowHDR = true; camera.allowMSAA = true;
            var cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cameraData.antialiasingQuality = AntialiasingQuality.High;
            cameraGO.AddComponent<AudioListener>();
            var orbit = cameraGO.AddComponent<CloudRailCamera>(); orbit.train = engine; orbit.Snap();
            var ambient = world.gameObject.AddComponent<SkyAmbience>();
            ambient.clouds = clouds.ToArray(); ambient.floatingIslets = islets.ToArray();
            ambient.orrery = GameObject.Find("Station celestial rings")?.transform;

            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.LookAt(new Vector3(0,-2,0), camera.transform.rotation, 50f);
            Selection.activeGameObject = world.gameObject;
            Debug.Log("Cloud Rail milestone one created. Scene: " + ScenePath + " | track length: " + path.Length.ToString("F1") + " m");
        }

        static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/'); Folder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
        static float R(float min, float max) => min + (float)rng.NextDouble() * (max - min);
        static Transform Group(string name, Transform parent = null)
        {
            var t = new GameObject(name).transform; if (parent != null) t.SetParent(parent, false); return t;
        }
        static Material Mat(string name, string hex, float metal = 0, float smooth = .25f)
        {
            string path = Gen + "/Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find(visualAssets?"CloudRail/Anime Light":"Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            ColorUtility.TryParseHtmlString(hex, out var color);
            m.SetColor("_BaseColor", color); m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", smooth);
            EditorUtility.SetDirty(m); mats[name] = m; return m;
        }
        static void Palette()
        {
            Mat("Ivory", "#F2E5C9"); Mat("Limestone", "#D6C5A3"); Mat("Porcelain", "#FFF3D9");
            Mat("Roof", "#4096AC", .08f); Mat("RoofLight", "#69BBC9", .12f); Mat("RoofDark", "#2F697D");
            Mat("Brass", "#D6AD64", .65f, .4f); Mat("Rail", "#827064", .55f); Mat("Sleepers", "#987C62");
            Mat("Grass", "#A9C67B"); Mat("GrassLight", "#B8CE8A"); Mat("GrassDark", "#91B477");
            Mat("RockA", "#899DB0"); Mat("RockB", "#A6B6C1"); Mat("RockC", "#73899E"); Mat("RockD", "#BCC6CE"); Mat("RockE", "#60798F");
            Mat("Window", "#284F67", .2f, .65f); Mat("WindowLight", "#78A8B9", .1f, .55f);
            Mat("Timber", "#856954"); Mat("Trunk", "#8B7C66"); Mat("Pink", "#E6B2C7"); Mat("PinkLight", "#F0C9D6");
            Mat("Leaf", "#75A78E"); Mat("LeafLight", "#A1C4A3"); Mat("Cloud", "#ECF2F1", 0, 0);
            Mat("Engine", "#327E95", .18f); Mat("EngineDark", "#294B60", .25f); Mat("Wheel", "#364C57", .35f);
            Mat("Lamp", "#FFE3A3"); mats["Lamp"].EnableKeyword("_EMISSION"); mats["Lamp"].SetColor("_EmissionColor", new Color(1,.62f,.2f) * .8f);
            var water = AssetDatabase.LoadAssetAtPath<Material>(Gen + "/Materials/Water.mat");
            if (water == null) { water = new Material(Shader.Find("CloudRail/Falling Water")); AssetDatabase.CreateAsset(water, Gen + "/Materials/Water.mat"); }
            mats["Water"] = water;
        }
        static Mesh SaveMesh(Mesh mesh, string name)
        {
            string path = Gen + "/Meshes/" + name + ".asset";
            mesh.name = name;
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (old != null) { EditorUtility.CopySerialized(mesh, old); Object.DestroyImmediate(mesh); return old; }
            AssetDatabase.CreateAsset(mesh, path); return mesh;
        }
        static GameObject Shape(string name, Mesh mesh, Transform p, Vector3 pos, Vector3 scale, Material material, Quaternion? rot = null)
        {
            var go = new GameObject(name); var t = go.transform; t.SetParent(p, false);
            t.localPosition = pos; t.localRotation = rot ?? Quaternion.identity; t.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = material;
            mr.receiveShadows = true; return go;
        }
        static GameObject Box(Transform p,string name,Vector3 pos,Vector3 size,string mat,Quaternion? rot=null)
        {
            if(visualAssets)return Shape(name,BeveledBox(size),p,pos,Vector3.one,mats[mat],rot);
            return Shape(name,cube,p,pos,size,mats[mat],rot);
        }

        static GameObject Ball(Transform p, string name, Vector3 pos, Vector3 size, string mat) => Shape(name, sphere, p, pos, size, mats[mat]);
        static Mesh FrustumMesh(int sides, float ratio)
        {
            string key = "Frustum_" + sides + "_" + Mathf.RoundToInt(ratio * 1000);
            if (shapes.TryGetValue(key, out var found)) return found;
            var v = new List<Vector3>(); var tr = new List<int>();
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides, b = (i + 1) * Mathf.PI * 2 / sides;
                Vector3 p0 = new Vector3(Mathf.Cos(a), -.5f, Mathf.Sin(a)), p1 = new Vector3(Mathf.Cos(b), -.5f, Mathf.Sin(b));
                Vector3 p2 = new Vector3(Mathf.Cos(b) * ratio, .5f, Mathf.Sin(b) * ratio), p3 = new Vector3(Mathf.Cos(a) * ratio, .5f, Mathf.Sin(a) * ratio);
                Tri(v,tr,p0,p3,p2); Tri(v,tr,p0,p2,p1);
                Tri(v,tr,Vector3.down * .5f,p0,p1); Tri(v,tr,Vector3.up * .5f,p2,p3);
            }
            var mesh = new Mesh(); mesh.SetVertices(v); mesh.SetTriangles(tr,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            shapes[key] = SaveMesh(mesh, key); return shapes[key];
        }
        static GameObject Cylinder(Transform p, string name, Vector3 pos, float radius, float height, string mat, int sides = 16, float topRatio = 1f, Quaternion? rot = null)
            => Shape(name, FrustumMesh(sides,topRatio), p, pos, new Vector3(radius,height,radius), mats[mat], rot);
        static void Tri(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c)
        {
            int n = v.Count; v.Add(a); v.Add(b); v.Add(c); t.Add(n); t.Add(n+1); t.Add(n+2);
        }
        static void Beam(Transform p, string name, Vector3 a, Vector3 b, float radius, string mat, int sides = 8)
            => Cylinder(p,name,(a+b)*.5f,radius,Vector3.Distance(a,b),mat,sides,1,Quaternion.FromToRotation(Vector3.up,b-a));

        static void Combine(Transform root, string name)
        {
            var filters = root.GetComponentsInChildren<MeshFilter>();
            var groups = new Dictionary<Material,List<CombineInstance>>();
            foreach (var f in filters)
            {
                var renderer = f.GetComponent<MeshRenderer>(); if (renderer == null || f.sharedMesh == null) continue;
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < f.sharedMesh.subMeshCount; i++)
                {
                    var m = materials[Mathf.Min(i, materials.Length-1)];
                    if (!groups.TryGetValue(m,out var list)) groups[m] = list = new List<CombineInstance>();
                    list.Add(new CombineInstance { mesh=f.sharedMesh, subMeshIndex=i, transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix });
                }
            }
            foreach (var group in groups)
            {
                var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(group.Value.ToArray(),true,true);
                var saved = SaveMesh(mesh,name + "_" + group.Key.name);
                Shape(group.Key.name,saved,root,Vector3.zero,Vector3.one,group.Key);
            }
            foreach (var f in filters) if (f != null) Object.DestroyImmediate(f.gameObject);
        }

        static void Island(Transform parent, Vector3 center, float rx, float rz, float depth, string label, int count = 18)
        {
            var root = Group(label,parent); root.localPosition = center;
            float[] radial = new float[count]; for (int i=0;i<count;i++) radial[i]=R(.91f,1.07f);
            float[] ys={0,-depth*.14f,-depth*.44f,-depth*.76f,-depth};
            float[] radii={1,.94f,.74f,.38f,0};
            var vertices = new List<Vector3>(); var faces = new List<int>[5]; for(int i=0;i<5;i++) faces[i]=new List<int>();
            for(int ring=0;ring<4;ring++) for(int i=0;i<count;i++)
            {
                int j=(i+1)%count;
                Func<int,int,Vector3> p=(k,r)=>new Vector3(Mathf.Cos(k*Mathf.PI*2/count)*rx*radial[k]*radii[r],ys[r]+(r>0&&r<4?Mathf.Sin(k*9+r)*depth*.025f:0),Mathf.Sin(k*Mathf.PI*2/count)*rz*radial[k]*radii[r]);
                var a=p(i,ring);var b=p(j,ring);var c=p(j,ring+1);var d=p(i,ring+1);
                var face=faces[rng.Next(5)];Tri(vertices,face,a,b,c);Tri(vertices,face,a,c,d);
            }
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.subMeshCount=5;
            for(int i=0;i<5;i++)mesh.SetTriangles(faces[i],i);
            SmoothRockNormals(mesh);mesh.RecalculateBounds();
            var rock=Shape("Faceted suspended bedrock",SaveMesh(mesh,"IslandRock_"+serial++),root,Vector3.zero,Vector3.one,mats["RockA"]);
            rock.GetComponent<MeshRenderer>().sharedMaterials=new[]{mats["RockA"],mats["RockB"],mats["RockC"],mats["RockD"],mats["RockE"]};
            var gv=new List<Vector3>();var gt=new List<int>();
            for(int i=0;i<count;i++)
            {
                int j=(i+1)%count;float a=i*Mathf.PI*2/count,b=j*Mathf.PI*2/count;
                Tri(gv,gt,Vector3.up*.03f,new Vector3(Mathf.Cos(b)*rx*radial[j],.03f,Mathf.Sin(b)*rz*radial[j]),new Vector3(Mathf.Cos(a)*rx*radial[i],.03f,Mathf.Sin(a)*rz*radial[i]));
            }
            var grass=new Mesh();grass.SetVertices(gv);grass.SetTriangles(gt,0);grass.RecalculateNormals();grass.RecalculateBounds();
            Shape("Meadow surface",SaveMesh(grass,"IslandMeadow_"+serial++),root,Vector3.zero,Vector3.one,mats["Grass"]);
            var rimVertices=new List<Vector3>();var rimTriangles=new List<int>();
            for(int i=0;i<count;i++)
            {
                int j=(i+1)%count;float a=i*Mathf.PI*2/count,b=j*Mathf.PI*2/count;
                var p0=new Vector3(Mathf.Cos(a)*rx*radial[i],.04f,Mathf.Sin(a)*rz*radial[i]);
                var p1=new Vector3(Mathf.Cos(b)*rx*radial[j],.04f,Mathf.Sin(b)*rz*radial[j]);
                Tri(rimVertices,rimTriangles,p0,p1,p1-Vector3.up*.4f);
                Tri(rimVertices,rimTriangles,p0,p1-Vector3.up*.4f,p0-Vector3.up*.4f);
            }
            var rimMesh=new Mesh();rimMesh.SetVertices(rimVertices);rimMesh.SetTriangles(rimTriangles,0);rimMesh.RecalculateNormals();rimMesh.RecalculateBounds();
            Shape("Continuous turf edge",SaveMesh(rimMesh,"Turf_"+serial++),root,Vector3.zero,Vector3.one,mats["GrassDark"]);
            if(rx>10) for(int i=0;i<17;i++)
            {
                float a=R(0,6.28f);float len=R(1,3.5f);Vector3 p=new Vector3(Mathf.Cos(a)*rx*.96f,-len*.5f-.3f,Mathf.Sin(a)*rz*.96f);
                Box(root,"Hanging greenery",p,new Vector3(.24f,len,.18f),"Leaf");
                for(int l=0;l<4;l++) Ball(root,"Vine leaves",p+new Vector3((l%2==0?.18f:-.18f),len*(l/4f-.5f),0),new Vector3(.55f,.23f,.4f),"Leaf");
            }
            Combine(root,"Island_"+serial++);
        }

        static Mesh ArchMesh(float span,float depth)
        {
            var v=new List<Vector3>();var t=new List<int>();int n=18;float half=span*.5f;
            for(int i=0;i<n;i++)
            {
                float x0=Mathf.Lerp(-half,half,(float)i/n),x1=Mathf.Lerp(-half,half,(float)(i+1)/n);
                float y0=-3.8f+3.6f*Mathf.Sqrt(Mathf.Max(0,1-x0*x0/(half*half)));
                float y1=-3.8f+3.6f*Mathf.Sqrt(Mathf.Max(0,1-x1*x1/(half*half)));
                for(int side=-1;side<=1;side+=2)
                {
                    float z=side*depth*.5f;
                    Vector3 a=new Vector3(x0,.35f,z),b=new Vector3(x1,.35f,z),c=new Vector3(x1,y1,z),d=new Vector3(x0,y0,z);
                    if(side==1){Tri(v,t,a,c,b);Tri(v,t,a,d,c);}else{Tri(v,t,a,b,c);Tri(v,t,a,c,d);}
                }
                Vector3 e=new Vector3(x0,y0,-depth*.5f),f=new Vector3(x1,y1,-depth*.5f),g=new Vector3(x1,y1,depth*.5f),h=new Vector3(x0,y0,depth*.5f);
                Tri(v,t,e,f,g);Tri(v,t,e,g,h);
                Tri(v,t,new Vector3(x0,.35f,-depth*.5f),new Vector3(x1,.35f,depth*.5f),new Vector3(x1,.35f,-depth*.5f));
                Tri(v,t,new Vector3(x0,.35f,-depth*.5f),new Vector3(x0,.35f,depth*.5f),new Vector3(x1,.35f,depth*.5f));
            }
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }

        static void Railway(Transform parent,SkyRailPath path)
        {
            var bridge=Group("Continuous limestone arch viaduct",parent);
            int count=44;float step=path.Length/count;
            for(int i=0;i<count;i++)
            {
                path.Evaluate(i*step,out var a,out var ra);path.Evaluate((i+1)*step,out var b,out _);
                var mid=(a+b)*.5f;mid.y=0;var rot=Quaternion.LookRotation(Vector3.Cross((b-a).normalized,Vector3.up),Vector3.up);
                float span=Vector3.Distance(a,b);
                Shape("Arch span",SaveMesh(ArchMesh(span,2.65f),"Arch_"+i),bridge,mid,Vector3.one,mats["Ivory"],rot);
                var pier=a; pier.y=-4;
                Box(bridge,"Pier",pier,new Vector3(.58f,1.5f,2.72f),"Ivory",rot);
                Box(bridge,"Pier foot",pier+Vector3.down*.72f,new Vector3(.84f,.2f,2.95f),"Limestone",rot);
                for(int side=-1;side<=1;side+=2)
                {
                    Vector3 last=Vector3.zero;
                    for(int k=0;k<=18;k++)
                    {
                        float x=Mathf.Lerp(-span*.5f,span*.5f,k/18f);
                        float y=-3.8f+3.6f*Mathf.Sqrt(Mathf.Max(0,1-x*x/(span*span*.25f)));
                        var pt=mid+rot*new Vector3(x,y-.035f,side*1.355f);
                        if(k>0)Beam(bridge,"Arch brass reveal",last,pt,.065f,"Brass",6);
                        last=pt;
                    }
                }
            }
            for(float d=0;d<path.Length;d+=.58f)
            {
                path.Evaluate(d,out var p,out var q);p.y=.53f;
                Box(bridge,"Oak sleeper",p,new Vector3(2.25f,.16f,.23f),"Sleepers",q);
            }
            int railCount=420;
            for(int i=0;i<railCount;i++)
            {
                path.Evaluate(i*path.Length/railCount,out var a,out var qa);path.Evaluate((i+1)*path.Length/railCount,out var b,out var qb);
                for(int side=-1;side<=1;side+=2)
                {
                    var a1=a+qa*Vector3.right*(side*.78f); var b1=b+qb*Vector3.right*(side*.78f);
                    var mid=(a1+b1)*.5f;mid.y=.79f;
                    Box(bridge,"Steel rail",mid,new Vector3(.13f,.23f,Vector3.Distance(a1,b1)+.025f),"Rail",Quaternion.LookRotation(b1-a1));
                    var edge=(a+b)*.5f+qa*Vector3.right*(side*1.34f);edge.y=.38f;
                    Box(bridge,"Bridge cornice",edge,new Vector3(.12f,.2f,Vector3.Distance(a,b)+.035f),"Limestone",Quaternion.LookRotation(b-a));
                }
            }
            Combine(bridge,"Viaduct");
        }

        static void Roof(Transform p,Vector3 center,float width,float depth,float height,string name)
        {
            if(visualAssets){SoftRoof(p,center,width,depth,height,name);return;}
            var v=new List<Vector3>();var t=new List<int>();float w=width*.5f,d=depth*.5f;
            Vector3 a=new Vector3(-w,0,-d),b=new Vector3(w,0,-d),c=new Vector3(w,0,d),e=new Vector3(-w,0,d),f=new Vector3(-w,height,0),g=new Vector3(w,height,0);
            Tri(v,t,a,f,g);Tri(v,t,a,g,b);Tri(v,t,e,c,g);Tri(v,t,e,g,f);
            Tri(v,t,a,e,f);Tri(v,t,b,g,c);
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            Shape(name,SaveMesh(mesh,"Roof_"+serial++),p,center,Vector3.one,mats["Roof"]);
            Beam(p,"Gilded roof ridge",center+f,center+g,.085f,"Brass");
            foreach(var pair in new[]{new[]{a,f},new[]{e,f},new[]{b,g},new[]{c,g}}) Beam(p,"Roof fascia",center+pair[0],center+pair[1],.12f,"Porcelain");
            Box(p,"Front eave",center+new Vector3(0,0,-d),new Vector3(width,.2f,.2f),"RoofLight");
            Box(p,"Rear eave",center+new Vector3(0,0,d),new Vector3(width,.2f,.2f),"RoofLight");
            for(float x=-w+.35f;x<w;x+=.7f)
            {
                Beam(p,"Standing roof seam",center+new Vector3(x,height+.035f,0),center+new Vector3(x,.035f,-d),.024f,"RoofLight",5);
                Beam(p,"Standing roof seam",center+new Vector3(x,height+.035f,0),center+new Vector3(x,.035f,d),.024f,"RoofLight",5);
            }
        }
        static void Window(Transform p,Vector3 pos,float width,float height,bool door=false)
        {
            Box(p,door?"Recessed station doorway":"Recessed arched window",pos,new Vector3(width,height,.12f),"Window");
            Cylinder(p,"Arched window crown",pos+Vector3.up*height*.5f,width*.5f,.13f,"Window",20,1,Quaternion.Euler(90,0,0));
            for(int side=-1;side<=1;side+=2)
                Box(p,"Stone window jamb",pos+Vector3.right*(side*(width*.5f+.1f)),new Vector3(.18f,height+.08f,.23f),"Porcelain");
            Vector3 last=Vector3.zero;
            for(int i=0;i<=14;i++)
            {
                float a=i*Mathf.PI/14;
                var v=pos+new Vector3(Mathf.Cos(a)*(width*.5f+.1f),height*.5f+Mathf.Sin(a)*(width*.5f+.1f),-.075f);
                if(i>0)Beam(p,"Stone window arch",last,v,.1f,"Porcelain",6);last=v;
            }
            Box(p,"Window sill",pos+Vector3.down*(height*.5f+.08f),new Vector3(width+.45f,.16f,.38f),"Limestone");
            Box(p,"Brass central mullion",pos+Vector3.forward*-.09f,new Vector3(.055f,height,.06f),"Brass");
            if(!door)Box(p,"Brass cross mullion",pos+Vector3.forward*-.09f,new Vector3(width,.055f,.06f),"Brass");
            else for(int side=-1;side<=1;side+=2)Ball(p,"Door handle",pos+new Vector3(side*.12f,-.25f,-.16f),Vector3.one*.1f,"Brass");
        }
        static void Text(Transform p,string name,string content,Vector3 pos,float size,Color color,Quaternion? rot=null)
        {
            var t=Group(name,p);t.localPosition=pos;t.localRotation=rot??Quaternion.identity;
            var text=t.gameObject.AddComponent<TextMesh>();text.text=content;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=64;text.characterSize=size;text.color=color;
            text.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;
        }
        static void Station(Transform parent)
        {
            var p=Group("Station architecture",parent);
            Box(p,"Limestone foundation",new Vector3(-1,.23f,-6),new Vector3(16,.46f,9),"Limestone");
            Box(p,"Ivory foundation cap",new Vector3(-1,.52f,-6),new Vector3(16.5f,.18f,9.3f),"Porcelain");
            Box(p,"Main station hall",new Vector3(-1,2.75f,-5.6f),new Vector3(11.5f,4.4f,5.8f),"Ivory");
            Box(p,"Lower wall belt",new Vector3(-1,1,-5.6f),new Vector3(11.7f,.3f,6),"Limestone");
            Box(p,"Cornice",new Vector3(-1,4.95f,-5.6f),new Vector3(11.95f,.3f,6.2f),"Porcelain");
            Roof(p,new Vector3(-1,5.12f,-5.6f),12.7f,7f,2.4f,"Station blue folded roof");
            for(int i=0;i<5;i++) Window(p,new Vector3(-5.45f+i*2.2f,2.35f,-8.57f),1.22f,1.9f,i==2);
            for(int side=-1;side<=1;side+=2)
            {
                var wing=Group("Station side wing",p);wing.localPosition=new Vector3(-1+side*7.3f,0,-5.5f);
                Box(wing,"Wing walls",new Vector3(0,1.85f,0),new Vector3(2.7f,2.6f,4.9f),"Ivory");
                Window(wing,new Vector3(0,1.75f,-2.52f),1.1f,1.25f);
                Roof(wing,new Vector3(0,3.3f,0),3.6f,5.7f,1.6f,"Wing roof");
            }
            Box(p,"Front canopy",new Vector3(-1,3.75f,-10.15f),new Vector3(13.6f,.2f,3.5f),"Roof");
            Box(p,"Canopy cream fascia",new Vector3(-1,3.58f,-11.93f),new Vector3(13.8f,.26f,.16f),"Porcelain");
            for(int i=0;i<6;i++)
            {
                float x=-7.15f+i*2.45f;
                Cylinder(p,"Canopy column",new Vector3(x,2.07f,-11.7f),.12f,2.9f,"Ivory");
                Cylinder(p,"Column foot",new Vector3(x,.76f,-11.7f),.23f,.25f,"Limestone");
                Beam(p,"Canopy bracket",new Vector3(x,2.9f,-11.7f),new Vector3(x+.4f,3.5f,-11.7f),.07f,"Brass");
            }
            Box(p,"Name board",new Vector3(-1,4.5f,-8.8f),new Vector3(5.7f,.66f,.15f),"RoofDark");
            Text(p,"Station name","C L O U D L I N E",new Vector3(-1,4.49f,-8.91f),.11f,new Color(1,.91f,.7f));
            Box(p,"Moonstone platform",new Vector3(-1,.43f,-14),new Vector3(17.8f,.86f,4.1f),"Limestone");
            Box(p,"Platform paving",new Vector3(-1,.91f,-14),new Vector3(18.15f,.15f,4.4f),"Ivory");
            Box(p,"Safety strip",new Vector3(-1,1.003f,-16.05f),new Vector3(18,.035f,.14f),"Brass");
            for(int i=0;i<18;i++)Box(p,"Platform paving joint",new Vector3(-9.6f+i,1,-14),new Vector3(.028f,.012f,3.85f),"Limestone");
            for(int i=0;i<4;i++) Box(p,"Approach step",new Vector3(9.2f-i*.4f,.12f+i*.105f,-10.6f),new Vector3(1.5f,.24f+i*.21f,2.2f),"Ivory");

            var tower=Group("Clock tower",p);tower.localPosition=new Vector3(-6.6f,0,-3.6f);
            Box(tower,"Tower shaft",new Vector3(0,6.5f,0),new Vector3(3.35f,12,3.4f),"Ivory");
            foreach(float y in new[]{1.2f,6.4f,9.2f,12.3f})Box(tower,"Tower cornice",new Vector3(0,y,0),new Vector3(3.65f,.23f,3.7f),"Porcelain");
            for(int side=-1;side<=1;side+=2)
            {
                Box(tower,"Corner pilaster",new Vector3(side*1.43f,6.5f,-1.77f),new Vector3(.28f,11.6f,.2f),"Limestone");
                Window(tower,new Vector3(side*.72f,8,-1.79f),.6f,1.3f);
            }
            Cylinder(tower,"Clock brass rim",new Vector3(0,10.65f,-1.83f),1.05f,.16f,"Brass",32,1,Quaternion.Euler(90,0,0));
            Cylinder(tower,"Clock porcelain face",new Vector3(0,10.65f,-1.94f),.91f,.07f,"Porcelain",32,1,Quaternion.Euler(90,0,0));
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6;Box(tower,"Clock index",new Vector3(Mathf.Sin(a)*.73f,10.65f+Mathf.Cos(a)*.73f,-2),new Vector3(.055f,.15f,.035f),"RoofDark",Quaternion.Euler(0,0,-a*Mathf.Rad2Deg));
            }
            Beam(tower,"Clock minute hand",new Vector3(0,10.65f,-2.06f),new Vector3(.22f,11.25f,-2.06f),.045f,"RoofDark");
            Beam(tower,"Clock hour hand",new Vector3(0,10.65f,-2.08f),new Vector3(-.36f,10.84f,-2.08f),.055f,"RoofDark");
            Cylinder(tower,"Tower blue spire",new Vector3(0,13.8f,0),2.75f,2.6f,"Roof",4,.06f,Quaternion.Euler(0,45,0));
            Cylinder(tower,"Spire gold finial",new Vector3(0,15.55f,0),.055f,1,"Brass");Ball(tower,"Spire pearl",new Vector3(0,15.6f,0),Vector3.one*.32f,"Lamp");
            var rings=Group("Station celestial rings",parent);rings.localPosition=new Vector3(-6.6f,15.5f,-3.6f);
            for(int j=0;j<2;j++)
            {
                Vector3 last=Vector3.zero;var tilt=Quaternion.Euler(j==0?16:-25,0,j==0?14:-12);
                for(int i=0;i<=64;i++)
                {
                    float a=i*Mathf.PI*2/64;var cur=tilt*new Vector3(Mathf.Cos(a)*(2.55f+j*.6f),0,Mathf.Sin(a)*(2.55f+j*.6f));
                    if(i>0)Beam(rings,"Celestial brass ring",last,cur,.045f,"Brass",6);last=cur;
                }
            }
            Ball(rings,"Orbiting star",new Vector3(2.55f,.65f,0),Vector3.one*.25f,"Lamp");
            Combine(rings,"CelestialRings");
            Combine(p,"Station");
        }

        static void Tree(Transform p,Vector3 position,float size,bool pink)
        {
            var g=Group(pink?"Blossom tree":"Sage tree",p);g.localPosition=position;g.localScale=Vector3.one*size;
            Cylinder(g,"Tree trunk",new Vector3(0,1.15f,0),.14f,2.3f,"Trunk",7,.65f);
            for(int i=0;i<4;i++)
            {
                float a=i*Mathf.PI*.5f+.3f;var end=new Vector3(Mathf.Cos(a)*.8f,2.7f+R(-.25f,.25f),Mathf.Sin(a)*.75f);
                Beam(g,"Tree branch",new Vector3(0,1.4f,0),end,.085f,"Trunk",6);
                Ball(g,"Crown cluster",end+Vector3.up*.25f,new Vector3(1.85f,1.2f,1.5f),pink?(i%2==0?"Pink":"PinkLight"):(i%2==0?"Leaf":"LeafLight"));
            }
            Ball(g,"Crown top",new Vector3(0,3.2f,0),new Vector3(2.35f,1.4f,1.9f),pink?"PinkLight":"Leaf");
        }
        static void Lamp(Transform p,Vector3 pos)
        {
            Cylinder(p,"Lamp foot",pos+Vector3.up*.15f,.24f,.3f,"Limestone");
            Cylinder(p,"Lamp post",pos+Vector3.up*1.6f,.055f,3,"RoofDark",10);
            Box(p,"Lantern box",pos+Vector3.up*3.2f,new Vector3(.33f,.53f,.33f),"Lamp");
            Cylinder(p,"Lantern roof",pos+Vector3.up*3.55f,.31f,.2f,"RoofDark",4,.05f,Quaternion.Euler(0,45,0));
        }
        static void Fence(Transform p,Vector3 a,Vector3 b,int count)
        {
            for(int i=0;i<=count;i++)
            {
                var pt=Vector3.Lerp(a,b,(float)i/count);Cylinder(p,"Promenade baluster",pt+Vector3.up*.55f,.055f,1.1f,"Porcelain",8);
                Ball(p,"Baluster cap",pt+Vector3.up*1.12f,Vector3.one*.16f,"Brass");
            }
            Beam(p,"Promenade handrail",a+Vector3.up,b+Vector3.up,.07f,"Porcelain");
            Beam(p,"Lower railing",a+Vector3.up*.3f,b+Vector3.up*.3f,.035f,"Porcelain");
        }
        static void Bench(Transform p,Vector3 pos,float yaw)
        {
            var g=Group("Station bench",p);g.localPosition=pos;g.localRotation=Quaternion.Euler(0,yaw,0);
            for(int i=0;i<4;i++)Box(g,"Bench seat slat",new Vector3(0,.55f,-.24f+i*.16f),new Vector3(2.1f,.09f,.12f),"Timber");
            for(int i=0;i<3;i++)Box(g,"Bench back slat",new Vector3(0,.85f+i*.16f,.34f),new Vector3(2.1f,.1f,.08f),"Timber");
            foreach(float x in new[]{-.8f,.8f})
            {
                Box(g,"Bench feet",new Vector3(x,.26f,0),new Vector3(.09f,.52f,.6f),"RoofDark");
                Box(g,"Bench back support",new Vector3(x,.86f,.36f),new Vector3(.08f,.8f,.08f),"RoofDark");
            }
        }
        static void Gardens(Transform p)
        {
            // Broad, quiet paving gives the architecture breathing room.
            Box(p,"Station approach",new Vector3(-1,.12f,-.25f),new Vector3(4.4f,.15f,5),"Ivory");
            Box(p,"Garden promenade",new Vector3(7,.13f,3),new Vector3(19,.17f,2.1f),"Ivory",Quaternion.Euler(0,-12,0));
            Cylinder(p,"Fountain plaza",new Vector3(8,.15f,2),4.1f,.25f,"Limestone",32);
            Cylinder(p,"Plaza paving",new Vector3(8,.31f,2),3.92f,.1f,"Ivory",32);
            Cylinder(p,"Fountain bowl",new Vector3(8,.59f,2),2.45f,.5f,"Porcelain",24);
            Cylinder(p,"Fountain water",new Vector3(8,.86f,2),2.15f,.035f,"Water",32);
            Cylinder(p,"Fountain stem",new Vector3(8,1.25f,2),.2f,1.3f,"Limestone",12);
            Cylinder(p,"Fountain top dish",new Vector3(8,1.8f,2),1.05f,.22f,"Ivory",24,.85f);
            Ball(p,"Fountain pearl",new Vector3(8,2.1f,2),Vector3.one*.45f,"Brass");
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6;Beam(p,"Fountain trickle",new Vector3(8+Mathf.Cos(a)*.9f,1.75f,2+Mathf.Sin(a)*.9f),new Vector3(8+Mathf.Cos(a)*1.2f,.9f,2+Mathf.Sin(a)*1.2f),.025f,"Water",5);
            }
            Vector3[] positions={new Vector3(-13,0,-5),new Vector3(-13,0,1),new Vector3(-8,0,5),new Vector3(-2,0,8),new Vector3(5,0,9),new Vector3(13,0,7),new Vector3(15,0,-3),new Vector3(11,0,-8),new Vector3(-16,0,5)};
            for(int i=0;i<positions.Length;i++)Tree(p,positions[i],i==0?1.28f:R(.85f,1.17f),i%3!=1);
            for(int i=0;i<48;i++)
            {
                float a=R(0,6.28f),rad=R(.78f,.92f);var pos=new Vector3(Mathf.Cos(a)*19*rad,.23f,Mathf.Sin(a)*13.8f*rad);
                Ball(p,"Meadow shrub",pos,new Vector3(R(.55f,1),R(.35f,.6f),R(.6f,1)),i%3==0?"GrassDark":"LeafLight");
                if(i%3==0)for(int f=0;f<3;f++)Ball(p,"Wildflower",pos+new Vector3(R(-.4f,.4f),.2f,R(-.4f,.4f)),Vector3.one*.13f,"Porcelain");
            }
            foreach(float x in new[]{-8f,-3f,3f,7f})Lamp(p,new Vector3(x,1,-14.7f));
            Bench(p,new Vector3(-5.5f,1,-13.1f),0);Bench(p,new Vector3(4.7f,1,-13.1f),0);
            Bench(p,new Vector3(12,.05f,3.5f),-70);
            Fence(p,new Vector3(-15,.1f,-9),new Vector3(-16,.1f,3),10);
            Fence(p,new Vector3(13,.1f,9),new Vector3(3,.1f,11.8f),10);
            // A small open observatory creates a second silhouette behind the station.
            var g=Group("Hilltop garden pavilion",p);g.localPosition=new Vector3(-6,0,8);
            Cylinder(g,"Pavilion terrace",Vector3.up*.25f,3.35f,.5f,"Limestone",12);
            Cylinder(g,"Pavilion base",Vector3.up*.6f,3.05f,.25f,"Ivory",12);
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4;Cylinder(g,"Pavilion column",new Vector3(Mathf.Cos(a)*2.3f,2.35f,Mathf.Sin(a)*2.3f),.13f,3.3f,"Porcelain",10);
            }
            Cylinder(g,"Pavilion entablature",Vector3.up*4.05f,2.85f,.25f,"Ivory",8);
            Cylinder(g,"Pavilion blue roof",Vector3.up*4.9f,3.2f,1.6f,"Roof",8,.03f);
            Ball(g,"Pavilion finial",Vector3.up*5.8f,Vector3.one*.25f,"Brass");
            Combine(p,"Gardens");
        }

        static void Waterfall(Transform p)
        {
            Box(p,"Spring channel",new Vector3(18,.13f,2.6f),new Vector3(5.5f,.16f,2.2f),"Water");
            for(int i=0;i<7;i++)
            {
                float x=20.65f+i*.035f,z=1.7f+i*.29f;
                Box(p,"Falling water ribbon",new Vector3(x,-12.5f,z),new Vector3(.16f,25,R(.22f,.38f)),i%3==0?"WindowLight":"Water");
            }
            for(int i=0;i<7;i++)Ball(p,"Waterfall mist",new Vector3(20.8f+R(-1,1),-24.5f+R(-.7f,.7f),2.6f+R(-1.4f,1.4f)),new Vector3(R(1.4f,2.8f),R(1,1.8f),R(1.5f,3)),"Cloud");
            Combine(p,"Waterfall");
        }
        static void Backdrop(Transform parent)
        {
            Vector3[] positions={new Vector3(-34,3,27),new Vector3(28,5,31),new Vector3(48,-1,6),new Vector3(-39,-5,-3),new Vector3(2,9,50)};
            float[] widths={8,7,5.5f,4.4f,4.5f};
            for(int i=0;i<positions.Length;i++)
            {
                var g=Group("Drifting island "+(i+1),parent);g.localPosition=positions[i];
                Island(g,Vector3.zero,widths[i],widths[i]*.72f,widths[i]*1.2f,"Satellite island",14);
                if(i<2)
                {
                    Tree(g,new Vector3(-2,0,-.5f),.9f,true);Tree(g,new Vector3(2.5f,0,1),.65f,false);
                    var tower=Group("Distant beacon",g);tower.localPosition=new Vector3(0,0,1.5f);
                    Cylinder(tower,"Beacon tower",Vector3.up*2,1.05f,4,"Ivory",8,.85f);
                    Cylinder(tower,"Beacon cornice",Vector3.up*4,1.2f,.2f,"Porcelain",8);
                    Cylinder(tower,"Beacon roof",Vector3.up*4.8f,1.5f,1.4f,"Roof",8,.04f);
                    Ball(tower,"Beacon light",Vector3.up*5.6f,Vector3.one*.24f,"Lamp");
                }
                Combine(g,"Satellite_"+i);islets.Add(g);
            }
        }
        static void Clouds(Transform p)
        {
            // Distinct banks below the island, plus a few distant clouds at the horizon.
            for(int i=0;i<16;i++)
            {
                float a=i*Mathf.PI*2/16;float radius=R(34,70);
                var g=Group("Cloud bank "+(i+1),p);g.localPosition=new Vector3(Mathf.Cos(a)*radius,R(-27,-16),Mathf.Sin(a)*radius);
                float size=R(4,8);
                for(int k=0;k<5;k++)
                {
                    float crest=1f-Mathf.Abs(k-2)/2f;
                    float puff=.73f+crest*.45f;
                    Ball(g,"Cloud puff",new Vector3((k-2)*size*.36f,crest*size*.3f,R(-.5f,.5f)),new Vector3(size*puff,size*puff*1.12f,size*puff),"Cloud");
                }
                Combine(g,"CloudBank_"+i);clouds.Add(g);
            }
            for(int i=0;i<6;i++)
            {
                var g=Group("High cirrus "+i,p);g.localPosition=new Vector3(-60+i*25,R(10,20),R(55,90));
                for(int j=0;j<4;j++)
                {
                    float crest=j==1||j==2?1:0;
                    Ball(g,"Cloud puff",new Vector3(j*2.6f,crest*1.7f,0),new Vector3(5.8f,4.8f+crest*1.1f,5),"Cloud");
                }
                Combine(g,"HighCloud_"+i);clouds.Add(g);
            }
        }

        static void Wheel(Transform p,Vector3 pos,float radius)
        {
            var pivot=Group("Animated driving wheel",p);pivot.localPosition=pos;wheels.Add(pivot);
            var rot=Quaternion.Euler(0,0,90);
            Cylinder(pivot,"Steel tyre",Vector3.zero,radius,.18f,"Wheel",20,1,rot);
            Cylinder(pivot,"Cream wheel face",new Vector3(Mathf.Sign(pos.x)*.1f,0,0),radius*.78f,.035f,"Ivory",20,1,rot);
            Cylinder(pivot,"Blue wheel inset",new Vector3(Mathf.Sign(pos.x)*.125f,0,0),radius*.59f,.045f,"Engine",16,1,rot);
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4;
                Beam(pivot,"Brass wheel spoke",new Vector3(Mathf.Sign(pos.x)*.155f,0,0),new Vector3(Mathf.Sign(pos.x)*.155f,Mathf.Sin(a)*radius*.7f,Mathf.Cos(a)*radius*.7f),.027f,"Brass",6);
            }
            Cylinder(pivot,"Wheel hub",new Vector3(Mathf.Sign(pos.x)*.17f,0,0),.105f,.075f,"Brass",12,1,rot);
            Combine(pivot,"Wheel_"+serial++);
        }
        static Transform Engine(Transform p,List<Transform> rods)
        {
            var body=Group("Locomotive bodywork",p);
            Box(body,"Chassis",new Vector3(0,.86f,.1f),new Vector3(2.05f,.25f,4.8f),"EngineDark");
            Box(body,"Running board",new Vector3(0,1.04f,.15f),new Vector3(2.3f,.13f,4.9f),"Ivory");
            Cylinder(body,"Long blue boiler",new Vector3(0,1.75f,.65f),.72f,2.75f,"Engine",24,1,Quaternion.Euler(90,0,0));
            foreach(float z in new[]{-.45f,.65f,1.72f})Cylinder(body,"Boiler brass band",new Vector3(0,1.75f,z),.746f,.09f,"Brass",24,1,Quaternion.Euler(90,0,0));
            Cylinder(body,"Smokebox face",new Vector3(0,1.75f,2.09f),.66f,.13f,"EngineDark",24,1,Quaternion.Euler(90,0,0));
            Cylinder(body,"Smokebox rim",new Vector3(0,1.75f,2.18f),.46f,.05f,"Brass",24,1,Quaternion.Euler(90,0,0));
            Cylinder(body,"Smokebox door",new Vector3(0,1.75f,2.22f),.4f,.05f,"Engine",24,1,Quaternion.Euler(90,0,0));
            Cylinder(body,"Headlamp rim",new Vector3(0,2.28f,2.27f),.23f,.28f,"Brass",20,1,Quaternion.Euler(90,0,0));
            Cylinder(body,"Headlamp glass",new Vector3(0,2.28f,2.43f),.18f,.025f,"Lamp",20,1,Quaternion.Euler(90,0,0));
            Cylinder(body,"Chimney stem",new Vector3(0,2.85f,1.2f),.22f,1.05f,"EngineDark",14,1.45f);
            Cylinder(body,"Chimney brass lip",new Vector3(0,3.39f,1.2f),.35f,.12f,"Brass",16);
            Cylinder(body,"Steam dome",new Vector3(0,2.54f,-.05f),.31f,.48f,"Brass",16,.8f);
            Ball(body,"Steam dome cap",new Vector3(0,2.8f,-.05f),new Vector3(.5f,.25f,.5f),"Brass");
            Box(body,"Cab back",new Vector3(0,1.94f,-1.9f),new Vector3(2.05f,1.6f,.12f),"Ivory");
            Box(body,"Cab back window",new Vector3(0,2.18f,-1.98f),new Vector3(1.35f,.7f,.04f),"Window");
            Box(body,"Cab front",new Vector3(0,2.15f,-.64f),new Vector3(2.05f,1.9f,.12f),"Ivory");
            for(int side=-1;side<=1;side+=2)
            {
                Box(body,"Cab lower side",new Vector3(side*1.02f,1.43f,-1.28f),new Vector3(.13f,.72f,1.4f),"Engine");
                foreach(float z in new[]{-1.96f,-.62f})Box(body,"Cab window pillar",new Vector3(side*1.02f,2.36f,z),new Vector3(.16f,1.28f,.15f),"Ivory");
                Box(body,"Cab side glazing",new Vector3(side*1.025f,2.35f,-1.28f),new Vector3(.04f,.88f,1.08f),"WindowLight");
                Box(body,"Cab gold waistline",new Vector3(side*1.105f,1.78f,-1.28f),new Vector3(.05f,.06f,1.5f),"Brass");
                Box(body,"Cab step",new Vector3(side*1.12f,.65f,-1.47f),new Vector3(.38f,.12f,.65f),"EngineDark");
                Beam(body,"Boiler handrail",new Vector3(side*.8f,2.08f,-.4f),new Vector3(side*.8f,2.08f,1.8f),.035f,"Brass");
            }
            Roof(body,new Vector3(0,3.03f,-1.3f),2.55f,1.95f,.44f,"Cab blue roof");
            Box(body,"Front buffer beam",new Vector3(0,.9f,2.52f),new Vector3(2.25f,.28f,.22f),"Ivory");
            foreach(float x in new[]{-.75f,.75f})Cylinder(body,"Front buffer",new Vector3(x,.9f,2.73f),.16f,.24f,"EngineDark",12,1,Quaternion.Euler(90,0,0));
            for(int i=0;i<7;i++)Beam(body,"Cowcatcher slat",new Vector3((i-3)*.26f,.75f,2.55f),new Vector3((i-3)*.35f,.3f,2.95f),.055f,"Brass");
            Box(body,"Coupling",new Vector3(0,.7f,-2.65f),new Vector3(.18f,.17f,.8f),"EngineDark");
            Combine(body,"LocomotiveBody");
            foreach(float x in new[]{-.94f,.94f}) foreach(float z in new[]{-1.3f,0,1.3f}) Wheel(p,new Vector3(x,.44f,z),.44f);
            foreach(float x in new[]{-1.2f,1.2f})
            {
                var rod=Box(p,"Animated coupling rod",new Vector3(x,.44f,0),new Vector3(.085f,.115f,2.7f),"Brass").transform;rods.Add(rod);
            }
            var chimney=Group("Chimney emission point",p);chimney.localPosition=new Vector3(0,3.48f,1.2f);
            return chimney;
        }
        static void Coach(Transform p)
        {
            var body=Group("Carriage bodywork",p);
            Box(body,"Carriage chassis",new Vector3(0,.8f,0),new Vector3(2.15f,.3f,4.7f),"EngineDark");
            Box(body,"Carriage floor",new Vector3(0,1.02f,0),new Vector3(2.3f,.17f,4.85f),"Ivory");
            Box(body,"Cream coach body",new Vector3(0,1.9f,0),new Vector3(2.15f,1.65f,3.7f),"Ivory");
            Box(body,"Blue waist band",new Vector3(0,1.46f,0),new Vector3(2.21f,.25f,3.76f),"Engine");
            foreach(float side in new[]{-1f,1f})
            {
                for(int i=0;i<3;i++)
                {
                    float z=-1.16f+i*1.15f;
                    Box(body,"Passenger glazing",new Vector3(side*1.09f,2.18f,z),new Vector3(.04f,.86f,.8f),"Window");
                    foreach(float off in new[]{-.43f,.43f})Box(body,"Window upright",new Vector3(side*1.13f,2.18f,z+off),new Vector3(.07f,1.02f,.07f),"Brass");
                    Box(body,"Window top",new Vector3(side*1.13f,2.65f,z),new Vector3(.07f,.06f,.9f),"Brass");
                    Box(body,"Window sill",new Vector3(side*1.14f,1.72f,z),new Vector3(.1f,.08f,.94f),"Porcelain");
                    Box(body,"Window divider",new Vector3(side*1.13f,2.18f,z),new Vector3(.07f,.86f,.035f),"Brass");
                }
                Box(body,"Coach gold lining",new Vector3(side*1.13f,1.3f,0),new Vector3(.045f,.06f,3.85f),"Brass");
                Box(body,"Coach lower step",new Vector3(side*1.14f,.65f,-2.05f),new Vector3(.36f,.13f,.6f),"EngineDark");
            }
            var coachRoof=Group("Longitudinal carriage roof",body);coachRoof.localPosition=new Vector3(0,2.89f,0);coachRoof.localRotation=Quaternion.Euler(0,90,0);
            Roof(coachRoof,Vector3.zero,4.65f,2.6f,.45f,"Carriage blue roof");
            foreach(float z in new[]{-2.3f,2.3f})
            {
                Fence(body,new Vector3(-.95f,1.04f,z),new Vector3(.95f,1.04f,z),4);
                Box(body,"Coupling",new Vector3(0,.7f,z*1.13f),new Vector3(.2f,.17f,.65f),"EngineDark");
            }
            Combine(body,"CarriageBody");
            foreach(float x in new[]{-.94f,.94f})foreach(float z in new[]{-1.4f,1.4f})Wheel(p,new Vector3(x,.44f,z),.44f);
        }
        static ParticleSystem Steam(Transform chimney)
        {
            var go=new GameObject("Soft drifting steam");go.transform.SetParent(chimney,false);
            var ps=go.AddComponent<ParticleSystem>();var main=ps.main;
            main.duration=6;main.loop=true;main.startLifetime=new ParticleSystem.MinMaxCurve(2.8f,4.8f);
            main.startSpeed=new ParticleSystem.MinMaxCurve(.7f,1.1f);main.startSize=new ParticleSystem.MinMaxCurve(.4f,.7f);
            main.startColor=new Color(.96f,.98f,1,.6f);main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=80;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=12;shape.radius=.12f;shape.rotation=new Vector3(-90,0,0);
            var emission=ps.emission;emission.rateOverTime=4.5f;
            var vel=ps.velocityOverLifetime;vel.enabled=true;vel.space=ParticleSystemSimulationSpace.World;vel.x=.22f;vel.y=.5f;vel.z=-.15f;
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.5f,1,3.6f));
            var col=ps.colorOverLifetime;col.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.5f,0),new GradientAlphaKey(.38f,.25f),new GradientAlphaKey(0,1)});col.color=gradient;
            string texturePath=Gen+"/SteamSoft.asset";var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if(texture==null)
            {
                texture=new Texture2D(64,64,TextureFormat.RGBA32,false);texture.name="Soft steam";
                for(int y=0;y<64;y++)for(int x=0;x<64;x++)
                {
                    float d=Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f))/31.5f;
                    texture.SetPixel(x,y,new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-d*d),2)));
                }
                texture.Apply();AssetDatabase.CreateAsset(texture,texturePath);
            }
            string matPath=Gen+"/Materials/Steam.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(material,matPath);}
            material.SetTexture("_BaseMap",texture);material.SetColor("_BaseColor",Color.white);
            material.SetFloat("_Surface",1);material.SetFloat("_Blend",0);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
            var render=go.GetComponent<ParticleSystemRenderer>();render.sharedMaterial=material;render.shadowCastingMode=ShadowCastingMode.Off;
            return ps;
        }
        static void Lighting(Transform world)
        {
            var sun=new GameObject("Warm morning sun");sun.transform.SetParent(world,false);sun.transform.rotation=Quaternion.Euler(43,-32,0);
            var light=sun.AddComponent<Light>();light.type=LightType.Directional;light.color=new Color(1,.94f,.82f);light.intensity=2.1f;
            light.shadows=LightShadows.Soft;light.shadowStrength=.83f;light.shadowBias=.035f;light.shadowNormalBias=.25f;RenderSettings.sun=light;
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.48f,.63f,.78f);RenderSettings.ambientEquatorColor=new Color(.48f,.58f,.66f);RenderSettings.ambientGroundColor=new Color(.27f,.36f,.46f);
            RenderSettings.ambientIntensity=1;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(.76f,.85f,.9f);RenderSettings.fogDensity=.0035f;
            string skyPath=Gen+"/Materials/PastelSky.mat";var sky=AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if(sky==null){sky=new Material(Shader.Find("CloudRail/Pastel Sky"));AssetDatabase.CreateAsset(sky,skyPath);}
            sky.SetColor("_Horizon",new Color(.72f,.86f,1));sky.SetColor("_Bottom",new Color(.78f,.89f,1));EditorUtility.SetDirty(sky);RenderSettings.skybox=sky;
            var go=new GameObject("Gentle bloom and color");go.transform.SetParent(world,false);var volume=go.AddComponent<Volume>();volume.isGlobal=true;
            string profilePath=Gen+"/MorningProfile.asset";var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,profilePath);}
            if(!profile.TryGet<Bloom>(out var bloom)){bloom=profile.Add<Bloom>(true);AssetDatabase.AddObjectToAsset(bloom,profile);}
            bloom.threshold.Override(1.3f);bloom.intensity.Override(.14f);bloom.scatter.Override(.6f);
            if(!profile.TryGet<Tonemapping>(out var tone)){tone=profile.Add<Tonemapping>(true);AssetDatabase.AddObjectToAsset(tone,profile);}
            tone.mode.Override(TonemappingMode.Neutral);
            volume.sharedProfile=profile;EditorUtility.SetDirty(profile);
            // Keep the template pipeline intact and save a milestone-specific quality asset.
            string pipelinePath=Gen+"/CloudRail_RenderPipeline.asset";
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if(pipeline==null)
            {
                pipeline=Object.Instantiate((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline);
                pipeline.name="CloudRail_RenderPipeline";AssetDatabase.CreateAsset(pipeline,pipelinePath);
            }
            pipeline.shadowDistance=145;pipeline.msaaSampleCount=4;EditorUtility.SetDirty(pipeline);QualitySettings.renderPipeline=pipeline;
            QualitySettings.shadowDistance=120;
        }
    }
}
