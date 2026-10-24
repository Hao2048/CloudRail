using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace CloudRail.Editor
{
 public static partial class CloudRailBuilder
 {
  static Vector3Int RockVertexKey(Vector3 p)=>new Vector3Int(Mathf.RoundToInt(p.x*10000),Mathf.RoundToInt(p.y*10000),Mathf.RoundToInt(p.z*10000));
  static void SmoothRockNormals(Mesh mesh)
  {
   var vertices=mesh.vertices;var tris=mesh.triangles;var sums=new Dictionary<Vector3Int,Vector3>();
   for(int i=0;i<tris.Length;i+=3){var n=Vector3.Cross(vertices[tris[i+1]]-vertices[tris[i]],vertices[tris[i+2]]-vertices[tris[i]]);for(int k=0;k<3;k++){var key=RockVertexKey(vertices[tris[i+k]]);sums.TryGetValue(key,out var old);sums[key]=old+n;}}
   var normals=new Vector3[vertices.Length];for(int i=0;i<normals.Length;i++)normals[i]=sums[RockVertexKey(vertices[i])].normalized;mesh.normals=normals;
  }
  [MenuItem("Cloud Rail/Repair Cliff Triangles")]
  public static void RepairCliffRendering()
  {
   if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");visualAssets=true;gameplayAssets=true;releaseAssets=desktopAssets=false;
   var game=Object.FindAnyObjectByType<JourneyGame>();var world=game.transform.parent;
   var rocks=world.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<Renderer>()!=null&&f.GetComponent<Renderer>().enabled&&f.GetComponent<Renderer>().sharedMaterial!=null&&f.GetComponent<Renderer>().sharedMaterial.name.StartsWith("Rock")).ToArray();
   foreach(var group in rocks.GroupBy(f=>f.transform.parent))
   {
    var filters=group.ToArray();float bottom=filters.Min(f=>f.GetComponent<Renderer>().bounds.min.y);var bottomPoints=new Dictionary<Vector3Int,Vector3>();
    foreach(var f in filters)foreach(var v in f.sharedMesh.vertices){var p=f.transform.TransformPoint(v);if(Mathf.Abs(p.y-bottom)<.002f)bottomPoints[RockVertexKey(p)]=p;}
    Vector3 tip=Vector3.zero;foreach(var p in bottomPoints.Values)tip+=p;if(bottomPoints.Count>0)tip/=bottomPoints.Count;
    var meshCopies=new Dictionary<MeshFilter,Mesh>();var worldVertices=new Dictionary<MeshFilter,Vector3[]>();var sums=new Dictionary<Vector3Int,Vector3>();
    foreach(var f in filters)
    {
     var mesh=Object.Instantiate(f.sharedMesh);var vertices=mesh.vertices;
     for(int i=0;i<vertices.Length;i++){var p=f.transform.TransformPoint(vertices[i]);if(Mathf.Abs(p.y-bottom)<.002f)vertices[i]=f.transform.InverseTransformPoint(tip);}
     mesh.vertices=vertices;var w=vertices.Select(v=>f.transform.TransformPoint(v)).ToArray();worldVertices[f]=w;
     for(int sub=0;sub<mesh.subMeshCount;sub++)
     {
      var source=mesh.GetTriangles(sub);var kept=new List<int>();
      for(int i=0;i<source.Length;i+=3){var n=Vector3.Cross(w[source[i+1]]-w[source[i]],w[source[i+2]]-w[source[i]]);if(n.sqrMagnitude<1e-10f)continue;
       for(int k=0;k<3;k++){int index=source[i+k];kept.Add(index);var key=RockVertexKey(w[index]);sums.TryGetValue(key,out var old);sums[key]=old+n;}}
      mesh.SetTriangles(kept,sub);
     }
     meshCopies[f]=mesh;
    }
    foreach(var f in filters)
    {
     var mesh=meshCopies[f];var w=worldVertices[f];var normals=new Vector3[w.Length];for(int i=0;i<w.Length;i++){sums.TryGetValue(RockVertexKey(w[i]),out var n);normals[i]=f.transform.InverseTransformDirection(n.normalized);}
     mesh.normals=normals;mesh.RecalculateBounds();if(mesh.uv.Length==w.Length)mesh.RecalculateTangents();
     string sourcePath=AssetDatabase.GetAssetPath(f.sharedMesh);string name=f.sharedMesh.name.StartsWith("SmoothCliff_")?f.sharedMesh.name:"SmoothCliff_"+AssetDatabase.AssetPathToGUID(sourcePath);
     f.sharedMesh=SaveMesh(mesh,name);var collider=f.GetComponent<MeshCollider>();if(collider!=null)collider.sharedMesh=f.sharedMesh;
    }
   }
   // The five former random face colours become one continuous stone response.
   foreach(var material in rocks.Select(f=>f.GetComponent<Renderer>().sharedMaterial).Distinct()){material.SetColor("_BaseColor",new Color(.56f,.65f,.73f));material.SetFloat("_Smoothness",.14f);EditorUtility.SetDirty(material);}
   RepairCottageWaterfall(world);
   AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
  }
  static void RepairCottageWaterfall(Transform world)
  {
   var cottage=world.Find("20  Railway scenic landmarks/Waterfall keeper cottage");if(cottage==null)return;
   foreach(var f in cottage.GetComponentsInChildren<MeshFilter>())
   {
    if(f.name=="Soft flowing waterfall")continue;var mesh=Object.Instantiate(f.sharedMesh);var vertices=mesh.vertices;var source=mesh.triangles;var kept=new List<int>();
    for(int i=0;i<source.Length;i+=3){bool ribbon=true,below=false;for(int k=0;k<3;k++){var p=vertices[source[i+k]];ribbon&=Mathf.Abs(p.x)<.65f&&Mathf.Abs(p.z+4.8f)<.2f;below|=p.y<-.02f;}if(ribbon&&below)continue;kept.Add(source[i]);kept.Add(source[i+1]);kept.Add(source[i+2]);}
    if(kept.Count==source.Length){Object.DestroyImmediate(mesh);continue;}mesh.triangles=kept.ToArray();mesh.RecalculateBounds();f.sharedMesh=SaveMesh(mesh,"CottageWithoutRigidRibbons_"+f.name);
   }
   var old=cottage.Find("Soft flowing waterfall");if(old!=null)Object.DestroyImmediate(old.gameObject);
   const string path="Assets/CloudRail/Generated/Visual/Materials/SoftWaterfall.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(Shader.Find("CloudRail/Soft Waterfall"));AssetDatabase.CreateAsset(material,path);}
   var v=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
   for(int side=0;side<2;side++){var q=Quaternion.Euler(0,side*90,0);int n=v.Count;v.Add(q*new Vector3(-.55f,0,0));v.Add(q*new Vector3(.55f,0,0));v.Add(q*new Vector3(.4f,-12,0));v.Add(q*new Vector3(-.4f,-12,0));uv.Add(new Vector2(0,0));uv.Add(new Vector2(1,0));uv.Add(new Vector2(1,1));uv.Add(new Vector2(0,1));tris.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});}
   var stream=new Mesh();stream.SetVertices(v);stream.SetUVs(0,uv);stream.SetTriangles(tris,0);stream.RecalculateBounds();var go=Shape("Soft flowing waterfall",SaveMesh(stream,"CottageWaterfallSoft"),cottage,new Vector3(0,0,-4.8f),Vector3.one,material);go.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
  }
 }
}
