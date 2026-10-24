using UnityEngine;
namespace CloudRail
{
 [DefaultExecutionOrder(250)]
 public sealed class JourneyScenicRoutes:MonoBehaviour
 {
  public JourneyGame game;
  public Transform[] viewpoints;
  public string[] titles,descriptions;
  bool[] visited;
  int count;
  void Awake(){visited=new bool[viewpoints.Length];}
  void LateUpdate()
  {
   if(!game.CanWalk||game.voyage.Active)return;
   for(int i=0;i<viewpoints.Length;i++)
   {
    if(visited[i]||Vector3.Distance(game.player.transform.position,viewpoints[i].position)>2.3f)continue;
    visited[i]=true;count++;game.hud.Toast(titles[i]+"\n"+descriptions[i],5);
   }
   if(game.CurrentStation==0&&game.player.transform.position.y>3)
    game.hud.objective.text="露台漫游 · 已发现 "+count+" / "+viewpoints.Length+" 处风景\n循阶梯登高，也可沿樱花步道绕行";
  }
 }
}
