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
  struct RockPoint{public Vector3 p,n;public RockPoint(Vector3 p,Vector3 n){this.p=p;this.n=n;}}
  static float StoneNoise(Vector3 p,float scale)=> (Mathf.PerlinNoise(p.x*scale+12.3f,p.y*scale+3.1f)+Mathf.PerlinNoise(p.z*scale+25.4f,p.y*scale+7.2f)+Mathf.PerlinNoise(p.x*scale+4.7f,p.z*scale+10.1f))/3;
  [MenuItem("Cloud Rail/Natural Rock Relief And Deep Cloud Sea")]
  public static void RockAndDeepSea()
  {
   if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");visualAssets=true;gameplayAssets=true;desktopAssets=releaseAssets=false;
   var game=Object.FindAnyObjectByType<JourneyGame>();var root=game.transform.parent;
   const string materialPath="Assets/CloudRail/Generated/Visual/Materials/NaturalRockRelief.mat";var stone=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(stone==null){stone=new Material(Shader.Find("CloudRail/Natural Continuous Rock"));AssetDatabase.CreateAsset(stone,materialPath);}stone.SetTexture("_Noise",AssetDatabase.LoadAssetAtPath<Texture3D>(Gen+"/CloudDensityNoise.asset"));stone.SetColor("_BaseColor",new Color(.55f,.64f,.71f));stone.SetFloat("_Relief",.78f);EditorUtility.SetDirty(stone);
   var filters=root.GetComponentsInChildren<MeshFilter>().Where(f=>{var r=f.GetComponent<Renderer>();return r!=null&&r.enabled&&r.sharedMaterial!=null&&(r.sharedMaterial.name.StartsWith("Rock")||r.sharedMaterial==stone);}).ToArray();
   var newMeshes=new Dictionary<MeshFilter,Mesh>();var normalSums=new Dictionary<Vector3Int,Vector3>();
   foreach(var f in filters)
   {
    var source=f.sharedMesh;if(source.name.StartsWith("DetailedCliff_")){f.GetComponent<Renderer>().sharedMaterial=stone;continue;}
    var vertices=source.vertices;var normals=source.normals;var indices=source.triangles;var output=new List<Vector3>();var outTriangles=new List<int>();
    var siblings=filters.Where(x=>x.transform.parent==f.transform.parent).Select(x=>x.GetComponent<Renderer>().bounds).ToArray();float top=siblings.Max(b=>b.max.y),bottom=siblings.Min(b=>b.min.y);
    var lawns=f.transform.parent.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.sharedMaterial!=null&&r.sharedMaterial.name=="Grass").ToArray();if(lawns.Length>0)top=lawns.Max(r=>r.bounds.max.y);
    Vector3 rockCenter=new Vector3((siblings.Min(b=>b.min.x)+siblings.Max(b=>b.max.x))*.5f,0,(siblings.Min(b=>b.min.z)+siblings.Max(b=>b.max.z))*.5f);
    bool cliff=top-bottom>4;int levels=siblings.Max(b=>Mathf.Max(b.size.x,b.size.z))>24?3:2;
    RockPoint Mid(RockPoint a,RockPoint b)=>new RockPoint((a.p+b.p)*.5f,(a.n+b.n).normalized);
    void Triangle(RockPoint a,RockPoint b,RockPoint c,int depth)
    {
     float longest=Mathf.Max((a.p-b.p).sqrMagnitude,Mathf.Max((b.p-c.p).sqrMagnitude,(c.p-a.p).sqrMagnitude));
     if(cliff&&depth<levels){var ab=Mid(a,b);var bc=Mid(b,c);var ca=Mid(c,a);Triangle(a,ab,ca,depth+1);Triangle(ab,b,bc,depth+1);Triangle(ca,bc,c,depth+1);Triangle(ab,bc,ca,depth+1);return;}
     foreach(var v in new[]{a,b,c})
     {
      Vector3 world=f.transform.TransformPoint(v.p);float fade=Mathf.SmoothStep(0,1,(top-world.y)/2.2f)*Mathf.SmoothStep(0,1,(world.y-bottom)/1.5f);
      float swell=(StoneNoise(world,.23f)-.5f)*1.6f;float strata=Mathf.Pow(1-Mathf.Abs(Mathf.Sin(world.y*1.1f+StoneNoise(world,.11f)*3)),5)*.13f;
      float amount=cliff?(swell-strata)*fade:0;world+=new Vector3(world.x-rockCenter.x,0,world.z-rockCenter.z).normalized*amount;
      outTriangles.Add(output.Count);output.Add(f.transform.InverseTransformPoint(world));
     }
    }
    for(int i=0;i<indices.Length;i+=3)Triangle(new RockPoint(vertices[indices[i]],normals[indices[i]]),new RockPoint(vertices[indices[i+1]],normals[indices[i+1]]),new RockPoint(vertices[indices[i+2]],normals[indices[i+2]]),0);
    var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.SetVertices(output);mesh.SetTriangles(outTriangles,0);mesh.RecalculateBounds();newMeshes[f]=mesh;
    for(int i=0;i<output.Count;i+=3){var a=f.transform.TransformPoint(output[i]);var b=f.transform.TransformPoint(output[i+1]);var c=f.transform.TransformPoint(output[i+2]);var n=Vector3.Cross(b-a,c-a);foreach(var p in new[]{a,b,c}){var key=RockVertexKey(p);normalSums.TryGetValue(key,out var old);normalSums[key]=old+n;}}
   }
   foreach(var item in newMeshes){var f=item.Key;var mesh=item.Value;var vs=mesh.vertices;var ns=new Vector3[vs.Length];for(int i=0;i<vs.Length;i++){normalSums.TryGetValue(RockVertexKey(f.transform.TransformPoint(vs[i])),out var n);ns[i]=f.transform.InverseTransformDirection(n.normalized);}mesh.normals=ns;f.sharedMesh=SaveMesh(mesh,"DetailedCliff_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(f.sharedMesh)));var collider=f.GetComponent<MeshCollider>();if(collider!=null)collider.sharedMesh=f.sharedMesh;f.GetComponent<Renderer>().sharedMaterial=stone;}
   var sea=root.Find("18  Continuous volumetric cloud sea");sea.gameObject.SetActive(true);var seaRenderer=sea.GetComponentInChildren<Renderer>();seaRenderer.sortingOrder=-20;seaRenderer.gameObject.layer=9;var mat=seaRenderer.sharedMaterial;mat.SetFloat("_Density",.72f);mat.SetColor("_Light",new Color(1,.98f,.95f));mat.SetColor("_Shade",new Color(.49f,.61f,.77f));EditorUtility.SetDirty(mat);
   // The underlayer is continuous; keep only four wisps around the island skyline.
   var clouds=root.Find("19  Cloud clusters around the islands");foreach(Transform t in clouds)t.gameObject.SetActive(t.name=="Soft cumulus cluster 9"||t.name=="Soft cumulus cluster 10"||t.name=="Soft cumulus cluster 11"||t.name=="Soft cumulus cluster 14");
   Camera.main.farClipPlane=5000;
   AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
  }
 }
}
