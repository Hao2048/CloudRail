using UnityEngine;
namespace CloudRail
{
 /// <summary>Cloud-only quality controls; does not lower scene resolution or geometry quality.</summary>
 public sealed class CloudRenderQuality:MonoBehaviour
 {
  public JourneyGame game;
  public Renderer sea;
  public GameObject[] nearbyClouds;
  MaterialPropertyBlock block;
  bool initialized,low;
  void LateUpdate()
  {
   bool next=game!=null&&game.LowEffects;if(initialized&&next==low)return;initialized=true;low=next;
   if(block==null)block=new MaterialPropertyBlock();sea.GetPropertyBlock(block);
   block.SetFloat("_CloudSteps",low?16:28);block.SetFloat("_FarSteps",low?10:16);block.SetFloat("_CloudDistance",low?440:640);sea.SetPropertyBlock(block);
   for(int i=0;i<nearbyClouds.Length;i++)if(nearbyClouds[i]!=null)nearbyClouds[i].SetActive(!low||i==0);
  }
 }
}
