using UnityEngine;
namespace CloudRail
{
    public sealed class JourneyFacingLabel : MonoBehaviour
    {
        Transform view;Renderer textRenderer;
        void Awake(){view=Camera.main.transform;textRenderer=GetComponent<Renderer>();}
        void LateUpdate()
        {
            Vector3 away=transform.position-view.position;
            if(textRenderer!=null)textRenderer.enabled=away.sqrMagnitude<625;
            if(away.sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(away,Vector3.up);
        }
    }
}
