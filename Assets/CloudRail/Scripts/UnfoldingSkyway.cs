using UnityEngine;
namespace CloudRail
{
    public sealed class UnfoldingSkyway : MonoBehaviour
    {
        public SkyVoyage voyage;
        public SkyRailPath path;
        public Transform[] foundations,sleepers,rails;
        public float[] distances;
        public bool animated=true;
        public float lead=29,unfoldLength=5,trail=55;
        static float Ease(float t){t=Mathf.Clamp01(t);return 1-Mathf.Pow(1-t,3);}
        void Piece(Transform part,float value,float lift,float tilt)
        {
            bool visible=value>.001f;if(part.gameObject.activeSelf!=visible)part.gameObject.SetActive(visible);
            if(!visible)return;
            float eased=Ease(value);
            // The broad support settles first; narrow track parts arrive a beat later.
            part.localPosition=Vector3.down*(1-eased)*lift;
            part.localRotation=Quaternion.Euler((1-eased)*tilt,0,0);
            part.localScale=new Vector3(1,Mathf.Lerp(.12f,1,eased),Mathf.Lerp(.78f,1,eased));
        }
        void LateUpdate()
        {
            bool unfold=animated&&voyage!=null&&voyage.Active;
            for(int i=0;i<distances.Length;i++)
            {
                float relative=unfold?Mathf.Repeat(distances[i]-voyage.Distance+path.Length*.5f,path.Length)-path.Length*.5f:0;
                float grow=unfold?(lead-relative)/unfoldLength:2;
                float retire=unfold?Mathf.Clamp01((relative+trail+10)/10):1;
                Piece(foundations[i],Mathf.Clamp01(grow)*retire,8,32);
                Piece(sleepers[i],Mathf.Clamp01(grow-.16f)*retire,4,-14);
                Piece(rails[i],Mathf.Clamp01(grow-.28f)*retire,2.7f,9);
            }
        }
    }
}
