using UnityEngine;
namespace CloudRail
{
 public sealed class ScenicRotor:MonoBehaviour
 {
  public float speed=12;
  void Update(){transform.Rotate(Vector3.forward,speed*Time.deltaTime,Space.Self);}
 }
}
