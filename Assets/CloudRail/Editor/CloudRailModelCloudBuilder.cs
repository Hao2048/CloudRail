using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace CloudRail.Editor
{
 public static partial class CloudRailBuilder
 {
  struct CloudLobe{public Vector3 at,r;public CloudLobe(Vector3 a,Vector3 r){at=a;this.r=r;}}
  static Mesh SculptCloud(int variant)
  {
   var random=new System.Random(831+variant*73);float Random(float a,float b)=>a+(float)random.NextDouble()*(b-a);
   var lobes=new List<CloudLobe>{new CloudLobe(new Vector3(0,-.85f,0),new Vector3(6.6f,1.5f,3.8f))};
   for(int i=0;i<5;i++){float x=-4.5f+i*2.2f;lobes.Add(new CloudLobe(new Vector3(x,(i==1||i==3?Random(1.1f,1.6f):Random(-.2f,.55f)),Random(-1,1)),new Vector3(Random(2.1f,2.7f),Random(1.9f,2.6f),Random(2.2f,2.9f))));}
   lobes.Add(new CloudLobe(new Vector3(Random(-1.6f,1.6f),1.8f,0),new Vector3(2.3f,2.7f,2.5f)));
   float Field(Vector3 p)
   {
    float d=100;
    foreach(var l in lobes){var q=p-l.at;var a=new Vector3(q.x/l.r.x,q.y/l.r.y,q.z/l.r.z);var b=new Vector3(q.x/(l.r.x*l.r.x),q.y/(l.r.y*l.r.y),q.z/(l.r.z*l.r.z));float next=a.sqrMagnitude<1e-8f?-Mathf.Min(l.r.x,Mathf.Min(l.r.y,l.r.z)):a.magnitude*(a.magnitude-1)/b.magnitude;float h=Mathf.Clamp01(.5f+.5f*(next-d)/.65f);d=Mathf.Lerp(next,d,h)-.65f*h*(1-h);}
    float grain=(Mathf.PerlinNoise(p.x*.38f+variant*3,p.y*.38f+9)+Mathf.PerlinNoise(p.z*.4f+12,p.x*.3f+variant)-1)*.14f;return d+grain;
   }
   Vector3 Normal(Vector3 p){const float e=.04f;return new Vector3(Field(p+Vector3.right*e)-Field(p-Vector3.right*e),Field(p+Vector3.up*e)-Field(p-Vector3.up*e),Field(p+Vector3.forward*e)-Field(p-Vector3.forward*e)).normalized;}
   const int nx=34,ny=20,nz=26;var points=new Vector3[nx*ny*nz];var values=new float[points.Length];int Index(int x,int y,int z)=>(x*ny+y)*nz+z;
   for(int x=0;x<nx;x++)for(int y=0;y<ny;y++)for(int z=0;z<nz;z++){var p=new Vector3(Mathf.Lerp(-9,9,x/(float)(nx-1)),Mathf.Lerp(-4,6,y/(float)(ny-1)),Mathf.Lerp(-6.5f,6.5f,z/(float)(nz-1)));int i=Index(x,y,z);points[i]=p;values[i]=Field(p);}
   var vertices=new List<Vector3>();var normals=new List<Vector3>();var tris=new List<int>();var cache=new Dictionary<ulong,int>();
   // Cache by sampling-grid edge, not rounded positions: adjacent tetrahedra must share exactly one vertex.
   int Edge(int a,int b){if(a>b){int swap=a;a=b;b=swap;}ulong key=((ulong)(uint)a<<32)|(uint)b;if(cache.TryGetValue(key,out int at))return at;var p=Vector3.Lerp(points[a],points[b],values[a]/(values[a]-values[b]));at=vertices.Count;vertices.Add(p);normals.Add(Normal(p));cache.Add(key,at);return at;}
   void Face(int ia,int ib,int ic){var a=vertices[ia];var b=vertices[ib];var c=vertices[ic];if(Vector3.Dot(Vector3.Cross(b-a,c-a),normals[ia]+normals[ib]+normals[ic])<0){int temp=ib;ib=ic;ic=temp;}if(ia!=ib&&ib!=ic&&ia!=ic)tris.AddRange(new[]{ia,ib,ic});}
   void Tet(int a,int b,int c,int d){int[] ids={a,b,c,d};var inside=new List<int>(4);var outside=new List<int>(4);foreach(int i in ids)if(values[i]<0)inside.Add(i);else outside.Add(i);if(inside.Count==0||inside.Count==4)return;if(inside.Count==1)Face(Edge(inside[0],outside[0]),Edge(inside[0],outside[1]),Edge(inside[0],outside[2]));else if(inside.Count==3)Face(Edge(outside[0],inside[0]),Edge(outside[0],inside[1]),Edge(outside[0],inside[2]));else{var p0=Edge(inside[0],outside[0]);var p1=Edge(inside[0],outside[1]);var p2=Edge(inside[1],outside[0]);var p3=Edge(inside[1],outside[1]);Face(p0,p1,p2);Face(p1,p3,p2);}}
   for(int x=0;x<nx-1;x++)for(int y=0;y<ny-1;y++)for(int z=0;z<nz-1;z++){int a=Index(x,y,z),b=Index(x+1,y,z),c=Index(x+1,y+1,z),d=Index(x,y+1,z),e=Index(x,y,z+1),f=Index(x+1,y,z+1),g=Index(x+1,y+1,z+1),h=Index(x,y+1,z+1);Tet(a,b,c,g);Tet(a,c,d,g);Tet(a,d,h,g);Tet(a,h,e,g);Tet(a,e,f,g);Tet(a,f,b,g);}
   var mesh=new Mesh{indexFormat=IndexFormat.UInt16};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetTriangles(tris,0);mesh.RecalculateBounds();return SaveClosedCloudMesh(mesh,"ClosedModelCloud_"+variant);
  }
  static Mesh SaveClosedCloudMesh(Mesh source,string name)
  {
   // Explicitly rebuild the vertex/index buffers. CopySerialized left stale GPU geometry after regeneration.
   string path=Gen+"/Meshes/"+name+".asset";source.name=name;
   var target=AssetDatabase.LoadAssetAtPath<Mesh>(path);
   if(target==null){AssetDatabase.CreateAsset(source,path);return source;}
   target.Clear();target.indexFormat=IndexFormat.UInt16;target.vertices=source.vertices;target.normals=source.normals;target.triangles=source.triangles;target.RecalculateBounds();EditorUtility.SetDirty(target);Object.DestroyImmediate(source);return target;
  }
  [MenuItem("Cloud Rail/Replace Volume Clouds With Sculpted Models")]
  public static void BuildModelClouds()
  {
   if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");visualAssets=true;gameplayAssets=true;desktopAssets=releaseAssets=false;
   var game=Object.FindAnyObjectByType<JourneyGame>();var world=game.transform.parent;var oldQuality=game.GetComponent<CloudRenderQuality>();if(oldQuality!=null)Object.DestroyImmediate(oldQuality);
   foreach(string name in new[]{"18  Continuous volumetric cloud sea","19  Cloud clusters around the islands"}){var t=world.Find(name);if(t!=null)t.gameObject.SetActive(false);}
   foreach(var r in world.GetComponentsInChildren<MeshRenderer>(true))if(r.name=="Cloud")r.enabled=false;
   var old=world.Find("23  Sculpted model cloud sea");if(old!=null)Object.DestroyImmediate(old.gameObject);var root=Group("23  Sculpted model cloud sea",world);
   const string path="Assets/CloudRail/Generated/Visual/Materials/SculptedCloud.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Shader.Find("CloudRail/Soft Model Cloud"));AssetDatabase.CreateAsset(mat,path);}mat.shader=Shader.Find("CloudRail/Soft Model Cloud");mat.SetFloat("_Cull",2);mat.SetColor("_BaseColor",new Color(.98f,.98f,.97f));mat.SetColor("_Shade",new Color(.60f,.72f,.87f));mat.enableInstancing=false;EditorUtility.SetDirty(mat);
   var meshes=new Mesh[6];for(int i=0;i<meshes.Length;i++)meshes[i]=SculptCloud(i);
   Vector3[] centers={new Vector3(-19,-25,-20),new Vector3(9,-29,-28),new Vector3(31,-24,3),new Vector3(-1,-26,28),new Vector3(-39,-27,16),new Vector3(44,-27,34),new Vector3(-53,-31,-33),new Vector3(31,-31,-51),new Vector3(8,-34,59),new Vector3(-77,-32,12),new Vector3(68,-29,9),new Vector3(-32,-29,-58),new Vector3(-65,-35,64),new Vector3(64,-34,64),new Vector3(-3,-36,-81),new Vector3(16,-34,91),new Vector3(-92,-38,-32),new Vector3(100,-35,-27),new Vector3(-84,-35,102),new Vector3(81,-37,112),new Vector3(-31,-40,130),new Vector3(39,-41,-125),new Vector3(-46,1,-4),new Vector3(60,4,25),new Vector3(-18,9,48)};
   var transforms=new List<Transform>();var random=new System.Random(4921);
   for(int i=0;i<centers.Length;i++){bool side=i>=22;float s=side?.8f:(i>=12?2.25f:1.55f);var size=new Vector3(s*(.9f+(float)random.NextDouble()*.4f),s*(.75f+(float)random.NextDouble()*.4f),s);var go=Shape("Sculpted cloud bank "+i,meshes[i%6],root,centers[i],size,mat,Quaternion.Euler(0,-50+(float)random.NextDouble()*65,0));go.layer=9;var r=go.GetComponent<Renderer>();r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;transforms.Add(go.transform);}
   var ambience=Object.FindAnyObjectByType<SkyAmbience>();ambience.clouds=transforms.ToArray();game.world.cloudBanks=transforms.Select(t=>t.gameObject).ToArray();
   AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
  }
 }
}
