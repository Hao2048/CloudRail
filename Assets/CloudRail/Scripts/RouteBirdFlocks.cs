using UnityEngine;
namespace CloudRail
{
 public sealed class RouteBirdFlocks:MonoBehaviour
 {
  public JourneyGame game;
  public Transform[] birds,leftWings,rightWings;
  public float[] zoneFractions={.12f,.44f,.79f};
  float visibility;Vector3[] previous;
  void Awake(){previous=new Vector3[birds.Length];}
  void LateUpdate()
  {
   var path=game.train.rig.path;bool scenic=game.voyage!=null&&game.voyage.Active;
   float distance=scenic?game.voyage.Distance:game.train.Distance;
   bool moving=scenic||game.train.Moving;float zone=0;
   if(moving)foreach(float f in zoneFractions){float delta=Mathf.Abs(Mathf.Repeat(distance-path.Length*f+path.Length*.5f,path.Length)-path.Length*.5f);zone=Mathf.Max(zone,1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(17,31,delta)));}
   float before=visibility;visibility=Mathf.MoveTowards(visibility,zone,Time.deltaTime*.5f);
   path.Evaluate(distance,out var anchor,out var rotation);Vector3 forward=rotation*Vector3.forward,right=rotation*Vector3.right;
   for(int i=0;i<birds.Length;i++)
   {
    var bird=birds[i];bool visible=visibility>.015f;if(bird.gameObject.activeSelf!=visible)bird.gameObject.SetActive(visible);if(!visible)continue;
    float a=Time.time*.65f+i*2.4f;float lift=5.5f+(i%3)*.7f+Mathf.Sin(a*.7f)*.7f+(1-visibility)*6;
    Vector3 target=anchor+right*(7+Mathf.Sin(a)*4)+forward*(-4+Mathf.Cos(a)*8-i*.5f)+Vector3.up*lift;
    if(before<.02f){bird.position=target;previous[i]=target;}else bird.position=Vector3.Lerp(bird.position,target,1-Mathf.Exp(-Time.deltaTime*2.1f));
    Vector3 tangent=bird.position-previous[i];if(tangent.sqrMagnitude>.00001f)bird.rotation=Quaternion.Slerp(bird.rotation,Quaternion.LookRotation(tangent.normalized,Vector3.up)*Quaternion.Euler(0,0,Mathf.Sin(a)*12),1-Mathf.Exp(-Time.deltaTime*4));
    previous[i]=bird.position;bird.localScale=Vector3.one*Mathf.SmoothStep(0,1,visibility)*(.63f+i%3*.07f);
    // Four to five gentle strokes then a gliding pause; the outer birds flap out of phase.
    float beat=Time.time*5.8f+i*.9f;float glide=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,.8f,Mathf.Sin(Time.time*.47f+i)));float flap=Mathf.Lerp(Mathf.Sin(beat)*24,5,glide);
    leftWings[i].localRotation=Quaternion.Euler(0,0,-flap);rightWings[i].localRotation=Quaternion.Euler(0,0,flap);
   }
  }
 }
}
