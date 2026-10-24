using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace CloudRail.Editor
{
 public static partial class CloudRailBuilder
 {
  static Mesh GullWing(int side)
  {
   var v=new List<Vector3>();var t=new List<int>();
   Vector3[] edge={new Vector3(.1f,0,.16f),new Vector3(.65f,.08f,.1f),new Vector3(1.4f,.04f,-.28f),new Vector3(.74f,.04f,-.25f),new Vector3(.12f,0,-.26f)};
   for(int i=0;i<edge.Length;i++)edge[i].x*=side;
   for(int k=1;k<edge.Length-1;k++){Tri(v,t,edge[0],edge[k],edge[k+1]);Tri(v,t,edge[0]-Vector3.up*.035f,edge[k+1]-Vector3.up*.035f,edge[k]-Vector3.up*.035f);}
   for(int k=0;k<edge.Length;k++){var a=edge[k];var b=edge[(k+1)%edge.Length];Tri(v,t,a,b,b-Vector3.up*.035f);Tri(v,t,a,b-Vector3.up*.035f,a-Vector3.up*.035f);}
   var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();return SaveMesh(mesh,"GullWing_"+side);
  }
  [MenuItem("Cloud Rail/Add Cloud Banks And Following Birds")]
  public static void AddBirdsAndClouds()
  {
   if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first");visualAssets=true;gameplayAssets=true;desktopAssets=releaseAssets=false;shapes.Clear();mats.Clear();
   foreach(string id in AssetDatabase.FindAssets("t:Material",new[]{Gen+"/Materials"})){var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(id));mats[m.name]=m;}
   var temp=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=temp.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(temp);temp=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere=temp.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(temp);
   var game=Object.FindAnyObjectByType<JourneyGame>();var world=game.transform.parent;var clouds=world.Find("19  Cloud clusters around the islands");var cloudMat=AssetDatabase.LoadAssetAtPath<Material>(Gen+"/Materials/SoftCloudClusters.mat");
   foreach(var t in clouds.GetComponentsInChildren<Transform>())if(t.name.StartsWith("Additional lower cloud"))Object.DestroyImmediate(t.gameObject);
   Vector3[] positions={new Vector3(-9,-25,-7),new Vector3(25,-28,20),new Vector3(-20,-31,26),new Vector3(-42,-28,-18),new Vector3(46,-27,6),new Vector3(10,-30,-42),new Vector3(-56,-32,24),new Vector3(27,-35,48),new Vector3(-42,-33,-49),new Vector3(65,-31,3)};
   for(int i=0;i<positions.Length;i++){var go=Shape("Additional lower cloud "+i,cube,clouds,positions[i],new Vector3(27+i%3*5,13+i%2*3,23+i%3*3),cloudMat);go.layer=9;go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;}
   var old=world.Find("22  Birds above the railway");if(old!=null)Object.DestroyImmediate(old.gameObject);var root=Group("22  Birds above the railway",world);var flock=root.gameObject.AddComponent<RouteBirdFlocks>();flock.game=game;flock.birds=new Transform[7];flock.leftWings=new Transform[7];flock.rightWings=new Transform[7];var left=GullWing(-1);var right=GullWing(1);
   for(int i=0;i<7;i++)
   {
    var bird=Group("Cloud gull "+(i+1),root);flock.birds[i]=bird;
    Ball(bird,"Feathered body",Vector3.zero,new Vector3(.24f,.27f,.78f),"Porcelain");Ball(bird,"Head",new Vector3(0,.1f,.32f),new Vector3(.24f,.23f,.27f),"Porcelain");
    Beam(bird,"Amber beak",new Vector3(0,.08f,.4f),new Vector3(0,.07f,.57f),.045f,"Brass",6);
    foreach(int side in new[]{-1,1})Ball(bird,"Eye",new Vector3(side*.108f,.13f,.36f),Vector3.one*.03f,"EngineDark");
    var wingL=Group("Left articulated wing",bird);var wingR=Group("Right articulated wing",bird);Shape("Left flight feathers",left,wingL,Vector3.zero,Vector3.one,mats["Porcelain"]);Shape("Right flight feathers",right,wingR,Vector3.zero,Vector3.one,mats["Porcelain"]);flock.leftWings[i]=wingL;flock.rightWings[i]=wingR;
    // Forked tail gives the flock a readable silhouette at the wide scenic camera distance.
    Beam(bird,"Tail feather",new Vector3(-.02f,0,-.28f),new Vector3(-.16f,.03f,-.64f),.065f,"Porcelain",6);Beam(bird,"Tail feather",new Vector3(.02f,0,-.28f),new Vector3(.16f,.03f,-.64f),.065f,"Porcelain",6);
    bird.gameObject.SetActive(false);
   }
   AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
  }
 }
}
