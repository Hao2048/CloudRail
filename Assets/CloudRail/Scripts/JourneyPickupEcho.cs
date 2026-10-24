using UnityEngine;
namespace CloudRail
{
    // Detached visual outlives the saved pickup; it never participates in collection.
    public sealed class JourneyPickupEcho : MonoBehaviour
    {
        Vector3 scale,origin;float elapsed;
        void Awake(){scale=transform.localScale;origin=transform.position;}
        void Update()
        {
            elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/.4f);
            transform.localScale=scale*(1-t*t);transform.position=origin+Vector3.up*Mathf.Sin(t*Mathf.PI*.5f)*.35f;
            transform.Rotate(0,Time.deltaTime*180,0);
            if(t>=1)Destroy(gameObject);
        }
    }
}
